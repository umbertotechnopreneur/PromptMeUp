// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;

namespace PromptMeUp.Application;

public sealed partial class ExperimentalWorkflow
{
    /// <summary>Manages explicit observation consent, retained evidence, reminders, and manually invoked reflection.</summary>
    public async Task RunLearningAsync(AppSettings settings, CancellationToken ct)
    {
        var initial = await store.SettingsAsync(ct).ConfigureAwait(false);
        if (!initial.Enabled)
        {
            if (view.Choose(text.Text("Lab.Learning"), text.Text("Lab.Back"), text.Text("Lab.Enable")) == 0)
            {
                return;
            }
            await store.SaveSettingsAsync(initial with { Enabled = true }, initial, ct).ConfigureAwait(false);
        }
        while (await LearningEnabledAsync(ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            var preferences = await store.SettingsAsync(ct).ConfigureAwait(false);
            var observations = await store.ObservationsAsync(ct).ConfigureAwait(false);
            shell.RenderNotice(text.Text("Lab.CaptureNotice"));
            shell.RenderNotice(text.Text("Lab.Retention"));
            view.Render(text.Text("Lab.Learning"),
                [(text.Text("Lab.Capture"), text.Text(preferences.CaptureObservations ? "Lab.Enabled" : "Lab.Off")),
                 (text.Text("Lab.Reminder"), text.Text(preferences.MaintenanceReminder ? "Lab.Enabled" : "Lab.Off")),
                 (text.Text("Lab.Observations"), observations.Count.ToString(CultureInfo.InvariantCulture)),
                 (text.Text("Lab.Dream") + " / " + text.Text("Lab.LastRun"), await LastReflectionAsync(true, ct).ConfigureAwait(false)),
                 (text.Text("Lab.Heartbeat") + " / " + text.Text("Lab.LastRun"), await LastReflectionAsync(false, ct).ConfigureAwait(false))]);
            var selected = view.Choose(text.Text("Lab.Learning"), text.Text("Lab.Back"), text.Text("Lab.Capture"),
                text.Text("Lab.Reminder"), text.Text("Lab.Observations"), text.Text("Lab.Clear"),
                text.Text("Lab.Proposals"), text.Text("Lab.Dream"), text.Text("Lab.Heartbeat"));
            switch (selected)
            {
                case 0:
                    return;
                case 1:
                    if (view.Confirm(text.Text(preferences.CaptureObservations ? "Lab.StopCapture" : "Lab.StartCapture")))
                    {
                        await store.SaveSettingsAsync(preferences with { CaptureObservations = !preferences.CaptureObservations }, preferences, ct).ConfigureAwait(false);
                    }
                    break;
                case 2:
                    await store.SaveSettingsAsync(preferences with { MaintenanceReminder = !preferences.MaintenanceReminder }, preferences, ct).ConfigureAwait(false);
                    break;
                case 3:
                    await ReviewObservationAsync(observations, ct).ConfigureAwait(false);
                    break;
                case 4:
                    if (view.Confirm(text.Text("Lab.Clear")))
                    {
                        await store.ClearObservationsAsync(ct).ConfigureAwait(false);
                        shell.RenderSuccess(text.Text("Lab.Saved"));
                    }
                    break;
                case 5:
                    await RunProposalsAsync(settings, ct).ConfigureAwait(false);
                    break;
                case 6:
                case 7:
                    await RunReflectionAsync(selected == 6, settings, ct).ConfigureAwait(false);
                    break;
            }
        }
    }

    /// <summary>Lists current-project suggestions and applies only explicitly reviewed, still-valid proposals.</summary>
    public async Task RunProposalsAsync(AppSettings settings, CancellationToken ct)
    {
        _ = settings;
        while (await LearningEnabledAsync(ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            var proposals = await store.ProposalsAsync(ct).ConfigureAwait(false);
            if (proposals.Count == 0)
            {
                shell.RenderNotice(text.Text("Lab.None"));
                return;
            }
            var index = view.Choose(text.Text("Lab.Proposals"), [text.Text("Lab.Back"),
                .. proposals.Select(proposal => text.Text("Lab.Operation." + proposal.Operation) + " — " + ProposalLabel(proposal.Text))]);
            if (index == 0)
            {
                return;
            }
            await ReviewProposalAsync(proposals[index - 1], ct).ConfigureAwait(false);
        }
    }

    /// <summary>Runs local duplicate discovery or shares only a displayed complete evidence batch for advisory OpenAI analysis.</summary>
    public async Task RunReflectionAsync(bool dream, AppSettings settings, CancellationToken ct)
    {
        if (!await LearningEnabledAsync(ct).ConfigureAwait(false))
        {
            return;
        }
        var preferences = await store.SettingsAsync(ct).ConfigureAwait(false);
        if (dream && !preferences.CaptureObservations)
        {
            shell.RenderNotice(text.Text("Lab.CaptureRequired"));
            return;
        }
        var revision = await store.RevisionAsync(ct).ConfigureAwait(false);
        MemoryReflectionBatch batch;
        if (dream)
        {
            var observations = await store.ObservationsAsync(ct).ConfigureAwait(false);
            batch = reflection.DreamBatch(observations, settings.MaxMessageCharacters);
        }
        else
        {
            var saved = await memories.ListAsync(ct).ConfigureAwait(false);
            if (saved.Count == 0)
            {
                shell.RenderNotice(text.Text("Lab.None"));
                return;
            }
            var local = reflection.FindDuplicates(saved);
            var count = await store.SaveProposalsAsync(local, ct, revision).ConfigureAwait(false);
            await MarkReflectionAsync(false, ct).ConfigureAwait(false);
            shell.RenderNotice(text.Text("Lab.LocalMaintenance"));
            shell.RenderNotice(text.Text("Lab.ReviewCount", count));
            batch = reflection.HeartbeatBatch(saved, settings.MaxMessageCharacters);
        }
        view.Render(text.Text(dream ? "Lab.Dream" : "Lab.Heartbeat"), [(text.Text("Lab.Sources"), batch.Request)]);
        if (batch.OmittedCount > 0)
        {
            shell.RenderNotice(text.Text("Lab.BoundedEvidence", batch.OmittedCount));
        }
        if (!view.Confirm(text.Text("Lab.Share")))
        {
            return;
        }
        if (!settings.SetupCompleted || !settings.AiEnabled)
        {
            throw new InvalidOperationException(text.Text("Error.SetupRequired"));
        }
        if (!secrets.IsConfigured(settings.ApiKeyVariable))
        {
            throw new InvalidOperationException(PromptMeUpApplication.AppendKeyRestartHint(
                text.Text("Error.ApiKeyMissing", settings.ApiKeyVariable), text, OperatingSystem.IsWindows()));
        }
        ct.ThrowIfCancellationRequested();
        var current = await store.SettingsAsync(ct).ConfigureAwait(false);
        if (!current.Enabled || (dream && !current.CaptureObservations)
            || await store.RevisionAsync(ct).ConfigureAwait(false) != revision)
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        var response = await assistant.SendAsync(dream ? "memory-dream" : "memory-heartbeat", batch.Request, settings, ct).ConfigureAwait(false);
        var proposals = reflection.Parse(response.Text, batch);
        var added = await store.SaveProposalsAsync(proposals, ct, revision).ConfigureAwait(false);
        await MarkReflectionAsync(dream, ct).ConfigureAwait(false);
        shell.RenderNotice(text.Text("Lab.ReviewCount", added));
        assistant.RenderSummaryAtEnd(response, settings);
    }

    /// <summary>Reviews a retained observation in full and allows explicit removal with its derived suggestions.</summary>
    private async Task ReviewObservationAsync(IReadOnlyList<LearningObservation> observations, CancellationToken ct)
    {
        if (observations.Count == 0)
        {
            shell.RenderNotice(text.Text("Lab.None"));
            return;
        }
        var selected = view.Choose(text.Text("Lab.Observations"), [text.Text("Lab.Back"),
            .. observations.Select(item => item.CreatedAt.ToString("u", CultureInfo.InvariantCulture) + " — " + ProposalLabel(item.Text))]);
        if (selected == 0)
        {
            return;
        }
        var observation = observations[selected - 1];
        view.Render(text.Text("Lab.Observations"), [(text.Text("Lab.Sources"), ObservationText(observation))]);
        if (view.Confirm(text.Text("Lab.ForgetObservation")))
        {
            if (!await store.ForgetObservationAsync(observation.Id, ct).ConfigureAwait(false))
            {
                throw new InvalidOperationException(text.Text("Lab.Invalid"));
            }
            shell.RenderSuccess(text.Text("Lab.Saved"));
        }
    }

    /// <summary>Shows exact target snapshots, full source observations, and edited text before final confirmation.</summary>
    private async Task ReviewProposalAsync(MemoryProposal proposal, CancellationToken ct)
    {
        var evidence = await store.ObservationsAsync(ct).ConfigureAwait(false);
        if (proposal.SourceIds.Any(id => !evidence.Any(item => item.Id == id)))
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        var sources = proposal.SourceIds.Select(id => evidence.Single(item => item.Id == id)).ToArray();
        var reviewed = proposal.Text;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            RenderProposal(proposal, reviewed, sources);
            var choices = new List<(string Key, string Action)>
            {
                ("Lab.Cancel", "cancel"), ("Lab.Reject", "reject")
            };
            if (proposal.Operation == "flag")
            {
                shell.RenderNotice(text.Text("Lab.FlagNotice"));
            }
            else
            {
                if (proposal.Operation is "add" or "merge")
                {
                    choices.Add(("Lab.Edit", "edit"));
                }
                choices.Add(("Lab.Approve", "approve"));
            }
            var selected = choices[view.Choose(text.Text("Lab.Proposals"), choices.Select(choice => text.Text(choice.Key)).ToArray())].Action;
            if (selected == "cancel")
            {
                return;
            }
            if (selected == "reject" && view.Confirm(text.Text("Lab.Reject")))
            {
                await store.RejectAsync(proposal, ct).ConfigureAwait(false);
                shell.RenderSuccess(text.Text("Lab.Saved"));
                return;
            }
            if (selected == "edit")
            {
                reviewed = reflection.ValidateReviewedText(view.Read(text.Text("Lab.Edit"), reviewed));
            }
            if (selected == "approve")
            {
                reviewed = reflection.ValidateReviewedText(reviewed);
                RenderProposal(proposal, reviewed, sources);
                if (view.Confirm(text.Text("Lab.Approve")))
                {
                    await store.ApproveAsync(proposal, reviewed, true, ct).ConfigureAwait(false);
                    shell.RenderSuccess(text.Text("Lab.Saved"));
                    return;
                }
            }
        }
    }

    /// <summary>Displays unabridged before/after content and evidence without changing any stored memory.</summary>
    private void RenderProposal(MemoryProposal proposal, string reviewed, IReadOnlyList<LearningObservation> sources)
    {
        var before = proposal.Targets.Count == 0 ? text.Text("Lab.NoPrevious")
            : string.Join("\n\n", proposal.Targets.Select(target => target.Id + " · " + target.UpdatedAt.ToString("u", CultureInfo.InvariantCulture) + "\n" + target.Text));
        var after = proposal.Operation == "archive" ? text.Text("Lab.Archived")
            : proposal.Operation == "flag" ? text.Text("Lab.Unchanged") : reviewed;
        var sourceText = sources.Count > 0 ? string.Join("\n\n", sources.Select(ObservationText))
            : string.Join("\n", proposal.Targets.Select(target => target.Id));
        view.Render(text.Text("Lab.Proposals"),
            [(text.Text("Lab.Operation"), text.Text("Lab.Operation." + proposal.Operation)),
             (text.Text("Lab.Kind"), text.Text("Lab.Kind." + proposal.Kind)),
             (text.Text("Lab.Before"), before), (text.Text("Lab.After"), after),
             (text.Text("Lab.Reason"), proposal.Operation is "archive" or "flag" ? proposal.Text + "\n\n" + proposal.Rationale : proposal.Rationale),
             (text.Text("Lab.Sources"), sourceText)]);
        if (proposal.Operation is "merge" or "archive")
        {
            shell.RenderWarning(text.Text("Lab.ForgetNotice"));
        }
    }

    /// <summary>Checks the project experiment gate before displaying or changing learning state.</summary>
    private async Task<bool> LearningEnabledAsync(CancellationToken ct)
    {
        if ((await store.SettingsAsync(ct).ConfigureAwait(false)).Enabled)
        {
            return true;
        }
        shell.RenderNotice(text.Text("Lab.Disabled"));
        return false;
    }

    /// <summary>Records a successful validated analysis separately from failed provider or persistence attempts.</summary>
    private Task MarkReflectionAsync(bool dream, CancellationToken ct) =>
        store.SetAsync(dream ? "last-dream" : "last-heartbeat", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture), ct);

    /// <summary>Reads the last successful maintenance time without hiding corrupt preference values.</summary>
    private async Task<string> LastReflectionAsync(bool dream, CancellationToken ct)
    {
        var value = await store.GetAsync(dream ? "last-dream" : "last-heartbeat", ct).ConfigureAwait(false);
        if (value is null)
        {
            return text.Text("Lab.Never");
        }
        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        return timestamp.ToString("u", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats the full observation with stable source, session, and capture-time metadata.</summary>
    private static string ObservationText(LearningObservation observation) =>
        observation.Id + " · " + observation.SessionId + " · " + observation.CreatedAt.ToString("u", CultureInfo.InvariantCulture) + "\n" + observation.Text;

    /// <summary>Bounds list labels only; the subsequent review always renders the full content.</summary>
    private static string ProposalLabel(string value) => value.Length <= 100 ? value.Replace('\n', ' ') : value[..100].Replace('\n', ' ') + "…";
}

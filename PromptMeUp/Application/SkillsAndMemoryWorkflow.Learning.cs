// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;

namespace PromptMeUp.Application;

public sealed partial class SkillsAndMemoryWorkflow
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
                text.Text("Lab.Dream"), text.Text("Lab.Heartbeat"));
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
                case 6:
                    await RunReflectionAsync(selected == 5, settings, ct).ConfigureAwait(false);
                    break;
            }
        }
    }

    /// <summary>Loads current suggestions and exact evidence for the shared inline memory editor.</summary>
    public async Task<MemoryProposalWorkspace> LoadProposalWorkspaceAsync(CancellationToken ct)
    {
        if (!(await store.SettingsAsync(ct).ConfigureAwait(false)).Enabled)
        {
            return MemoryProposalWorkspace.Disabled;
        }
        return new(true,
            await store.ProposalsAsync(ct).ConfigureAwait(false),
            await store.ObservationsAsync(ct).ConfigureAwait(false));
    }

    /// <summary>Applies one explicitly confirmed inline proposal decision after refreshing all validity checks.</summary>
    public async Task ApplyProposalReviewAsync(string proposalId, string? reviewed, bool approve, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proposalId);
        if (!(await store.SettingsAsync(ct).ConfigureAwait(false)).Enabled)
        {
            throw new InvalidOperationException(text.Text("Lab.Disabled"));
        }
        var proposal = (await store.ProposalsAsync(ct).ConfigureAwait(false))
            .SingleOrDefault(item => string.Equals(item.Id, proposalId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(text.Text("Lab.Invalid"));
        var evidence = await store.ObservationsAsync(ct).ConfigureAwait(false);
        if (proposal.SourceIds.Any(id => !evidence.Any(item => string.Equals(item.Id, id, StringComparison.Ordinal))))
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        if (!approve)
        {
            await store.RejectAsync(proposal, ct).ConfigureAwait(false);
            return;
        }
        if (proposal.Operation == "flag")
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        var finalText = reflection.ValidateReviewedText(proposal.Operation is "add" or "merge"
            ? reviewed ?? string.Empty
            : proposal.Text);
        await store.ApproveAsync(proposal, finalText, true, ct).ConfigureAwait(false);
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

    /// <summary>Checks the project skills and memory gate before displaying or changing learning state.</summary>
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

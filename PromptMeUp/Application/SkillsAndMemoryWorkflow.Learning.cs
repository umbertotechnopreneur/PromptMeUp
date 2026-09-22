// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;

namespace PromptMeUp.Application;

public sealed partial class SkillsAndMemoryWorkflow
{
    /// <summary>Manages explicit observation consent, retained evidence, reminders, and manually invoked reflection.</summary>
    public async Task RunLearningAsync(AppSettings settings, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var preferences = await store.SettingsAsync(ct).ConfigureAwait(false);
            var observations = await store.ObservationsAsync(ct).ConfigureAwait(false);
            var groups = await BuildLearningGroups(preferences, observations, ct).ConfigureAwait(false);
            var selected = view.ChooseSkills(text.Text("Lab.Learning"), groups);
            if (selected is null)
            {
                return;
            }

            switch (selected.Item.Action)
            {
                case SkillMenuAction.EnableSkillsAndMemory:
                    await store.SaveSettingsAsync(preferences with { Enabled = true }, preferences, ct).ConfigureAwait(false);
                    break;
                case SkillMenuAction.ToggleCapture:
                    if (ConfirmLearningAction(text.Text(preferences.CaptureObservations ? "Lab.StopCapture" : "Lab.StartCapture"),
                        text.Text("Lab.CaptureNotice")))
                    {
                        await store.SaveSettingsAsync(preferences with { CaptureObservations = !preferences.CaptureObservations }, preferences, ct).ConfigureAwait(false);
                    }
                    break;
                case SkillMenuAction.ToggleReminder:
                    await store.SaveSettingsAsync(preferences with { MaintenanceReminder = !preferences.MaintenanceReminder }, preferences, ct).ConfigureAwait(false);
                    break;
                case SkillMenuAction.ReviewObservations:
                    await ReviewObservationAsync(observations, ct).ConfigureAwait(false);
                    break;
                case SkillMenuAction.ClearObservations:
                    if (ConfirmLearningAction(text.Text("Lab.Clear"), text.Text("Lab.Clear")))
                    {
                        await store.ClearObservationsAsync(ct).ConfigureAwait(false);
                        shell.RenderSuccess(text.Text("Lab.Saved"));
                    }
                    break;
                case SkillMenuAction.Dream:
                case SkillMenuAction.Heartbeat:
                    await RunReflectionAsync(selected.Item.Action == SkillMenuAction.Dream, settings, ct).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported memory menu action.");
            }
        }
    }

    /// <summary>Builds one compact Memory workspace that uses the shared skills navigation and footer conventions.</summary>
    private async Task<IReadOnlyList<SkillMenuGroup>> BuildLearningGroups(SkillsAndMemorySettings preferences,
        IReadOnlyList<LearningObservation> observations, CancellationToken ct)
    {
        if (!preferences.Enabled)
        {
            return
            [
                new SkillMenuGroup(text.Text("Lab.Learning"), text.Text("Lab.Disabled"),
                [new SkillMenuItem(SkillMenuAction.EnableSkillsAndMemory, text.Text("Lab.Enable"), text.Text("Lab.Enable"),
                    Icon: "⏻")], "🧠")
            ];
        }

        var captureState = text.Text(preferences.CaptureObservations ? "Lab.Enabled" : "Lab.Off");
        var reminderState = text.Text(preferences.MaintenanceReminder ? "Lab.Enabled" : "Lab.Off");
        var commands = new List<SkillMenuItem>
        {
            new(SkillMenuAction.ToggleCapture,
                text.Text("Lab.Capture") + " — " + captureState,
                text.Text("Lab.CaptureNotice"), Icon: "📝"),
            new(SkillMenuAction.ToggleReminder,
                text.Text("Lab.Reminder") + " — " + reminderState,
                text.Text("Lab.Reminder"), Icon: "⏰"),
            new(SkillMenuAction.ReviewObservations,
                text.Text("Lab.Observations") + " — " + observations.Count.ToString(CultureInfo.InvariantCulture),
                text.Text("Lab.Observations"), Icon: "📋"),
            new(SkillMenuAction.ClearObservations, text.Text("Lab.Clear"), text.Text("Lab.Clear"), Icon: "🧹"),
            new(SkillMenuAction.Dream,
                text.Text("Lab.Dream") + " — " + await LastReflectionAsync(true, ct).ConfigureAwait(false),
                text.Text("Lab.Dream"), Icon: "💭"),
            new(SkillMenuAction.Heartbeat,
                text.Text("Lab.Heartbeat") + " — " + await LastReflectionAsync(false, ct).ConfigureAwait(false),
                text.Text("Lab.Heartbeat"), Icon: "💓")
        };
        return [new SkillMenuGroup(text.Text("Lab.Learning"), text.Text("Lab.Retention"), commands, "🧠")];
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
        if (!ConfirmReflection(dream, batch))
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
        var selected = view.ChooseSkills(text.Text("Lab.Observations"),
        [
            new SkillMenuGroup(text.Text("Lab.Observations"), text.Text("Lab.Retention"),
                observations.Select(item => new SkillMenuItem(SkillMenuAction.ReviewObservations,
                    item.CreatedAt.ToString("u", CultureInfo.InvariantCulture) + " — " + ProposalLabel(item.Text),
                    ObservationText(item), Icon: "📋", ActionName: item.Id)).ToArray(), "🧠")
        ]);
        if (selected?.Item.ActionName is not { } id)
        {
            return;
        }
        var observation = observations.Single(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        if (ConfirmLearningAction(text.Text("Lab.ForgetObservation"), ObservationText(observation)))
        {
            if (!await store.ForgetObservationAsync(observation.Id, ct).ConfigureAwait(false))
            {
                throw new InvalidOperationException(text.Text("Lab.Invalid"));
            }
            shell.RenderSuccess(text.Text("Lab.Saved"));
        }
    }

    /// <summary>Checks the global skills and memory gate before displaying or changing learning state.</summary>
    private async Task<bool> LearningEnabledAsync(CancellationToken ct)
    {
        if ((await store.SettingsAsync(ct).ConfigureAwait(false)).Enabled)
        {
            return true;
        }
        shell.RenderNotice(text.Text("Lab.Disabled"));
        return false;
    }

    /// <summary>Shows a shared fullscreen confirmation instead of writing a second scrolling prompt below the workspace.</summary>
    private bool ConfirmLearningAction(string title, string description) =>
        view.ChooseSkills(title,
        [
            new SkillMenuGroup(text.Text("Lab.Learning"), description,
            [
                new SkillMenuItem(SkillMenuAction.Cancel, text.Text("Lab.Cancel"), description, Icon: "↩"),
                new SkillMenuItem(SkillMenuAction.Confirm, text.Text("Lab.Confirm"), description, Icon: "✓")
            ], "🧠")
        ])?.Item.Action == SkillMenuAction.Confirm;

    /// <summary>Summarizes the reviewed evidence without rendering its provider JSON in the terminal.</summary>
    private bool ConfirmReflection(bool dream, MemoryReflectionBatch batch)
    {
        var count = dream ? batch.Observations.Count : batch.Memories.Count;
        var description = text.Text("Lab.EvidenceSummary", count, batch.OmittedCount);
        return ConfirmLearningAction(text.Text(dream ? "Lab.Dream" : "Lab.Heartbeat"),
            description + " " + text.Text("Lab.Share"));
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

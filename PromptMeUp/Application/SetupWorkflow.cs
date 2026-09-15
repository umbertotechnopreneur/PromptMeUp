// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Coordinates settings forms, explicit credential updates, and setup activity records.</summary>
public sealed class SetupWorkflow(
    ISettingsService settings,
    IEnvironmentSecretService secrets,
    IAiConversationWorkflow conversationWorkflow,
    ApplicationActivityRecorder activity,
    ISetupView setupView,
    IConsoleShellView shell,
    ILocalizationService text,
    IThemeCatalogService? themes = null,
    IPricingService? pricing = null,
    MemoryManagerWorkflow? memories = null)
{
    /// <summary>Opens the shared settings screen with appearance selected.</summary>
    public Task<int> RunThemeAsync(AppSettings current, CancellationToken cancellationToken) =>
        RunAsync(current, cancellationToken, SettingsSection.Theme);

    /// <summary>Opens the shared settings screen with AI preferences selected.</summary>
    public Task<int> RunAiSettingsAsync(AppSettings current, CancellationToken cancellationToken) =>
        RunAsync(current, cancellationToken, SettingsSection.Ai);

    /// <summary>Collects all settings from the requested section, saves the submitted draft, and optionally tests OpenAI.</summary>
    public async Task<int> RunAsync(
        AppSettings current,
        CancellationToken cancellationToken,
        SettingsSection initialSection = SettingsSection.General)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (!Enum.IsDefined(initialSection))
        {
            throw new ArgumentOutOfRangeException(nameof(initialSection));
        }
        var submission = setupView.Collect(new SetupViewState(
            current,
            secrets.IsConfigured(current.ApiKeyVariable),
            secrets.IsConfigured(current.AdminKeyVariable))
        {
            InitialSection = initialSection,
            Costs = pricing is null ? null : await pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false),
            OpenMemories = () => OpenMemories(cancellationToken),
            ContextBudgetOverridden = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PROMPTMEUP_CONTEXT_TOKENS"))
        });
        if (submission is null)
        {
            shell.RenderNotice(text.Text("Setup.Cancelled"));
            await activity.TryRecordAsync("setup", "cancelled", null, new { }).ConfigureAwait(false);
            return 0;
        }

        var selectedTheme = themes?.Resolve(submission.Settings.Theme);
        var secretGuidance = new List<string>();
        if (submission.ApiKey is not null)
        {
            secretGuidance.Add(secrets.StoreForCurrentUser(submission.Settings.ApiKeyVariable, submission.ApiKey).Guidance);
        }
        if (submission.AdminKey is not null)
        {
            secretGuidance.Add(secrets.StoreForCurrentUser(submission.Settings.AdminKeyVariable, submission.AdminKey).Guidance);
        }

        await settings.SaveAsync(submission.Settings, cancellationToken).ConfigureAwait(false);
        if (selectedTheme is not null)
        {
            TerminalTheme.Apply(selectedTheme);
        }
        if (submission.Settings.Language != current.Language)
        {
            text.SetLanguage(submission.Settings.Language);
        }
        shell.RenderSuccess(text.Text("Setup.Saved"));
        foreach (var guidance in secretGuidance)
        {
            shell.RenderMuted(guidance);
        }
        if (secretGuidance.Count > 0 && OperatingSystem.IsWindows())
        {
            shell.RenderWarning(text.Text("Setup.KeyRestartRequired"));
        }
        await activity.TryRecordAsync(
            "setup",
            "completed",
            null,
            new
            {
                submission.Settings.Language,
                submission.Settings.Theme,
                submission.Settings.Model,
                submission.Settings.ReasoningEffort,
                submission.Settings.OutputDetail,
                submission.Settings.PromptCachingEnabled,
                submission.Settings.MaxConversationTurns,
                submission.Settings.MaxContextPercent
            }).ConfigureAwait(false);
        if (submission.TestConnection)
        {
            await conversationWorkflow.RunConnectionTestAsync(submission.Settings, cancellationToken).ConfigureAwait(false);
        }
        return 0;
    }

    /// <summary>Handles the memory navigation event while the passive settings draft remains open.</summary>
    private void OpenMemories(CancellationToken cancellationToken)
    {
        var workflow = memories ?? throw new InvalidOperationException("The memory manager is unavailable.");
        workflow.RunAsync(cancellationToken).GetAwaiter().GetResult();
    }
}

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
    ILocalizationService text)
{
    /// <summary>Edits only AI preferences after initial setup without touching secrets or other configuration.</summary>
    public async Task<int> RunAiSettingsAsync(AppSettings current, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PROMPTMEUP_CONTEXT_TOKENS")))
        {
            shell.RenderNotice(text.Text("AiSettings.ContextOverride"));
        }
        var selected = setupView.CollectAiSettings(current);
        if (selected is null)
        {
            shell.RenderNotice(text.Text("Setup.Cancelled"));
            await activity.TryRecordAsync("ai-settings", "cancelled", null, new { }).ConfigureAwait(false);
            return 0;
        }

        var updated = current with
        {
            AiEnabled = selected.AiEnabled,
            Model = selected.Model,
            ReasoningEffort = selected.ReasoningEffort,
            OutputDetail = selected.OutputDetail,
            ReviewCommandsWithAi = selected.ReviewCommandsWithAi,
            PromptCachingEnabled = selected.PromptCachingEnabled,
            MaxConversationTurns = selected.MaxConversationTurns,
            MaxMessageCharacters = selected.MaxMessageCharacters,
            MaxContextPercent = selected.MaxContextPercent,
            ContextTokenBudget = selected.ContextTokenBudget,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await settings.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        shell.RenderSuccess(text.Text("AiSettings.Saved"));
        await activity.TryRecordAsync("ai-settings", "completed", null, new
        {
            updated.AiEnabled,
            updated.Model,
            updated.ReasoningEffort,
            updated.ContextTokenBudget
        }).ConfigureAwait(false);
        return 0;
    }

    /// <summary>Collects setup settings, persists secrets safely, saves preferences, and optionally tests OpenAI.</summary>
    public async Task<int> RunAsync(AppSettings current, CancellationToken cancellationToken)
    {
        var submission = setupView.Collect(new SetupViewState(
            current,
            secrets.IsConfigured(current.ApiKeyVariable),
            secrets.IsConfigured(current.AdminKeyVariable)));
        if (submission is null)
        {
            shell.RenderNotice(text.Text("Setup.Cancelled"));
            await activity.TryRecordAsync("setup", "cancelled", null, new { }).ConfigureAwait(false);
            return 0;
        }

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
        text.SetLanguage(submission.Settings.Language);
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
}

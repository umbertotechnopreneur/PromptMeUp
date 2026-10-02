// SPDX-License-Identifier: MIT

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Coordinates explicit onboarding consent, credential verification, and resumable preferences.</summary>
public sealed class FirstRunWorkflow(ISettingsService settings, IEnvironmentSecretService secrets,
    IOpenAiService openAi, SettingsFeatureOverviewService features, IFirstRunView view,
    IConsoleShellView shell, ILocalizationService text, IDesktopLauncherService desktop, ILogger<FirstRunWorkflow> logger,
    CommandGuideWorkflow guide)
{
    /// <summary>Completes three guided steps without opening the general settings screen.</summary>
    public async Task<int> RunAsync(AppSettings current, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(current);
        logger.LogInformation("Onboarding started. Language={Language}, ProtectedStorage={ProtectedStorage}", current.Language, OperatingSystem.IsWindows());
        var result = await RunStepsAsync(current, ct).ConfigureAwait(false);
        if (!result.Completed)
        {
            return 0;
        }

        if (desktop.IsAvailable && await view.ChooseDesktopAsync(ct).ConfigureAwait(false))
        {
            try
            {
                var desktopResult = desktop.Create();
                logger.LogInformation("Onboarding desktop shortcut result. Result={Result}", desktopResult);
                shell.RenderSuccess(text.Text(desktopResult == DesktopLauncherResult.Created ? "Home.DesktopCreated" : "Home.DesktopExists"));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or System.Runtime.InteropServices.COMException or InvalidOperationException
                or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                logger.LogWarning("Onboarding desktop shortcut failed. ExceptionType={ExceptionType}, StackTrace={StackTrace}",
                    exception.GetType().FullName, new System.Diagnostics.StackTrace(exception, fNeedFileInfo: false).ToString());
                shell.RenderWarning(text.Text("Home.DesktopFailed"));
            }
        }

        view.RenderReady(result.Name, guide.DocumentPath);
        logger.LogInformation("Onboarding completed.");
        if (await view.ChooseGuideAsync(ct).ConfigureAwait(false))
        {
            guide.Open();
        }
        return 0;
    }

    /// <summary>Collects and persists three resumable onboarding steps in ordinary terminal scrollback.</summary>
    private async Task<FirstRunResult> RunStepsAsync(AppSettings current, CancellationToken ct)
    {
        view.RenderWelcome();
        var step = 1;
        var verified = false;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (step == 1)
            {
                logger.LogInformation("Onboarding step opened. Step=1, Name=Language");
                var language = await view.ChooseLanguageAsync(current.Language, ct).ConfigureAwait(false);
                logger.LogInformation("Onboarding step answered. Step=1, Action={Action}, Language={Language}", language.Action, language.Value);
                if (language.Action == FirstRunAction.Exit) { return new(false, current.PreferredName); }
                text.SetLanguage(language.Value);
                current = current with { Language = language.Value, SetupCompleted = false, UpdatedAt = DateTimeOffset.UtcNow };
                await settings.SaveAsync(current, ct).ConfigureAwait(false);
                step = 2;
            }
            if (step == 2)
            {
                logger.LogInformation("Onboarding step opened. Step=2, Name=Connection");
                var key = await view.ReadKeyAsync(secrets.IsConfigured(current.ApiKeyVariable), OperatingSystem.IsWindows(), ct)
                    .ConfigureAwait(false);
                logger.LogInformation("Onboarding step answered. Step=2, Action={Action}", key.Action);
                if (key.Action == FirstRunAction.Exit) { return new(false, current.PreferredName); }
                if (key.Action == FirstRunAction.Back) { step = 1; continue; }
                var candidate = key.Value ?? secrets.Load(current.ApiKeyVariable)
                    ?? throw new InvalidOperationException(text.Text("Oobe.InvalidKey"));
                var result = await VerifyAndStoreAsync(candidate, current, ct).ConfigureAwait(false);
                if (result == FirstRunAction.Exit) { return new(false, current.PreferredName); }
                if (result == FirstRunAction.Back) { continue; }
                verified = true;
                shell.RenderSuccess(text.Text("Oobe.Connected"));
                step = 3;
            }
            logger.LogInformation("Onboarding step opened. Step=3, Name=Personalization");
            var overview = await features.ReadAsync(ct).ConfigureAwait(false);
            if (overview.CatalogUnavailable)
            {
                throw new InvalidOperationException(text.Text("Lab.Invalid"));
            }
            var selection = await view.ReadPreferencesAsync(current.PreferredName, overview, ct).ConfigureAwait(false);
            logger.LogInformation("Onboarding step answered. Step=3, Action={Action}", selection.Action);
            if (selection.Action == FirstRunAction.Exit) { return new(false, current.PreferredName); }
            if (selection.Action == FirstRunAction.Back) { step = 3; continue; }
            if (!verified) { throw new InvalidOperationException("Onboarding requires a verified OpenAI connection."); }
            var selectedSkills = selection.Value.EnabledSkills.ToHashSet(StringComparer.Ordinal);
            var preferences = overview.Settings with
            {
                Enabled = selection.Value.Enabled,
                CaptureObservations = selection.Value.Enabled && selection.Value.Capture,
                AutomaticSkills = selection.Value.Enabled && selectedSkills.Count > 0,
                MaintenanceReminder = false
            };
            var skills = overview.Skills.Select(item => new SettingsSkillChange(item.Skill, item.Enabled,
                selection.Value.Enabled && selectedSkills.Contains(item.Skill.Name))
            {
                ExpectedApprovalFingerprint = item.ApprovalFingerprint
            }).ToArray();
            await features.SaveAsync(new(overview.Settings, preferences, skills,
                CaptureConsent: selection.Value.Capture, ClearLearningConsent: true), ct).ConfigureAwait(false);
            current = current with
            {
                PreferredName = selection.Value.Name,
                SetupCompleted = true,
                AiEnabled = true,
                DirectModeEnabled = !selection.Value.ConfirmCommands,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await settings.SaveAsync(current, ct).ConfigureAwait(false);
            logger.LogInformation("Onboarding preferences saved. MemoriesEnabled={MemoriesEnabled}, LearningCapture={LearningCapture}, DirectMode={DirectMode}",
                preferences.Enabled, preferences.CaptureObservations, current.DirectModeEnabled);
            return new(true, current.PreferredName);
        }
    }

    /// <summary>Tests a candidate before persisting it, offering bounded retries without exposing provider error bodies.</summary>
    private async Task<FirstRunAction> VerifyAndStoreAsync(string candidate, AppSettings current, CancellationToken ct)
    {
        var attempt = 0;
        while (true)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            logger.LogInformation("Onboarding connection check started. Attempt={Attempt}", ++attempt);
            var error = string.Empty;
            try
            {
                using var temporary = secrets.UseTemporary(current.ApiKeyVariable, candidate);
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                deadline.CancelAfter(TimeSpan.FromSeconds(30));
                await shell.RunWithStatusAsync(text.Text("Oobe.Verifying"),
                    () => openAi.TestConnectionAsync(current, text.Language, deadline.Token)).ConfigureAwait(false);
            }
            catch (OpenAiRequestException exception)
            {
                error = exception.StatusCode switch
                {
                    401 => "KeyRejected",
                    403 or 404 => "AccessDenied",
                    429 => exception.ProviderCode == "insufficient_quota" ? "QuotaExceeded" : "RateLimited",
                    _ => exception.ErrorCode == "responses_api_timeout" ? "NetworkError" : "ServiceError"
                };
            }
            catch (HttpRequestException) { error = "NetworkError"; }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { error = "NetworkError"; }
            if (error.Length == 0)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    secrets.StoreForCurrentUser(current.ApiKeyVariable, candidate);
                    logger.LogInformation("Onboarding connection verified and credential stored. Attempt={Attempt}, ElapsedMs={ElapsedMs}", attempt, timer.ElapsedMilliseconds);
                    return FirstRunAction.Next;
                }
                catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException)
                {
                    error = "StorageError";
                }
            }
            logger.LogWarning("Onboarding connection check failed. Attempt={Attempt}, Category={Category}, ElapsedMs={ElapsedMs}", attempt, error, timer.ElapsedMilliseconds);
            var action = await view.ReadConnectionFailureAsync(error, ct).ConfigureAwait(false);
            logger.LogInformation("Onboarding connection recovery selected. Action={Action}", action);
            if (action != FirstRunAction.Next) { return action; }
        }
    }
}

/// <summary>Reports whether onboarding finished and carries the safe display name to the final screen.</summary>
internal readonly record struct FirstRunResult(bool Completed, string Name);

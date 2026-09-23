// SPDX-License-Identifier: MIT

using System.ComponentModel;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Coordinates explicit onboarding consent, credential verification, and resumable preferences.</summary>
public sealed class FirstRunWorkflow(ISettingsService settings, IEnvironmentSecretService secrets,
    IOpenAiService openAi, SettingsFeatureOverviewService features, IFirstRunView view,
    IConsoleShellView shell, ILocalizationService text, IDesktopLauncherService desktop)
{
    /// <summary>Completes four guided steps without opening the general settings screen.</summary>
    public async Task<int> RunAsync(AppSettings current, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(current);
        view.RenderWelcome();
        var step = 1;
        var verified = false;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (step == 1)
            {
                var language = await view.ChooseLanguageAsync(current.Language, ct).ConfigureAwait(false);
                if (language.Action == FirstRunAction.Exit) { return 0; }
                text.SetLanguage(language.Value);
                current = current with { Language = language.Value, SetupCompleted = false, UpdatedAt = DateTimeOffset.UtcNow };
                await settings.SaveAsync(current, ct).ConfigureAwait(false);
                step = 2;
            }
            if (step == 2)
            {
                var key = await view.ReadKeyAsync(secrets.IsConfigured(current.ApiKeyVariable), OperatingSystem.IsWindows(), ct)
                    .ConfigureAwait(false);
                if (key.Action == FirstRunAction.Exit) { return 0; }
                if (key.Action == FirstRunAction.Back) { step = 1; continue; }
                var candidate = key.Value ?? secrets.Load(current.ApiKeyVariable)
                    ?? throw new InvalidOperationException(text.Text("Oobe.InvalidKey"));
                var result = await VerifyAndStoreAsync(candidate, current, ct).ConfigureAwait(false);
                if (result == FirstRunAction.Exit) { return 0; }
                if (result == FirstRunAction.Back) { continue; }
                verified = true;
                shell.RenderSuccess(text.Text("Oobe.Connected"));
                step = 3;
            }
            if (step == 3)
            {
                var name = await view.ReadNameAsync(current.PreferredName, ct).ConfigureAwait(false);
                if (name.Action == FirstRunAction.Exit) { return 0; }
                if (name.Action == FirstRunAction.Back) { step = 2; continue; }
                current = current with { PreferredName = name.Value, UpdatedAt = DateTimeOffset.UtcNow };
                await settings.SaveAsync(current, ct).ConfigureAwait(false);
                step = 4;
            }
            var overview = await features.ReadAsync(ct).ConfigureAwait(false);
            var memory = await view.ReadMemoryAsync(overview.Settings, ct).ConfigureAwait(false);
            if (memory.Action == FirstRunAction.Exit) { return 0; }
            if (memory.Action == FirstRunAction.Back) { step = 3; continue; }
            if (!verified) { throw new InvalidOperationException("Onboarding requires a verified OpenAI connection."); }
            if (overview.CatalogUnavailable)
            {
                throw new InvalidOperationException(text.Text("Lab.Invalid"));
            }
            var preferences = overview.Settings with
            {
                Enabled = memory.Value.Enabled,
                CaptureObservations = memory.Value.Enabled && memory.Value.Capture,
                AutomaticSkills = false,
                MaintenanceReminder = false
            };
            // Skill activation remains an explicit later decision, including when replaying onboarding after reset.
            var skills = overview.Skills.Select(item => new SettingsSkillChange(item.Skill, item.Enabled, false)
            {
                ExpectedApprovalFingerprint = item.ApprovalFingerprint
            }).ToArray();
            await features.SaveAsync(new(overview.Settings, preferences, skills,
                CaptureConsent: memory.Value.Capture, ClearLearningConsent: true), ct).ConfigureAwait(false);
            current = current with
            {
                SetupCompleted = true,
                AiEnabled = true,
                DirectModeEnabled = !memory.Value.ConfirmCommands,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await settings.SaveAsync(current, ct).ConfigureAwait(false);
            if (desktop.IsAvailable && await view.ChooseDesktopAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    var result = desktop.Create();
                    shell.RenderSuccess(text.Text(result == DesktopLauncherResult.Created ? "Home.DesktopCreated" : "Home.DesktopExists"));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                    or System.Runtime.InteropServices.COMException or InvalidOperationException
                    or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                {
                    shell.RenderWarning(text.Text("Home.DesktopFailed"));
                }
            }
            view.RenderReady(current.PreferredName);
            return 0;
        }
    }

    /// <summary>Tests a candidate before persisting it, offering bounded retries without exposing provider error bodies.</summary>
    private async Task<FirstRunAction> VerifyAndStoreAsync(string candidate, AppSettings current, CancellationToken ct)
    {
        while (true)
        {
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
                    return FirstRunAction.Next;
                }
                catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException)
                {
                    error = "StorageError";
                }
            }
            var action = await view.ReadConnectionFailureAsync(error, ct).ConfigureAwait(false);
            if (action != FirstRunAction.Next) { return action; }
        }
    }
}

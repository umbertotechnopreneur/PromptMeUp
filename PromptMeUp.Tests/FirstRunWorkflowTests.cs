// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Xunit;

namespace PromptMeUp.Tests;

public sealed class FirstRunWorkflowTests
{
    /// <summary>Completes only after verification and persists independent memory and learning choices.</summary>
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task CompletionRequiresVerificationAndExplicitCapture(bool memories, bool capture, bool addDesktop)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var store = new SkillsAndMemoryStore(fixture.Paths, new SensitiveDataRedactor(), text);
        var events = new List<string>();
        var ready = false;
        var diagnostics = new RecordingLogger();
        var desktopCreated = false;
        var desktop = TestProxy.Create<IDesktopLauncherService>((method, _) =>
        {
            if (method.Name == "get_IsAvailable") { return true; }
            Assert.Equal("Create", method.Name);
            Assert.True(addDesktop);
            desktopCreated = true;
            return DesktopLauncherResult.Created;
        });
        var secrets = Secrets(events);
        var openAi = TestProxy.Create<IOpenAiService>((method, _) =>
        {
            Assert.Equal("TestConnectionAsync", method.Name);
            events.Add("verified");
            return Task.FromResult<ConnectionTestResult>(null!);
        });
        var view = TestProxy.Create<IFirstRunView>((method, args) => method.Name switch
        {
            "RenderWelcome" => null,
            "ChooseLanguageAsync" => Task.FromResult(new FirstRunInput<string>(FirstRunAction.Next, "it")),
            "ReadKeyAsync" => Task.FromResult(new FirstRunInput<string?>(FirstRunAction.Next, SyntheticKey())),
            "ReadPreferencesAsync" => Task.FromResult(new FirstRunInput<FirstRunPreferences>(FirstRunAction.Next,
                new("Luca", memories, capture, true, []))),
            "ChooseDesktopAsync" => Task.FromResult(addDesktop),
            "RenderReady" => MarkReady(args, () => ready = true),
            "ChooseGuideAsync" => Task.FromResult(false),
            _ => throw new InvalidOperationException("Unexpected first-run step: " + method.Name)
        });
        var workflow = CreateWorkflow(fixture, text, store, secrets, openAi, view, desktop, diagnostics);

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));

        Assert.Equal(new[] { "temporary", "verified", "disposed", "stored" }, events);
        Assert.True(ready);
        Assert.Equal(addDesktop, desktopCreated);
        Assert.Contains("Onboarding completed.", diagnostics.Messages);
        Assert.Equal(3, diagnostics.Messages.Count(message => message.StartsWith("Onboarding step opened.", StringComparison.Ordinal)));
        Assert.DoesNotContain(SyntheticKey(), string.Join('\n', diagnostics.Messages), StringComparison.Ordinal);
        Assert.DoesNotContain("Luca", string.Join('\n', diagnostics.Messages), StringComparison.Ordinal);
        var saved = await fixture.Database.LoadSettingsAsync(default);
        Assert.True(saved.SetupCompleted);
        Assert.Equal("Luca", saved.PreferredName);
        Assert.Equal("it", saved.Language);
        Assert.False(saved.DirectModeEnabled);
        var preferences = await store.SettingsAsync(default);
        Assert.Equal(memories, preferences.Enabled);
        Assert.Equal(capture, preferences.CaptureObservations);
        Assert.False(preferences.AutomaticSkills);
    }

    /// <summary>Keeps failed candidates out of persistent storage and retries without asking for another paste.</summary>
    [Theory]
    [InlineData(401, null, "KeyRejected")]
    [InlineData(403, null, "AccessDenied")]
    [InlineData(429, "insufficient_quota", "QuotaExceeded")]
    [InlineData(429, "rate_limit_exceeded", "RateLimited")]
    public async Task RejectedKeyDoesNotCompleteOrPersist(int status, string? providerCode, string expectedError)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var store = new SkillsAndMemoryStore(fixture.Paths, new SensitiveDataRedactor(), text);
        var events = new List<string>();
        var errors = new List<string>();
        var pastes = 0;
        var openAi = TestProxy.Create<IOpenAiService>((method, _) =>
        {
            Assert.Equal("TestConnectionAsync", method.Name);
            return Task.FromException<ConnectionTestResult>(new OpenAiRequestException("Synthetic failure", "responses_api_failed", status)
            {
                ProviderCode = providerCode
            });
        });
        var view = TestProxy.Create<IFirstRunView>((method, args) =>
        {
            switch (method.Name)
            {
                case "RenderWelcome": return null;
                case "ChooseLanguageAsync": return Task.FromResult(new FirstRunInput<string>(FirstRunAction.Next, "en"));
                case "ReadKeyAsync":
                    pastes++;
                    return Task.FromResult(new FirstRunInput<string?>(FirstRunAction.Next, SyntheticKey()));
                case "ReadConnectionFailureAsync":
                    errors.Add((string)args[0]!);
                    return Task.FromResult(errors.Count == 1 ? FirstRunAction.Next : FirstRunAction.Exit);
                default: throw new InvalidOperationException("Verification must prevent later steps: " + method.Name);
            }
        });
        var workflow = CreateWorkflow(fixture, text, store, Secrets(events), openAi, view);

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));

        Assert.Equal(1, pastes);
        Assert.Equal(new[] { expectedError, expectedError }, errors);
        Assert.Equal(new[] { "temporary", "disposed", "temporary", "disposed" }, events);
        Assert.False((await fixture.Database.LoadSettingsAsync(default)).SetupCompleted);
        Assert.False((await store.SettingsAsync(default)).CaptureObservations);
    }

    /// <summary>Restores nested validation credentials across awaits without touching the real environment or vault.</summary>
    [Fact]
    public async Task TemporaryCredentialsRestoreTheirOuterScope()
    {
        var secrets = new EnvironmentSecretService(NullLogger<EnvironmentSecretService>.Instance);
        var outer = SyntheticKey();
        var inner = "sk-" + new string('y', 32);
        using var first = secrets.UseTemporary(AppSettings.DefaultApiKeyVariable, outer);
        Assert.Equal(outer, secrets.Load(AppSettings.DefaultApiKeyVariable));
        using (secrets.UseTemporary(AppSettings.DefaultApiKeyVariable, inner))
        {
            await Task.Yield();
            Assert.Equal(inner, secrets.Load(AppSettings.DefaultApiKeyVariable));
        }
        Assert.Equal(outer, secrets.Load(AppSettings.DefaultApiKeyVariable));
    }

    /// <summary>Builds the workflow over isolated real preferences while rejecting unrelated provider calls.</summary>
    private static FirstRunWorkflow CreateWorkflow(RegressionFixture fixture, ILocalizationService text,
        SkillsAndMemoryStore store, IEnvironmentSecretService secrets, IOpenAiService openAi, IFirstRunView view,
        IDesktopLauncherService? desktop = null, Microsoft.Extensions.Logging.ILogger<FirstRunWorkflow>? logger = null)
    {
        var catalog = new SkillCatalogService(fixture.Paths, store, text, RegressionFixture.CreatePackagedPrompts());
        var shell = TestProxy.Create<IConsoleShellView>((method, args) => method.Name switch
        {
            "RunWithStatusAsync" => ((Func<Task<ConnectionTestResult>>)args[1]!)(),
            "RenderSuccess" => null,
            _ => throw new InvalidOperationException("Unexpected shell action: " + method.Name)
        });
        return new(new SettingsService(fixture.Database), secrets, openAi,
            new SettingsFeatureOverviewService(store, catalog, localization: text), view, shell, text,
            desktop ?? TestProxy.Create<IDesktopLauncherService>((method, _) => method.Name == "get_IsAvailable"
                ? false : throw new InvalidOperationException("Desktop writes are forbidden in this test.")),
            logger ?? NullLogger<FirstRunWorkflow>.Instance,
            new CommandGuideWorkflow(new CommandGuideService(), shell, text, NullLogger<CommandGuideWorkflow>.Instance));
    }

    /// <summary>Tracks validation and persistence order without accessing a real secret store.</summary>
    private static IEnvironmentSecretService Secrets(List<string> events) =>
        TestProxy.Create<IEnvironmentSecretService>((method, _) =>
        {
            switch (method.Name)
            {
                case "IsConfigured": return false;
                case "UseTemporary":
                    events.Add("temporary");
                    return new Scope(() => events.Add("disposed"));
                case "StoreForCurrentUser":
                    events.Add("stored");
                    return new SecretStoreResult("Synthetic vault");
                default: throw new InvalidOperationException("Unexpected secret operation: " + method.Name);
            }
        });

    /// <summary>Verifies that only the chosen nickname reaches the completion view.</summary>
    private static object? MarkReady(object?[] args, Action mark)
    {
        Assert.Equal("Luca", args[0]);
        mark();
        return null;
    }

    /// <summary>Creates a visibly synthetic candidate that is never sent to a provider.</summary>
    private static string SyntheticKey() => "sk-" + new string('x', 32);

    /// <summary>Captures operational diagnostics to ensure credentials and nicknames never enter logs.</summary>
    private sealed class RecordingLogger : Microsoft.Extensions.Logging.ILogger<FirstRunWorkflow>
    {
        internal List<string> Messages { get; } = [];

        /// <summary>Provides an unused scope without retaining user data.</summary>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>Enables all operational levels for assertions.</summary>
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        /// <summary>Captures only the formatted diagnostic event emitted by the workflow.</summary>
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }

    /// <summary>Tracks disposal of a fake credential scope.</summary>
    private sealed class Scope(Action dispose) : IDisposable
    {
        /// <summary>Records the end of validation.</summary>
        public void Dispose() => dispose();
    }
}

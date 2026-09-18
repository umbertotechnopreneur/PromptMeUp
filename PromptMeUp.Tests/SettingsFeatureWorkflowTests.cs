// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class SettingsFeatureWorkflowTests
{
    /// <summary>Opening or cancelling Settings reads status without loading credentials, invoking a child menu, or changing consent.</summary>
    [Fact]
    public async Task RunAsync_StatusOnly_PreservesDisabledDefaults()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var loads = 0;
        var services = Settings(() => loads++, _ => throw new InvalidOperationException("Unexpected settings save."));
        var workflow = Workflow(fixture, services, state =>
        {
            Assert.NotNull(state.FeatureOverview);
            Assert.False(state.FeatureOverview.Settings.Enabled);
            Assert.NotNull(state.OpenSkills);
            Assert.NotNull(state.OpenLearning);
            return null;
        }, () => throw new InvalidOperationException("Unexpected child menu."));

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));

        Assert.Equal(0, loads);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_revisions;"));
    }

    /// <summary>A malformed local package is reported as unavailable without blocking unrelated settings or enabling experiments.</summary>
    [Fact]
    public async Task RunAsync_InvalidCatalog_StillOpensSettings()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", "malformed");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"), "Invalid synthetic package");
        var collected = false;
        var services = Settings(() => throw new InvalidOperationException("Unexpected action settings load."),
            _ => throw new InvalidOperationException("Unexpected save."));
        var workflow = Workflow(fixture, services, state =>
        {
            collected = true;
            Assert.True(state.FeatureOverview!.CatalogUnavailable);
            Assert.False(state.FeatureOverview.Settings.Enabled);
            return null;
        }, () => throw new InvalidOperationException("Unexpected child menu."));

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));

        Assert.True(collected);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
    }

    /// <summary>An explicitly attempted action on a disabled skill reports its error while keeping the parent draft saveable.</summary>
    [Fact]
    public async Task RunAsync_InvalidChildAction_ReportsErrorAndPreservesDraft()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var store = new ExperimentalStore(fixture.Paths, new SensitiveDataRedactor(), text);
        await store.SaveSettingsAsync(new(Enabled: true), new(), default);
        var catalog = new SkillCatalogService(fixture.Paths, store, text,
            new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));
        var index = catalog.List().ToList().FindIndex(skill => skill.Name == "filesystem");
        Assert.True(index >= 0);
        var keys = new Queue<ConsoleKeyInfo>([
            .. Enumerable.Repeat(Key(ConsoleKey.DownArrow), index + 5), Key(ConsoleKey.Enter),
            .. Enumerable.Repeat(Key(ConsoleKey.DownArrow), 3), Key(ConsoleKey.Enter)]);
        var errors = new List<string>();
        AppSettings? saved = null;
        var expected = AppSettings.Default with { SetupCompleted = true, PreferredName = "Morgan" };
        var workflow = Workflow(fixture, Settings(() => { }, value => saved = value), state =>
        {
            var refreshed = state.OpenSkills!();
            Assert.Equal(state.FeatureOverview, refreshed);
            return new SetupSubmission(expected, null, null, false);
        }, keys.Dequeue, errors.Add);

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));

        Assert.Empty(keys);
        Assert.Equal(text.Text("Lab.Activate"), Assert.Single(errors));
        Assert.Same(expected, saved);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings WHERE name LIKE 'skill:%';"));
    }

    /// <summary>Explicit child-menu decisions survive parent cancellation and return refreshed project status.</summary>
    [Fact]
    public async Task RunAsync_ChildMenus_PreserveImmediateConsentAcrossParentCancel()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var loads = 0;
        var input = new Queue<ConsoleKeyInfo>([
            Key(ConsoleKey.DownArrow), Key(ConsoleKey.Enter), Key(ConsoleKey.Enter), Key(ConsoleKey.Enter)]);
        var services = Settings(() => loads++, _ => throw new InvalidOperationException("Unexpected parent save."));
        var workflow = Workflow(fixture, services, state =>
        {
            Assert.False(state.FeatureOverview!.Settings.Enabled);
            var skills = state.OpenSkills!();
            Assert.True(skills.Settings.Enabled);
            Assert.False(skills.Settings.CaptureObservations);
            Assert.Equal(0, skills.EnabledSkillCount);
            var learning = state.OpenLearning!();
            Assert.Equal(skills, learning);
            return null;
        }, input.Dequeue);

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));

        Assert.Empty(input);
        Assert.Equal(2, loads);
        var preferences = await new ExperimentalStore(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService()).SettingsAsync(default);
        Assert.Equal(new ExperimentalSettings(Enabled: true), preferences);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_observations;"));
    }

    /// <summary>Escape closes only the child menu, leaving the parent's pending submission available to save.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunAsync_ChildEscape_PreservesParentSubmission(bool skills)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var loads = 0;
        AppSettings? saved = null;
        var expected = AppSettings.Default with { SetupCompleted = true, PreferredName = "Morgan" };
        var services = Settings(() => loads++, value => saved = value);
        var workflow = Workflow(fixture, services, state =>
        {
            var refreshed = (skills ? state.OpenSkills : state.OpenLearning)!();
            Assert.Equal(state.FeatureOverview, refreshed);
            return new SetupSubmission(expected, null, null, false);
        }, () => throw new InteractiveFlowCanceledException());

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));

        Assert.Equal(1, loads);
        Assert.Same(expected, saved);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
    }

    /// <summary>Application cancellation must propagate rather than reopening the parent settings form.</summary>
    [Fact]
    public async Task RunAsync_ShutdownInChildMenu_PropagatesCancellation()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var cancellation = new CancellationTokenSource();
        var services = Settings(() => { }, _ => throw new InvalidOperationException("Unexpected save."));
        var workflow = Workflow(fixture, services, state =>
        {
            state.OpenSkills!();
            throw new InvalidOperationException("The child must propagate shutdown.");
        }, () =>
        {
            cancellation.Cancel();
            throw new InteractiveFlowCanceledException();
        });

        await Assert.ThrowsAsync<InteractiveFlowCanceledException>(() => workflow.RunAsync(AppSettings.Default, cancellation.Token));
    }

    /// <summary>Supplies persisted preferences separately from the caller's draft and records only explicit saves.</summary>
    private static ISettingsService Settings(Action loaded, Action<AppSettings> saved) =>
        TestProxy.Create<ISettingsService>((method, args) =>
        {
            switch (method.Name)
            {
                case "LoadAsync":
                    loaded();
                    return Task.FromResult(AppSettings.Default with { SetupCompleted = true, AiEnabled = false });
                case "SaveAsync":
                    saved((AppSettings)args[0]!);
                    return Task.CompletedTask;
                default:
                    throw new NotSupportedException(method.Name);
            }
        });

    /// <summary>Wires production child menus to disposable storage and synthetic input with provider and command calls forbidden.</summary>
    private static SetupWorkflow Workflow(RegressionFixture fixture, ISettingsService settings,
        Func<SetupViewState, SetupSubmission?> collect, Func<ConsoleKeyInfo> readKey, Action<string>? error = null)
    {
        var text = new LocalizationService();
        var redactor = new SensitiveDataRedactor();
        var store = new ExperimentalStore(fixture.Paths, redactor, text);
        var catalog = new SkillCatalogService(fixture.Paths, store, text,
            new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));
        var secrets = TestProxy.Create<IEnvironmentSecretService>((method, _) => method.Name == "IsConfigured"
            ? false : throw new InvalidOperationException("Credentials must not be read or changed."));
        var shell = TestProxy.Create<IConsoleShellView>((method, args) =>
        {
            if (method.Name == "RenderError" && error is not null)
            {
                error((string)args[0]!);
                return null;
            }
            return method.Name is "RenderNotice" or "RenderSuccess"
                ? null : throw new InvalidOperationException("Unexpected shell action: " + method.Name);
        });
        var console = Console(readKey);
        var experimental = new ExperimentalWorkflow(catalog, new SkillActionService(redactor, text), store,
            TestProxy.Create<IAuthorizedCommandWorkflow>((_, _) => throw new InvalidOperationException("Commands are forbidden.")),
            fixture.Audit, new ExperimentalView(console, text), shell, text, new MemoryReflectionService(redactor, text),
            new PersistentMemoryService(fixture.Paths, redactor, text, NullLogger<PersistentMemoryService>.Instance),
            new ArtifactAssistant(TestProxy.Create<IOpenAiService>((_, _) => throw new InvalidOperationException("Provider calls are forbidden.")),
                fixture.Audit, new BoundedTextInput(redactor, text), shell, text),
            secrets, new ReminderService(fixture.Paths, redactor, text, catalog));
        return new SetupWorkflow(settings, secrets,
            TestProxy.Create<IAiConversationWorkflow>((_, _) => throw new InvalidOperationException("AI calls are forbidden.")),
            new ApplicationActivityRecorder(fixture.Audit, NullLogger<ApplicationActivityRecorder>.Instance),
            TestProxy.Create<ISetupView>((_, args) => collect((SetupViewState)args[0]!)), shell, text,
            experimental: experimental, featureOverview: new SettingsFeatureOverviewService(store, catalog));
    }

    /// <summary>Creates an in-memory interactive terminal without accessing the user's real console.</summary>
    private static IAnsiConsole Console(Func<ConsoleKeyInfo> readKey)
    {
        var input = TestProxy.Create<IAnsiConsoleInput>((method, _) => method.Name switch
        {
            "ReadKey" => readKey(),
            "ReadKeyAsync" => Task.FromResult<ConsoleKeyInfo?>(readKey()),
            "IsKeyAvailable" => true,
            _ => throw new NotSupportedException(method.Name)
        });
        var rendering = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter())
        });
        rendering.Profile.Width = 240;
        rendering.Profile.Capabilities.Interactive = true;
        rendering.Profile.Capabilities.AlternateBuffer = false;
        return TestProxy.Create<IAnsiConsole>((method, args) => method.Name == "get_Input"
            ? input : method.Invoke(rendering, args));
    }

    /// <summary>Creates one synthetic navigation key for the child selection prompt.</summary>
    private static ConsoleKeyInfo Key(ConsoleKey key) => new(key == ConsoleKey.Enter ? '\r' : '\0', key, false, false, false);
}

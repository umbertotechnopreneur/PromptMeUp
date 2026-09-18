// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class SettingsFeatureWorkflowTests
{
    /// <summary>Opening and cancelling settings leaves project defaults and consent untouched.</summary>
    [Fact]
    public async Task RunAsync_Cancel_PreservesDisabledDefaults()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var workflow = Workflow(fixture, state =>
        {
            Assert.NotNull(state.FeatureOverview);
            Assert.False(state.FeatureOverview.Settings.Enabled);
            Assert.NotEmpty(state.FeatureOverview.Skills);
            return null;
        }, _ => throw new InvalidOperationException("Unexpected settings save."));

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_revisions;"));
    }

    /// <summary>A malformed local package does not prevent opening unrelated settings.</summary>
    [Fact]
    public async Task RunAsync_InvalidCatalog_StillOpensSettings()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", "malformed");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"), "Invalid synthetic package");
        var collected = false;
        var workflow = Workflow(fixture, state =>
        {
            collected = true;
            Assert.True(state.FeatureOverview!.CatalogUnavailable);
            Assert.False(state.FeatureOverview.Settings.Enabled);
            return null;
        }, _ => throw new InvalidOperationException("Unexpected settings save."));

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));
        Assert.True(collected);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
    }

    /// <summary>A single submission saves both inline project preferences and ordinary app settings.</summary>
    [Fact]
    public async Task RunAsync_Save_AppliesInlineFeatureAndOrdinaryDrafts()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var expected = AppSettings.Default with { SetupCompleted = true, PreferredName = "Morgan" };
        var desired = new ExperimentalSettings(Enabled: true, AutomaticSkills: true);
        AppSettings? saved = null;
        var workflow = Workflow(fixture, state => new SetupSubmission(expected, null, null, false)
        {
            Features = new(state.FeatureOverview!.Settings, desired, [])
        }, value => { saved = value; return Task.CompletedTask; });

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));
        Assert.Same(expected, saved);
        Assert.Equal(desired, await Store(fixture).SettingsAsync(default));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_observations;"));
    }

    /// <summary>An ordinary settings save does not create or purge feature data.</summary>
    [Fact]
    public async Task RunAsync_NoFeatureChanges_DoesNotPersistFeatureSettings()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var saves = 0;
        var workflow = Workflow(fixture, _ => new SetupSubmission(AppSettings.Default, null, null, false),
            _ => { saves++; return Task.CompletedTask; });

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));
        Assert.Equal(1, saves);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_revisions;"));
    }

    /// <summary>Saving from the fullscreen editor reopens the saved draft on its active section until the user cancels.</summary>
    [Fact]
    public async Task RunAsync_KeepOpen_ReopensSavedSettingsOnSelectedSection()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var expected = AppSettings.Default with { SetupCompleted = true, Theme = "violet" };
        var states = new List<SetupViewState>();
        var saves = 0;
        var workflow = Workflow(fixture, state =>
        {
            states.Add(state);
            return states.Count == 1
                ? new SetupSubmission(expected, null, null, false)
                {
                    KeepOpen = true,
                    SelectedSection = SettingsSection.Theme
                }
                : null;
        }, _ =>
        {
            saves++;
            return Task.CompletedTask;
        });

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));
        Assert.Equal(1, saves);
        Assert.Collection(states,
            first =>
            {
                Assert.Equal(SettingsSection.General, first.InitialSection);
                Assert.False(first.SaveSucceeded);
            },
            second =>
            {
                Assert.Equal(expected, second.Settings);
                Assert.Equal(SettingsSection.Theme, second.InitialSection);
                Assert.True(second.SaveSucceeded);
            });
    }

    /// <summary>A concurrent preference update rejects the stale draft before credentials or app settings are written.</summary>
    [Fact]
    public async Task RunAsync_StaleDraft_DoesNotApplyAnySubmissionWrites()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var newer = new ExperimentalSettings(Enabled: true, CaptureObservations: true);
        var workflow = Workflow(fixture, state =>
        {
            Store(fixture).SaveSettingsAsync(newer, state.FeatureOverview!.Settings, default).GetAwaiter().GetResult();
            return new SetupSubmission(AppSettings.Default, "unused-synthetic-value", null, false)
            {
                Features = new(state.FeatureOverview.Settings, new(Enabled: true), [])
            };
        }, _ => throw new InvalidOperationException("Unexpected settings save."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.RunAsync(AppSettings.Default, default));
        Assert.Equal(newer, await Store(fixture).SettingsAsync(default));
    }

    /// <summary>Capture requires explicit consent before any submission writes can occur.</summary>
    [Fact]
    public async Task RunAsync_MissingCaptureConsent_RejectsSave()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var workflow = Workflow(fixture, state => new SetupSubmission(AppSettings.Default, "unused-synthetic-value", null, false)
        {
            Features = new(state.FeatureOverview!.Settings, new(Enabled: true, CaptureObservations: true), [])
        }, _ => throw new InvalidOperationException("Unexpected settings save."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.RunAsync(AppSettings.Default, default));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
    }

    /// <summary>A later app settings failure accurately reports already committed feature preferences.</summary>
    [Fact]
    public async Task RunAsync_LaterSaveFailure_ReportsPartialPersistence()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var desired = new ExperimentalSettings(Enabled: true);
        var warnings = new List<string>();
        var workflow = Workflow(fixture, state => new SetupSubmission(AppSettings.Default, null, null, false)
        {
            Features = new(state.FeatureOverview!.Settings, desired, [])
        }, _ => Task.FromException(new IOException("Synthetic settings write failure.")), warnings.Add);

        await Assert.ThrowsAsync<IOException>(() => workflow.RunAsync(AppSettings.Default, default));
        Assert.Equal(desired, await Store(fixture).SettingsAsync(default));
        Assert.Equal(new LocalizationService().Text("Settings.FeaturesPartiallySaved"), Assert.Single(warnings));
    }

    /// <summary>A simultaneous language and skill edit budgets the target translation before persisting any part of the submission.</summary>
    [Fact]
    public async Task RunAsync_NewLanguageOversizedSkill_RejectsAndRestoresOriginalLanguage()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var realPrompts = new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
        var translations = SupportedLanguages.Codes.ToDictionary(language => language, _ => "Short synthetic instructions.");
        translations["it"] = new string('a', 8000);
        var translated = new PromptDefinition("skill-set_reminder", 1, "Synthetic translated budget regression", [],
            translations, new Dictionary<string, string>());
        var prompts = TestProxy.Create<IPromptCatalogService>((method, args) => method.Name == nameof(IPromptCatalogService.GetAsync)
            ? (string)args[0]! == translated.Id
                ? Task.FromResult(translated)
                : realPrompts.GetAsync((string)args[0]!, (CancellationToken)args[1]!)
            : throw new NotSupportedException(method.Name));
        var warnings = new List<string>();
        var workflow = Workflow(fixture, state =>
        {
            Assert.Equal("en", text.Language);
            var skill = Assert.Single(state.FeatureOverview!.Skills, item => item.Skill.Name == "set_reminder");
            return new SetupSubmission(AppSettings.Default with { Language = "it" }, null, null, false)
            {
                Features = new(state.FeatureOverview.Settings, new(Enabled: true),
                [new(skill.Skill, skill.Enabled, true) { ExpectedApprovalFingerprint = skill.ApprovalFingerprint }])
            };
        }, _ => throw new InvalidOperationException("App settings must not be saved after a failed target-language budget check."),
            warnings.Add, text, prompts);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.RunAsync(AppSettings.Default, default));

        var italian = new LocalizationService();
        italian.SetLanguage("it");
        Assert.Equal(italian.Text("Lab.SkillTooLarge", "set_reminder", SkillCatalogService.MaximumContextTokens), error.Message);
        Assert.Equal("en", text.Language);
        Assert.Empty(warnings);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_revisions;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests;"));
    }

    /// <summary>A successfully validated feature save restores the old language for later writes and applies the target only after success.</summary>
    [Fact]
    public async Task RunAsync_NewLanguageValidSkill_AppliesLanguageOnlyAfterSuccessfulSave()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var saved = false;
        var workflow = Workflow(fixture, state =>
        {
            var skill = Assert.Single(state.FeatureOverview!.Skills, item => item.Skill.Name == "set_reminder");
            return new SetupSubmission(AppSettings.Default with { Language = "it" }, null, null, false)
            {
                Features = new(state.FeatureOverview.Settings, new(Enabled: true),
                [new(skill.Skill, skill.Enabled, true) { ExpectedApprovalFingerprint = skill.ApprovalFingerprint }])
            };
        }, value =>
        {
            Assert.Equal("en", text.Language);
            Assert.Equal("it", value.Language);
            saved = true;
            return Task.CompletedTask;
        }, localization: text);

        Assert.Equal(0, await workflow.RunAsync(AppSettings.Default, default));

        Assert.True(saved);
        Assert.Equal("it", text.Language);
        Assert.True((await Store(fixture).SettingsAsync(default)).Enabled);
    }

    /// <summary>Creates a feature store over the isolated test database.</summary>
    private static ExperimentalStore Store(RegressionFixture fixture) =>
        new(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService());

    /// <summary>Uses real feature persistence while forbidding provider calls and unexpected secret access.</summary>
    private static SetupWorkflow Workflow(RegressionFixture fixture, Func<SetupViewState, SetupSubmission?> collect,
        Func<AppSettings, Task> save, Action<string>? warning = null, ILocalizationService? localization = null,
        IPromptCatalogService? prompts = null)
    {
        var text = localization ?? new LocalizationService();
        var store = Store(fixture);
        var catalog = new SkillCatalogService(fixture.Paths, store, text,
            prompts ?? new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));
        var secrets = TestProxy.Create<IEnvironmentSecretService>((method, _) => method.Name == "IsConfigured"
            ? false : throw new InvalidOperationException("Credentials must not be read or changed."));
        var shell = TestProxy.Create<IConsoleShellView>((method, args) =>
        {
            if (method.Name == "RenderWarning" && warning is not null)
            {
                warning((string)args[0]!);
                return null;
            }
            return method.Name is "RenderNotice" or "RenderSuccess"
                ? null : throw new InvalidOperationException("Unexpected shell action: " + method.Name);
        });
        var settings = TestProxy.Create<ISettingsService>((method, args) => method.Name == "SaveAsync"
            ? save((AppSettings)args[0]!) : throw new InvalidOperationException("Unexpected settings action."));
        return new SetupWorkflow(settings, secrets,
            TestProxy.Create<IAiConversationWorkflow>((_, _) => throw new InvalidOperationException("AI calls are forbidden.")),
            new ApplicationActivityRecorder(fixture.Audit, NullLogger<ApplicationActivityRecorder>.Instance),
            TestProxy.Create<ISetupView>((_, args) => collect((SetupViewState)args[0]!)), shell, text,
            featureOverview: new SettingsFeatureOverviewService(store, catalog));
    }
}

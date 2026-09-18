// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class SettingsFeatureViewTests
{
    private const int SectionCount = 12;

    /// <summary>Merely viewing, saving, or cancelling a settings section never opens a feature workflow.</summary>
    [Theory]
    [InlineData(SettingsSection.General, true)]
    [InlineData(SettingsSection.Skills, false)]
    [InlineData(SettingsSection.Learning, true)]
    [InlineData(SettingsSection.Privacy, false)]
    public void Collect_WithoutExplicitOpen_DoesNotInvokeFeatureCallbacks(SettingsSection section, bool save)
    {
        var fields = section == SettingsSection.General ? 1 : 0;
        var harness = Create(Choose(fields + SectionCount + (save ? 0 : 1)));
        var calls = 0;
        var overview = new SettingsFeatureOverview(new(Enabled: true), 2, 10);
        var state = new SetupViewState(AppSettings.Default with { AiEnabled = false }, false, false)
        {
            InitialSection = section,
            FeatureOverview = overview,
            OpenSkills = () => { calls++; return overview; },
            OpenLearning = () => { calls++; return overview; },
            OpenMemories = () => calls++
        };

        var submission = harness.View.Collect(state);

        Assert.Equal(save, submission is not null);
        Assert.Equal(0, calls);
        Assert.Empty(harness.Keys);
        Assert.Contains(harness.Text.Text("Settings.FeaturesTitle"), Plain(harness.Output), StringComparison.Ordinal);
        Assert.Contains("2 / 10", Plain(harness.Output), StringComparison.Ordinal);
    }

    /// <summary>Explicit nested navigation refreshes saved feature state without saving or losing the parent draft.</summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public void Collect_OpenFeature_RefreshesOverviewAndPreservesDraft(bool learning, bool save)
    {
        var input = Choose(0).Concat(Type("Draft name\r"))
            .Concat(Choose(3 + (learning ? 8 : 7)))
            .Concat(Choose(3))
            .Concat(Choose(1 + 5))
            .Concat(Choose(3 + SectionCount + (save ? 0 : 1)));
        var harness = Create(input);
        var original = AppSettings.Default with { AiEnabled = false, PreferredName = "Saved name" };
        var persisted = new SettingsFeatureOverview(new(), 0, 10);
        var updated = new SettingsFeatureOverview(new(Enabled: true, CaptureObservations: true), 4, 10);
        var skillCalls = 0;
        var learningCalls = 0;
        var state = new SetupViewState(original, false, false)
        {
            InitialSection = SettingsSection.Personalization,
            FeatureOverview = persisted,
            OpenSkills = () =>
            {
                skillCalls++;
                harness.Text.SetLanguage("fr");
                return persisted = updated;
            },
            OpenLearning = () =>
            {
                learningCalls++;
                harness.Text.SetLanguage("fr");
                return persisted = updated;
            }
        };

        var submission = harness.View.Collect(state);

        Assert.Equal(save, submission is not null);
        Assert.Equal(learning ? 0 : 1, skillCalls);
        Assert.Equal(learning ? 1 : 0, learningCalls);
        Assert.Equal(updated, persisted);
        Assert.Equal("Saved name", original.PreferredName);
        if (save)
        {
            Assert.Equal("Draft name", submission!.Settings.PreferredName);
            Assert.Equal(original.Model, submission.Settings.Model);
        }
        Assert.Equal("en", harness.Text.Language);
        Assert.Empty(harness.Keys);
        var rendered = Plain(harness.Output);
        Assert.Contains("4 / 10", rendered, StringComparison.Ordinal);
        Assert.Contains(harness.Text.Text("Settings.DraftStatus"), rendered, StringComparison.Ordinal);
        Assert.Contains("cancelling Settings does not undo", rendered, StringComparison.Ordinal);
        Assert.Contains("saved AI, model and credentials", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("Comment vous appeler", rendered, StringComparison.Ordinal);
    }

    /// <summary>A missing feature snapshot remains unknown rather than appearing to revoke consent.</summary>
    [Fact]
    public void Collect_WithoutSnapshot_ShowsUnavailableInsteadOfDisabled()
    {
        var harness = Create(Choose(SectionCount + 1));

        Assert.Null(harness.View.Collect(new SetupViewState(AppSettings.Default, false, false)
        {
            InitialSection = SettingsSection.Privacy
        }));

        var rendered = Plain(harness.Output);
        Assert.Matches(Regex.Escape(harness.Text.Text("Settings.FeatureExperiment") + ":") + @"\s+"
            + Regex.Escape(harness.Text.Text("Costs.Unavailable")), rendered);
        Assert.DoesNotContain("0 / 0", rendered, StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>A damaged skill catalog hides its unknown count without losing saved feature state or privacy navigation.</summary>
    [Fact]
    public void Collect_UnavailableCatalog_ShowsWarningAndRealConsentFlags()
    {
        var harness = Create(Choose(SectionCount + 1));

        Assert.Null(harness.View.Collect(new SetupViewState(AppSettings.Default, false, false)
        {
            InitialSection = SettingsSection.Privacy,
            FeatureOverview = new SettingsFeatureOverview(new(Enabled: true, CaptureObservations: true), 0, 0)
            {
                CatalogUnavailable = true
            }
        }));

        var rendered = Plain(harness.Output);
        Assert.Contains(harness.Text.Text("Settings.SkillCatalogUnavailable"), rendered, StringComparison.Ordinal);
        Assert.Matches(Regex.Escape(harness.Text.Text("Settings.FeatureSkills") + ":") + @"\s+"
            + Regex.Escape(harness.Text.Text("Costs.Unavailable")), rendered);
        Assert.Matches(Regex.Escape(harness.Text.Text("Settings.FeatureCapture") + ":") + @"\s+"
            + Regex.Escape(harness.Text.Text("Lab.Enabled")), rendered);
        Assert.DoesNotContain("0 / 0", rendered, StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>The master switch makes persisted sub-options inactive without erasing their stored consent flags.</summary>
    [Fact]
    public void Collect_DisabledExperiment_ShowsEffectiveOffState()
    {
        var harness = Create(Choose(SectionCount + 1));
        var overview = new SettingsFeatureOverview(new(Enabled: false, AutomaticSkills: true, CaptureObservations: true, MaintenanceReminder: true), 0, 10);

        Assert.Null(harness.View.Collect(new SetupViewState(AppSettings.Default, false, false)
        {
            InitialSection = SettingsSection.Privacy,
            FeatureOverview = overview
        }));

        var rendered = Plain(harness.Output);
        Assert.Matches(Regex.Escape(harness.Text.Text("Settings.FeatureCapture") + ":") + @"\s+"
            + Regex.Escape(harness.Text.Text("Lab.Off")), rendered);
        Assert.Contains(harness.Text.Text("Settings.FeaturesGate"), rendered, StringComparison.Ordinal);
        Assert.True(overview.Settings.CaptureObservations);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Privacy facts and explicit menu-persistence guidance remain available in every supported language.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public void PrivacyCopy_AllLanguages_ExplainsDataAndControl(string language)
    {
        var harness = Create(Choose(SectionCount + 1));
        harness.Text.SetLanguage(language);

        Assert.Null(harness.View.Collect(new SetupViewState(AppSettings.Default with { Language = language }, false, false)
        {
            InitialSection = SettingsSection.Privacy,
            FeatureOverview = new SettingsFeatureOverview(new(), 0, 10)
        }));

        var rendered = Regex.Replace(Plain(harness.Output), @"\s+", " ");
        foreach (var key in new[] { "Local", "Provider", "Learning", "Skills", "Control" })
        {
            Assert.Contains(Regex.Replace(harness.Text.Text("Settings.Privacy" + key + "Info"), @"\s+", " "), rendered, StringComparison.Ordinal);
        }
        Assert.Contains(harness.Text.Text("Settings.PrivacyProvider"), rendered, StringComparison.Ordinal);
        Assert.Contains("OpenAI", harness.Text.Text("Settings.PrivacyProvider"), StringComparison.Ordinal);
        Assert.Contains("200", harness.Text.Text("Settings.PrivacyLearningInfo"), StringComparison.Ordinal);
        Assert.Contains("30", harness.Text.Text("Settings.PrivacyLearningInfo"), StringComparison.Ordinal);
        Assert.NotEmpty(harness.Text.Text("Settings.FeatureMenuNotice"));
        Assert.Empty(harness.Keys);
    }

    /// <summary>Creates the real passive view with fake keyboard input, output, theme data, and no external workflows.</summary>
    private static Harness Create(IEnumerable<ConsoleKeyInfo> inputKeys)
    {
        var keys = new Queue<ConsoleKeyInfo>(inputKeys);
        var input = TestProxy.Create<IAnsiConsoleInput>((method, _) => method.Name switch
        {
            "ReadKey" => keys.Dequeue(),
            "ReadKeyAsync" => Task.FromResult<ConsoleKeyInfo?>(keys.Dequeue()),
            "IsKeyAvailable" => keys.Count > 0,
            _ => throw new NotSupportedException(method.Name)
        });
        var output = new StringWriter();
        var rendering = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(output)
        });
        rendering.Profile.Width = 360;
        rendering.Profile.Capabilities.Interactive = true;
        rendering.Profile.Capabilities.AlternateBuffer = false;
        var console = TestProxy.Create<IAnsiConsole>((method, args) => method.Name == "get_Input" ? input : method.Invoke(rendering, args));
        var text = new LocalizationService();
        var shell = new ConsoleShellView(console, text);
        shell.Configure(new ConsoleRenderOptions(true, true));
        var themes = TestProxy.Create<IThemeCatalogService>((method, _) => method.Name switch
        {
            "Resolve" => TerminalThemeDefinition.Default,
            "get_Themes" => new[] { TerminalThemeDefinition.Default },
            _ => throw new NotSupportedException(method.Name)
        });
        var about = TestProxy.Create<IAboutView>((method, _) => throw new NotSupportedException(method.Name));
        var view = new FullscreenSetupView(console, text, shell, new PromptInjectionProtectionService(), new SensitiveDataRedactor(), themes, about);
        return new Harness(view, text, output, keys);
    }

    /// <summary>Chooses one explicit fallback-menu action using only deterministic fake key presses.</summary>
    private static IEnumerable<ConsoleKeyInfo> Choose(int index) =>
        Enumerable.Repeat(Key(ConsoleKey.DownArrow), index).Append(Key(ConsoleKey.Enter));

    /// <summary>Types ordinary draft text without reading or changing the real terminal.</summary>
    private static IEnumerable<ConsoleKeyInfo> Type(string value) => value.Select(character =>
        character == '\r' ? Key(ConsoleKey.Enter) : new ConsoleKeyInfo(character, (ConsoleKey)0, false, false, false));

    /// <summary>Creates an input event for one navigation or confirmation key.</summary>
    private static ConsoleKeyInfo Key(ConsoleKey key) => new(key == ConsoleKey.Enter ? '\r' : '\0', key, false, false, false);

    /// <summary>Removes rendering escapes while preserving human-readable output for assertions.</summary>
    private static string Plain(StringWriter output) => Regex.Replace(output.ToString(), @"\x1B\[[0-?]*[ -/]*[@-~]", string.Empty, RegexOptions.CultureInvariant);

    private sealed record Harness(FullscreenSetupView View, LocalizationService Text, StringWriter Output, Queue<ConsoleKeyInfo> Keys);
}

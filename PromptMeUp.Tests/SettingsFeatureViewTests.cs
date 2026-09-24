// SPDX-License-Identifier: MIT

using System.Reflection;
using System.Text.RegularExpressions;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Tests;

public sealed class SettingsFeatureViewTests
{
    private const int SectionCount = 12;

    /// <summary>Viewing and saving unchanged tabs neither opens child menus nor submits feature edits.</summary>
    [Theory]
    [InlineData(SettingsSection.General, 1)]
    [InlineData(SettingsSection.Skills, 2)]
    [InlineData(SettingsSection.Learning, 3)]
    [InlineData(SettingsSection.Privacy, 0)]
    public void Collect_UnchangedSave_DoesNotSubmitFeaturesOrOpenMemories(SettingsSection section, int fieldCount)
    {
        var harness = Create(Choose(fieldCount + SectionCount));
        var calls = 0;
        var submission = harness.View.Collect(State(section, new(new(Enabled: true), 0, 0)) with
        {
            OpenMemories = () => calls++
        });

        Assert.NotNull(submission);
        Assert.Null(submission.Features);
        Assert.Equal(0, calls);
        Assert.Empty(harness.Keys);
    }

    /// <summary>The shared master and collection preferences remain drafts until Save and require explicit consent.</summary>
    [Fact]
    public void Collect_InlineSkillsAndMemory_SaveOneDraftWithExplicitConsent()
    {
        var keys = Choose(0).Concat(Choose(1)).Concat(Choose(2 + 8))
            .Concat(Choose(1)).Concat(Choose(1)).Concat(Choose(4 + SectionCount))
            .Concat(Choose(3)).Concat(Choose(1)).Concat(Choose(4 + SectionCount));
        var harness = Create(keys);
        var original = new SettingsFeatureOverview(new(), 0, 0);

        var submission = harness.View.Collect(State(SettingsSection.Skills, original));

        Assert.NotNull(submission);
        Assert.NotNull(submission.Features);
        Assert.True(submission.Features.Settings.Enabled);
        Assert.True(submission.Features.Settings.CaptureObservations);
        Assert.True(submission.Features.CaptureConsent);
        Assert.False(original.Settings.Enabled);
        Assert.False(original.Settings.CaptureObservations);
        Assert.Contains(harness.Text.Text("Settings.FeatureConsentRequired"), Plain(harness.Output), StringComparison.Ordinal);
        Assert.Contains(harness.Text.Text("Settings.CaptureProviderInfo"), Plain(harness.Output), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Cancel abandons feature changes without mutating the supplied snapshot.</summary>
    [Fact]
    public void Collect_CancelFeatureEdit_DiscardsDraft()
    {
        var harness = Create(Choose(0).Concat(Choose(1)).Concat(Choose(2 + SectionCount + 1)));
        var overview = new SettingsFeatureOverview(new(), 0, 0);

        Assert.Null(harness.View.Collect(State(SettingsSection.Skills, overview)));

        Assert.False(overview.Settings.Enabled);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Editing a feature tab preserves ordinary preferences in the same Save operation.</summary>
    [Fact]
    public void Collect_FeatureEdit_PreservesPersonalizationDraft()
    {
        var typed = "Draft name".Select(character => new ConsoleKeyInfo(character, (ConsoleKey)0, false, false, false)).Append(Key(ConsoleKey.Enter));
        var harness = Create(Choose(0).Concat(typed).Concat(Choose(3 + 7))
            .Concat(Choose(0)).Concat(Choose(1)).Concat(Choose(2 + SectionCount)));
        var state = State(SettingsSection.Personalization, new(new(), 0, 0));

        var submission = harness.View.Collect(state);

        Assert.Equal("Draft name", submission!.Settings.PreferredName);
        Assert.True(submission.Features!.Settings.Enabled);
        Assert.Empty(state.Settings.PreferredName);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Enabling the master never silently revives a retained collection preference.</summary>
    [Fact]
    public void Collect_EnableMaster_ResetsLatentCaptureAndRequiresClearConsent()
    {
        var harness = Create(Choose(0).Concat(Choose(1)).Concat(Choose(3)).Concat(Choose(1))
            .Concat(Choose(4 + SectionCount)));

        var submission = harness.View.Collect(State(SettingsSection.Learning,
            new(new(Enabled: false, CaptureObservations: true), 0, 0)));

        Assert.True(submission!.Features!.Settings.Enabled);
        Assert.False(submission.Features.Settings.CaptureObservations);
        Assert.False(submission.Features.CaptureConsent);
        Assert.True(submission.Features.ClearLearningConsent);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Changing the collection toggle invalidates a previous unsaved acknowledgement.</summary>
    [Fact]
    public void Collect_ChangedCaptureDraft_RequiresFreshConsent()
    {
        var keys = Choose(1).Concat(Choose(1)).Concat(Choose(3)).Concat(Choose(1))
            .Concat(Choose(1)).Concat(Choose(1)).Concat(Choose(1)).Concat(Choose(1))
            .Concat(Choose(4 + SectionCount)).Concat(Choose(4 + SectionCount + 1));
        var harness = Create(keys);

        Assert.Null(harness.View.Collect(State(SettingsSection.Learning, new(new(Enabled: true), 0, 0))));

        Assert.Contains(harness.Text.Text("Settings.FeatureConsentRequired"), Plain(harness.Output), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Package activation shows literal source before the choice and binds the exact inspected approval.</summary>
    [Fact]
    public void Collect_EnableSkill_PreviewsSourceAndPreservesApprovalFingerprint()
    {
        var skill = Skill();
        var harness = Create(Choose(2).Concat(Choose(1)).Concat(Choose(3 + SectionCount)));
        var overview = new SettingsFeatureOverview(new(Enabled: true), 0, 1)
        {
            Skills = [new(skill, false) { ApprovalFingerprint = "older-approval" }]
        };

        var submission = harness.View.Collect(State(SettingsSection.Skills, overview));

        var change = Assert.Single(submission!.Features!.Skills);
        Assert.Same(skill, change.Skill);
        Assert.False(change.ExpectedEnabled);
        Assert.True(change.Enabled);
        Assert.Equal("older-approval", change.ExpectedApprovalFingerprint);
        var rendered = Plain(harness.Output);
        Assert.Contains(skill.Instructions, rendered, StringComparison.Ordinal);
        Assert.Contains(skill.Scripts["read"], rendered, StringComparison.Ordinal);
        Assert.Contains(skill.Directory, rendered, StringComparison.Ordinal);
        Assert.Contains("[red]literal[/]", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("\u001b[2J", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>A repeated opening summary is displayed once without modifying the inspected instructions or scripts.</summary>
    [Theory]
    [InlineData("bundled", "\n", true)]
    [InlineData("local", "\r\n", true)]
    [InlineData("local", "\n", false)]
    public void SkillOverview_RepeatedIntroduction_ShowsDescriptionOnce(string origin, string newline, bool title)
    {
        const string description = "Inspect Git status, commits, branches and diffs.";
        var instructions = (title ? "# git" + newline + "  " + newline : string.Empty)
            + description + newline + newline + "Review the full command before execution. [bold]Literal text[/]";
        var skill = Skill() with { Name = "git", Description = description, Instructions = instructions, Origin = origin };

        var rendered = RenderSkillOverview(skill);

        Assert.Single(Regex.Matches(rendered, Regex.Escape(description)));
        Assert.Contains(instructions.ReplaceLineEndings("\n"), rendered, StringComparison.Ordinal);
        Assert.Contains(skill.Directory, rendered, StringComparison.Ordinal);
        Assert.Contains(skill.Scripts["read"], rendered, StringComparison.Ordinal);
        Assert.Equal(instructions, skill.Instructions);
        Assert.Equal("fingerprint", skill.Fingerprint);
    }

    /// <summary>Unique metadata and summaries that occur only in later examples or longer paragraphs remain visible.</summary>
    [Theory]
    [InlineData("Different instructions.")]
    [InlineData("Package details with additional context.")]
    [InlineData("package details")]
    [InlineData("Read this first.\n\nPackage details")]
    [InlineData("```text\nPackage details\n```")]
    [InlineData("> Package details")]
    public void SkillOverview_DistinctIntroduction_PreservesDescriptionAndFullBody(string instructions)
    {
        var skill = Skill() with { Instructions = "# Imported skill\n\n" + instructions, UnavailableReason = "Needs review." };

        var rendered = RenderSkillOverview(skill);

        Assert.Contains(skill.Description + "\n\n" + skill.Instructions, rendered, StringComparison.Ordinal);
        Assert.Contains(skill.Scripts["read"], rendered, StringComparison.Ordinal);
        Assert.Contains(skill.UnavailableReason, rendered, StringComparison.Ordinal);
    }

    /// <summary>Renders one passive preview without terminal navigation, filesystem reads, or executing package instructions.</summary>
    private static string RenderSkillOverview(SkillDefinition skill)
    {
        var harness = Create([]);
        var overview = (IRenderable)typeof(FullscreenSetupView)
            .GetMethod("CreateSkillOverview", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(harness.View, [skill])!;
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(harness.Output)
        });
        console.Profile.Width = 360;
        console.Write(overview);
        return Plain(harness.Output).ReplaceLineEndings("\n");
    }

    /// <summary>Only bundled package names are translated; local package identities stay literal.</summary>
    [Theory]
    [InlineData("bundled", "File e cartelle")]
    [InlineData("local", "filesystem")]
    public void Collect_SkillLabel_LocalizesBundledPackagesOnly(string origin, string expected)
    {
        var harness = Create(Choose(3 + SectionCount));
        harness.Text.SetLanguage("it");
        var skill = Skill() with { Name = "filesystem", Origin = origin };
        var state = State(SettingsSection.Skills, new SettingsFeatureOverview(new(Enabled: true), 0, 1)
        {
            Skills = [new(skill, false)]
        });

        Assert.NotNull(harness.View.Collect(state with { Settings = state.Settings with { Language = "it" } }));

        Assert.Contains(expected, Plain(harness.Output), StringComparison.Ordinal);
        if (origin == "local") Assert.DoesNotContain("File e cartelle", Plain(harness.Output), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>An unsupported approved package can be turned off but is never offered for activation afterward.</summary>
    [Fact]
    public void Collect_UnsupportedApprovedSkill_CanOnlyDeactivate()
    {
        var skill = Skill() with { UnavailableReason = "unsupported platform" };
        var harness = Create(Choose(2).Concat(Choose(1)).Concat(Choose(3 + SectionCount)));
        var overview = new SettingsFeatureOverview(new(Enabled: true), 0, 1)
        {
            Skills = [new(skill, true) { ApprovalFingerprint = skill.Fingerprint }]
        };

        var submission = harness.View.Collect(State(SettingsSection.Skills, overview));

        Assert.False(Assert.Single(submission!.Features!.Skills).Enabled);
        Assert.Contains("unsupported platform", Plain(harness.Output), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Disabling the shared gate cannot save before the explicit destructive acknowledgement.</summary>
    [Fact]
    public void Collect_DisableMaster_RequiresClearConsent()
    {
        var keys = Choose(0).Concat(Choose(1)).Concat(Choose(2 + SectionCount))
            .Concat(Choose(1)).Concat(Choose(1)).Concat(Choose(2 + SectionCount));
        var harness = Create(keys);

        var submission = harness.View.Collect(State(SettingsSection.Skills, new(new(Enabled: true, CaptureObservations: true), 0, 0)));

        Assert.NotNull(submission);
        Assert.NotNull(submission.Features);
        Assert.False(submission.Features.Settings.Enabled);
        Assert.True(submission.Features.ClearLearningConsent);
        Assert.Contains(harness.Text.Text("Settings.DisableFeaturesConsentInfo"), Plain(harness.Output), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>A missing snapshot offers no feature controls or fabricated consent state.</summary>
    [Theory]
    [InlineData(SettingsSection.Skills)]
    [InlineData(SettingsSection.Learning)]
    public void Collect_WithoutSnapshot_ShowsUnavailableWithoutFeatureEdits(SettingsSection section)
    {
        var harness = Create(Choose(SectionCount));

        var submission = harness.View.Collect(State(section, null));

        Assert.Null(submission!.Features);
        Assert.Contains(harness.Text.Text("Costs.Unavailable"), Plain(harness.Output), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>A damaged catalog keeps other preferences usable without offering unknown package approvals.</summary>
    [Fact]
    public void Collect_UnavailableCatalog_ShowsWarning()
    {
        var harness = Create(Choose(2 + SectionCount + 1));

        Assert.Null(harness.View.Collect(State(SettingsSection.Skills,
            new SettingsFeatureOverview(new(Enabled: true), 0, 0) { CatalogUnavailable = true })));

        Assert.Contains(harness.Text.Text("Settings.SkillCatalogUnavailable"), Plain(harness.Output), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Privacy contains localized plain facts rather than a duplicate feature-status table.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public void PrivacyCopy_AllLanguages_IsReadOnlyAndConcise(string language)
    {
        var harness = Create(Choose(SectionCount + 1));
        harness.Text.SetLanguage(language);
        var state = State(SettingsSection.Privacy, new(new(), 0, 0));

        Assert.Null(harness.View.Collect(state with { Settings = state.Settings with { Language = language } }));

        var rendered = Regex.Replace(Plain(harness.Output), @"\s+", " ");
        foreach (var key in new[] { "Local", "Provider", "Learning", "Skills", "Control" })
            Assert.Contains(Regex.Replace(harness.Text.Text("Settings.Privacy" + key + "Info"), @"\s+", " "), rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(harness.Text.Text("Settings.FeatureMaster"), rendered, StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Creates a local settings snapshot with no credential or provider work.</summary>
    private static SetupViewState State(SettingsSection section, SettingsFeatureOverview? overview) =>
        new(AppSettings.Default with { AiEnabled = false }, false, false) { InitialSection = section, FeatureOverview = overview };

    /// <summary>Provides inspected package text without touching files or executing its script.</summary>
    private static SkillDefinition Skill() => new("[red]literal[/]", "Package details", "1", "Complete literal instructions [bold]data[/]",
        "inspected-package", "local", "fingerprint", null, new Dictionary<string, string> { ["read"] = "Write-Output 'literal inspected script'" });

    /// <summary>Creates the passive view with fake input, output, and theme data.</summary>
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
        var view = new FullscreenSetupView(console, text, shell, new PromptInjectionProtectionService(), new SensitiveDataRedactor(), themes, about,
            new ScriptLanguageCatalog());
        return new Harness(view, text, output, keys);
    }

    /// <summary>Chooses a fallback-menu action using deterministic fake key presses.</summary>
    private static IEnumerable<ConsoleKeyInfo> Choose(int index) =>
        Enumerable.Repeat(Key(ConsoleKey.DownArrow), index).Append(Key(ConsoleKey.Enter));

    /// <summary>Creates an event for one navigation or confirmation key.</summary>
    private static ConsoleKeyInfo Key(ConsoleKey key) => new(key == ConsoleKey.Enter ? '\r' : '\0', key, false, false, false);

    /// <summary>Removes rendering escapes while preserving human-readable output.</summary>
    private static string Plain(StringWriter output) => Regex.Replace(output.ToString(), @"\x1B\[[0-?]*[ -/]*[@-~]", string.Empty, RegexOptions.CultureInvariant);

    private sealed record Harness(FullscreenSetupView View, LocalizationService Text, StringWriter Output, Queue<ConsoleKeyInfo> Keys);
}

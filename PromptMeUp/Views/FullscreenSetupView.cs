// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public sealed class FullscreenSetupView : ISetupView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly IPromptInjectionProtectionService _protection;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly IThemeCatalogService _themes;
    private readonly IAboutView _about;
    private readonly IScriptLanguageCatalog _scriptLanguages;

    /// <summary>Creates settings forms that collect drafts without persisting preferences or credentials.</summary>
    public FullscreenSetupView(
        IAnsiConsole console,
        ILocalizationService text,
        IConsoleShellView shell,
        IPromptInjectionProtectionService protection,
        ISensitiveDataRedactor redactor,
        IThemeCatalogService themes,
        IAboutView about,
        IScriptLanguageCatalog scriptLanguages)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _protection = protection ?? throw new ArgumentNullException(nameof(protection));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _themes = themes ?? throw new ArgumentNullException(nameof(themes));
        _about = about ?? throw new ArgumentNullException(nameof(about));
        _scriptLanguages = scriptLanguages ?? throw new ArgumentNullException(nameof(scriptLanguages));
    }

    /// <summary>Collects the complete setup draft and restores the current language and palette on every exit.</summary>
    public SetupSubmission? Collect(SetupViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var originalLanguage = _text.Language;
        var originalTheme = TerminalTheme.Current;
        var draft = new SetupDraft(state.Settings, state.FeatureOverview)
        {
            TestConnection = !state.Settings.SetupCompleted
        };
        try
        {
            TerminalTheme.Apply(_themes.Resolve(draft.Settings.Theme));
            var pages = CreateSetupPages(draft, state);
            var initialTitleKey = state.InitialSection == SettingsSection.About ? "About.MenuLabel" : "Settings." + state.InitialSection;
            var initialPage = pages.ToList().FindIndex(page => page.TitleKey == initialTitleKey);
            if (initialPage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(state), "Unsupported settings section.");
            }
            var (saved, selectedPage) = CollectPages(pages, initialPage, draft);
            if (!saved)
            {
                return null;
            }

            var settings = draft.Settings with
            {
                SetupCompleted = true,
                PreferredName = PreferredNamePolicy.Normalize(draft.Settings.PreferredName, _redactor),
                CustomInstruction = _protection.Protect(draft.Settings.CustomInstruction).SanitizedText,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            return new SetupSubmission(
                settings,
                draft.ApiKey,
                draft.AdminKey,
                settings.AiEnabled && HasApiKey(draft, state) && draft.TestConnection)
            {
                Features = draft.FeatureChanges(),
                KeepOpen = true,
                SelectedSection = SelectedSection(pages[selectedPage])
            };
        }
        finally
        {
            _text.SetLanguage(originalLanguage);
            TerminalTheme.Apply(originalTheme);
        }
    }

    /// <summary>Collects a validated settings draft and records the section that initiated its explicit save.</summary>
    private (bool Saved, int SelectedPage) CollectPages(IReadOnlyList<FormPage> pages, int initialPage, SetupDraft draft)
    {
        if (FullscreenForm.CanUse(_console))
        {
            var form = new FullscreenForm(_console, _text, _shell.Options);
            var saved = form.Run("Settings.Title", pages, () => ValidatePages(pages) ?? ValidateFeatures(draft), initialPage);
            return (saved, form.SelectedPageIndex);
        }
        _shell.RenderNotice(_text.Text("Form.Unavailable"));
        var promptForm = new SettingsPromptForm(_console, _text, _shell.Options);
        var savedDraft = promptForm.Run(pages, initialPage, () => ValidatePages(pages) ?? ValidateFeatures(draft));
        return (savedDraft, promptForm.SelectedPageIndex);
    }

    /// <summary>Maps a visible form page back to the stable settings section used when reopening the editor.</summary>
    private static SettingsSection SelectedSection(FormPage page) => page.TitleKey == "About.MenuLabel"
        ? SettingsSection.About
        : page.TitleKey.StartsWith("Settings.", StringComparison.Ordinal)
            && Enum.TryParse<SettingsSection>(page.TitleKey["Settings.".Length..], ignoreCase: false, out var section)
            ? section
            : SettingsSection.General;

    /// <summary>Groups every preference into stable sidebar sections that share one local draft.</summary>
    private IReadOnlyList<FormPage> CreateSetupPages(SetupDraft draft, SetupViewState state)
    {
        var ai = new List<FormField>
        {
            Toggle("Setup.AiEnabled", () => draft.Settings.AiEnabled,
                value => draft.Settings = draft.Settings with { AiEnabled = value })
        };
        ai.AddRange(CreateModelFields(draft));
        ai.Add(Toggle("Direct.RequireConfirmation", () => !draft.Settings.DirectModeEnabled,
            value => draft.Settings = draft.Settings with { DirectModeEnabled = !value }) with
        { HelpKey = "Direct.RequireConfirmationHelp" });
        ai.Add(Toggle("Setup.CommandReview", () => draft.Settings.ReviewCommandsWithAi,
            value => draft.Settings = draft.Settings with { ReviewCommandsWithAi = value }));
        ai.Add(Toggle("Setup.PromptCaching", () => draft.Settings.PromptCachingEnabled,
            value => draft.Settings = draft.Settings with { PromptCachingEnabled = value }));
        ai.Add(Toggle("Settings.SessionSummary", () => draft.Settings.ShowSessionSummaryDuringWork,
            value => draft.Settings = draft.Settings with { ShowSessionSummaryDuringWork = value }) with
        { HelpKey = "Settings.SessionSummaryHelp" });
        var context = CreateContextFields(draft).ToArray();
        if (state.ContextBudgetOverridden)
        {
            context[0] = context[0] with { HelpKey = "AiSettings.ContextOverride" };
        }
        return AddSaveNotice(
        [
            new("Settings.General",
            [
                new("language", "Setup.Language", () => draft.Settings.Language, value => SetLanguage(draft, value))
                {
                    Choices = () => SupportedLanguages.All.Select(item => new FormChoice(item.Code, LanguageLabel(item, _shell.Options))).ToArray()
                }
            ]) { HelpKey = "Settings.GeneralHelp", Overview = () => CreateGeneralOverview(draft, state) },
            new("Settings.Ai", ai)
            {
                HelpKey = "Settings.AiHelp",
                Overview = () => ModelPricingTable.Create(_text, _shell.Options, state.Costs?.Prices ?? [],
                    draft.Settings.Model, selectableModelsOnly: true)
            },
            new("Settings.Credentials",
            [
                Secret("api-key", "Status.ApiKey", () => draft.ApiKey,
                    value => draft.ApiKey = value, () => HasApiKey(draft, state), () => true),
                Secret("admin-key", "Status.AdminKey", () => draft.AdminKey,
                    value => draft.AdminKey = value, () => state.AdminKeyConfigured || draft.AdminKey is not null,
                    () => true),
                new("endpoint", "Setup.Endpoint", () => draft.Settings.Endpoint,
                    value => draft.Settings = draft.Settings with { Endpoint = value.Trim() })
                {
                    Validate = value => OpenAiEndpointPolicy.IsAllowed(value) ? null : _text.Text("Setup.EndpointError"),
                    MaxLength = 2_048
                },
                Toggle("Form.TestConnection", () => draft.TestConnection, value => draft.TestConnection = value,
                    () => draft.Settings.AiEnabled && HasApiKey(draft, state))
            ]) { HelpKey = "Settings.CredentialsHelp" },
            new("Settings.Context", context) { HelpKey = "Settings.ContextHelp" },
            new("Settings.Commands",
            [
                new("script-language", "Setup.ScriptLanguage", () => _scriptLanguages.Get(draft.Settings.ScriptLanguage).StorageValue,
                    value => draft.Settings = draft.Settings with { ScriptLanguage = ScriptLanguageCatalog.ParseStorageValue(value) })
                {
                    Choices = () => _scriptLanguages.List()
                        .Select(definition => new FormChoice(definition.StorageValue, _text.Text("Script.Language." + definition.Language)))
                        .ToArray(),
                    HelpKey = "Setup.ScriptLanguageHelp"
                },
                Integer("Setup.CommandTimeout", () => draft.Settings.CommandTimeoutSeconds,
                    value => draft.Settings = draft.Settings with { CommandTimeoutSeconds = value }, 5, 300),
                Integer("Setup.MaxCommandOutput", () => draft.Settings.MaxCommandOutputCharacters,
                    value => draft.Settings = draft.Settings with { MaxCommandOutputCharacters = value }, 1_000, 32_768)
            ]) { HelpKey = "Settings.CommandsHelp" },
            new("Settings.Personalization",
            [
                new("preferred-name", "Setup.PreferredName", () => draft.Settings.PreferredName,
                    value => draft.Settings = draft.Settings with { PreferredName = PreferredNamePolicy.Normalize(value, _redactor) })
                {
                    Validate = ValidatePreferredName,
                    HelpKey = "Setup.PreferredNameHelp",
                    MaxLength = PreferredNamePolicy.MaximumLength,
                    DefaultToCurrentValue = false
                },
                new("custom-instruction", "Setup.Custom", () => draft.Settings.CustomInstruction,
                    value => draft.Settings = draft.Settings with { CustomInstruction = _protection.Protect(value).SanitizedText })
                {
                    Validate = ValidatePreamble,
                    HelpKey = "Form.PreambleHelp"
                },
                Toggle("Setup.Location", () => draft.Settings.IncludeWindowsLocation,
                    value => draft.Settings = draft.Settings with { IncludeWindowsLocation = value })
            ]) { HelpKey = "Settings.PersonalizationHelp" },
            new("Settings.Theme",
            [
                new("theme", "Theme.Select", () => draft.Settings.Theme, value => SetTheme(draft, value))
                {
                    Choices = () => _themes.Themes.Select(theme => new FormChoice(theme.Id, ThemeName(theme))).ToArray(),
                    HelpKey = "Theme.Preview"
                }
            ]) { HelpKey = "Settings.ThemeHelp", Overview = () => CreateThemeOverview(draft) },
            new("Settings.Skills", CreateSkillsFields(draft))
            {
                HelpKey = "Settings.FeaturesDraftHelp",
                Overview = () => CreateFeaturePageNotice(draft, skills: true)
            },
            new("Settings.Learning", CreateMemoryFields(draft))
            {
                HelpKey = "Settings.FeaturesDraftHelp",
                Overview = () => CreateFeaturePageNotice(draft, skills: false)
            },
            new("Settings.Memories", [])
            {
                Open = () => OpenSavedMenu(state.OpenMemories ?? throw new InvalidOperationException("Memory navigation must be configured.")),
                HelpKey = "MemoryManager.OpenHint",
                Overview = () => new Rows(
                    new Text(_text.Text("MemoryManager.Help"), Style.Parse(TerminalTheme.Primary)),
                    new Text(" "), new Text(_text.Text("Settings.SavedMemoriesNotice"), Style.Parse(TerminalTheme.Warning)))
            },
            new("Settings.Privacy", [])
            {
                HelpKey = "Settings.PrivacyHelp",
                Overview = CreatePrivacyOverview
            },
            new("About.MenuLabel", [])
            {
                HelpKey = "About.SettingsHint",
                Overview = () => _about.CreateContent()
            }
        ], state.SaveSucceeded);
    }

    /// <summary>Adds a clear success acknowledgement to every section when the saved editor immediately reopens.</summary>
    private IReadOnlyList<FormPage> AddSaveNotice(IReadOnlyList<FormPage> pages, bool saveSucceeded)
    {
        if (!saveSucceeded)
        {
            return pages;
        }
        return pages.Select(page =>
        {
            var overview = page.Overview;
            return page with
            {
                Overview = () => overview is null
                    ? new Text(_text.Text("Setup.Saved"), Style.Parse(TerminalTheme.Success))
                    : new Rows(
                        new Text(_text.Text("Setup.Saved"), Style.Parse(TerminalTheme.Success)),
                        new Text(" "), overview())
            };
        }).ToArray();
    }

    /// <summary>Introduces first-run setup with shared product details, then shows draft status and cached usage after setup.</summary>
    private IRenderable CreateGeneralOverview(SetupDraft draft, SetupViewState state)
    {
        if (!state.Settings.SetupCompleted)
        {
            return _about.CreateContent(renderInstallationCard: true);
        }
        var costs = state.Costs;
        var unavailable = _text.Text("Costs.Unavailable");
        var status = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn();
        AddOverviewMetric(status, "Status.Setup", _text.Text(state.Settings.SetupCompleted ? "Status.Completed" : "Status.Required"));
        AddOverviewMetric(status, "Setup.AiEnabled", _text.Text(draft.Settings.AiEnabled ? "Common.Yes" : "Common.No"));
        AddOverviewMetric(status, "Status.Model", draft.Settings.Model);
        AddOverviewMetric(status, "Status.ApiKey", _text.Text(HasApiKey(draft, state) ? "Status.Ready" : "Status.Missing"));
        AddOverviewMetric(status, "Status.AdminKey", _text.Text(state.AdminKeyConfigured || draft.AdminKey is not null ? "Status.Ready" : "Status.Missing"));
        var usage = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn();
        AddOverviewMetric(usage, "Costs.TodayEstimate", costs is null ? unavailable : $"${costs.EstimatedCostTodayUsd.ToString("0.########", _text.Culture)}");
        AddOverviewMetric(usage, "Costs.MonthEstimate", costs is null ? unavailable : $"${costs.EstimatedCostCurrentMonthUsd.ToString("0.########", _text.Culture)}");
        AddOverviewMetric(usage, "Costs.ApiCost", costs?.ActualOrganizationCostCurrentMonthUsd is { } actual ? $"${actual.ToString("0.########", _text.Culture)}" : unavailable);
        AddOverviewMetric(usage, "Costs.Requests", costs?.RequestsToday.ToString("N0", _text.Culture) ?? unavailable);
        AddOverviewMetric(usage, "Costs.Tokens", costs?.TotalTokensToday.ToString("N0", _text.Culture) ?? unavailable);
        AddOverviewMetric(usage, "Costs.LastSync", costs?.LastPricingSync?.ToLocalTime().ToString("g", _text.Culture) ?? unavailable);
        return new Rows(
            new Text(TerminalTheme.IconPrefix(_shell.Options, "🪞", "=") + _text.Text("Settings.DraftStatus"), Style.Parse("bold " + TerminalTheme.Accent)),
            status, new Text(" "),
            new Text(TerminalTheme.IconPrefix(_shell.Options, "📊", "=") + _text.Text("Main.Costs"), Style.Parse("bold " + TerminalTheme.Accent)),
            usage);
    }

    /// <summary>Builds ordinary skill preference fields from the inspected snapshot without reading files.</summary>
    private IReadOnlyList<FormField> CreateSkillsFields(SetupDraft draft)
    {
        if (draft.FeatureSettings is null) return [];
        var fields = new List<FormField>
        {
            FeatureMaster(draft),
            Toggle("Settings.FeatureAutomatic", () => draft.FeatureSettings!.AutomaticSkills,
                value => draft.FeatureSettings = draft.FeatureSettings! with { AutomaticSkills = value },
                () => draft.FeatureSettings!.Enabled) with { HelpKey = "Settings.SkillsHelp" }
        };
        foreach (var item in draft.Skills)
        {
            fields.Add(Toggle("Settings.FeatureSkills", () => item.Enabled, value => item.Enabled = value,
                () => draft.FeatureSettings!.Enabled) with
            {
                Label = () => SkillLabel(item.State.Skill),
                HelpKey = "Settings.SkillApprovalHelp",
                Overview = () => CreateSkillOverview(item.State.Skill),
                Choices = () => item.State.Skill.UnavailableReason is null || item.Enabled
                    ? [new("true", _text.Text("Common.Yes")), new("false", _text.Text("Common.No"))]
                    : [new("false", _text.Text("Common.No"))]
            });
        }
        fields.Add(ClearLearningConsent(draft));
        return fields;
    }

    /// <summary>Builds local collection preferences and explicit acknowledgements in the same draft.</summary>
    private IReadOnlyList<FormField> CreateMemoryFields(SetupDraft draft)
    {
        if (draft.FeatureSettings is null) return [];
        return
        [
            FeatureMaster(draft),
            Toggle("Settings.FeatureCapture", () => draft.FeatureSettings!.CaptureObservations, draft.SetCapture,
                () => draft.FeatureSettings!.Enabled) with { HelpKey = "Settings.LearningHelp" },
            Toggle("Settings.FeatureReminder", () => draft.FeatureSettings!.MaintenanceReminder,
                value => draft.FeatureSettings = draft.FeatureSettings! with { MaintenanceReminder = value },
                () => draft.FeatureSettings!.Enabled) with { HelpKey = "Settings.LearningHelp" },
            Toggle("Settings.CaptureConsent", () => draft.CaptureConsent, value => draft.CaptureConsent = value,
                () => draft.NeedsCaptureConsent) with
            {
                Overview = CreateCaptureConsentOverview,
                HelpKey = "Settings.FeaturesDraftHelp"
            },
            ClearLearningConsent(draft)
        ];
    }

    /// <summary>Links the two sections to the same project gate without reviving latent collection.</summary>
    private FormField FeatureMaster(SetupDraft draft) => Toggle("Settings.FeatureMaster", () => draft.FeatureSettings!.Enabled,
        draft.SetMaster) with
    { HelpKey = "Settings.FeatureMasterHelp" };

    /// <summary>Requires an explicit acknowledgement before a draft can delete collected evidence.</summary>
    private FormField ClearLearningConsent(SetupDraft draft) => Toggle("Settings.ClearLearningConsent", () => draft.ClearLearningConsent,
        value => draft.ClearLearningConsent = value, () => draft.NeedsClearConsent) with
    {
        Overview = () => Disclosure(draft.FeatureSettings!.Enabled ? "Lab.StopCapture" : "Settings.DisableFeaturesConsentInfo", "Lab.Retention"),
        HelpKey = "Settings.FeaturesDraftHelp"
    };

    /// <summary>Keeps ordinary sections compact while distinguishing missing snapshots and damaged catalogs.</summary>
    private IRenderable CreateFeaturePageNotice(SetupDraft draft, bool skills)
    {
        if (draft.FeatureSettings is null) return new Text(_text.Text("Costs.Unavailable"), Style.Parse(TerminalTheme.Warning));
        if (skills && draft.FeatureOverview!.CatalogUnavailable)
            return new Text(_text.Text("Settings.SkillCatalogUnavailable"), Style.Parse(TerminalTheme.Warning));
        return new Text(_text.Text(draft.FeatureSettings.Enabled ? skills ? "Settings.SkillsHelp" : "Settings.LearningHelp"
            : "Settings.FeatureEnableFirst"), Style.Parse(TerminalTheme.Muted));
    }

    /// <summary>Displays the complete inspected content as literal text before approval, with no filesystem access.</summary>
    private IRenderable CreateSkillOverview(SkillDefinition skill)
    {
        var rows = new List<IRenderable>
        {
            new Text(SafePreview(skill.Name + " " + skill.Version), Style.Parse("bold " + TerminalTheme.Accent)),
            new Text(SafePreview(skill.Directory), Style.Parse(TerminalTheme.Muted)),
            new Text(SafePreview(HasRepeatedSkillIntroduction(skill)
                ? skill.Instructions : skill.Description + "\n\n" + skill.Instructions), Style.Parse(TerminalTheme.Primary))
        };
        foreach (var script in skill.Scripts)
        {
            rows.Add(new Text(SafePreview(script.Key + ".ps1"), Style.Parse(TerminalTheme.Accent)));
            rows.Add(new Text(SafePreview(script.Value), Style.Parse(TerminalTheme.Primary)));
        }
        if (skill.UnavailableReason is not null)
            rows.Add(new Text(SafePreview(skill.UnavailableReason), Style.Parse(TerminalTheme.Warning)));
        return new Rows(rows);
    }

    /// <summary>Omits only a summary repeated verbatim in the opening paragraph, leaving the inspected source untouched.</summary>
    private static bool HasRepeatedSkillIntroduction(SkillDefinition skill)
    {
        var lines = skill.Instructions.ReplaceLineEndings("\n").Split('\n').SkipWhile(string.IsNullOrWhiteSpace);
        if (lines.FirstOrDefault()?.StartsWith("# ", StringComparison.Ordinal) == true)
            lines = lines.Skip(1).SkipWhile(string.IsNullOrWhiteSpace);
        var introduction = string.Join('\n', lines.TakeWhile(line => !string.IsNullOrWhiteSpace(line))).Trim();
        return string.Equals(introduction, skill.Description.ReplaceLineEndings("\n").Trim(), StringComparison.Ordinal);
    }

    /// <summary>Localizes known bundled package labels without renaming imported identities.</summary>
    private string SkillLabel(SkillDefinition skill)
    {
        var key = "Lab.SkillName." + skill.Name;
        var label = skill.Origin == "bundled" ? _text.Text(key) : skill.Name;
        return string.Equals(label, key, StringComparison.Ordinal) ? skill.Name : label;
    }

    /// <summary>Separates collection consent into readable facts about local data, OpenAI, retention, and deletion.</summary>
    private IRenderable CreateCaptureConsentOverview()
    {
        var guide = new List<IRenderable>
        {
            new Text(_text.Text("Lab.StartCapture"), Style.Parse("bold " + TerminalTheme.Primary)),
            new Text(" ")
        };
        foreach (var (titleKey, icon, keys) in new[]
        {
            ("Settings.PrivacyLocal", "💻", new[] { "Settings.CaptureLocalInfo" }),
            ("Settings.PrivacyProvider", "📤", new[] { "Settings.CaptureProviderInfo" }),
            ("Settings.CaptureRetention", "⏳", new[] { "Settings.CaptureLimitInfo", "Settings.CaptureExpiryInfo" }),
            ("Settings.PrivacyControl", "🗑️", new[] { "Settings.CaptureDeleteInfo", "Settings.CaptureSavedInfo", "Settings.CaptureHistoryInfo" })
        })
        {
            guide.Add(new Text(TerminalTheme.IconPrefix(_shell.Options, icon, "-") + _text.Text(titleKey),
                Style.Parse("bold " + TerminalTheme.Accent)));
            var points = new Grid().AddColumn(new GridColumn().NoWrap()).AddColumn();
            foreach (var key in keys)
            {
                points.AddRow(new Text("-", Style.Parse(TerminalTheme.Accent)),
                    new Text(_text.Text(key), Style.Parse(TerminalTheme.Primary)));
            }
            guide.Add(points);
            guide.Add(new Text(" "));
        }
        return new Rows(guide);
    }

    /// <summary>Retains complete privacy disclosures only while their explicit acknowledgement is focused.</summary>
    private IRenderable Disclosure(params string[] keys) => new Rows(keys.Select(key =>
        new Text(_text.Text(key), Style.Parse(TerminalTheme.Warning))));

    /// <summary>Preserves inspected source line breaks while removing terminal controls and markup interpretation.</summary>
    private static string SafePreview(string value) => new(value.Where(character => !char.IsControl(character) || character is '\n' or '\t').ToArray());

    /// <summary>Blocks saving until required data-collection or destructive acknowledgements are explicit.</summary>
    private string? ValidateFeatures(SetupDraft draft) => draft.NeedsCaptureConsent && !draft.CaptureConsent
        || draft.NeedsClearConsent && !draft.ClearLearningConsent ? _text.Text("Settings.FeatureConsentRequired") : null;

    /// <summary>Shows privacy facts as a readable guide that the form can scroll without exposing preference controls.</summary>
    private IRenderable CreatePrivacyOverview()
    {
        var guide = new List<IRenderable>
        {
            new Text(_text.Text("Settings.PrivacyHelp"), Style.Parse(TerminalTheme.Muted)),
            new Text(" ")
        };
        foreach (var (key, icon) in new[]
        {
            ("Local", "💻"), ("Provider", "📤"), ("Learning", "🧠"), ("Skills", "🌐"), ("Control", "🗑️")
        })
        {
            guide.Add(new Text(TerminalTheme.IconPrefix(_shell.Options, icon, "-") + _text.Text("Settings.Privacy" + key),
                Style.Parse("bold " + TerminalTheme.Accent)));
            guide.Add(new Text(_text.Text("Settings.Privacy" + key + "Info"), Style.Parse(TerminalTheme.Primary)));
            guide.Add(new Text(" "));
        }
        return new Rows(guide);
    }

    /// <summary>Preserves the parent draft's display preferences around menus that save their own changes immediately.</summary>
    private void OpenSavedMenu(Action open)
    {
        var language = _text.Language;
        var theme = TerminalTheme.Current;
        _shell.RenderNotice(_text.Text("Settings.SavedMemoriesNotice"));
        try
        {
            open();
        }
        finally
        {
            _text.SetLanguage(language);
            TerminalTheme.Apply(theme);
        }
    }

    /// <summary>Separates each muted summary label from its whitesmoke value without editable brackets.</summary>
    private void AddOverviewMetric(Grid grid, string labelKey, string value) => grid.AddRow(
        new Text(_text.Text(labelKey) + ":", Style.Parse(TerminalTheme.Muted)),
        new Text(value, Style.Parse("bold " + TerminalTheme.FieldValue)));

    /// <summary>Shows the selected file's full attribution and live palette samples without accessing the filesystem.</summary>
    private IRenderable CreateThemeOverview(SetupDraft draft)
    {
        var theme = _themes.Resolve(draft.Settings.Theme);
        var metadata = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn(new GridColumn().LeftAligned());
        AddThemeMetadata(metadata, "Theme.Metadata.Path", theme.SourcePath);
        AddThemeMetadata(metadata, "Theme.Metadata.Author", theme.Author);
        AddThemeMetadata(metadata, "Theme.Metadata.Website", theme.Website, link: theme.Website);
        AddThemeMetadata(metadata, "Theme.Metadata.Description", theme.Description);
        var samples = new Grid().AddColumn(new GridColumn().NoWrap()).AddColumn();
        samples.AddRow(
            new Text(_text.Text("Theme.Semantic.Success"), Style.Parse("bold " + TerminalTheme.Success)),
            new Text(_text.Text("Theme.Semantic.SuccessHelp"), Style.Parse(TerminalTheme.Primary)));
        samples.AddRow(
            new Text(_text.Text("Theme.Semantic.Warning"), Style.Parse("bold " + TerminalTheme.Warning)),
            new Text(_text.Text("Theme.Semantic.WarningHelp"), Style.Parse(TerminalTheme.Primary)));
        samples.AddRow(
            new Text(_text.Text("Theme.Semantic.Error"), Style.Parse("bold " + TerminalTheme.Error)),
            new Text(_text.Text("Theme.Semantic.ErrorHelp"), Style.Parse(TerminalTheme.Primary)));
        return new Rows(metadata, new Text(_text.Text("Settings.ThemePreview"), Style.Parse(TerminalTheme.Muted)),
            new Text(" "), samples);
    }

    /// <summary>Wraps complete metadata values and gives validated website URLs a terminal hyperlink.</summary>
    private void AddThemeMetadata(Grid grid, string labelKey, string? value, string? link = null)
    {
        var display = value is null ? _text.Text("Theme.Metadata.Unavailable")
            : new string(value.Select(character => char.IsControl(character) ? ' ' : character).ToArray());
        IRenderable renderedValue = link is null ? new Text(display, Style.Parse(TerminalTheme.FieldValue))
            : new ThemeLink(display, link);
        grid.AddRow(new Text(_text.Text(labelKey) + ":", Style.Parse(TerminalTheme.Muted)), renderedValue);
        grid.AddEmptyRow();
    }

    /// <summary>Attaches a validated hyperlink while retaining literal, wrapped website text.</summary>
    private sealed class ThemeLink(string value, string url) : IRenderable
    {
        private readonly IRenderable _value = new Text(value, Style.Parse("underline " + TerminalTheme.Info));
        private readonly Link _link = new(url);

        /// <summary>Measures website text using the same wrapping rules as other metadata.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => _value.Measure(options, maxWidth);

        /// <summary>Preserves line boundaries while applying the link to each visible text segment.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            foreach (var segment in _value.Render(options, maxWidth))
            {
                yield return segment.IsLineBreak || segment.IsControlCode ? segment
                    : new Segment(segment.Text, segment.Style, _link);
            }
        }
    }

    /// <summary>Builds the shared model, supported reasoning, and answer detail selectors.</summary>
    private IReadOnlyList<FormField> CreateModelFields(SetupDraft draft) =>
    [
        new("model", "Setup.Model", () => draft.Settings.Model, value => SetModel(draft, value))
        {
            Choices = () => AiModelCatalog.Models.Select(model => new FormChoice(model.Id, model.DisplayName)).ToArray()
        },
        new("reasoning", "Setup.Reasoning", () => draft.Settings.ReasoningEffort,
            value => draft.Settings = draft.Settings with { ReasoningEffort = value })
        {
            Choices = () => AiModelCatalog.Resolve(draft.Settings.Model).ReasoningEfforts
                .Select(value => new FormChoice(value, _text.Text("Reasoning." + value))).ToArray()
        },
        new("detail", "Setup.Detail", () => draft.Settings.OutputDetail,
            value => draft.Settings = draft.Settings with { OutputDetail = value })
        {
            Choices = () => new[] { "compact", "balanced", "detailed" }
                .Select(value => new FormChoice(value, DetailName(value))).ToArray()
        }
    ];

    /// <summary>Builds the four context limits shared by complete setup and focused AI settings.</summary>
    private IReadOnlyList<FormField> CreateContextFields(SetupDraft draft) =>
    [
        Integer("AiSettings.ContextBudget", () => draft.Settings.ContextTokenBudget,
            value => draft.Settings = draft.Settings with { ContextTokenBudget = value }, 4_000, 200_000),
        Integer("Setup.MaxTurns", () => draft.Settings.MaxConversationTurns,
            value => draft.Settings = draft.Settings with { MaxConversationTurns = value }, 2, 50),
        Integer("Setup.MaxMessage", () => draft.Settings.MaxMessageCharacters,
            value => draft.Settings = draft.Settings with { MaxMessageCharacters = value }, 500, 100_000),
        Integer("Setup.MaxContext", () => draft.Settings.MaxContextPercent,
            value => draft.Settings = draft.Settings with { MaxContextPercent = value }, 10, 95)
    ];

    /// <summary>Creates a localized yes/no selector whose stored value remains independent of UI language.</summary>
    private FormField Toggle(string labelKey, Func<bool> read, Action<bool> write, Func<bool>? visible = null) =>
        new(labelKey, labelKey, () => read() ? "true" : "false", value => write(bool.Parse(value)))
        {
            Choices = () => [new("true", _text.Text("Common.Yes")), new("false", _text.Text("Common.No"))],
            ValueColor = () => read() ? TerminalTheme.Success : TerminalTheme.Muted,
            IsVisible = visible
        };

    /// <summary>Creates an integer editor with the same inclusive range used by the waterfall setup.</summary>
    private FormField Integer(string labelKey, Func<int> read, Action<int> write, int minimum, int maximum) =>
        new(labelKey, labelKey, () => read().ToString(CultureInfo.InvariantCulture),
            value => write(int.Parse(value, NumberStyles.Integer, _text.Culture)))
        {
            Validate = value => int.TryParse(value, NumberStyles.Integer, _text.Culture, out var number)
                && number >= minimum && number <= maximum ? null : _text.Text("Setup.RangeError", minimum, maximum),
            MaxLength = 12,
            Help = () => _text.Text("Setup.RangeError", minimum, maximum)
        };

    /// <summary>Captures an optional replacement while rendering only configured or missing status.</summary>
    private FormField Secret(
        string key,
        string labelKey,
        Func<string?> read,
        Action<string?> write,
        Func<bool> configured,
        Func<bool> visible) =>
        new(key, labelKey, () => read() ?? string.Empty, value => write(value.Length == 0 ? null : value))
        {
            Secret = true,
            Display = () => _text.Text(read() is not null ? "Form.SecretEntered" : configured() ? "Status.Ready" : "Status.Missing"),
            Validate = value => value.Length == 0 || OpenAiKeyPolicy.IsPlausible(value) ? null : _text.Text("Setup.KeyError"),
            IsVisible = visible,
            HelpKey = "Form.CredentialsHelp"
        };

    /// <summary>Validates an optional display name without exposing rejected input in errors.</summary>
    private string? ValidatePreferredName(string value)
    {
        try
        {
            _ = PreferredNamePolicy.Normalize(value, _redactor);
            return null;
        }
        catch (ArgumentException)
        {
            return _text.Text("Setup.PreferredNameInvalid");
        }
    }

    /// <summary>Rejects recognizable secrets, oversized preambles, and instruction override attempts before saving.</summary>
    private string? ValidatePreamble(string value)
    {
        var result = _protection.Protect(value);
        if (!string.Equals(result.SanitizedText, _redactor.Redact(result.SanitizedText), StringComparison.Ordinal))
        {
            return _text.Text("Setup.PreambleSecret");
        }
        if (!result.IsWithinWordLimit)
        {
            return _text.Text("Setup.PreambleTooLong", result.WordCount, PromptInjectionProtectionService.MaximumPreambleWords);
        }
        return result.IsSafe ? null : _text.Text("Setup.PreambleUnsafe");
    }

    /// <summary>Revalidates every field and dynamic choice before accepting the complete draft.</summary>
    private string? ValidatePages(IReadOnlyList<FormPage> pages)
    {
        foreach (var field in pages.SelectMany(page => page.Fields))
        {
            var value = field.Read();
            var error = field.Validate?.Invoke(value);
            if (error is null && field.Choices is not null
                && !field.Choices().Any(choice => string.Equals(choice.Value, value, StringComparison.Ordinal)))
            {
                error = _text.Text("Cli.Invalid");
            }
            if (error is not null)
            {
                return FullscreenForm.FieldLabel(field, _text) + ": " + error;
            }
        }
        return null;
    }

    /// <summary>Applies a language choice immediately so every page and summary follows the draft language.</summary>
    private void SetLanguage(SetupDraft draft, string value)
    {
        _text.SetLanguage(value);
        draft.Settings = draft.Settings with { Language = _text.Language };
    }

    /// <summary>Applies a preview palette immediately while retaining its identifier in the unpersisted draft.</summary>
    private void SetTheme(SetupDraft draft, string value)
    {
        var theme = _themes.Resolve(value);
        TerminalTheme.Apply(theme);
        draft.Settings = draft.Settings with { Theme = theme.Id };
    }

    /// <summary>Keeps reasoning valid when moving to a model with a different supported effort set.</summary>
    private static void SetModel(SetupDraft draft, string value)
    {
        var model = AiModelCatalog.Resolve(value);
        var reasoning = model.ReasoningEfforts.Contains(draft.Settings.ReasoningEffort, StringComparer.Ordinal)
            ? draft.Settings.ReasoningEffort
            : model.ReasoningEfforts[0];
        draft.Settings = draft.Settings with { Model = model.Id, ReasoningEffort = reasoning };
    }

    /// <summary>Shows the same flag, native language name, and code in choices and the setup review.</summary>
    internal static string LanguageLabel(SupportedLanguage language, ConsoleRenderOptions options) =>
        TerminalTheme.IconPrefix(options, language.Flag, "@") + language.NativeName + " (" + language.Code + ")";

    /// <summary>Resolves the three supported answer-detail labels without inventing an unknown choice.</summary>
    private string DetailName(string value) => _text.Text(value switch
    {
        "compact" => "Setup.Compact",
        "balanced" => "Setup.Balanced",
        "detailed" => "Setup.Detailed",
        _ => throw new InvalidOperationException(_text.Text("Cli.Invalid"))
    });

    /// <summary>Localizes bundled theme names while preserving user-supplied catalog names.</summary>
    private string ThemeName(TerminalThemeDefinition theme) => theme.Id switch
    {
        "cyan" => _text.Text("Theme.Cyan"),
        "green" => _text.Text("Theme.Green"),
        "amber" => _text.Text("Theme.Amber"),
        "ocean" => _text.Text("Theme.Ocean"),
        "cobalt" => _text.Text("Theme.Cobalt"),
        "violet" => _text.Text("Theme.Violet"),
        "rose" => _text.Text("Theme.Rose"),
        "coral" => _text.Text("Theme.Coral"),
        "forest" => _text.Text("Theme.Forest"),
        "mint" => _text.Text("Theme.Mint"),
        "midnight" => _text.Text("Theme.Midnight"),
        "coffee" => _text.Text("Theme.Coffee"),
        "graphite" => _text.Text("Theme.Graphite"),
        _ => theme.Name
    };

    /// <summary>Checks whether an invocation has a previously configured key or a new local replacement.</summary>
    private static bool HasApiKey(SetupDraft draft, SetupViewState state) => state.ApiKeyConfigured || draft.ApiKey is not null;

    private sealed class SetupDraft
    {
        /// <summary>Starts a local settings draft without reading or copying stored secret values.</summary>
        public SetupDraft(AppSettings settings, SettingsFeatureOverview? overview = null)
        {
            Settings = settings;
            FeatureOverview = overview;
            FeatureSettings = overview?.Settings;
            Skills = overview is { CatalogUnavailable: false } ? overview.Skills.Select(item => new SkillDraft(item)).ToArray() : [];
        }

        public AppSettings Settings { get; set; }
        public SettingsFeatureOverview? FeatureOverview { get; }
        public SkillsAndMemorySettings? FeatureSettings { get; set; }
        public IReadOnlyList<SkillDraft> Skills { get; }
        public bool CaptureConsent { get; set; }
        public bool ClearLearningConsent { get; set; }
        public bool NeedsCaptureConsent => FeatureSettings is { Enabled: true, CaptureObservations: true }
            && FeatureOverview?.Settings is not { Enabled: true, CaptureObservations: true };
        public bool NeedsClearConsent => FeatureOverview is { } original && FeatureSettings is { } current
            && ((original.Settings.Enabled && !current.Enabled) || (original.Settings.CaptureObservations && !current.CaptureObservations));
        public string? ApiKey { get; set; }
        public string? AdminKey { get; set; }
        public bool TestConnection { get; set; }

        /// <summary>Changes the shared master gate and invalidates earlier acknowledgements without persisting anything.</summary>
        public void SetMaster(bool enabled)
        {
            var current = FeatureSettings!;
            FeatureSettings = current with { Enabled = enabled, CaptureObservations = enabled && !current.Enabled ? false : current.CaptureObservations };
            CaptureConsent = false;
            ClearLearningConsent = false;
        }

        /// <summary>Changes collection preference and requires fresh acknowledgement for its final transition.</summary>
        public void SetCapture(bool enabled)
        {
            FeatureSettings = FeatureSettings! with { CaptureObservations = enabled };
            CaptureConsent = false;
            ClearLearningConsent = false;
        }

        /// <summary>Returns an optimistic snapshot only when an explicit save changes feature preferences or package approvals.</summary>
        public SettingsFeatureChanges? FeatureChanges()
        {
            if (FeatureOverview is null || FeatureSettings is null
                || FeatureSettings == FeatureOverview.Settings && Skills.All(item => item.Enabled == item.State.Enabled)) return null;
            return new(FeatureOverview.Settings, FeatureSettings, Skills.Select(item =>
                new SettingsSkillChange(item.State.Skill, item.State.Enabled, item.Enabled)
                {
                    ExpectedApprovalFingerprint = item.State.ApprovalFingerprint
                }).ToArray(), CaptureConsent, ClearLearningConsent);
        }
    }

    /// <summary>Keeps each inspected package and its unsaved approval separate from the supplied snapshot.</summary>
    private sealed class SkillDraft(SettingsSkillState state)
    {
        public SettingsSkillState State { get; } = state;
        public bool Enabled { get; set; } = state.Enabled;
    }
}

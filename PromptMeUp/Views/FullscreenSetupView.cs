// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public sealed class FullscreenSetupView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly IPromptInjectionProtectionService _protection;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly IThemeCatalogService _themes;
    private readonly IAboutView _about;

    /// <summary>Creates settings forms that collect drafts without persisting preferences or credentials.</summary>
    public FullscreenSetupView(
        IAnsiConsole console,
        ILocalizationService text,
        IConsoleShellView shell,
        IPromptInjectionProtectionService protection,
        ISensitiveDataRedactor redactor,
        IThemeCatalogService themes,
        IAboutView about)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _protection = protection ?? throw new ArgumentNullException(nameof(protection));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _themes = themes ?? throw new ArgumentNullException(nameof(themes));
        _about = about ?? throw new ArgumentNullException(nameof(about));
    }

    /// <summary>Collects the complete setup draft and restores the current language and palette on every exit.</summary>
    public SetupSubmission? Collect(SetupViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var originalLanguage = _text.Language;
        var originalTheme = TerminalTheme.Current;
        var draft = new SetupDraft(state.Settings) { TestConnection = !state.Settings.SetupCompleted };
        try
        {
            TerminalTheme.Apply(_themes.Resolve(draft.Settings.Theme));
            var pages = CreateSetupPages(draft, state);
            var initialPage = pages.ToList().FindIndex(page => page.TitleKey == "Settings." + state.InitialSection);
            if (initialPage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(state), "Unsupported settings section.");
            }
            var saved = FullscreenForm.CanUse(_console)
                ? new FullscreenForm(_console, _text, _shell.Options).Run(
                    "Settings.Title", pages, () => ValidatePages(pages), initialPage: initialPage)
                : new SettingsPromptForm(_console, _text, _shell.Options).Run(
                    pages, initialPage, () => ValidatePages(pages));
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
                settings.AiEnabled && HasApiKey(draft, state) && draft.TestConnection);
        }
        finally
        {
            _text.SetLanguage(originalLanguage);
            TerminalTheme.Apply(originalTheme);
        }
    }

    /// <summary>Groups every preference into stable sidebar sections that share one local draft.</summary>
    private IReadOnlyList<FormPage> CreateSetupPages(SetupDraft draft, SetupViewState state)
    {
        var ai = new List<FormField>
        {
            Toggle("Setup.AiEnabled", () => draft.Settings.AiEnabled,
                value => draft.Settings = draft.Settings with { AiEnabled = value })
        };
        ai.AddRange(CreateModelFields(draft));
        ai.Add(Toggle("Setup.CommandReview", () => draft.Settings.ReviewCommandsWithAi,
            value => draft.Settings = draft.Settings with { ReviewCommandsWithAi = value }));
        ai.Add(Toggle("Setup.PromptCaching", () => draft.Settings.PromptCachingEnabled,
            value => draft.Settings = draft.Settings with { PromptCachingEnabled = value }));
        var context = CreateContextFields(draft).ToArray();
        if (state.ContextBudgetOverridden)
        {
            context[0] = context[0] with { HelpKey = "AiSettings.ContextOverride" };
        }
        return
        [
            new("Settings.General",
            [
                new("language", "Setup.Language", () => draft.Settings.Language, value => SetLanguage(draft, value))
                {
                    Choices = () => SupportedLanguages.All.Select(item => new FormChoice(item.Code, LanguageLabel(item))).ToArray()
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
            new("Settings.Memories", [])
            {
                Open = () => (state.OpenMemories ?? throw new InvalidOperationException("Memory navigation must be configured."))(),
                HelpKey = "MemoryManager.OpenHint",
                Preview = () => new Text(_text.Text("MemoryManager.Help"), Style.Parse(TerminalTheme.Primary)),
                PreviewRows = 3
            },
            new("About.MenuLabel", [])
            {
                Open = _about.Render,
                HelpKey = "About.OpenHint",
                Preview = () => new Text(_text.Text("Help.About"), Style.Parse(TerminalTheme.Primary)),
                PreviewRows = 3
            }
        ];
    }

    /// <summary>Combines current draft status and cached usage in one passive, unboxed overview.</summary>
    private IRenderable CreateGeneralOverview(SetupDraft draft, SetupViewState state)
    {
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
            new Text(TerminalTheme.IconPrefix(_shell.Options, "🪞", "=") + _text.Text("Main.Status"), Style.Parse("bold " + TerminalTheme.Accent)),
            status, new Text(" "),
            new Text(TerminalTheme.IconPrefix(_shell.Options, "📊", "=") + _text.Text("Main.Costs"), Style.Parse("bold " + TerminalTheme.Accent)),
            usage);
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
                return _text.Text(field.LabelKey) + ": " + error;
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
    private string LanguageLabel(SupportedLanguage language) =>
        TerminalTheme.IconPrefix(_shell.Options, language.Flag, "@") + language.NativeName + " (" + language.Code + ")";

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
        public SetupDraft(AppSettings settings) => Settings = settings;

        public AppSettings Settings { get; set; }
        public string? ApiKey { get; set; }
        public string? AdminKey { get; set; }
        public bool TestConnection { get; set; }
    }
}

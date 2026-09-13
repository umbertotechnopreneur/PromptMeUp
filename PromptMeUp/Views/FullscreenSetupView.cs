// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public sealed class FullscreenSetupView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly IPromptInjectionProtectionService _protection;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly IThemeCatalogService _themes;

    /// <summary>Creates settings forms that collect drafts without persisting preferences or credentials.</summary>
    public FullscreenSetupView(
        IAnsiConsole console,
        ILocalizationService text,
        IConsoleShellView shell,
        IPromptInjectionProtectionService protection,
        ISensitiveDataRedactor redactor,
        IThemeCatalogService themes)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _protection = protection ?? throw new ArgumentNullException(nameof(protection));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _themes = themes ?? throw new ArgumentNullException(nameof(themes));
    }

    /// <summary>Collects the complete setup draft and restores the current language and palette on every exit.</summary>
    public SetupSubmission? Collect(SetupViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var originalLanguage = _text.Language;
        var originalTheme = TerminalTheme.Current;
        var draft = new SetupDraft(state.Settings);
        try
        {
            _text.SetLanguage(draft.Settings.Language);
            TerminalTheme.Apply(_themes.Resolve(draft.Settings.Theme));
            var pages = CreateSetupPages(draft, state);
            var saved = new FullscreenForm(_console, _text).Run(
                "Main.Setup", pages, () => CreateSetupSummary(draft, state), () => ValidatePages(pages));
            if (!saved)
            {
                return null;
            }

            var settings = draft.Settings with
            {
                SetupCompleted = true,
                CustomInstruction = _protection.Protect(draft.Settings.CustomInstruction).SanitizedText,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            return new SetupSubmission(
                settings,
                settings.AiEnabled ? draft.ApiKey : null,
                settings.AiEnabled ? draft.AdminKey : null,
                settings.AiEnabled && HasApiKey(draft, state) && draft.TestConnection);
        }
        finally
        {
            _text.SetLanguage(originalLanguage);
            TerminalTheme.Apply(originalTheme);
        }
    }

    /// <summary>Edits the ten focused AI settings while keeping unrelated preferences and secrets untouched.</summary>
    public AppSettings? CollectAiSettings(AppSettings current)
    {
        ArgumentNullException.ThrowIfNull(current);
        var draft = new SetupDraft(current);
        var fields = new List<FormField>
        {
            Toggle("Setup.AiEnabled", () => draft.Settings.AiEnabled,
                value => draft.Settings = draft.Settings with { AiEnabled = value })
        };
        fields.AddRange(CreateModelFields(draft));
        fields.Add(Toggle("Setup.CommandReview", () => draft.Settings.ReviewCommandsWithAi,
            value => draft.Settings = draft.Settings with { ReviewCommandsWithAi = value }));
        fields.Add(Toggle("Setup.PromptCaching", () => draft.Settings.PromptCachingEnabled,
            value => draft.Settings = draft.Settings with { PromptCachingEnabled = value }));
        fields.AddRange(CreateContextFields(draft));
        FormPage[] pages = [new("AiSettings.Title", fields)];
        return new FullscreenForm(_console, _text).Run(
            "AiSettings.Title", pages, () => CreateAiSummary(draft.Settings), () => ValidatePages(pages))
            ? draft.Settings
            : null;
    }

    /// <summary>Groups all setup fields into six revisitable pages with conditional credential and AI choices.</summary>
    private IReadOnlyList<FormPage> CreateSetupPages(SetupDraft draft, SetupViewState state)
    {
        var advanced = CreateContextFields(draft).ToList();
        advanced.Add(Integer("Setup.MaxCommandOutput", () => draft.Settings.MaxCommandOutputCharacters,
            value => draft.Settings = draft.Settings with { MaxCommandOutputCharacters = value }, 1_000, 32_768));
        advanced.Add(Integer("Setup.CommandTimeout", () => draft.Settings.CommandTimeoutSeconds,
            value => draft.Settings = draft.Settings with { CommandTimeoutSeconds = value }, 5, 300));
        advanced.Add(new FormField("endpoint", "Setup.Endpoint", () => draft.Settings.Endpoint,
            value => draft.Settings = draft.Settings with { Endpoint = value.Trim() })
        {
            Validate = value => OpenAiEndpointPolicy.IsAllowed(value) ? null : _text.Text("Setup.EndpointError"),
            MaxLength = 2_048
        });
        return
        [
            new("Form.General",
            [
                new("language", "Setup.Language", () => draft.Settings.Language, value => SetLanguage(draft, value))
                {
                    Choices = () => SupportedLanguages.All.Select(item => new FormChoice(item.Code,
                        TerminalTheme.IconPrefix(_shell.Options, item.Flag, "@") + item.NativeName + " (" + item.Code + ")")).ToArray()
                },
                Toggle("Setup.AiEnabled", () => draft.Settings.AiEnabled,
                    value => draft.Settings = draft.Settings with { AiEnabled = value })
            ]),
            new("Setup.Keys",
            [
                Secret("api-key", "Form.ApiKeyReplacement", () => draft.ApiKey,
                    value => draft.ApiKey = value, () => HasApiKey(draft, state), () => draft.Settings.AiEnabled),
                Secret("admin-key", "Form.AdminKeyReplacement", () => draft.AdminKey,
                    value => draft.AdminKey = value, () => state.AdminKeyConfigured || draft.AdminKey is not null,
                    () => draft.Settings.AiEnabled),
                Toggle("Form.TestConnection", () => draft.TestConnection, value => draft.TestConnection = value,
                    () => draft.Settings.AiEnabled && HasApiKey(draft, state))
            ]),
            new("Setup.Model", CreateModelFields(draft)),
            new("Setup.Preferences",
            [
                new("custom-instruction", "Setup.Custom", () => draft.Settings.CustomInstruction,
                    value => draft.Settings = draft.Settings with { CustomInstruction = _protection.Protect(value).SanitizedText })
                {
                    Validate = ValidatePreamble,
                    HelpKey = "Form.PreambleHelp"
                },
                Toggle("Setup.Location", () => draft.Settings.IncludeWindowsLocation,
                    value => draft.Settings = draft.Settings with { IncludeWindowsLocation = value }),
                Toggle("Setup.CommandReview", () => draft.Settings.ReviewCommandsWithAi,
                    value => draft.Settings = draft.Settings with { ReviewCommandsWithAi = value }, () => draft.Settings.AiEnabled),
                Toggle("Setup.PromptCaching", () => draft.Settings.PromptCachingEnabled,
                    value => draft.Settings = draft.Settings with { PromptCachingEnabled = value }, () => draft.Settings.AiEnabled)
            ]),
            new("Form.Advanced", advanced),
            new("Theme.Title",
            [
                new("theme", "Theme.Select", () => draft.Settings.Theme, value => SetTheme(draft, value))
                {
                    Choices = () => _themes.Themes.Select(theme => new FormChoice(theme.Id, ThemeName(theme))).ToArray(),
                    HelpKey = "Theme.Preview"
                }
            ])
        ];
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
            Display = () => _text.Text(configured() ? "Status.Ready" : "Status.Missing"),
            Validate = value => value.Length == 0 || OpenAiKeyPolicy.IsPlausible(value) ? null : _text.Text("Setup.KeyError"),
            IsVisible = visible,
            HelpKey = "Form.CredentialsHelp"
        };

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

    /// <summary>Creates a review of all settings using only credential status and custom-instruction presence.</summary>
    private IReadOnlyList<FormSummary> CreateSetupSummary(SetupDraft draft, SetupViewState state)
    {
        var settings = draft.Settings;
        var rows = new List<FormSummary>
        {
            Summary("Setup.Language", SupportedLanguages.All.First(item => item.Code == settings.Language).NativeName),
            Summary("Status.ApiKey", _text.Text(state.ApiKeyConfigured || (settings.AiEnabled && draft.ApiKey is not null)
                ? "Status.Ready" : "Status.Missing")),
            Summary("Status.AdminKey", _text.Text(state.AdminKeyConfigured || (settings.AiEnabled && draft.AdminKey is not null)
                ? "Status.Ready" : "Status.Missing"))
        };
        rows.AddRange(CreateAiSummary(settings));
        rows.Add(Summary("Setup.Custom", YesNo(!string.IsNullOrWhiteSpace(settings.CustomInstruction))));
        rows.Add(Summary("Setup.Location", YesNo(settings.IncludeWindowsLocation)));
        rows.Add(Summary("Setup.MaxCommandOutput", settings.MaxCommandOutputCharacters.ToString("N0", _text.Culture)));
        rows.Add(Summary("Setup.CommandTimeout", settings.CommandTimeoutSeconds.ToString("N0", _text.Culture)));
        rows.Add(Summary("Setup.Endpoint", settings.Endpoint));
        rows.Add(Summary("Theme.Select", ThemeName(_themes.Resolve(settings.Theme))));
        rows.Add(Summary("Form.TestConnection", YesNo(settings.AiEnabled && HasApiKey(draft, state) && draft.TestConnection)));
        return rows;
    }

    /// <summary>Creates a focused review containing exactly the ten AI settings that will be saved.</summary>
    private IReadOnlyList<FormSummary> CreateAiSummary(AppSettings settings) =>
    [
        Summary("Setup.AiEnabled", YesNo(settings.AiEnabled)),
        Summary("Setup.Model", AiModelCatalog.Resolve(settings.Model).DisplayName),
        Summary("Setup.Reasoning", _text.Text("Reasoning." + settings.ReasoningEffort)),
        Summary("Setup.Detail", DetailName(settings.OutputDetail)),
        Summary("Setup.CommandReview", YesNo(settings.ReviewCommandsWithAi)),
        Summary("Setup.PromptCaching", YesNo(settings.PromptCachingEnabled)),
        Summary("AiSettings.ContextBudget", settings.ContextTokenBudget.ToString("N0", _text.Culture)),
        Summary("Setup.MaxTurns", settings.MaxConversationTurns.ToString("N0", _text.Culture)),
        Summary("Setup.MaxMessage", settings.MaxMessageCharacters.ToString("N0", _text.Culture)),
        Summary("Setup.MaxContext", settings.MaxContextPercent.ToString(_text.Culture) + "%")
    ];

    /// <summary>Resolves a localized summary label while keeping its value plain text.</summary>
    private FormSummary Summary(string key, string value) => new(_text.Text(key), value);

    /// <summary>Formats a boolean preference in the active draft language.</summary>
    private string YesNo(bool value) => _text.Text(value ? "Common.Yes" : "Common.No");

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
        public bool TestConnection { get; set; } = true;
    }
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface ISetupView
{
    SetupSubmission? Collect(SetupViewState state);
}

public sealed class SetupView : ISetupView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;

    /// <summary>Creates the interactive setup form.</summary>
    public SetupView(IAnsiConsole console, ILocalizationService text, IConsoleShellView shell)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    /// <summary>Collects a complete configuration while keeping entered secrets outside the settings model.</summary>
    public SetupSubmission? Collect(SetupViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var originalLanguage = _text.Language;
        try
        {
            return CollectCore(state);
        }
        finally
        {
            _text.SetLanguage(originalLanguage);
        }
    }

    /// <summary>Runs the localized wizard while the public boundary preserves the live session language.</summary>
    private SetupSubmission? CollectCore(SetupViewState state)
    {
        var current = state.Settings;
        var stage = 0;
        BeginStage(++stage, _text.Text("Setup.Header"), _text.Text("Setup.Subtitle"));

        var languageChoices = SupportedLanguages.All
            .OrderBy(item => item.Code == current.Language ? 0 : 1)
            .ToArray();
        var language = _console.Prompt(
            new SelectionPrompt<SupportedLanguage>()
                .Title(Markup.Escape(_text.Text("Setup.Language")))
                .UseConverter(item => $"{Markup.Escape(item.NativeName)}  [{TerminalTheme.Muted}]({Markup.Escape(item.Code)})[/]")
                .AddChoices(languageChoices));
        _text.SetLanguage(language.Code);

        var aiEnabled = _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text("Setup.AiEnabled")))
        {
            DefaultValue = current.AiEnabled
        });
        string? apiKey = null;
        string? adminKey = null;
        if (aiEnabled)
        {
            BeginStage(++stage, _text.Text("Setup.Keys"));
            apiKey = PromptForSecret(
                current.ApiKeyVariable,
                state.ApiKeyConfigured,
                "Setup.KeyStatus",
                "Setup.SetKey",
                "Setup.KeyPrompt");
            adminKey = PromptForSecret(
                current.AdminKeyVariable,
                state.AdminKeyConfigured,
                "Setup.AdminKeyStatus",
                "Setup.SetAdminKey",
                "Setup.AdminKeyPrompt");
        }

        BeginStage(++stage, _text.Text("Setup.Model"));
        var model = PromptForModel(current.Model);
        var descriptor = AiModelCatalog.Resolve(model);
        var reasoningChoices = descriptor.ReasoningEfforts
            .OrderBy(value => value == current.ReasoningEffort ? 0 : 1)
            .ToArray();
        var reasoning = _console.Prompt(
            new SelectionPrompt<string>()
                .Title(Markup.Escape(_text.Text("Setup.Reasoning")))
                .UseConverter(value => _text.Text($"Reasoning.{value}"))
                .AddChoices(reasoningChoices));
        var detailChoices = new[] { "compact", "balanced", "detailed" }
            .OrderBy(value => value == current.OutputDetail ? 0 : 1)
            .ToArray();
        var detail = _console.Prompt(
            new SelectionPrompt<string>()
                .Title(Markup.Escape(_text.Text("Setup.Detail")))
                .UseConverter(value => _text.Text(value switch
                {
                    "compact" => "Setup.Compact",
                    "detailed" => "Setup.Detailed",
                    _ => "Setup.Balanced"
                }))
                .AddChoices(detailChoices));

        BeginStage(++stage, _text.Text("Setup.Preferences"));
        var customInstructionPrompt = new TextPrompt<string>(Markup.Escape(_text.Text("Setup.Custom"))).AllowEmpty();
        if (!string.IsNullOrWhiteSpace(current.CustomInstruction))
        {
            customInstructionPrompt.DefaultValue(current.CustomInstruction);
        }

        var customInstruction = _console.Prompt(customInstructionPrompt).Trim();
        var includeLocation = _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text("Setup.Location")))
        {
            DefaultValue = current.IncludeWindowsLocation
        });
        var reviewCommands = current.ReviewCommandsWithAi;
        var promptCaching = current.PromptCachingEnabled;
        if (aiEnabled)
        {
            reviewCommands = _console.Prompt(new ConfirmationPrompt(
                Markup.Escape(_text.Text("Setup.CommandReview")))
            {
                DefaultValue = current.ReviewCommandsWithAi
            });
            promptCaching = _console.Prompt(new ConfirmationPrompt(
                Markup.Escape(_text.Text("Setup.PromptCaching")))
            {
                DefaultValue = current.PromptCachingEnabled
            });
        }

        var maxTurns = current.MaxConversationTurns;
        var maxMessageCharacters = current.MaxMessageCharacters;
        var maxContextPercent = current.MaxContextPercent;
        var maxCommandOutputCharacters = current.MaxCommandOutputCharacters;
        var commandTimeoutSeconds = current.CommandTimeoutSeconds;
        var endpoint = current.Endpoint;
        if (_console.Prompt(new ConfirmationPrompt(Markup.Escape(_text.Text("Setup.EditAdvanced")))
        {
            DefaultValue = false
        }))
        {
            BeginStage(++stage, _text.Text("Setup.MemoryLimits"));
            maxTurns = PromptForBoundedInteger("Setup.MaxTurns", maxTurns, 2, 50);
            maxMessageCharacters = PromptForBoundedInteger("Setup.MaxMessage", maxMessageCharacters, 500, 100_000);
            maxContextPercent = PromptForBoundedInteger("Setup.MaxContext", maxContextPercent, 10, 95);
            maxCommandOutputCharacters = PromptForBoundedInteger("Setup.MaxCommandOutput", maxCommandOutputCharacters, 1_000, 32_768);
            commandTimeoutSeconds = PromptForBoundedInteger("Setup.CommandTimeout", commandTimeoutSeconds, 5, 300);
            endpoint = _console.Prompt(
                new TextPrompt<string>(Markup.Escape(_text.Text("Setup.Endpoint")))
                    .DefaultValue(endpoint)
                    .Validate(value => OpenAiEndpointPolicy.IsAllowed(value)
                        ? ValidationResult.Success()
                        : ValidationResult.Error($"[red]{Markup.Escape(_text.Text("Setup.EndpointError"))}[/]")));
        }

        var settings = current with
        {
            SetupCompleted = true,
            Language = language.Code,
            AiEnabled = aiEnabled,
            Model = model,
            ReasoningEffort = reasoning,
            OutputDetail = detail,
            CustomInstruction = customInstruction,
            IncludeWindowsLocation = includeLocation,
            ReviewCommandsWithAi = reviewCommands,
            PromptCachingEnabled = promptCaching,
            MaxConversationTurns = maxTurns,
            MaxMessageCharacters = maxMessageCharacters,
            MaxContextPercent = maxContextPercent,
            MaxCommandOutputCharacters = maxCommandOutputCharacters,
            CommandTimeoutSeconds = commandTimeoutSeconds,
            Endpoint = endpoint.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        BeginStage(++stage, _text.Text("Setup.Summary"));
        RenderSummary(
            settings,
            apiKey is not null || state.ApiKeyConfigured,
            adminKey is not null || state.AdminKeyConfigured);
        if (!_console.Prompt(new ConfirmationPrompt(Markup.Escape(_text.Text("Setup.Confirm")))
        {
            DefaultValue = true
        }))
        {
            return null;
        }

        var canTest = aiEnabled && (apiKey is not null || state.ApiKeyConfigured);
        var testConnection = canTest && _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text("Setup.Test")))
        {
            DefaultValue = true
        });
        return new SetupSubmission(settings, apiKey, adminKey, testConnection);
    }

    /// <summary>Marks one compact wizard stage while retaining every earlier terminal interaction.</summary>
    private void BeginStage(int stage, string title, string? subtitle = null)
    {
        TerminalTheme.WriteRule(
            _console,
            TerminalTheme.IconPrefix(_shell.Options, "⚙", "~") + _text.Text("Main.Setup"),
            TerminalTheme.Accent);
        _console.MarkupLine(
            $"[{TerminalTheme.Muted}]{stage:00}[/]  [bold {TerminalTheme.Info}]{Markup.Escape(title)}[/]");
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(subtitle)}[/]");
        }

        _console.MarkupLine($"[mediumpurple2]{Markup.Escape(_text.Text("Navigation.Shortcuts"))}[/]");
        _console.WriteLine();
    }

    /// <summary>Shows secret status and optionally captures a replacement without echoing its value or length.</summary>
    private string? PromptForSecret(
        string variable,
        bool configured,
        string statusKey,
        string changeKey,
        string promptKey)
    {
        var color = configured ? "green" : "yellow";
        var state = configured ? _text.Text("Status.Ready") : _text.Text("Status.Missing");
        _console.Write(TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(
                TerminalTheme.IconPrefix(_shell.Options, "●", "+") + _text.Text(statusKey),
                state,
                color)
        ], preferredPairs: 1, width: _console.Profile.Width));
        var change = _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text(changeKey)))
        {
            DefaultValue = !configured && variable == AppSettings.DefaultApiKeyVariable
        });
        if (!change)
        {
            return null;
        }

        return _console.Prompt(
            new TextPrompt<string>(Markup.Escape(_text.Text(promptKey)))
                .Secret(mask: null)
                .Validate(value => OpenAiKeyPolicy.IsPlausible(value)
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[red]{Markup.Escape(_text.Text("Setup.KeyError"))}[/]")));
    }

    /// <summary>Displays product-oriented model choices and returns the selected identifier.</summary>
    private AiModelDescriptor PromptForModelDescriptor(string currentModel)
    {
        var choices = AiModelCatalog.Models
            .OrderBy(model => model.Id == currentModel ? 0 : 1)
            .ToArray();
        var prompt = new SelectionPrompt<AiModelDescriptor>()
            .Title(Markup.Escape(_text.Text("Setup.Model")))
            .PageSize(8)
            .UseConverter(model => $"[bold {TerminalTheme.Primary}]{Markup.Escape(model.DisplayName)}[/]  [{TerminalTheme.Muted}]{Markup.Escape(_text.Text($"Model.{model.Id}"))}[/]")
            .HighlightStyle(new Style(Color.MediumPurple2))
            .AddChoices(choices);

        return _console.Prompt(prompt);
    }

    /// <summary>Returns only the stable model identifier selected from the model choices.</summary>
    private string PromptForModel(string currentModel) => PromptForModelDescriptor(currentModel).Id;

    /// <summary>Collects one integer setting while showing its accepted inclusive range.</summary>
    private int PromptForBoundedInteger(string key, int current, int minimum, int maximum) =>
        _console.Prompt(
            new TextPrompt<int>($"{Markup.Escape(_text.Text(key))} [{TerminalTheme.Muted}]({minimum:N0}–{maximum:N0})[/]")
                .DefaultValue(current)
                .Validate(value => value >= minimum && value <= maximum
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[red]{Markup.Escape(_text.Text("Setup.RangeError", minimum, maximum))}[/]")));

    /// <summary>Renders the setup choices as a compact borderless summary before saving.</summary>
    private void RenderSummary(AppSettings settings, bool hasApiKey, bool hasAdminKey)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().RightAligned().NoWrap());
        grid.AddColumn(new GridColumn().LeftAligned());
        var yes = _text.Text("Common.Yes");
        var no = _text.Text("Common.No");
        var ready = _text.Text("Status.Ready");
        var missing = _text.Text("Status.Missing");
        AddSummaryRow(
            grid,
            _text.Text("Setup.Language"),
            SupportedLanguages.All.First(item => item.Code == settings.Language).NativeName);
        AddSummaryRow(grid, _text.Text("Setup.AiEnabled"), settings.AiEnabled ? yes : no);
        AddSummaryRow(grid, _text.Text("Status.ApiKey"), hasApiKey ? ready : missing);
        AddSummaryRow(grid, _text.Text("Status.AdminKey"), hasAdminKey ? ready : missing);
        AddSummaryRow(grid, _text.Text("Setup.Model"), settings.Model);
        AddSummaryRow(grid, _text.Text("Setup.Reasoning"), _text.Text($"Reasoning.{settings.ReasoningEffort}"));
        AddSummaryRow(grid, _text.Text("Setup.Detail"), _text.Text(settings.OutputDetail switch
        {
            "compact" => "Setup.Compact",
            "detailed" => "Setup.Detailed",
            _ => "Setup.Balanced"
        }));
        AddSummaryRow(grid, _text.Text("Setup.Custom"), string.IsNullOrWhiteSpace(settings.CustomInstruction) ? no : yes);
        AddSummaryRow(grid, _text.Text("Setup.Location"), settings.IncludeWindowsLocation ? yes : no);
        AddSummaryRow(grid, _text.Text("Setup.CommandReview"), settings.ReviewCommandsWithAi ? yes : no);
        AddSummaryRow(grid, _text.Text("Setup.PromptCaching"), settings.PromptCachingEnabled ? yes : no);
        AddSummaryRow(grid, _text.Text("Setup.MaxTurns"), settings.MaxConversationTurns.ToString("N0", _text.Culture));
        AddSummaryRow(grid, _text.Text("Setup.MaxContext"), $"{settings.MaxContextPercent}%");
        AddSummaryRow(grid, _text.Text("Setup.MaxMessage"), settings.MaxMessageCharacters.ToString("N0", _text.Culture));
        AddSummaryRow(grid, _text.Text("Setup.MaxCommandOutput"), settings.MaxCommandOutputCharacters.ToString("N0", _text.Culture));
        AddSummaryRow(grid, _text.Text("Setup.CommandTimeout"), settings.CommandTimeoutSeconds.ToString("N0", _text.Culture));
        _console.Write(grid);
        _console.WriteLine();
    }

    /// <summary>Adds one escaped label/value pair to the setup summary grid.</summary>
    private static void AddSummaryRow(Grid grid, string label, string value) =>
        grid.AddRow(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(label)}:[/]"),
            new Text(value, Style.Parse(TerminalTheme.Primary)));
}

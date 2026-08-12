// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface ISetupView
{
    SetupSubmission? Collect(AppSettings current);
}

public sealed class SetupView : ISetupView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IEnvironmentSecretService _secrets;

    /// <summary>Creates the interactive AS/400-inspired setup form.</summary>
    public SetupView(IAnsiConsole console, ILocalizationService text, IEnvironmentSecretService secrets)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    /// <summary>Collects a complete configuration while keeping entered secrets outside the settings model.</summary>
    public SetupSubmission? Collect(AppSettings current)
    {
        ArgumentNullException.ThrowIfNull(current);
        _console.Write(new Panel(
            $"[bold green]{Markup.Escape(_text.Text("Setup.Title"))}[/]\n[grey]{Markup.Escape(_text.Text("Setup.Subtitle"))}[/]")
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader($" {Markup.Escape(_text.Text("Setup.Header"))} "),
            Padding = new Padding(2, 1)
        });

        var languageChoices = SupportedLanguages.All
            .OrderBy(item => item.Code == current.Language ? 0 : 1)
            .ToArray();
        var language = _console.Prompt(
            new SelectionPrompt<SupportedLanguage>()
                .Title(Markup.Escape(_text.Text("Setup.Language")))
                .UseConverter(item => $"{item.NativeName}  [grey]({item.Code})[/]")
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
            apiKey = PromptForSecret(
                current.ApiKeyVariable,
                "Setup.KeyStatus",
                "Setup.SetKey",
                "Setup.KeyPrompt");
            adminKey = PromptForSecret(
                current.AdminKeyVariable,
                "Setup.AdminKeyStatus",
                "Setup.SetAdminKey",
                "Setup.AdminKeyPrompt");
        }

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
        var reviewCommands = aiEnabled && _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text("Setup.CommandReview")))
        {
            DefaultValue = current.ReviewCommandsWithAi
        });
        var promptCaching = aiEnabled && _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text("Setup.PromptCaching")))
        {
            DefaultValue = current.PromptCachingEnabled
        });
        _console.Write(new Rule($"[green]{Markup.Escape(_text.Text("Setup.MemoryLimits"))}[/]")
        {
            Style = Style.Parse("green")
        });
        var maxTurns = PromptForBoundedInteger("Setup.MaxTurns", current.MaxConversationTurns, 2, 50);
        var maxMessageCharacters = PromptForBoundedInteger("Setup.MaxMessage", current.MaxMessageCharacters, 500, 100_000);
        var maxContextPercent = PromptForBoundedInteger("Setup.MaxContext", current.MaxContextPercent, 10, 95);
        var maxCommandOutputCharacters = PromptForBoundedInteger("Setup.MaxCommandOutput", current.MaxCommandOutputCharacters, 1_000, 32_768);
        var commandTimeoutSeconds = PromptForBoundedInteger("Setup.CommandTimeout", current.CommandTimeoutSeconds, 5, 300);
        var endpoint = _console.Prompt(
            new TextPrompt<string>(Markup.Escape(_text.Text("Setup.Endpoint")))
                .DefaultValue(current.Endpoint)
                .Validate(value => OpenAiEndpointPolicy.IsAllowed(value)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Use the official https://api.openai.com/v1/responses endpoint.[/]")));

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
        RenderSummary(settings, apiKey is not null || _secrets.IsConfigured(settings.ApiKeyVariable));
        if (!_console.Prompt(new ConfirmationPrompt(Markup.Escape(_text.Text("Setup.Confirm")))
        {
            DefaultValue = true
        }))
        {
            return null;
        }

        var canTest = aiEnabled && (apiKey is not null || _secrets.IsConfigured(settings.ApiKeyVariable));
        var testConnection = canTest && _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text("Setup.Test")))
        {
            DefaultValue = true
        });
        return new SetupSubmission(settings, apiKey, adminKey, testConnection);
    }

    /// <summary>Shows secret status and optionally captures a masked replacement.</summary>
    private string? PromptForSecret(string variable, string statusKey, string changeKey, string promptKey)
    {
        var configured = _secrets.IsConfigured(variable);
        var color = configured ? "green" : "yellow";
        var state = configured ? _text.Text("Status.Ready") : _text.Text("Status.Missing");
        _console.MarkupLine($"[{color}]●[/] {Markup.Escape(_text.Text(statusKey))}: [bold]{Markup.Escape(state)}[/]");
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
                .Secret()
                .Validate(value => _secrets.LooksLikeOpenAiKey(value)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]The key must start with sk- and contain no whitespace.[/]")));
    }

    /// <summary>Displays product-oriented model cards and returns the selected identifier.</summary>
    private AiModelDescriptor PromptForModelDescriptor(string currentModel)
    {
        var choices = AiModelCatalog.Models
            .OrderBy(model => model.Id == currentModel ? 0 : 1)
            .ToArray();
        var prompt = new SelectionPrompt<AiModelDescriptor>()
            .Title(Markup.Escape(_text.Text("Setup.Model")))
            .PageSize(8)
            .UseConverter(model => $"[bold]{Markup.Escape(model.DisplayName)}[/]  [grey]{Markup.Escape(_text.Text($"Model.{model.Id}"))}[/]")
            .HighlightStyle(new Style(Color.MediumPurple2))
            .AddChoices(choices);

        return _console.Prompt(prompt);
    }

    /// <summary>Returns only the stable model identifier selected from the model card list.</summary>
    private string PromptForModel(string currentModel) => PromptForModelDescriptor(currentModel).Id;

    /// <summary>Collects one integer setting while showing its accepted inclusive range.</summary>
    private int PromptForBoundedInteger(string key, int current, int minimum, int maximum) =>
        _console.Prompt(
            new TextPrompt<int>($"{Markup.Escape(_text.Text(key))} [grey]({minimum:N0}–{maximum:N0})[/]")
                .DefaultValue(current)
                .Validate(value => value >= minimum && value <= maximum
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[red]Enter a value from {minimum:N0} to {maximum:N0}.[/]")));

    /// <summary>Renders the setup choices as a compact green-screen summary before saving.</summary>
    private void RenderSummary(AppSettings settings, bool hasApiKey)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn();
        AddSummaryRow(grid, _text.Text("Setup.Language"), SupportedLanguages.All.First(item => item.Code == settings.Language).NativeName);
        AddSummaryRow(grid, _text.Text("Setup.AiEnabled"), settings.AiEnabled ? _text.Text("Common.Yes") : _text.Text("Common.No"));
        AddSummaryRow(grid, _text.Text("Status.ApiKey"), hasApiKey ? _text.Text("Status.Ready") : _text.Text("Status.Missing"));
        AddSummaryRow(grid, _text.Text("Setup.Model"), settings.Model);
        AddSummaryRow(grid, _text.Text("Setup.Reasoning"), _text.Text($"Reasoning.{settings.ReasoningEffort}"));
        AddSummaryRow(grid, _text.Text("Setup.CommandReview"), settings.ReviewCommandsWithAi ? _text.Text("Common.Yes") : _text.Text("Common.No"));
        AddSummaryRow(grid, _text.Text("Setup.PromptCaching"), settings.PromptCachingEnabled ? _text.Text("Common.Yes") : _text.Text("Common.No"));
        AddSummaryRow(grid, _text.Text("Setup.MaxTurns"), settings.MaxConversationTurns.ToString(_text.Culture));
        AddSummaryRow(grid, _text.Text("Setup.MaxContext"), $"{settings.MaxContextPercent}%");
        AddSummaryRow(grid, _text.Text("Setup.Endpoint"), settings.Endpoint);
        _console.Write(new Panel(grid)
        {
            Header = new PanelHeader($" {_text.Text("Setup.Summary")} "),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Green)
        });
    }

    /// <summary>Adds one escaped label/value pair to the setup summary grid.</summary>
    private static void AddSummaryRow(Grid grid, string label, string value) =>
        grid.AddRow(new Markup($"[green]{Markup.Escape(label)}[/]"), new Text(value));
}

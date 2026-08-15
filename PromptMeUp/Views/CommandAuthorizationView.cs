// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface ICommandAuthorizationView
{
    ApprovedCommand? PreviewAndAuthorize(string command, CommandRiskAssessment assessment);

    void RenderExecutionResult(CommandExecutionResult result);
}

public sealed class CommandAuthorizationView : ICommandAuthorizationView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IPoorMarkdownRenderer _markdown;

    /// <summary>Creates the mandatory preview and authorization gate for shell commands.</summary>
    public CommandAuthorizationView(
        IAnsiConsole console,
        ILocalizationService text,
        IPoorMarkdownRenderer markdown)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _markdown = markdown ?? throw new ArgumentNullException(nameof(markdown));
    }

    /// <summary>Renders exact command text, advisory risk, and data notice before asking for explicit authorization.</summary>
    public ApprovedCommand? PreviewAndAuthorize(string command, CommandRiskAssessment assessment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(assessment);
        var color = RiskColor(assessment.Level);
        TerminalTheme.WriteBlock(_console, _text.Text("Command.Preview"), command, color.ToMarkup());
        _console.MarkupLine(
            $"[{color.ToMarkup()}]●[/] [bold]{Markup.Escape(_text.Text("Command.Risk"))}: {assessment.Score}/100 · {Markup.Escape(assessment.Level.ToString().ToUpperInvariant())}[/]");
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(assessment.UsedAi ? _text.Text("Command.AiReview") : _text.Text("Command.LocalReview"))}[/]");
        _markdown.Render(assessment.DescriptionMarkdown);
        if (!string.IsNullOrWhiteSpace(assessment.Advisory))
        {
            _console.MarkupLine($"[yellow]{Markup.Escape(assessment.Advisory)}[/]");
        }

        _console.MarkupLine($"[yellow]{Markup.Escape(_text.Text("Command.SendOutput"))}[/]");
        var authorized = _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text("Command.Authorize")))
        {
            DefaultValue = false
        });
        if (!authorized)
        {
            _console.MarkupLine($"[yellow]{Markup.Escape(_text.Text("Command.Cancelled"))}[/]");
            return null;
        }

        return ApprovedCommand.Create(command, assessment);
    }

    /// <summary>Shows bounded stdout, stderr, timeout, and exit metadata after an authorized command finishes.</summary>
    public void RenderExecutionResult(CommandExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var body = string.IsNullOrWhiteSpace(result.StandardOutput) ? "(no stdout)" : result.StandardOutput;
        var outputColor = result.ExitCode == 0 && !result.TimedOut ? "green" : "yellow";
        TerminalTheme.WriteBlock(_console, _text.Text("Command.Output"), body, outputColor);
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            TerminalTheme.WriteBlock(_console, "STDERR", result.StandardError, "red");
        }

        _console.MarkupLine(
            $"[{TerminalTheme.Muted}]exit:[/] [{TerminalTheme.Primary}]{(result.ExitCode?.ToString() ?? "timeout")}[/]   " +
            $"[{TerminalTheme.Muted}]elapsed:[/] [{TerminalTheme.Primary}]{result.ElapsedMilliseconds} ms[/]   " +
            $"[{TerminalTheme.Muted}]truncated:[/] [{TerminalTheme.Primary}]{result.OutputTruncated.ToString().ToLowerInvariant()}[/]");
    }

    /// <summary>Maps risk severity to a stable visual color.</summary>
    private static Color RiskColor(CommandRiskLevel level) => level switch
    {
        CommandRiskLevel.Low => Color.Green,
        CommandRiskLevel.Medium => Color.Yellow,
        CommandRiskLevel.High => Color.Orange1,
        CommandRiskLevel.Critical => Color.Red,
        _ => Color.Grey70
    };
}

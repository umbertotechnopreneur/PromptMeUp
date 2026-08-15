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
    private readonly IConsoleShellView _shell;

    /// <summary>Creates the mandatory preview and authorization gate for shell commands.</summary>
    public CommandAuthorizationView(
        IAnsiConsole console,
        ILocalizationService text,
        IPoorMarkdownRenderer markdown,
        IConsoleShellView shell)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _markdown = markdown ?? throw new ArgumentNullException(nameof(markdown));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    /// <summary>Renders exact command text, advisory risk, and data notice before asking for explicit authorization.</summary>
    public ApprovedCommand? PreviewAndAuthorize(string command, CommandRiskAssessment assessment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(assessment);
        var color = RiskColor(assessment.Level);
        TerminalTheme.WriteSection(
            _console,
            $"{TerminalTheme.IconPrefix(_shell.Options, "🚦", ">")}{_text.Text("Command.Preview")}",
            command,
            TerminalTheme.Info);
        _console.MarkupLine(
            $"[bold {color.ToMarkup()}]{Markup.Escape(RiskIcon(assessment.Level))}{Markup.Escape(_text.Text("Command.Risk"))}: {assessment.Score}/100 · {Markup.Escape(_text.Text($"Command.Risk.{assessment.Level}"))}[/]");
        var reviewIcon = TerminalTheme.IconPrefix(_shell.Options, assessment.UsedAi ? "🤖" : "🛡", assessment.UsedAi ? "AI" : "!");
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(reviewIcon)}{Markup.Escape(assessment.UsedAi ? _text.Text("Command.AiReview") : _text.Text("Command.LocalReview"))}[/]");
        _console.WriteLine();
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
        var outputColor = result.ExitCode == 0 && !result.TimedOut ? "green" : "yellow";
        var resultIcon = TerminalTheme.IconPrefix(_shell.Options, outputColor == "green" ? "✅" : "⚠", outputColor == "green" ? "+" : "!");
        TerminalTheme.WriteRule(_console, $"{resultIcon}{_text.Text("Command.Output")}", outputColor);
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            _console.MarkupLine($"[{TerminalTheme.Muted}]STDOUT[/]");
            foreach (var line in result.StandardOutput.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                _console.MarkupLine($"  [{TerminalTheme.Primary}]{Markup.Escape(line)}[/]");
            }

            _console.WriteLine();
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            var errorIcon = TerminalTheme.IconPrefix(_shell.Options, "⚠", "!");
            _console.MarkupLine($"[bold red]{Markup.Escape(errorIcon)}STDERR[/]");
            foreach (var line in result.StandardError.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                _console.MarkupLine($"  [{TerminalTheme.Primary}]{Markup.Escape(line)}[/]");
            }

            _console.WriteLine();
        }

        var metadata = TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(
                TerminalTheme.IconPrefix(_shell.Options, "↳", ">") + _text.Text("Command.ExitCode"),
                result.ExitCode?.ToString() ?? _text.Text("Command.Timeout"),
                outputColor),
            TerminalTheme.CompactMetric(
                TerminalTheme.IconPrefix(_shell.Options, "⏱", "t") + _text.Text("Command.Elapsed"),
                $"{result.ElapsedMilliseconds} ms"),
            TerminalTheme.CompactMetric(
                TerminalTheme.IconPrefix(_shell.Options, "✂", "#") + _text.Text("Command.Truncated"),
                result.OutputTruncated ? _text.Text("Common.Yes") : _text.Text("Common.No"))
        ], preferredPairs: 3, width: _console.Profile.Width);
        _console.Write(metadata);
        _console.WriteLine();
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

    /// <summary>Maps each risk level to a recognisable, accessible command-review indicator.</summary>
    private string RiskIcon(CommandRiskLevel level) => level switch
    {
        CommandRiskLevel.Low => TerminalTheme.IconPrefix(_shell.Options, "🟢", "+"),
        CommandRiskLevel.Medium => TerminalTheme.IconPrefix(_shell.Options, "🟡", "!"),
        CommandRiskLevel.High => TerminalTheme.IconPrefix(_shell.Options, "🟠", "!!"),
        CommandRiskLevel.Critical => TerminalTheme.IconPrefix(_shell.Options, "🔴", "x"),
        _ => TerminalTheme.IconPrefix(_shell.Options, "⚪", "?")
    };
}

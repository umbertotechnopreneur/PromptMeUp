// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface ICommandAuthorizationView
{
    void RenderPreview(string command, CommandRiskAssessment assessment);

    Task<bool> AuthorizeAsync(CommandExecutionMode executionMode, CancellationToken cancellationToken);

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

    /// <summary>Renders the same exact command, risk assessment and output notice for both interaction views.</summary>
    public void RenderPreview(string command, CommandRiskAssessment assessment)
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
            $"[bold {color}]{Markup.Escape(RiskIcon(assessment.Level))}{Markup.Escape(_text.Text("Command.Risk"))}: {assessment.Score}/100 · {Markup.Escape(_text.Text($"Command.Risk.{assessment.Level}"))}[/]");
        _console.WriteLine();
        var reviewIcon = TerminalTheme.IconPrefix(_shell.Options, assessment.UsedAi ? "🤖" : "🛡", assessment.UsedAi ? "AI" : "!");
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(reviewIcon)}{Markup.Escape(assessment.UsedAi ? _text.Text("Command.AiReview") : _text.Text("Command.LocalReview"))}[/]");
        _console.WriteLine();
        _markdown.Render(assessment.DescriptionMarkdown);
        _console.WriteLine();
        if (!string.IsNullOrWhiteSpace(assessment.Advisory))
        {
            _console.MarkupLine($"[{TerminalTheme.Warning}]{Markup.Escape(assessment.Advisory)}[/]");
        }

        _console.MarkupLine($"[{TerminalTheme.Warning}]{Markup.Escape(_text.Text("Command.SendOutput"))}[/]");
        _console.WriteLine();
    }

    /// <summary>Chooses the normal confirmation or direct countdown without performing execution or risk review.</summary>
    public Task<bool> AuthorizeAsync(CommandExecutionMode executionMode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return executionMode switch
        {
            CommandExecutionMode.Confirm => Task.FromResult(Confirm()),
            CommandExecutionMode.Direct => new CommandCountdownView(_console, _text).WaitAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(executionMode))
        };
    }

    /// <summary>Asks for explicit approval with a default-negative prompt in the normal view.</summary>
    private bool Confirm()
    {
        var authorized = _console.Prompt(new ConfirmationPrompt(
            Markup.Escape(_text.Text("Command.Authorize")))
        {
            DefaultValue = false
        });
        if (!authorized)
        {
            _console.MarkupLine($"[{TerminalTheme.Warning}]{Markup.Escape(_text.Text("Command.Cancelled"))}[/]");
        }

        return authorized;
    }

    /// <summary>Shows bounded stdout, stderr, timeout, and exit metadata after an authorized command finishes.</summary>
    public void RenderExecutionResult(CommandExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var succeeded = result.ExitCode == 0 && !result.TimedOut;
        var outputColor = succeeded ? TerminalTheme.Success : TerminalTheme.Warning;
        var resultIcon = TerminalTheme.IconPrefix(_shell.Options, succeeded ? "✅" : "⚠", succeeded ? "+" : "!");
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
            var labelDecoration = _shell.Options.NoAnimation
                ? Decoration.Bold
                : Decoration.Bold | Decoration.SlowBlink;
            _console.Markup($"[bold {TerminalTheme.Error}]{Markup.Escape(errorIcon)}[/]");
            _console.Write(new Text("STDERR", new Style(
                foreground: Style.Parse(TerminalTheme.Error).Foreground, decoration: labelDecoration)));
            _console.WriteLine();
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

    /// <summary>Maps risk severity to the active theme while retaining separate labels and indicators.</summary>
    private static string RiskColor(CommandRiskLevel level) => level switch
    {
        CommandRiskLevel.Low => TerminalTheme.Success,
        CommandRiskLevel.Medium => TerminalTheme.Warning,
        CommandRiskLevel.High => TerminalTheme.Warning,
        CommandRiskLevel.Critical => TerminalTheme.Error,
        _ => TerminalTheme.Muted
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

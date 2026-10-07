// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public interface ICommandAuthorizationView
{
    void RenderPreview(string command, CommandRiskAssessment assessment);

    Task<bool> AuthorizeAsync(CommandExecutionMode executionMode, CancellationToken cancellationToken);

    ICommandOutputSession BeginExecution(TimeSpan silenceNoticeInterval);

    void RenderExecutionResult(CommandExecutionResult result);
}

public sealed class CommandAuthorizationView : ICommandAuthorizationView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IPoorMarkdownRenderer _markdown;
    private readonly IConsoleShellView _shell;
    private readonly ICommandClipboard _clipboard;
    private readonly Func<CommandDecision>? _decisionOverride;
    private string? _previewedCommand;

    /// <summary>Creates the mandatory preview and authorization gate for shell commands.</summary>
    public CommandAuthorizationView(
        IAnsiConsole console,
        ILocalizationService text,
        IPoorMarkdownRenderer markdown,
        IConsoleShellView shell,
        ICommandClipboard clipboard)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _markdown = markdown ?? throw new ArgumentNullException(nameof(markdown));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
    }

    /// <summary>Allows command-decision tests to exercise the authorization gate without a physical terminal.</summary>
    internal CommandAuthorizationView(IAnsiConsole console, ILocalizationService text,
        IPoorMarkdownRenderer markdown, IConsoleShellView shell, ICommandClipboard clipboard,
        Func<CommandDecision> decisionOverride) : this(console, text, markdown, shell, clipboard)
    {
        _decisionOverride = decisionOverride ?? throw new ArgumentNullException(nameof(decisionOverride));
    }

    /// <summary>Renders the same exact command, risk assessment and output notice for both interaction views.</summary>
    /// <param name="command">The exact command requiring authorization.</param>
    /// <param name="assessment">The risk assessment and review description to display.</param>
    /// <exception cref="ArgumentException">Thrown when command is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when command or assessment is null.</exception>
    public void RenderPreview(string command, CommandRiskAssessment assessment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(assessment);
        _previewedCommand = command;
        var color = RiskColor(assessment.Level);
        TerminalTheme.WriteRule(
            _console,
            $"{TerminalTheme.IconPrefix(_shell.Options, "🚦", ">")}{_text.Text("Command.Preview")}",
            TerminalTheme.Info);
        _console.WriteLine();
        foreach (var line in command.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            _console.MarkupLine($"  [bold {TerminalTheme.Primary}]{Markup.Escape(line)}[/]");
        }

        _console.WriteLine();
        _console.MarkupLine(
            $" [{color}]{Markup.Escape(RiskIcon(assessment.Level))}{Markup.Escape(_text.Text("Command.Risk"))}: [bold]{assessment.Score}/100[/] · [bold]{Markup.Escape(_text.Text($"Command.Risk.{assessment.Level}"))}[/][/]");
        _console.WriteLine();
        var reviewIcon = TerminalTheme.IconPrefix(_shell.Options, assessment.UsedAi ? "🤖" : "🛡", assessment.UsedAi ? "AI" : "!");
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(reviewIcon)}{Markup.Escape(assessment.UsedAi ? _text.Text("Command.AiReview") : _text.Text("Command.LocalReview"))}[/]");
        _console.WriteLine();
        _markdown.RenderCommandReview(assessment.DescriptionMarkdown);
        _console.WriteLine();
        if (!string.IsNullOrWhiteSpace(assessment.Advisory))
        {
            ConversationText.Write(_console, new Markup($"[{TerminalTheme.Warning}]{Markup.Escape(assessment.Advisory)}[/]"));
        }

        ConversationText.Write(_console, new Markup($"[{TerminalTheme.Warning}]{Markup.Escape(_text.Text("Command.SendOutput"))}[/]"));
        _console.WriteLine();
    }

    /// <summary>Chooses the normal confirmation or direct countdown without performing execution or risk review.</summary>
    /// <param name="executionMode">The command confirmation or direct countdown mode.</param>
    /// <param name="cancellationToken">The token used to cancel the interaction.</param>
    /// <exception cref="ArgumentOutOfRangeException">The execution mode is unsupported.</exception>
    /// <exception cref="ApplicationExitRequestedException">The user successfully copied the command and requested exit.</exception>
    public async Task<bool> AuthorizeAsync(CommandExecutionMode executionMode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var state = new TerminalStateScope(_console, _text, TerminalActivityState.NeedsInput);
        return await (executionMode switch
        {
            CommandExecutionMode.Confirm => Task.FromResult(Confirm()),
            CommandExecutionMode.Direct => new CommandCountdownView(_console, _text).WaitAsync(cancellationToken,
                () => CommandCopyView.Copy(_console, _text, _clipboard, _previewedCommand, exitAfterCopy: true)),
            _ => throw new ArgumentOutOfRangeException(nameof(executionMode))
        }).ConfigureAwait(false);
    }

    /// <summary>Asks for default-negative approval and leaves a compact receipt for the accepted command.</summary>
    /// <exception cref="ApplicationExitRequestedException">The user successfully copied the command and requested exit.</exception>
    private bool Confirm()
    {
        var choice = _decisionOverride?.Invoke() ?? TerminalConversationPrompt.Select(_console, _text,
        [
            new TerminalMenuChoice<CommandDecision>(CommandDecision.Cancel, _text.Text("Common.No"), Tone: TerminalMenuTone.Muted),
            new TerminalMenuChoice<CommandDecision>(CommandDecision.Execute, _text.Text("Common.Yes"), Tone: TerminalMenuTone.Caution),
            new TerminalMenuChoice<CommandDecision>(CommandDecision.Copy, _text.Text("Command.Copy")),
            new TerminalMenuChoice<CommandDecision>(CommandDecision.CopyAndExit, _text.Text("Command.CopyAndExit"))
        ], _text.Text("Command.Authorize"), echoSelection: false);
        if (choice is CommandDecision.Copy or CommandDecision.CopyAndExit)
        {
            CommandCopyView.Copy(_console, _text, _clipboard, _previewedCommand, exitAfterCopy: choice == CommandDecision.CopyAndExit);
            return false;
        }
        var authorized = choice == CommandDecision.Execute;
        if (authorized)
        {
            var command = TerminalText.ClipMiddle(_previewedCommand ?? string.Empty, 50);
            _console.Write(new Text(_text.Text("Command.Accepted", command), Style.Parse(TerminalTheme.Info)));
            _console.WriteLine();
            _console.WriteLine();
        }
        else
        {
            _console.Write(new TerminalActivityRow(_text.Text("Command.Cancelled"),
                TerminalActivityState.Cancelled, useSymbols: !_shell.Options.NoEmoji));
            _console.WriteLine();
        }

        return authorized;
    }

    internal enum CommandDecision { Cancel, Execute, Copy, CopyAndExit }

    /// <summary>Creates a passive live-output view that can request local command interruption.</summary>
    /// <param name="silenceNoticeInterval">How long silence lasts before displaying an elapsed-time notice.</param>
    public ICommandOutputSession BeginExecution(TimeSpan silenceNoticeInterval) => new TerminalCommandOutput(_console, _text, silenceNoticeInterval);

    /// <summary>Shows bounded stdout, stderr, timeout, and exit metadata after an authorized command finishes.</summary>
    /// <param name="result">The captured output and execution status of the authorized command.</param>
    /// <exception cref="ArgumentNullException">Thrown when result is null.</exception>
    public void RenderExecutionResult(CommandExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var succeeded = result.ExitCode == 0 && !result.TimedOut && !result.Cancelled;
        var exitLabel = result.Cancelled ? _text.Text("Command.Interrupted")
            : result.TimedOut ? _text.Text("Command.Timeout") : result.ExitCode?.ToString() ?? _text.Text("Command.Timeout");
        var summary = _text.Text("Command.Output") + " · " + _text.Text("Command.ExitCode") + ": " + exitLabel;
        var details = new List<IRenderable>
        {
            new TerminalActivityRow(summary,
                result.Cancelled ? TerminalActivityState.Cancelled : succeeded ? TerminalActivityState.Completed : TerminalActivityState.Failed,
                TimeSpan.FromMilliseconds(result.ElapsedMilliseconds), !_shell.Options.NoEmoji)
        };
        if (!result.OutputWasStreamed && !string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            details.Add(new Text("STDOUT", Style.Parse(TerminalTheme.Muted)));
            details.Add(new Text(TerminalText.Safe(result.StandardOutput), Style.Parse(TerminalTheme.Primary)));
        }

        if (!result.OutputWasStreamed && !string.IsNullOrWhiteSpace(result.StandardError))
        {
            // Successful commands can also write progress and informational messages to stderr.
            var errorColor = succeeded ? TerminalTheme.Warning : TerminalTheme.Error;
            details.Add(new Text("STDERR", Style.Parse($"bold {errorColor}")));
            details.Add(new Text(TerminalText.Safe(result.StandardError), Style.Parse(TerminalTheme.Primary)));
        }

        if (string.IsNullOrWhiteSpace(result.StandardOutput) && string.IsNullOrWhiteSpace(result.StandardError))
        {
            details.Add(new Text(_text.Text("Command.NoOutput"), Style.Parse(TerminalTheme.Muted)));
        }
        if (result.OutputTruncated)
        {
            details.Add(new Text(_text.Text("Command.OutputTruncated"), Style.Parse(TerminalTheme.Warning)));
        }
        _console.Write(new Rows(details));
        _console.WriteLine();
        WriteOutputIssueLink();
    }

    /// <summary>Offers the public issue tracker without attaching command text or output.</summary>
    private void WriteOutputIssueLink()
    {
        const string issuesUrl = "https://github.com/umbertotechnopreneur/PromptMeUp/issues";
        _console.WriteLine();
        _console.MarkupLine($"[{TerminalTheme.Warning}]{TerminalTheme.IconPrefix(_shell.Options, "⚠️", "!")}{Markup.Escape(_text.Text("Command.ReportOutputProblem"))}[/]");
        _console.MarkupLine($"  [{TerminalTheme.Info} link={issuesUrl}]{issuesUrl}[/]");
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

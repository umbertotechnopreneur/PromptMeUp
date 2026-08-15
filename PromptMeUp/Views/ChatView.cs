// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IChatView
{
    void RenderIntro();

    string ReadMessage(int maximumCharacters);

    void RenderUser(string text);

    void RenderAssistant(string markdown, bool animate, CancellationToken cancellationToken);

    void RenderMemoryPruned(int messageCount);
}

public sealed class ChatView : IChatView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IPoorMarkdownRenderer _markdown;
    private readonly IConsoleShellView _shell;

    /// <summary>Creates the lightweight multi-turn chat control.</summary>
    public ChatView(
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

    /// <summary>Draws the chat heading and its small slash-command vocabulary without clearing prior output.</summary>
    public void RenderIntro()
    {
        var icon = TerminalTheme.IconPrefix(_shell.Options, "💬", ">");
        TerminalTheme.WriteRule(_console, $"{icon}{_text.Text("Chat.Title")}", TerminalTheme.Accent);
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Chat.Branding"))}[/]");
        _console.WriteLine();
        RenderCommandHint(_text.Text("Chat.Command.RunSyntax"), "Chat.Command.Run");
        RenderCommandHint("/clear", "Chat.Command.Clear");
        RenderCommandHint("/costs", "Chat.Command.Costs");
        RenderCommandHint("/status", "Chat.Command.Status");
        RenderCommandHint("/exit", "Chat.Command.Exit");
        _console.WriteLine();
    }

    /// <summary>Reads one bounded user message and reports its exact character usage beneath the prompt.</summary>
    public string ReadMessage(int maximumCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCharacters);
        var label = $"{TerminalTheme.IconPrefix(_shell.Options, "👤", ">")}{_text.Text("Chat.You")} ›";
        var input = _console.Prompt(
            new TextPrompt<string>($"[bold {TerminalTheme.Accent}]{Markup.Escape(label)}[/] ")
                .AllowEmpty()
                .ValidationErrorMessage($"[red]{Markup.Escape(_text.Text("Chat.InputTooLong", maximumCharacters))}[/]")
                .Validate(value => value.Length <= maximumCharacters
                    ? ValidationResult.Success()
                    : ValidationResult.Error()));
        var remaining = maximumCharacters - input.Length;
        _console.MarkupLine(
            $"  [{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Chat.InputCount", input.Length, maximumCharacters, remaining))}[/]");
        _console.WriteLine();
        return input;
    }

    /// <summary>Renders one automatic user message with the same single-line conversational rhythm as typed input.</summary>
    public void RenderUser(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var icon = TerminalTheme.IconPrefix(_shell.Options, "👤", ">");
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        _console.WriteLine();
        _console.Markup(
            $"[bold {TerminalTheme.Accent}]{Markup.Escape(icon)}{Markup.Escape(_text.Text("Chat.You"))}[/] " +
            $"[{TerminalTheme.Muted}]›[/] ");
        _console.MarkupLine($"[{TerminalTheme.Primary}]{Markup.Escape(lines[0])}[/]");
        foreach (var line in lines.Skip(1))
        {
            _console.MarkupLine($"  [{TerminalTheme.Primary}]{Markup.Escape(line)}[/]");
        }

        _console.WriteLine();
    }

    /// <summary>Renders a model response through the Markdown renderer so formatting never degrades into raw source text.</summary>
    public void RenderAssistant(string markdown, bool animate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        cancellationToken.ThrowIfCancellationRequested();
        var (heading, body) = SeparateLeadingHeading(markdown);
        var icon = TerminalTheme.IconPrefix(_shell.Options, "🤖", "AI");
        _console.WriteLine();
        _console.Markup($"[bold {TerminalTheme.Success}]{Markup.Escape(icon)}{Markup.Escape(_text.Text("Chat.Assistant"))}[/]");
        if (!string.IsNullOrWhiteSpace(heading))
        {
            _console.MarkupLine($" [{TerminalTheme.Muted}]·[/] [bold {TerminalTheme.Primary}]{Markup.Escape(heading)}[/]");
        }
        else
        {
            _console.WriteLine();
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            _console.WriteLine();
            if (animate && !_shell.Options.NoAnimation && !Console.IsOutputRedirected)
            {
                _markdown.RenderAnimated(body, cancellationToken);
            }
            else
            {
                _markdown.Render(body);
            }
        }

        _console.WriteLine();
    }

    /// <summary>Notifies the user when old active-context messages were pruned but remain in the session ledger.</summary>
    public void RenderMemoryPruned(int messageCount) =>
        _console.MarkupLine($"[yellow]{Markup.Escape(_text.Text("Chat.Pruned", messageCount))}[/]");

    /// <summary>Renders one slash command and its localized behavior as a compact two-column guide.</summary>
    private void RenderCommandHint(string command, string descriptionKey) =>
        _console.MarkupLine(
            $"  [bold {TerminalTheme.Info}]{Markup.Escape(command),-18}[/] " +
            $"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text(descriptionKey))}[/]");

    /// <summary>Separates one leading Markdown heading so the assistant identity and response title share a single line.</summary>
    private static (string? Heading, string Body) SeparateLeadingHeading(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lineEnd = normalized.IndexOf('\n');
        var firstLine = lineEnd >= 0 ? normalized[..lineEnd] : normalized;
        var markerCount = firstLine.TakeWhile(character => character == '#').Count();
        if (markerCount is < 1 or > 3 || firstLine.Length <= markerCount || !char.IsWhiteSpace(firstLine[markerCount]))
        {
            return (null, markdown);
        }

        var heading = firstLine[markerCount..].Trim();
        if (string.IsNullOrWhiteSpace(heading))
        {
            return (null, markdown);
        }

        var body = lineEnd < 0 ? string.Empty : normalized[(lineEnd + 1)..].TrimStart('\n');
        return (heading, body);
    }
}

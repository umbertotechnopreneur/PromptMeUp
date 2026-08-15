// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IChatView
{
    void RenderIntro();

    string ReadMessage();

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
        var icon = TerminalTheme.Icon(_shell.Options, "💬", ">");
        _console.WriteLine();
        var banner = new Rows(
            new Markup($"[bold {TerminalTheme.Primary}]{Markup.Escape(_text.Text("Chat.Title"))}[/]"),
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Chat.Branding"))}[/]"),
            new Markup($"[{TerminalTheme.Info}]{Markup.Escape(_text.Text("Chat.Hint"))}[/]"));
        _console.Write(TerminalTheme.Panel(banner, $"{icon} PromptMeUp"));
        _console.WriteLine();
    }

    /// <summary>Reads one user message without interpreting it as Spectre markup.</summary>
    public string ReadMessage() => _console.Prompt(
        new TextPrompt<string>($"[bold mediumpurple2]{Markup.Escape(_text.Text("Chat.You"))} ›[/] ")
            .AllowEmpty());

    /// <summary>Renders one non-interactive user message as a high-contrast compact block.</summary>
    public void RenderUser(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var icon = TerminalTheme.Icon(_shell.Options, "👤", ">");
        TerminalTheme.WriteBlock(_console, $"{icon} {_text.Text("Chat.You")}", text, TerminalTheme.Accent);
    }

    /// <summary>Renders a model response through the Markdown renderer so formatting never degrades into raw source text.</summary>
    public void RenderAssistant(string markdown, bool animate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        _ = animate;
        cancellationToken.ThrowIfCancellationRequested();
        var icon = TerminalTheme.Icon(_shell.Options, "🤖", "AI");
        _console.MarkupLine($"[bold {TerminalTheme.Success}]{Markup.Escape(icon)} {Markup.Escape(_text.Text("Chat.Assistant"))}[/]");
        _markdown.Render(markdown);
        _console.WriteLine();
    }

    /// <summary>Notifies the user when old active-context messages were pruned but remain in the session ledger.</summary>
    public void RenderMemoryPruned(int messageCount) =>
        _console.MarkupLine($"[yellow]{Markup.Escape(_text.Text("Chat.Pruned", messageCount))}[/]");
}

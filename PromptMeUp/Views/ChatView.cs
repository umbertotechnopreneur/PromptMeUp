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

    /// <summary>Draws the chat heading and its small slash-command vocabulary.</summary>
    public void RenderIntro()
    {
        _console.MarkupLine($"[bold deepskyblue1]{Markup.Escape(_text.Text("Chat.Title"))}[/]");
        _console.MarkupLine($"[grey]{Markup.Escape(_text.Text("Chat.Hint"))}[/]");
        _console.WriteLine();
    }

    /// <summary>Reads one user message without interpreting it as Spectre markup.</summary>
    public string ReadMessage() => _console.Prompt(
        new TextPrompt<string>($"[bold mediumpurple2]{Markup.Escape(_text.Text("Chat.You"))} ›[/] ")
            .AllowEmpty());

    /// <summary>Renders one non-interactive user message as a safe chat bubble.</summary>
    public void RenderUser(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _console.MarkupLine($"[bold mediumpurple2]{Markup.Escape(_text.Text("Chat.You"))} ›[/]");
        _console.Write(new Panel(new Text(text))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.MediumPurple2)
        });
    }

    /// <summary>Renders a model response using poor Markdown, with optional bounded teletype animation.</summary>
    public void RenderAssistant(string markdown, bool animate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        _console.MarkupLine($"[bold green]{Markup.Escape(_text.Text("Chat.Assistant"))} ›[/]");
        if (!animate || _shell.Options.NoAnimation || Console.IsOutputRedirected)
        {
            _markdown.Render(markdown);
            return;
        }

        foreach (var line in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var character in line)
            {
                _console.Write(character.ToString());
                Thread.Sleep(4);
            }
            _console.WriteLine();
        }
    }

    /// <summary>Notifies the user when old active-context messages were pruned but remain in the session ledger.</summary>
    public void RenderMemoryPruned(int messageCount) =>
        _console.MarkupLine($"[yellow]{Markup.Escape(_text.Text("Chat.Pruned", messageCount))}[/]");
}

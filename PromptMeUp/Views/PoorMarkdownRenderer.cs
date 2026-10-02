// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public interface IPoorMarkdownRenderer
{
    void Render(string markdown);

    void RenderAnimated(string markdown, CancellationToken cancellationToken);

    void Render(MarkdownDocument document, bool animate, CancellationToken cancellationToken);
}

public sealed partial class PoorMarkdownRenderer : IPoorMarkdownRenderer
{
    private readonly IAnsiConsole _console;

    /// <summary>Creates the deliberately small, sanitized Markdown renderer.</summary>
    public PoorMarkdownRenderer(IAnsiConsole console) =>
        _console = console ?? throw new ArgumentNullException(nameof(console));

    /// <summary>Renders a safe, readable Markdown subset with headings, lists, emphasis, links, and fenced code.</summary>
    /// <param name="markdown">Source text to parse and render.</param>
    /// <exception cref="ArgumentNullException">The source is missing.</exception>
    public void Render(string markdown) => Render(Parse(markdown), animate: false, CancellationToken.None);

    /// <summary>Renders the readable Markdown subset progressively without ever exposing raw formatting markers.</summary>
    /// <param name="markdown">Source text to parse and render.</param>
    /// <param name="cancellationToken">Cancels output between blocks and animation chunks.</param>
    /// <exception cref="ArgumentNullException">The source is missing.</exception>
    /// <exception cref="OperationCanceledException">Rendering was cancelled.</exception>
    public void RenderAnimated(string markdown, CancellationToken cancellationToken) =>
        Render(Parse(markdown), animate: true, cancellationToken);

    /// <summary>Writes previously parsed blocks while retaining their individual animation policy.</summary>
    /// <param name="document">Parsed content shared with the history view.</param>
    /// <param name="animate">Whether ordinary prose should use the teletype presentation.</param>
    /// <param name="cancellationToken">Cancels output between blocks and animation chunks.</param>
    /// <exception cref="ArgumentNullException">The document is missing.</exception>
    /// <exception cref="OperationCanceledException">Rendering was cancelled.</exception>
    public void Render(MarkdownDocument document, bool animate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        var animationChunkSize = Math.Max(1, (int)Math.Ceiling(document.SourceLength / 450d));
        foreach (var block in document.Blocks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConversationText.Write(_console, block.Content, animate && block.Animate, animationChunkSize, cancellationToken);
        }
    }

    /// <summary>Builds the same formatted content for a read-only history or disclosure viewport.</summary>
    /// <param name="markdown">Source text to retain as renderable blocks.</param>
    /// <exception cref="ArgumentNullException">The source is missing.</exception>
    internal static IRenderable Content(string markdown) => Parse(markdown);

    /// <summary>Parses once for both the visible answer and its retained history entry.</summary>
    /// <param name="markdown">Source text to sanitize and parse with the current theme.</param>
    /// <exception cref="ArgumentNullException">The source is missing.</exception>
    public static MarkdownDocument Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return new MarkdownDocument(ParseBlocks(markdown), markdown.Length);
    }

    /// <summary>Parses one sanitized Markdown subset into reusable blocks without writing to the terminal.</summary>
    /// <param name="markdown">Non-null source text supplied by the parser entry point.</param>
    private static IReadOnlyList<MarkdownBlock> ParseBlocks(string markdown)
    {
        var blocks = new List<MarkdownBlock>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [new MarkdownBlock(new Text(" "), false)];
        }

        string? codeLanguage = null;
        var codeLines = new List<string>();
        var previousBlank = true;
        foreach (var rawLine in TerminalText.Safe(markdown).Trim('\n').Split('\n'))
        {
            var fence = FencePattern().Match(rawLine);
            if (codeLanguage is not null)
            {
                if (fence.Success)
                {
                    blocks.Add(new MarkdownBlock(CodeBlock(codeLanguage, codeLines), false));
                    previousBlank = false;
                    codeLanguage = null;
                    codeLines.Clear();
                }
                else
                {
                    codeLines.Add(rawLine);
                }
                continue;
            }

            if (fence.Success)
            {
                codeLanguage = fence.Groups[1].Value.Trim();
                continue;
            }

            var line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                if (!previousBlank)
                {
                    blocks.Add(new MarkdownBlock(new Text(" "), false));
                }
                previousBlank = true;
                continue;
            }
            previousBlank = false;

            var heading = HeadingPattern().Match(line);
            if (heading.Success)
            {
                blocks.Add(new MarkdownBlock(Heading(heading.Groups[1].Value.Length, heading.Groups[2].Value), false));
                continue;
            }

            var list = RawListPattern().Match(line);
            if (list.Success)
            {
                var marker = int.TryParse(list.Groups["ordered"].Value.TrimEnd('.'), out var ordinal)
                    ? $"{ordinal}."
                    : "•";
                var indent = new string(' ', Math.Min(6, list.Groups["indent"].Value.Length));
                blocks.Add(new MarkdownBlock(new Markup(
                    $"{indent}[{TerminalTheme.Accent}]{Markup.Escape(marker)}[/] " +
                    $"[{TerminalTheme.Primary}]{RenderInline(list.Groups["content"].Value)}[/]"), true));
                continue;
            }

            // Markdown table syntax is intentionally not interpreted by this reduced renderer.
            blocks.Add(new MarkdownBlock(new Markup(line.TrimStart().StartsWith('|')
                ? $"[{TerminalTheme.Primary}]{Markup.Escape(line)}[/]"
                : $"[{TerminalTheme.Primary}]{RenderInline(line)}[/]"), true));
        }

        if (codeLanguage is not null)
        {
            blocks.Add(new MarkdownBlock(CodeBlock(codeLanguage, codeLines), false));
        }
        return blocks;
    }

    /// <summary>Renders one heading level with a stable visual hierarchy for a terminal viewport.</summary>
    private static IRenderable Heading(int level, string text)
    {
        var inline = RenderInline(text);
        return level switch
        {
            1 => new Markup($"[bold {TerminalTheme.Accent}]✦ {inline}[/]"),
            2 => new Markup($"[{TerminalTheme.Info}]◆[/] [bold {TerminalTheme.Primary}]{inline}[/]"),
            _ => new Markup($"[{TerminalTheme.Accent}]▸[/] [bold {TerminalTheme.Primary}]{inline}[/]")
        };
    }

    /// <summary>Renders one literal fenced-code block without allowing its content to become Spectre markup.</summary>
    private static IRenderable CodeBlock(string language, IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var label = string.IsNullOrWhiteSpace(language) ? "code" : language;
        var content = string.Join(Environment.NewLine, lines);
        return new Rows(new ThemeSeparator($"⌘ {label}", TerminalTheme.Info), new Text(content, Style.Parse(TerminalTheme.Primary)));
    }

    /// <summary>Converts only bold spans, inline code, and validated HTTP links into Spectre markup.</summary>
    private static string RenderInline(string source)
    {
        var builder = new StringBuilder();
        var cursor = 0;
        foreach (Match match in InlinePattern().Matches(source))
        {
            builder.Append(Markup.Escape(source[cursor..match.Index]));
            if (match.Groups["bold"].Success)
            {
                builder.Append("[bold]")
                    .Append(Markup.Escape(match.Groups["bold"].Value))
                    .Append("[/]");
            }
            else if (match.Groups["code"].Success)
            {
                builder.Append($"[bold {TerminalTheme.Info}]")
                    .Append(Markup.Escape(match.Groups["code"].Value))
                    .Append("[/]");
            }
            else if (Uri.TryCreate(match.Groups["url"].Value, UriKind.Absolute, out var uri)
                     && uri.Scheme is "http" or "https")
            {
                builder.Append("[link=")
                    .Append(Markup.Escape(uri.AbsoluteUri))
                    .Append(']')
                    .Append(Markup.Escape(match.Groups["linkText"].Value))
                    .Append("[/]");
            }
            else
            {
                builder.Append(Markup.Escape(match.Value));
            }

            cursor = match.Index + match.Length;
        }

        builder.Append(Markup.Escape(source[cursor..]));
        return builder.ToString();
    }

    /// <summary>Recognizes one to three leading heading markers.</summary>
    [GeneratedRegex(@"^(#{1,3})\s+(.+)$")]
    private static partial Regex HeadingPattern();

    /// <summary>Recognizes unordered and numbered list markers.</summary>
    [GeneratedRegex(@"^(?<indent>\s*)(?:(?<unordered>[-*])|(?<ordered>\d+\.))\s+(?<content>.+)$")]
    private static partial Regex RawListPattern();

    /// <summary>Recognizes a fenced code-block delimiter with an optional language label.</summary>
    [GeneratedRegex(@"^\s*```([^\s`]*)\s*$")]
    private static partial Regex FencePattern();

    /// <summary>Recognizes bold spans, inline code, and Markdown HTTP links without enabling arbitrary markup.</summary>
    [GeneratedRegex(@"\*\*(?<bold>.+?)\*\*|`(?<code>[^`\r\n]+)`|\[(?<linkText>[^\]\r\n]+)\]\((?<url>https?://[^\s)]+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex InlinePattern();

}

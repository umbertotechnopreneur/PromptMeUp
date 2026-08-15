// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IPoorMarkdownRenderer
{
    void Render(string markdown);

    void RenderAnimated(string markdown, CancellationToken cancellationToken);
}

public sealed partial class PoorMarkdownRenderer : IPoorMarkdownRenderer
{
    private readonly IAnsiConsole _console;

    /// <summary>Creates the deliberately small, sanitized Markdown renderer.</summary>
    public PoorMarkdownRenderer(IAnsiConsole console) =>
        _console = console ?? throw new ArgumentNullException(nameof(console));

    /// <summary>Renders a safe, readable Markdown subset with headings, lists, emphasis, links, and fenced code.</summary>
    public void Render(string markdown) => RenderCore(markdown, animate: false, CancellationToken.None);

    /// <summary>Renders the readable Markdown subset progressively without ever exposing raw formatting markers.</summary>
    public void RenderAnimated(string markdown, CancellationToken cancellationToken) =>
        RenderCore(markdown, animate: true, cancellationToken);

    /// <summary>Renders sanitized Markdown either immediately or with a bounded teletype presentation.</summary>
    private void RenderCore(string markdown, bool animate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            _console.WriteLine();
            return;
        }

        var animationChunkSize = Math.Max(1, (int)Math.Ceiling(markdown.Length / 450d));
        string? codeLanguage = null;
        var codeLines = new List<string>();
        foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fence = FencePattern().Match(rawLine);
            if (codeLanguage is not null)
            {
                if (fence.Success)
                {
                    RenderCodeBlock(codeLanguage, codeLines);
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
                _console.WriteLine();
                continue;
            }

            var heading = HeadingPattern().Match(line);
            if (heading.Success)
            {
                RenderHeading(heading.Groups[1].Value.Length, heading.Groups[2].Value);
                continue;
            }

            var list = RawListPattern().Match(line);
            if (list.Success)
            {
                var marker = int.TryParse(list.Groups["ordered"].Value.TrimEnd('.'), out var ordinal)
                    ? $"{ordinal}."
                    : "•";
                var indent = new string(' ', Math.Min(6, list.Groups["indent"].Value.Length));
                if (animate)
                {
                    RenderAnimatedInline(
                        list.Groups["content"].Value,
                        $"{indent}[{TerminalTheme.Accent}]{Markup.Escape(marker)}[/] ",
                        animationChunkSize,
                        cancellationToken);
                }
                else
                {
                    _console.MarkupLine(
                        $"{indent}[{TerminalTheme.Accent}]{Markup.Escape(marker)}[/] " +
                        $"[{TerminalTheme.Primary}]{RenderInline(list.Groups["content"].Value)}[/]");
                }
                continue;
            }

            // Markdown table syntax is intentionally not interpreted by this reduced renderer.
            if (line.TrimStart().StartsWith('|') || !animate)
            {
                _console.MarkupLine(line.TrimStart().StartsWith('|')
                    ? $"[{TerminalTheme.Primary}]{Markup.Escape(line)}[/]"
                    : $"[{TerminalTheme.Primary}]{RenderInline(line)}[/]");
            }
            else
            {
                RenderAnimatedInline(line, string.Empty, animationChunkSize, cancellationToken);
            }
        }

        if (codeLanguage is not null)
        {
            RenderCodeBlock(codeLanguage, codeLines);
        }
    }

    /// <summary>Types one inline-formatted line in bounded grapheme chunks while retaining safe semantic styling.</summary>
    private void RenderAnimatedInline(
        string source,
        string prefixMarkup,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        _console.Markup(prefixMarkup);
        var cursor = 0;
        foreach (Match match in InlinePattern().Matches(source))
        {
            WriteAnimatedText(source[cursor..match.Index], TerminalTheme.Primary, bold: false, chunkSize, cancellationToken);
            if (match.Groups["bold"].Success)
            {
                WriteAnimatedText(match.Groups["bold"].Value, TerminalTheme.Primary, bold: true, chunkSize, cancellationToken);
            }
            else if (match.Groups["code"].Success)
            {
                _console.Markup($"[black on grey85] {Markup.Escape(match.Groups["code"].Value)} [/]");
            }
            else if (Uri.TryCreate(match.Groups["url"].Value, UriKind.Absolute, out var uri)
                     && uri.Scheme is "http" or "https")
            {
                _console.Markup(
                    $"[link={Markup.Escape(uri.AbsoluteUri)}]{Markup.Escape(match.Groups["linkText"].Value)}[/]");
            }
            else
            {
                WriteAnimatedText(match.Value, TerminalTheme.Primary, bold: false, chunkSize, cancellationToken);
            }

            cursor = match.Index + match.Length;
        }

        WriteAnimatedText(source[cursor..], TerminalTheme.Primary, bold: false, chunkSize, cancellationToken);
        _console.WriteLine();
    }

    /// <summary>Writes escaped Unicode text progressively with a total-animation budget independent of response length.</summary>
    private void WriteAnimatedText(
        string value,
        string color,
        bool bold,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        if (value.Length == 0)
        {
            return;
        }

        var chunk = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(value);
        var count = 0;
        while (elements.MoveNext())
        {
            cancellationToken.ThrowIfCancellationRequested();
            chunk.Append(elements.GetTextElement());
            count++;
            if (count < chunkSize)
            {
                continue;
            }

            WriteAnimatedChunk(chunk, color, bold);
            count = 0;
            Thread.Sleep(4);
        }

        if (chunk.Length > 0)
        {
            WriteAnimatedChunk(chunk, color, bold);
        }
    }

    /// <summary>Flushes one escaped teletype chunk with its current inline emphasis.</summary>
    private void WriteAnimatedChunk(StringBuilder chunk, string color, bool bold)
    {
        var style = bold ? $"bold {color}" : color;
        _console.Markup($"[{style}]{Markup.Escape(chunk.ToString())}[/]");
        chunk.Clear();
    }

    /// <summary>Renders one heading level with a stable visual hierarchy for a terminal viewport.</summary>
    private void RenderHeading(int level, string text)
    {
        var inline = RenderInline(text);
        switch (level)
        {
            case 1:
                TerminalTheme.WriteRule(_console, $"✦ {text}", TerminalTheme.Accent);
                break;
            case 2:
                _console.MarkupLine($"[bold {TerminalTheme.Info}]◆[/] [bold {TerminalTheme.Primary}]{inline}[/]");
                break;
            default:
                _console.MarkupLine($"[{TerminalTheme.Accent}]▸[/] [bold {TerminalTheme.Primary}]{inline}[/]");
                break;
        }
    }

    /// <summary>Renders one literal fenced-code block without allowing its content to become Spectre markup.</summary>
    private void RenderCodeBlock(string language, IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var label = string.IsNullOrWhiteSpace(language) ? "code" : language;
        var content = string.Join(Environment.NewLine, lines);
        TerminalTheme.WriteSection(_console, $"⌘ {label}", content, TerminalTheme.Info);
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
                builder.Append("[black on grey85] ")
                    .Append(Markup.Escape(match.Groups["code"].Value))
                    .Append(" [/]");
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

// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IPoorMarkdownRenderer
{
    void Render(string markdown);
}

public sealed partial class PoorMarkdownRenderer : IPoorMarkdownRenderer
{
    private readonly IAnsiConsole _console;

    /// <summary>Creates the deliberately small, sanitized Markdown renderer.</summary>
    public PoorMarkdownRenderer(IAnsiConsole console) =>
        _console = console ?? throw new ArgumentNullException(nameof(console));

    /// <summary>Renders headings, lists, bold text, links, and plain paragraphs; HTML and tables stay plain.</summary>
    public void Render(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            _console.WriteLine();
            return;
        }

        foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                _console.WriteLine();
                continue;
            }

            var heading = HeadingPattern().Match(line);
            if (heading.Success)
            {
                _console.MarkupLine($"[bold deepskyblue1]{RenderInline(heading.Groups[2].Value)}[/]");
                continue;
            }

            var list = RawListPattern().Match(line);
            if (list.Success)
            {
                var marker = int.TryParse(list.Groups[2].Value.TrimEnd('.'), out var ordinal)
                    ? $"{ordinal}."
                    : "•";
                _console.MarkupLine($"  [mediumpurple2]{Markup.Escape(marker)}[/] {RenderInline(list.Groups[3].Value)}");
                continue;
            }

            // Markdown table syntax is intentionally not interpreted by this reduced renderer.
            _console.MarkupLine(line.TrimStart().StartsWith('|')
                ? Markup.Escape(line)
                : RenderInline(line));
        }
    }

    /// <summary>Converts only bold spans and validated HTTP links into Spectre markup.</summary>
    private static string RenderInline(string source)
    {
        var builder = new StringBuilder();
        var cursor = 0;
        foreach (Match match in InlinePattern().Matches(source))
        {
            builder.Append(Markup.Escape(source[cursor..match.Index]));
            if (match.Groups[1].Success)
            {
                builder.Append("[bold]")
                    .Append(Markup.Escape(match.Groups[1].Value))
                    .Append("[/]");
            }
            else if (Uri.TryCreate(match.Groups[3].Value, UriKind.Absolute, out var uri)
                     && uri.Scheme is "http" or "https")
            {
                builder.Append("[link=")
                    .Append(Markup.Escape(uri.AbsoluteUri))
                    .Append(']')
                    .Append(Markup.Escape(match.Groups[2].Value))
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
    [GeneratedRegex(@"^\s*(?:([-*])|(\d+\.))\s+(.+)$")]
    private static partial Regex RawListPattern();

    /// <summary>Recognizes bold spans and Markdown HTTP links without enabling arbitrary markup.</summary>
    [GeneratedRegex(@"\*\*(.+?)\*\*|\[([^\]\r\n]+)\]\((https?://[^\s)]+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex InlinePattern();

}

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

    /// <summary>Renders a safe, readable Markdown subset with headings, lists, emphasis, links, and fenced code.</summary>
    public void Render(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            _console.WriteLine();
            return;
        }

        string? codeLanguage = null;
        var codeLines = new List<string>();
        foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
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
                _console.MarkupLine(
                    $"{indent}[{TerminalTheme.Accent}]{Markup.Escape(marker)}[/] " +
                    $"[{TerminalTheme.Primary}]{RenderInline(list.Groups["content"].Value)}[/]");
                continue;
            }

            // Markdown table syntax is intentionally not interpreted by this reduced renderer.
            _console.MarkupLine(line.TrimStart().StartsWith('|')
                ? $"[{TerminalTheme.Primary}]{Markup.Escape(line)}[/]"
                : $"[{TerminalTheme.Primary}]{RenderInline(line)}[/]");
        }

        if (codeLanguage is not null)
        {
            RenderCodeBlock(codeLanguage, codeLines);
        }
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

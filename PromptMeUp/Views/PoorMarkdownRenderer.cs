// SPDX-License-Identifier: MIT

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
        var previousBlank = true;
        foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Trim('\n').Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fence = FencePattern().Match(rawLine);
            if (codeLanguage is not null)
            {
                if (fence.Success)
                {
                    RenderCodeBlock(codeLanguage, codeLines);
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
                    _console.WriteLine();
                }
                previousBlank = true;
                continue;
            }
            previousBlank = false;

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
                ConversationText.Write(_console, new Markup(
                    $"{indent}[{TerminalTheme.Accent}]{Markup.Escape(marker)}[/] " +
                    $"[{TerminalTheme.Primary}]{RenderInline(list.Groups["content"].Value)}[/]"),
                    animate, animationChunkSize, cancellationToken);
                continue;
            }

            // Markdown table syntax is intentionally not interpreted by this reduced renderer.
            ConversationText.Write(_console, new Markup(line.TrimStart().StartsWith('|')
                ? $"[{TerminalTheme.Primary}]{Markup.Escape(line)}[/]"
                : $"[{TerminalTheme.Primary}]{RenderInline(line)}[/]"), animate, animationChunkSize, cancellationToken);
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
                ConversationText.Write(_console, new Markup($"[bold {TerminalTheme.Accent}]✦ {inline}[/]"));
                break;
            case 2:
                ConversationText.Write(_console, new Markup($"[{TerminalTheme.Info}]◆[/] [bold {TerminalTheme.Primary}]{inline}[/]"));
                break;
            default:
                ConversationText.Write(_console, new Markup($"[{TerminalTheme.Accent}]▸[/] [bold {TerminalTheme.Primary}]{inline}[/]"));
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

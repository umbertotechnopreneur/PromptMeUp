// SPDX-License-Identifier: MIT

using Spectre.Console;

namespace PromptMeUp.Views;

internal static class TerminalTheme
{
    internal const string Accent = "mediumpurple2";
    internal const string Info = "deepskyblue1";

    /// <summary>Writes one frameless section heading with optional muted context and deliberate whitespace.</summary>
    internal static void WriteHeading(IAnsiConsole console, string title, string? subtitle = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        console.MarkupLine($"[bold {Accent}]{Markup.Escape(title)}[/]");
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            console.MarkupLine($"[grey]{Markup.Escape(subtitle)}[/]");
        }

        console.WriteLine();
    }

    /// <summary>Writes escaped multiline content under a colored label without surrounding it with a card.</summary>
    internal static void WriteBlock(IAnsiConsole console, string label, string content, string color = Info)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(content);
        console.MarkupLine($"[bold {color}]{Markup.Escape(label)}[/]");
        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            console.MarkupLine($"  {Markup.Escape(line)}");
        }

        console.WriteLine();
    }
}

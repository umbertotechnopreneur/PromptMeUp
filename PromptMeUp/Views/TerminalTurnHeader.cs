// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Identifies the source of a waterfall entry independently of its body.</summary>
internal enum TerminalTurnKind { User, Assistant, Plan, Script, Tool }

/// <summary>Gives waterfall entries a consistent, unboxed identity using the active theme.</summary>
internal static class TerminalTurnHeader
{
    /// <summary>Writes a role, optional title and optional divider without enclosing the turn in a panel.</summary>
    internal static void Write(IAnsiConsole console, ILocalizationService text, TerminalTurnKind kind,
        string? title = null, bool showSeparator = true)
    {
        var (icon, fallback, color) = kind switch
        {
            TerminalTurnKind.User => ("👤", ">", TerminalTheme.Accent),
            TerminalTurnKind.Assistant => ("✦", "AI", TerminalTheme.Success),
            TerminalTurnKind.Plan => ("🧭", "#", TerminalTheme.Accent),
            TerminalTurnKind.Script => ("📝", "/", TerminalTheme.Info),
            _ => ("⚡", ">", TerminalTheme.Warning)
        };
        var options = TerminalSession.For(console).Options;
        var heading = TerminalTheme.IconPrefix(options, icon, fallback) + text.Text("Terminal.Role." + kind);
        if (!string.IsNullOrWhiteSpace(title)) heading += " · " + TerminalText.Safe(title).Replace('\n', ' ');
        console.WriteLine();
        if (showSeparator)
        {
            console.Write(new ThemeSeparator(heading, color));
        }
        else
        {
            console.MarkupLine($"[bold {color}]{Markup.Escape(heading)}[/]");
        }
    }
}

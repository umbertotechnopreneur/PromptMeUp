// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Identifies the source of a waterfall entry independently of its body.</summary>
internal enum TerminalTurnKind { User, Assistant, Plan, Script, Tool }

/// <summary>Gives waterfall entries a consistent, unboxed identity using the active theme.</summary>
internal static class TerminalTurnHeader
{
    /// <summary>Writes a role, optional title and optional divider without enclosing the turn in a panel.</summary>
    /// <param name="console">The console receiving the turn heading.</param>
    /// <param name="text">The localized role labels.</param>
    /// <param name="kind">The source of the turn.</param>
    /// <param name="title">An optional title after the role.</param>
    /// <param name="showSeparator">Whether to display a divider beside the heading.</param>
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
            console.Write(new Padder(new ThemeSeparator(heading, color),
                new Padding(TerminalSession.For(console).ChatIndent, 0, 0, 0)));
        }
        else
        {
            console.MarkupLine($"{new string(' ', TerminalSession.For(console).ChatIndent)}[bold {color}]{Markup.Escape(heading)}[/]");
        }
    }
}

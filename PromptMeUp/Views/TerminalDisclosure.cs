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
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Shares bounded summaries and on-demand details without hiding output in unsupported terminals.</summary>
internal static class TerminalDisclosure
{
    /// <summary>Retains the full local result and renders it inline unless an accessible details viewer is available.</summary>
    internal static void Write(IAnsiConsole console, ILocalizationService text, TerminalTurnKind kind,
        string title, IRenderable content, int characters, bool collapse = true, bool deferReview = false)
    {
        var history = TerminalSession.For(console).History;
        var retained = history.Add(kind, title, content, characters, hasDetails: collapse);
        if (!collapse || !retained || !FullscreenViewport.CanUse(console))
        {
            console.Write(content);
            console.WriteLine();
            return;
        }
        console.MarkupLine($"[{TerminalTheme.Info}]▸ {Markup.Escape(text.Text("Terminal.DetailsAvailable"))}[/]");
        if (!deferReview) Review(console, text);
    }

    /// <summary>Lets a completed one-shot result be inspected before returning control to the invoking shell.</summary>
    private static void Review(IAnsiConsole console, ILocalizationService text)
    {
        using var state = new TerminalStateScope(console, text, TerminalActivityState.Completed);
        using var paste = new TerminalPasteScope(console);
        var reader = new TerminalInputReader(console.Input, 1024, win32Encoding: OperatingSystem.IsWindows());
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Terminal.ReviewKeys"))}[/]");
        try
        {
            while (true)
            {
                var input = reader.Read();
                if (input.Key is not { } key) continue;
                if (key.Key == ConsoleKey.Enter) return;
                if (TerminalHistoryView.IsShortcut(key)) TerminalHistoryView.Show(console, text, key, reader);
            }
        }
        catch (InteractiveFlowCanceledException)
        {
            // Escape leaves completed output; it never replays or approves an action.
        }
    }
}

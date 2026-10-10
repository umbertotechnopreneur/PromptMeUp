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

/// <summary>Mirrors a quiet UI state in the terminal title and restores the previous title at the interaction boundary.</summary>
internal sealed class TerminalStateScope : IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly bool _hasTitle;
    private bool _disposed;

    /// <summary>Sets the console-local state and saves the terminal title only for an interactive ANSI terminal.</summary>
    internal TerminalStateScope(IAnsiConsole console, ILocalizationService text, TerminalActivityState state)
    {
        _console = console;
        TerminalSession.For(console).State = state;
        _hasTitle = console.Profile.Capabilities.Ansi && console.Profile.Capabilities.Interactive
            && console.Profile.Out.IsTerminal && !Console.IsOutputRedirected;
        if (_hasTitle)
        {
            var title = "PromptMeUp · " + TerminalText.Safe(text.Text("Terminal.State." + state)).Replace('\n', ' ');
            // Xterm title-stack sequences preserve the invoking shell's title, including nested interactions.
            console.WriteAnsi(writer => writer.Write("\u001b[22;0t\u001b]0;" + title + "\u0007"));
        }
    }

    /// <summary>Restores the saved title even when input is cancelled or an operation fails.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hasTitle) _console.WriteAnsi(writer => writer.Write("\u001b[23;0t"));
    }
}

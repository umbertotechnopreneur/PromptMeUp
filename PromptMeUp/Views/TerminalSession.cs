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


using System.Diagnostics;
using System.Runtime.CompilerServices;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Holds ephemeral presentation state shared by views on the same console, without persisting conversation data.</summary>
internal sealed class TerminalSession
{
    private static readonly ConditionalWeakTable<IAnsiConsole, TerminalSession> Sessions = new();
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();

    internal ConsoleRenderOptions Options { get; private set; } = new(false, false);
    internal TimeSpan Elapsed => _elapsed.Elapsed;
    internal string ModeKey { get; set; } = "Terminal.Mode.Chat";
    internal TerminalActivityState State { get; set; } = TerminalActivityState.Ready;
    internal ShellRuntimeStatus? LastStatus { get; set; }
    internal TerminalTranscript History { get; } = new();
    internal bool HasPromptDock { get; set; }
    internal int ChatIndent => ModeKey == "Terminal.Mode.Chat" && HasPromptDock ? 2 : 0;

    /// <summary>Finds the console-local presentation state without coupling views to application services.</summary>
    internal static TerminalSession For(IAnsiConsole console) => Sessions.GetValue(console, _ => new TerminalSession());

    /// <summary>Changes workflow identity without showing metrics left over from a different workflow.</summary>
    internal void SetMode(ConversationDisplayMode mode, bool hasPromptDock = false)
    {
        var next = "Terminal.Mode." + mode;
        if (ModeKey != next) LastStatus = null;
        ModeKey = next;
        HasPromptDock = hasPromptDock;
    }

    /// <summary>Starts a new top-level invocation without changing terminal scrollback.</summary>
    internal void Reset(ConsoleRenderOptions options)
    {
        Options = options;
        ModeKey = "Terminal.Mode.Chat";
        State = TerminalActivityState.Ready;
        LastStatus = null;
        HasPromptDock = false;
        History.Clear();
        _elapsed.Restart();
    }
}

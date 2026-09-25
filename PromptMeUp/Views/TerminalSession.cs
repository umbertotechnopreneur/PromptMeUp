// SPDX-License-Identifier: MIT

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

    /// <summary>Finds the console-local presentation state without coupling views to application services.</summary>
    internal static TerminalSession For(IAnsiConsole console) => Sessions.GetValue(console, _ => new TerminalSession());

    /// <summary>Starts a new top-level invocation without changing terminal scrollback.</summary>
    internal void Reset(ConsoleRenderOptions options)
    {
        Options = options;
        ModeKey = "Terminal.Mode.Chat";
        State = TerminalActivityState.Ready;
        _elapsed.Restart();
    }
}

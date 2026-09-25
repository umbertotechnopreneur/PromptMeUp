// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Describes a quiet, nonanimated state independently of an operation's implementation.</summary>
internal enum TerminalActivityState { Ready, Working, NeedsInput, Completed, Cancelled, Failed }

/// <summary>Renders one compact activity with a semantic symbol and optional elapsed time.</summary>
internal sealed class TerminalActivityRow(string label, TerminalActivityState state,
    TimeSpan? elapsed = null, bool useSymbols = true) : IRenderable
{
    /// <summary>Lets the parent allocate a single row at the available terminal width.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) => new(1, Math.Max(1, maxWidth));

    /// <summary>Keeps the status and duration visible while clipping only the activity label.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0) return [];
        var duration = elapsed is { } time ? " · " + Duration(time) : string.Empty;
        var symbol = Symbol(state, useSymbols && options.Unicode);
        var prefix = symbol + " ";
        if (TerminalText.Width(prefix + duration) >= maxWidth) duration = string.Empty;
        var title = TerminalText.Clip(label, Math.Max(0, maxWidth - TerminalText.Width(prefix + duration)));
        return Segment.Truncate([
            new Segment(prefix, Style.Parse(Color(state))),
            new Segment(title, Style.Parse(TerminalTheme.Primary)),
            new Segment(duration, Style.Parse(TerminalTheme.Muted))], maxWidth);
    }

    /// <summary>Uses existing semantic theme colors without adding a new accent palette.</summary>
    internal static string Color(TerminalActivityState state) => state switch
    {
        TerminalActivityState.Completed => TerminalTheme.Success,
        TerminalActivityState.NeedsInput or TerminalActivityState.Cancelled => TerminalTheme.Warning,
        TerminalActivityState.Failed => TerminalTheme.Error,
        _ => TerminalTheme.Info
    };

    /// <summary>Provides one stable state indicator and a text-terminal fallback.</summary>
    internal static string Symbol(TerminalActivityState state, bool unicode = true) => (state, unicode) switch
    {
        (TerminalActivityState.Ready, true) => "◇",
        (TerminalActivityState.Working, true) => "✦",
        (TerminalActivityState.NeedsInput, true) => "✋",
        (TerminalActivityState.Completed, true) => "✓",
        (TerminalActivityState.Cancelled or TerminalActivityState.Failed, true) => "✕",
        (TerminalActivityState.Working, false) => "*",
        (TerminalActivityState.Completed, false) => "+",
        (TerminalActivityState.Cancelled or TerminalActivityState.Failed, false) => "x",
        _ => ">"
    };

    /// <summary>Formats measured time without implying progress or estimating a completion time.</summary>
    internal static string Duration(TimeSpan elapsed) => elapsed.TotalSeconds < 1
        ? $"{elapsed.TotalMilliseconds:0} ms"
        : elapsed.TotalMinutes < 1 ? $"{elapsed.TotalSeconds:0.0} s" : $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}";
}

// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Describes one terminal action without coupling its presentation to a command handler.</summary>
internal sealed record TerminalAction(string Label, string Color, bool Selected = false, bool Enabled = true);

/// <summary>Renders consistently spaced, keyboard-focused actions without an enclosing frame.</summary>
internal static class TerminalActionBar
{
    /// <summary>Places actions in one open row and optionally omits each button's brackets.</summary>
    internal static IRenderable Create(IReadOnlyList<TerminalAction> actions, bool showBrackets = true)
    {
        ArgumentNullException.ThrowIfNull(actions);
        if (actions.Count == 0)
        {
            throw new ArgumentException("An action bar needs at least one action.", nameof(actions));
        }

        var row = new Grid();
        foreach (var action in actions)
        {
            row.AddColumn();
        }
        row.AddRow(actions.Select(action => (IRenderable)new Button(action, showBrackets)).ToArray());
        return row;
    }

    /// <summary>Renders one action with a visible focus marker and the current theme selection colors.</summary>
    private sealed class Button(TerminalAction action, bool showBrackets) : IRenderable
    {
        private readonly string _label = (action.Selected && action.Enabled ? ">" : " ")
            + (showBrackets ? $" [ {action.Label} ]" : $" {action.Label}");

        /// <summary>Accepts the available width without requesting a wrapped action row.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) =>
            new(0, Math.Min(new Segment(_label).CellCount(), Math.Max(0, maxWidth)));

        /// <summary>Clips an oversized label while retaining complete terminal cells.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var color = action.Enabled ? action.Color : TerminalTheme.Muted;
            var style = Style.Parse(action.Selected && action.Enabled
                ? $"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"
                : color);
            return maxWidth > 0
                ? Segment.SplitOverflow(new Segment(_label, style), Overflow.Ellipsis, maxWidth)
                : [];
        }
    }
}

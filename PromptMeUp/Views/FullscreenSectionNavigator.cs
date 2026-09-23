// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Tracks a numbered fullscreen sidebar and renders its focused visible range consistently.</summary>
internal sealed class FullscreenSectionNavigator
{
    private const int NumberTimeoutMilliseconds = 1_000;
    private int? _pendingNumber;

    internal int SelectedIndex { get; private set; }

    internal bool IsFocused { get; set; }

    internal DateTimeOffset? PendingSelectionDeadline { get; private set; }

    /// <summary>Starts navigation at a valid selected item and removes incomplete numeric shortcuts.</summary>
    internal void Reset(int selectedIndex, int itemCount, bool focused)
    {
        ValidateCount(itemCount);
        SelectedIndex = Math.Clamp(selectedIndex, 0, itemCount - 1);
        IsFocused = focused;
        _pendingNumber = null;
        PendingSelectionDeadline = null;
    }

    /// <summary>Moves one or more positions without wrapping the sidebar selection.</summary>
    internal void Move(int delta, int itemCount)
    {
        ValidateCount(itemCount);
        SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, itemCount - 1);
    }

    /// <summary>Sets a zero-based sidebar selection while preserving the current focus region.</summary>
    internal void Select(int index, int itemCount)
    {
        ValidateCount(itemCount);
        SelectedIndex = Math.Clamp(index, 0, itemCount - 1);
    }

    /// <summary>Handles one- and two-digit sidebar shortcuts without requiring Enter.</summary>
    internal bool TrySelectNumber(ConsoleKeyInfo key, int itemCount)
    {
        ValidateCount(itemCount);
        if (key.Modifiers != 0 || key.KeyChar is < '0' or > '9')
        {
            return false;
        }
        var digit = key.KeyChar - '0';
        if (_pendingNumber is { } firstDigit)
        {
            ClearPendingNumber();
            var oneBased = firstDigit * 10 + digit;
            Select(oneBased <= itemCount ? oneBased - 1 : firstDigit - 1, itemCount);
            IsFocused = true;
            return true;
        }
        if (digit > 0 && digit * 10 <= itemCount)
        {
            _pendingNumber = digit;
            PendingSelectionDeadline = DateTimeOffset.UtcNow.AddMilliseconds(NumberTimeoutMilliseconds);
            return true;
        }
        if (digit is > 0 && digit <= itemCount)
        {
            Select(digit - 1, itemCount);
            IsFocused = true;
            return true;
        }
        return false;
    }

    /// <summary>Completes a delayed one-digit shortcut after its possible second digit did not arrive.</summary>
    internal bool CompletePendingNumber(int itemCount)
    {
        ValidateCount(itemCount);
        if (_pendingNumber is not { } number)
        {
            return false;
        }
        ClearPendingNumber();
        if (number > itemCount)
        {
            return false;
        }
        Select(number - 1, itemCount);
        IsFocused = true;
        return true;
    }

    /// <summary>Builds a clipped sidebar with dot-numbered labels and an explicit focus marker.</summary>
    internal IRenderable Render<T>(
        IReadOnlyList<T> items,
        Func<T, string> label,
        int bodyRows,
        string heading,
        bool unicode)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(heading);
        ValidateCount(items.Count);
        SelectedIndex = Math.Clamp(SelectedIndex, 0, items.Count - 1);

        var availableRows = Math.Max(1, bodyRows - 2);
        var spacing = availableRows >= items.Count * 2 ? 2 : 1;
        var capacity = Math.Max(1, availableRows / spacing);
        var offset = Math.Clamp(SelectedIndex - capacity + 1, 0, Math.Max(0, items.Count - capacity));
        var rows = new List<IRenderable>();
        for (var index = offset; index < Math.Min(items.Count, offset + capacity); index++)
        {
            var active = index == SelectedIndex;
            var selected = active && IsFocused;
            var value = $"{(active ? ">" : " ")} {index + 1}. {Safe(label(items[index]))}";
            rows.Add(new FullscreenLine(value, Style.Parse(selected
                ? $"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"
                : active ? TerminalTheme.Accent : TerminalTheme.Primary)));
            if (spacing == 2)
            {
                rows.Add(new Text(" "));
            }
        }
        if (items.Count > capacity)
        {
            var above = offset > 0 ? unicode ? "↑ " : "^ " : string.Empty;
            var below = offset + capacity < items.Count ? unicode ? " ↓" : " v" : string.Empty;
            rows.Add(new FullscreenLine(above + "..." + below, Style.Parse(TerminalTheme.Info)));
        }
        return new Padder(new Rows(
            new FullscreenLine(heading, Style.Parse(TerminalTheme.Accent)),
            new Text(" "),
            new Rows(rows)), new Padding(2, 0, 2, 0));
    }

    /// <summary>Rejects an empty collection before arithmetic or keyboard selection can become ambiguous.</summary>
    private static void ValidateCount(int itemCount)
    {
        if (itemCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "A fullscreen navigator needs at least one item.");
        }
    }

    /// <summary>Removes an incomplete numeric prefix after a selection is resolved or abandoned.</summary>
    private void ClearPendingNumber()
    {
        _pendingNumber = null;
        PendingSelectionDeadline = null;
    }

    /// <summary>Removes terminal controls and line breaks from a sidebar label without interpreting markup.</summary>
    private static string Safe(string value) => new(value.ReplaceLineEndings(" ")
        .Select(character => char.IsControl(character) ? ' ' : character).ToArray());
}

/// <summary>Renders one styled terminal row without allowing a long label to wrap into another region.</summary>
internal sealed class FullscreenLine(string value, Style style) : IRenderable
{
    /// <summary>Measures one clipped terminal row within the provided content width.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        new(0, Math.Min(new Segment(value).CellCount(), Math.Max(0, maxWidth)));

    /// <summary>Truncates whole terminal cells with an ellipsis instead of wrapping a sidebar or heading row.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        maxWidth > 0 ? Segment.SplitOverflow(new Segment(value, style), Overflow.Ellipsis, maxWidth) : [];
}

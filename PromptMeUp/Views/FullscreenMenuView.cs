// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Describes one selectable action in a reusable fullscreen menu.</summary>
internal sealed record FullscreenMenuItem(string Icon, string Label, string Description);

/// <summary>Presents a passive numbered menu with the shared fullscreen header, sidebar, and footer.</summary>
internal sealed class FullscreenMenuView(IAnsiConsole console, ILocalizationService text, ConsoleRenderOptions options)
{
    private readonly FullscreenSectionNavigator _navigator = new();
    private readonly FullscreenInput _input = new(console, text);
    private MenuFocus _focus = MenuFocus.Items;
    private FullscreenFrame? _lastFrame;

    /// <summary>Displays a disposable fullscreen menu and returns the selected zero-based item or null for back.</summary>
    internal int? Select(string title, IReadOnlyList<FullscreenMenuItem> items, string backLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(backLabel);
        if (items.Count == 0)
        {
            throw new ArgumentException("A fullscreen menu needs at least one item.", nameof(items));
        }
        if (!FullscreenViewport.CanUse(console))
        {
            throw new InvalidOperationException(text.Text("Form.Unavailable"));
        }

        _navigator.Reset(0, items.Count, focused: true);
        _focus = MenuFocus.Items;
        _lastFrame = null;
        _input.Reset();
        try
        {
            int? selected = null;
            FullscreenViewport.Run(console, () => selected = RunLoop(title, items, backLabel));
            return selected;
        }
        catch (InteractiveFlowCanceledException)
        {
            return null;
        }
        finally
        {
            _input.Reset();
        }
    }

    /// <summary>Processes menu, back action, numeric shortcuts, and terminal resize without embedding workflow behavior.</summary>
    private int? RunLoop(string title, IReadOnlyList<FullscreenMenuItem> items, string backLabel)
    {
        while (true)
        {
            void Paint() => PaintScreen(title, items, backLabel);
            Paint();
            var readKey = _input.ReadKey(Paint, _navigator.PendingSelectionDeadline);
            if (readKey is null)
            {
                _navigator.CompletePendingNumber(items.Count);
                _focus = MenuFocus.Items;
                continue;
            }
            var key = readKey.Value;
            if (key.Key is ConsoleKey.Escape or ConsoleKey.Q)
            {
                return null;
            }
            if (console.Profile.Width < 60 || console.Profile.Height < 20)
            {
                continue;
            }
            var before = _navigator.SelectedIndex;
            if (_navigator.TrySelectNumber(key, items.Count))
            {
                if (_navigator.SelectedIndex != before)
                {
                    _focus = MenuFocus.Items;
                }
                continue;
            }
            switch (key.Key)
            {
                case ConsoleKey.F6:
                case ConsoleKey.Tab:
                    _focus = _focus == MenuFocus.Items ? MenuFocus.Back : MenuFocus.Items;
                    break;
                case ConsoleKey.Enter:
                    return _focus == MenuFocus.Items ? _navigator.SelectedIndex : null;
                case ConsoleKey.RightArrow when _focus == MenuFocus.Items:
                    return _navigator.SelectedIndex;
                case ConsoleKey.LeftArrow:
                    _focus = MenuFocus.Items;
                    break;
                case ConsoleKey.UpArrow:
                    if (_focus == MenuFocus.Items)
                    {
                        _navigator.Move(-1, items.Count);
                    }
                    else
                    {
                        _focus = MenuFocus.Items;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (_focus == MenuFocus.Items)
                    {
                        _navigator.Move(1, items.Count);
                    }
                    else
                    {
                        _focus = MenuFocus.Items;
                    }
                    break;
                case ConsoleKey.PageUp when _focus == MenuFocus.Items:
                    _navigator.Move(-Math.Max(1, VisibleItemRows()), items.Count);
                    break;
                case ConsoleKey.PageDown when _focus == MenuFocus.Items:
                    _navigator.Move(Math.Max(1, VisibleItemRows()), items.Count);
                    break;
                case ConsoleKey.Home when _focus == MenuFocus.Items:
                    _navigator.Select(0, items.Count);
                    break;
                case ConsoleKey.End when _focus == MenuFocus.Items:
                    _navigator.Select(items.Count - 1, items.Count);
                    break;
            }
        }
    }

    /// <summary>Renders one setup-shaped menu frame with selected-item information in the content region.</summary>
    private void PaintScreen(string title, IReadOnlyList<FullscreenMenuItem> items, string backLabel)
    {
        var frame = FullscreenViewport.BeginFrame(console, _lastFrame);
        _lastFrame = frame;
        var width = Math.Max(1, frame.Width - 1);
        var height = Math.Max(1, frame.Height - 1);
        if (frame.Width < 60 || frame.Height < 20)
        {
            console.Write(new Text(text.Text("Form.TooSmall"), Style.Parse(TerminalTheme.Warning)));
            return;
        }

        var bodyRows = height - FullscreenHeader.Height - FullscreenFooter.Height();
        _navigator.IsFocused = _focus == MenuFocus.Items;
        var active = items[_navigator.SelectedIndex];
        var content = new Padder(new Rows(
            new FullscreenLine(MenuLabel(active), Style.Parse("bold " + TerminalTheme.Accent)),
            new Text(" "),
            new FullscreenLine(Safe(active.Description), Style.Parse(TerminalTheme.Primary))), new Padding(2, 0, 2, 0));
        var actions = new Grid().AddColumn();
        actions.AddRow(FullscreenFooter.Button(
            TerminalTheme.IconPrefix(options, "↩️", "x") + backLabel,
            TerminalTheme.Warning,
            _focus == MenuFocus.Back));
        var footerKey = new Segment(text.Text("Lab.MenuFooter", items.Count)).CellCount() > width - 4
            ? "Lab.MenuFooterCompact"
            : "Lab.MenuFooter";
        var footer = FullscreenFooter.Create(
            new FullscreenLine(text.Text("Lab.MenuNotice"), Style.Parse(TerminalTheme.Muted)),
            actions,
            FullscreenFooter.Shortcuts(text.Text(footerKey, items.Count)));
        console.Write(FullscreenWorkspace.Create(title, options, frame.Width, content,
            _navigator.Render(items, MenuLabel, bodyRows, text.Text("Form.Sections"), console.Profile.Capabilities.Unicode),
            footer, FullscreenFooter.NoticeRows));
    }

    /// <summary>Computes a conservative page movement amount after shared chrome and sidebar headings.</summary>
    private int VisibleItemRows() => Math.Max(1, console.Profile.Height - FullscreenHeader.Height - FullscreenFooter.Height() - 2);

    /// <summary>Applies the shared emoji fallback and spacing rules before rendering a menu item in either region.</summary>
    private string MenuLabel(FullscreenMenuItem item) =>
        TerminalTheme.IconPrefix(options, item.Icon, "-") + Safe(item.Label);

    /// <summary>Flattens externally supplied labels and descriptions before writing them to the terminal.</summary>
    private static string Safe(string value) => new(value.ReplaceLineEndings(" ")
        .Select(character => char.IsControl(character) ? ' ' : character).ToArray());

    private enum MenuFocus
    {
        Items,
        Back
    }
}

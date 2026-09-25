// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Describes one selectable command in the content region of a reusable fullscreen menu.</summary>
internal sealed record FullscreenMenuItem(string Icon, string Label, string Description,
    string? Details = null, bool CanExecute = true, string? InputLabel = null,
    string InitialInput = "", bool MultilineInput = false);

/// <summary>Describes one compact sidebar group and the commands displayed in its content region.</summary>
internal sealed record FullscreenMenuGroup(
    string Icon,
    string Label,
    string Description,
    IReadOnlyList<FullscreenMenuItem> Items,
    bool ShowItemDescriptions = true);

/// <summary>Identifies one command selected from a fullscreen menu group.</summary>
internal readonly record struct FullscreenMenuSelection(int GroupIndex, int ItemIndex, string? Input);

/// <summary>Presents numbered groups and inline commands with the shared fullscreen header, sidebar, and footer.</summary>
internal sealed class FullscreenMenuView(IAnsiConsole console, ILocalizationService text, ConsoleRenderOptions options)
{
    private readonly FullscreenSectionNavigator _groupNavigator = new();
    private readonly FullscreenInput _input = new(console, text);
    private MenuFocus _focus = MenuFocus.Groups;
    private int _selectedItemIndex;
    private int _itemOffset;
    private int _detailOffset;
    private readonly Dictionary<(int Group, int Item), string> _drafts = [];
    private int _selectedGroupIndex;
    private int _caret;
    private string? _editorError;
    private FullscreenFrame? _lastFrame;

    /// <summary>Displays a disposable fullscreen menu and returns the selected grouped command or null for back.</summary>
    internal FullscreenMenuSelection? Select(string title, IReadOnlyList<FullscreenMenuGroup> groups, string backLabel,
        Func<FullscreenMenuSelection, IReadOnlyList<FullscreenMenuGroup>?>? handleInline = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentException.ThrowIfNullOrWhiteSpace(backLabel);
        if (groups.Count == 0)
        {
            throw new ArgumentException("A fullscreen menu needs at least one group.", nameof(groups));
        }
        if (!FullscreenViewport.CanUse(console))
        {
            throw new InvalidOperationException(text.Text("Form.Unavailable"));
        }

        _selectedGroupIndex = Math.Clamp(_selectedGroupIndex, 0, groups.Count - 1);
        _groupNavigator.Reset(_selectedGroupIndex, groups.Count, focused: true);
        _focus = MenuFocus.Groups;
        ResetItems();
        _drafts.Clear();
        _caret = 0;
        _editorError = null;
        EnsureDraft(groups);
        _lastFrame = null;
        _input.Reset();
        try
        {
            FullscreenMenuSelection? selected = null;
            FullscreenViewport.Run(console, () => selected = RunLoop(title, groups, backLabel, handleInline));
            return selected;
        }
        catch (InteractiveFlowCanceledException)
        {
            return null;
        }
        finally
        {
            _selectedGroupIndex = _groupNavigator.SelectedIndex;
            _input.Reset();
        }
    }

    /// <summary>Processes grouped commands, one back action, numeric shortcuts, and terminal resize.</summary>
    private FullscreenMenuSelection? RunLoop(string title, IReadOnlyList<FullscreenMenuGroup> groups, string backLabel,
        Func<FullscreenMenuSelection, IReadOnlyList<FullscreenMenuGroup>?>? handleInline)
    {
        FullscreenMenuSelection? result = null;
        bool HandleSelection(FullscreenMenuSelection selection)
        {
            var refreshed = handleInline?.Invoke(selection);
            if (refreshed is null)
            {
                result = selection;
                return true;
            }

            groups = refreshed;
            _selectedGroupIndex = Math.Clamp(_groupNavigator.SelectedIndex, 0, groups.Count - 1);
            _groupNavigator.Reset(_selectedGroupIndex, groups.Count, _focus == MenuFocus.Groups);
            var items = groups[_groupNavigator.SelectedIndex].Items;
            _selectedItemIndex = items.Count == 0 ? 0 : Math.Clamp(_selectedItemIndex, 0, items.Count - 1);
            _itemOffset = Math.Min(_itemOffset, Math.Max(0, items.Count - 1));
            _detailOffset = 0;
            EnsureDraft(groups);
            return false;
        }

        while (true)
        {
            void Paint() => PaintScreen(title, groups, backLabel);
            Paint();
            var readKey = _input.ReadKey(Paint, _groupNavigator.PendingSelectionDeadline);
            if (readKey is null)
            {
                var pendingBefore = _groupNavigator.SelectedIndex;
                _groupNavigator.CompletePendingNumber(groups.Count);
                if (_groupNavigator.SelectedIndex != pendingBefore)
                {
                    ResetItems();
                    EnsureDraft(groups);
                }
                _focus = MenuFocus.Groups;
                continue;
            }
            var key = readKey.Value;
            var items = groups[_groupNavigator.SelectedIndex].Items;
            if (_focus == MenuFocus.Editor && items.Count > 0)
            {
                if (EditInput(key, items[_selectedItemIndex]))
                {
                    if (HandleSelection(new FullscreenMenuSelection(_groupNavigator.SelectedIndex, _selectedItemIndex,
                        CurrentDraft(items[_selectedItemIndex]))))
                    {
                        return result;
                    }
                }
                continue;
            }
            if (key.Key is ConsoleKey.Escape or ConsoleKey.Q)
            {
                return null;
            }
            if (console.Profile.Width < 60 || console.Profile.Height < 20)
            {
                continue;
            }
            var before = _groupNavigator.SelectedIndex;
            if (_groupNavigator.TrySelectNumber(key, groups.Count))
            {
                if (_groupNavigator.SelectedIndex != before)
                {
                    ResetItems();
                    EnsureDraft(groups);
                    _focus = MenuFocus.Groups;
                }
                continue;
            }
            switch (key.Key)
            {
                case ConsoleKey.F6:
                    _focus = _focus == MenuFocus.Groups
                        ? items.Count > 0 ? MenuFocus.Commands : MenuFocus.Back
                        : MenuFocus.Groups;
                    break;
                case ConsoleKey.Tab:
                    MoveFocus(items.Count, SelectedHasInput(items), SelectedHasDetails(items),
                        (key.Modifiers & ConsoleModifiers.Shift) != 0 ? -1 : 1);
                    break;
                case ConsoleKey.LeftArrow when (key.Modifiers & ConsoleModifiers.Control) != 0:
                    _focus = MenuFocus.Groups;
                    break;
                case ConsoleKey.Enter:
                    if (_focus == MenuFocus.Back)
                    {
                        return null;
                    }
                    if (_focus == MenuFocus.Commands && items.Count > 0 && items[_selectedItemIndex].InputLabel is not null)
                    {
                        BeginEdit(items[_selectedItemIndex]);
                        break;
                    }
                    if (_focus == MenuFocus.Commands && items.Count > 0 && items[_selectedItemIndex].CanExecute)
                    {
                        if (HandleSelection(new FullscreenMenuSelection(_groupNavigator.SelectedIndex, _selectedItemIndex, null)))
                        {
                            return result;
                        }
                        break;
                    }
                    if (items.Count > 0)
                    {
                        _focus = MenuFocus.Commands;
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (_focus == MenuFocus.Commands && items.Count > 0 && items[_selectedItemIndex].InputLabel is not null)
                    {
                        BeginEdit(items[_selectedItemIndex]);
                        break;
                    }
                    if (_focus == MenuFocus.Commands && items.Count > 0 && items[_selectedItemIndex].CanExecute)
                    {
                        if (HandleSelection(new FullscreenMenuSelection(_groupNavigator.SelectedIndex, _selectedItemIndex, null)))
                        {
                            return result;
                        }
                        break;
                    }
                    if (_focus == MenuFocus.Groups && items.Count > 0)
                    {
                        _focus = MenuFocus.Commands;
                    }
                    break;
                case ConsoleKey.LeftArrow:
                    _focus = _focus == MenuFocus.Details ? MenuFocus.Commands : MenuFocus.Groups;
                    break;
                case ConsoleKey.UpArrow:
                    if (_focus == MenuFocus.Details)
                    {
                        MoveDetails(-1, items);
                    }
                    else if (_focus == MenuFocus.Commands && items.Count > 0)
                    {
                        MoveItem(-1, items);
                    }
                    else
                    {
                        MoveGroup(-1, groups);
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (_focus == MenuFocus.Details)
                    {
                        MoveDetails(1, items);
                    }
                    else if (_focus == MenuFocus.Commands && items.Count > 0)
                    {
                        MoveItem(1, items);
                    }
                    else
                    {
                        MoveGroup(1, groups);
                    }
                    break;
                case ConsoleKey.PageUp when _focus == MenuFocus.Details:
                    MoveDetails(-VisibleDetailRows(), items);
                    break;
                case ConsoleKey.PageDown when _focus == MenuFocus.Details:
                    MoveDetails(VisibleDetailRows(), items);
                    break;
                case ConsoleKey.PageUp when _focus == MenuFocus.Groups:
                    MoveGroup(-1, groups);
                    break;
                case ConsoleKey.PageDown when _focus == MenuFocus.Groups:
                    MoveGroup(1, groups);
                    break;
                case ConsoleKey.PageUp when _focus == MenuFocus.Commands && items.Count > 0:
                    MoveItem(-VisibleItemRows(), items);
                    break;
                case ConsoleKey.PageDown when _focus == MenuFocus.Commands && items.Count > 0:
                    MoveItem(VisibleItemRows(), items);
                    break;
                case ConsoleKey.Home when _focus == MenuFocus.Details:
                    _detailOffset = 0;
                    break;
                case ConsoleKey.End when _focus == MenuFocus.Details:
                    MoveDetails(int.MaxValue, items);
                    break;
                case ConsoleKey.Home when _focus == MenuFocus.Groups:
                    MoveGroup(-_groupNavigator.SelectedIndex, groups);
                    break;
                case ConsoleKey.End when _focus == MenuFocus.Groups:
                    MoveGroup(groups.Count - 1 - _groupNavigator.SelectedIndex, groups);
                    break;
                case ConsoleKey.Home when _focus == MenuFocus.Commands && items.Count > 0:
                    _selectedItemIndex = 0;
                    _itemOffset = 0;
                    _detailOffset = 0;
                    EnsureDraft(groups);
                    break;
                case ConsoleKey.End when _focus == MenuFocus.Commands && items.Count > 0:
                    _selectedItemIndex = items.Count - 1;
                    _detailOffset = 0;
                    EnsureDraft(groups);
                    break;
            }
        }
    }

    /// <summary>Renders one setup-shaped frame with compact groups and inline commands.</summary>
    private void PaintScreen(string title, IReadOnlyList<FullscreenMenuGroup> groups, string backLabel)
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
        _groupNavigator.IsFocused = _focus == MenuFocus.Groups;
        var active = groups[_groupNavigator.SelectedIndex];
        var content = Content(active, bodyRows);
        var actions = TerminalActionBar.Create(
        [
            new TerminalAction(TerminalTheme.IconPrefix(options, "↩️", "x") + backLabel,
                TerminalTheme.Warning, _focus == MenuFocus.Back)
        ]);
        var footerKey = _focus == MenuFocus.Editor ? "Lab.MenuEditorFooter" : "Lab.MenuFooter";
        var footer = FullscreenFooter.Create(
            new FullscreenLine(_editorError ?? SelectedDescription(active),
                Style.Parse(_editorError is null ? TerminalTheme.Muted : TerminalTheme.Error)),
            actions,
            FullscreenFooter.Shortcuts(text.Text(footerKey, groups.Count),
                text.Text(footerKey + "Compact", groups.Count), width - 4));
        console.Write(FullscreenWorkspace.Create(title, options, frame.Width, content,
            _groupNavigator.Render(groups, GroupLabel, bodyRows, text.Text("Form.Sections"), console.Profile.Capabilities.Unicode),
            footer, FullscreenFooter.NoticeRows));
    }

    /// <summary>Builds the central command list as setup-style label and value fields.</summary>
    private IRenderable Content(FullscreenMenuGroup group, int bodyRows)
    {
        var rows = new List<IRenderable>
        {
            FullscreenWorkspace.SectionHeading(GroupLabel(group),
                _focus is MenuFocus.Commands or MenuFocus.Editor or MenuFocus.Details),
            new FullscreenLine(Safe(group.Description), Style.Parse(TerminalTheme.Primary)),
            new Text(" "),
            new FullscreenLine(text.Text("Lab.Commands"), Style.Parse("bold " + TerminalTheme.Primary))
        };
        if (group.Items.Count == 0)
        {
            rows.Add(new Text(" "));
            rows.Add(new FullscreenLine(text.Text("Lab.None"), Style.Parse(TerminalTheme.Muted)));
            return new Padder(new Rows(rows.ToArray()), new Padding(2, 0, 2, 0));
        }

        _selectedItemIndex = Math.Clamp(_selectedItemIndex, 0, group.Items.Count - 1);
        var selectedItem = group.Items[_selectedItemIndex];
        var hasDetails = !string.IsNullOrWhiteSpace(selectedItem.Details);
        var inputRows = selectedItem.InputLabel is null ? 0 : selectedItem.MultilineInput ? 6 : 3;
        var detailRows = hasDetails ? DetailCapacity(bodyRows - inputRows) : 0;
        var visibleRows = Math.Max(1, bodyRows - 8 - inputRows - detailRows);
        EnsureItemVisible(group.Items.Count, visibleRows);
        var end = Math.Min(group.Items.Count, _itemOffset + visibleRows);
        var grid = new Grid().AddColumn(new GridColumn().RightAligned());
        if (group.ShowItemDescriptions)
        {
            grid.AddColumn(new GridColumn());
        }
        for (var index = _itemOffset; index < end; index++)
        {
            var item = group.Items[index];
            var selected = index == _selectedItemIndex;
            var focused = selected && _focus == MenuFocus.Commands;
            var style = focused
                ? Style.Parse($"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}")
                : Style.Parse(!item.CanExecute ? TerminalTheme.Muted
                    : selected ? "bold " + TerminalTheme.Primary : TerminalTheme.Primary);
            var marker = focused ? "> " : "  ";
            var label = new FullscreenLine(marker + ItemLabel(item), style);
            if (group.ShowItemDescriptions)
            {
                grid.AddRow(label, new FullscreenLine(Safe(item.Description), style));
            }
            else
            {
                grid.AddRow(label);
            }
        }
        rows.Add(grid);
        if (_itemOffset > 0 || end < group.Items.Count)
        {
            rows.Add(new FullscreenLine(text.Text("Lab.CommandPosition", _selectedItemIndex + 1, group.Items.Count),
                Style.Parse(TerminalTheme.Muted)));
        }
        if (selectedItem.InputLabel is not null)
        {
            var editing = _focus == MenuFocus.Editor;
            var editorStyle = Style.Parse(editing
                ? $"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"
                : TerminalTheme.Primary);
            rows.Add(new Text(" "));
            rows.Add(new FullscreenLine((editing ? "> " : string.Empty) + selectedItem.InputLabel,
                Style.Parse((editing ? "bold " : string.Empty) + TerminalTheme.Accent)));
            if (selectedItem.MultilineInput)
            {
                rows.Add(TextArea(selectedItem, 4, editorStyle));
            }
            else
            {
                var editorWidth = Math.Max(8, console.Profile.Width
                    - FullscreenWorkspace.SidebarWidth(console.Profile.Width) - 12);
                rows.Add(new FullscreenLine("[ " + EditorValue(selectedItem, editorWidth) + " ]", editorStyle));
            }
        }
        if (hasDetails)
        {
            var detailLines = SafeDetails(selectedItem.Details!).Split('\n');
            _detailOffset = Math.Clamp(_detailOffset, 0, Math.Max(0, detailLines.Length - detailRows));
            var detailEnd = Math.Min(detailLines.Length, _detailOffset + detailRows);
            var detailMarker = _focus == MenuFocus.Details ? "> " : string.Empty;
            rows.Add(new Text(" "));
            rows.Add(new FullscreenLine(detailMarker + text.Text("Lab.Package") + " — " + ItemLabel(selectedItem),
                Style.Parse((_focus == MenuFocus.Details ? "bold " : string.Empty) + TerminalTheme.Accent)));
            rows.Add(new Text(string.Join('\n', detailLines[_detailOffset..detailEnd]),
                Style.Parse(TerminalTheme.Primary)));
            if (_detailOffset > 0 || detailEnd < detailLines.Length)
            {
                rows.Add(new FullscreenLine(text.Text("Lab.DetailPosition", _detailOffset + 1, detailLines.Length),
                    Style.Parse(TerminalTheme.Muted)));
            }
        }
        return new Padder(new Rows(rows.ToArray()), new Padding(2, 0, 2, 0));
    }

    /// <summary>Shows the active command's concise description in the footer instead of repeating it in a second column.</summary>
    private string SelectedDescription(FullscreenMenuGroup group)
    {
        if (group.Items.Count == 0)
        {
            return group.Description;
        }

        var item = group.Items[Math.Clamp(_selectedItemIndex, 0, group.Items.Count - 1)];
        return string.IsNullOrWhiteSpace(item.Description) ? group.Description : item.Description;
    }

    /// <summary>Cycles forward or backward through the available content and footer areas.</summary>
    private void MoveFocus(int itemCount, bool hasInput, bool hasDetails, int direction)
    {
        var areas = new List<MenuFocus> { MenuFocus.Groups };
        if (itemCount > 0)
        {
            areas.Add(MenuFocus.Commands);
        }
        if (hasInput)
        {
            areas.Add(MenuFocus.Editor);
        }
        if (hasDetails)
        {
            areas.Add(MenuFocus.Details);
        }
        areas.Add(MenuFocus.Back);
        var index = areas.IndexOf(_focus);
        _focus = areas[(index + direction + areas.Count) % areas.Count];
    }

    /// <summary>Moves to another sidebar group and resets its command selection.</summary>
    private void MoveGroup(int delta, IReadOnlyList<FullscreenMenuGroup> groups)
    {
        var before = _groupNavigator.SelectedIndex;
        _groupNavigator.Move(delta, groups.Count);
        if (_groupNavigator.SelectedIndex != before)
        {
            ResetItems();
            EnsureDraft(groups);
        }
        _focus = MenuFocus.Groups;
    }

    /// <summary>Moves within the active group while keeping the highlighted command in view.</summary>
    private void MoveItem(int delta, IReadOnlyList<FullscreenMenuItem> items)
    {
        _selectedItemIndex = (_selectedItemIndex + delta) % items.Count;
        if (_selectedItemIndex < 0)
        {
            _selectedItemIndex += items.Count;
        }
        _detailOffset = 0;
        EnsureDraft(items[_selectedItemIndex]);
    }

    /// <summary>Resets the central command cursor after the active sidebar group changes.</summary>
    private void ResetItems()
    {
        _selectedItemIndex = 0;
        _itemOffset = 0;
        _detailOffset = 0;
        _caret = 0;
        _editorError = null;
    }

    /// <summary>Moves keyboard focus into the selected inline textbox or textarea.</summary>
    private void BeginEdit(FullscreenMenuItem item)
    {
        EnsureDraft(item);
        _caret = CurrentDraft(item).Length;
        _editorError = null;
        _focus = MenuFocus.Editor;
    }

    /// <summary>Edits the current inline skill value and reports when it is ready to submit.</summary>
    private bool EditInput(ConsoleKeyInfo key, FullscreenMenuItem item)
    {
        _editorError = null;
        if (key.Key == ConsoleKey.Escape)
        {
            _focus = MenuFocus.Commands;
            return false;
        }
        if (key.Key == ConsoleKey.F6)
        {
            _focus = MenuFocus.Groups;
            return false;
        }
        if (key.Key == ConsoleKey.Tab)
        {
            _focus = (key.Modifiers & ConsoleModifiers.Shift) != 0 ? MenuFocus.Commands
                : string.IsNullOrWhiteSpace(item.Details) ? MenuFocus.Back : MenuFocus.Details;
            return false;
        }
        if (key.Key == ConsoleKey.Enter
            && (!item.MultilineInput || (key.Modifiers & ConsoleModifiers.Control) != 0))
        {
            if (!item.CanExecute || string.IsNullOrWhiteSpace(CurrentDraft(item)))
            {
                _editorError = text.Text("Lab.Invalid");
                return false;
            }
            return true;
        }
        if (key.Key == ConsoleKey.Enter)
        {
            InsertInput(item, "\n");
            return false;
        }
        if (key.Key == ConsoleKey.U && (key.Modifiers & ConsoleModifiers.Control) != 0)
        {
            SetDraft(item, string.Empty);
            _caret = 0;
            return false;
        }

        var value = CurrentDraft(item);
        if (key.Key == ConsoleKey.Home)
        {
            _caret = item.MultilineInput ? value.LastIndexOf('\n', Math.Max(0, _caret - 1)) + 1 : 0;
        }
        else if (key.Key == ConsoleKey.End)
        {
            var end = item.MultilineInput ? value.IndexOf('\n', _caret) : -1;
            _caret = end < 0 ? value.Length : end;
        }
        else if (item.MultilineInput && key.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow)
        {
            MoveCaretVertically(value, key.Key == ConsoleKey.UpArrow ? -1 : 1);
        }
        else if (key.Key == ConsoleKey.LeftArrow && _caret > 0)
        {
            _caret = TerminalTextElements.Previous(value, _caret);
        }
        else if (key.Key == ConsoleKey.RightArrow && _caret < value.Length)
        {
            _caret = TerminalTextElements.Next(value, _caret);
        }
        else if (key.Key == ConsoleKey.Backspace && _caret > 0)
        {
            var previous = TerminalTextElements.Previous(value, _caret);
            SetDraft(item, value.Remove(previous, _caret - previous));
            _caret = previous;
        }
        else if (key.Key == ConsoleKey.Delete && _caret < value.Length)
        {
            var next = TerminalTextElements.Next(value, _caret);
            SetDraft(item, value.Remove(_caret, next - _caret));
        }
        else if (!char.IsControl(key.KeyChar) && (key.Modifiers & ConsoleModifiers.Control) == 0)
        {
            InsertInput(item, key.KeyChar.ToString());
        }
        return false;
    }

    /// <summary>Inserts bounded text at the current Unicode-safe caret position.</summary>
    private void InsertInput(FullscreenMenuItem item, string addition)
    {
        var value = CurrentDraft(item);
        if (value.Length + addition.Length > 4096)
        {
            _editorError = text.Text("Lab.Invalid");
            return;
        }
        SetDraft(item, value.Insert(_caret, addition));
        _caret += addition.Length;
    }

    /// <summary>Moves the textarea caret to the closest column on an adjacent logical line.</summary>
    private void MoveCaretVertically(string value, int delta)
    {
        var lineStart = value.LastIndexOf('\n', Math.Max(0, _caret - 1)) + 1;
        var column = _caret - lineStart;
        if (delta < 0)
        {
            if (lineStart == 0)
            {
                return;
            }
            var previousEnd = lineStart - 1;
            var previousStart = value.LastIndexOf('\n', Math.Max(0, previousEnd - 1)) + 1;
            _caret = Math.Min(previousStart + column, previousEnd);
            return;
        }
        var currentEnd = value.IndexOf('\n', _caret);
        if (currentEnd < 0)
        {
            return;
        }
        var nextStart = currentEnd + 1;
        var nextEnd = value.IndexOf('\n', nextStart);
        _caret = Math.Min(nextStart + column, nextEnd < 0 ? value.Length : nextEnd);
    }

    /// <summary>Initializes the active field draft after section or command navigation.</summary>
    private void EnsureDraft(IReadOnlyList<FullscreenMenuGroup> groups)
    {
        var items = groups[_groupNavigator.SelectedIndex].Items;
        if (items.Count > 0)
        {
            EnsureDraft(items[Math.Clamp(_selectedItemIndex, 0, items.Count - 1)]);
        }
    }

    /// <summary>Initializes one field without overwriting edits made during this workspace session.</summary>
    private void EnsureDraft(FullscreenMenuItem item)
    {
        if (item.InputLabel is not null)
        {
            _drafts.TryAdd((_groupNavigator.SelectedIndex, _selectedItemIndex), NormalizeInput(item.InitialInput));
        }
    }

    /// <summary>Returns the draft associated with the current group and command.</summary>
    private string CurrentDraft(FullscreenMenuItem item)
    {
        EnsureDraft(item);
        return _drafts.GetValueOrDefault((_groupNavigator.SelectedIndex, _selectedItemIndex), string.Empty);
    }

    /// <summary>Updates the draft associated with the current group and command.</summary>
    private void SetDraft(FullscreenMenuItem item, string value)
    {
        _ = item;
        _drafts[(_groupNavigator.SelectedIndex, _selectedItemIndex)] = NormalizeInput(value);
    }

    /// <summary>Fits a focused textbox around its caret and flattens passive line breaks.</summary>
    private string EditorValue(FullscreenMenuItem item, int width)
    {
        var value = CurrentDraft(item);
        if (_focus != MenuFocus.Editor)
        {
            return Safe(value);
        }
        var sourceCaret = Math.Clamp(_caret, 0, value.Length);
        var beforeCaret = Safe(value[..sourceCaret]);
        var afterCaret = Safe(value[sourceCaret..]);
        var safe = beforeCaret + afterCaret;
        var caret = beforeCaret.Length;
        var budget = Math.Max(1, width - 3);
        var start = Math.Max(0, caret - budget / 2);
        var end = Math.Min(safe.Length, start + budget);
        start = Math.Max(0, end - budget);
        return (start > 0 ? "…" : string.Empty) + safe[start..Math.Min(caret, end)] + "|"
            + safe[Math.Min(caret, end)..end] + (end < safe.Length ? "…" : string.Empty);
    }

    /// <summary>Renders a bounded multiline field around its current caret line.</summary>
    private IRenderable TextArea(FullscreenMenuItem item, int visibleRows, Style style)
    {
        var value = CurrentDraft(item);
        var sourceCaret = Math.Clamp(_caret, 0, value.Length);
        var marked = _focus == MenuFocus.Editor ? value.Insert(sourceCaret, "|") : value;
        var lines = SafeDetails(marked).Split('\n');
        var caretLine = value[..sourceCaret].Count(character => character == '\n');
        var start = Math.Clamp(caretLine - visibleRows + 1, 0, Math.Max(0, lines.Length - visibleRows));
        return new Rows(lines.Skip(start).Take(visibleRows).Select((line, index) =>
            (IRenderable)new FullscreenLine((_focus == MenuFocus.Editor && start + index == caretLine ? "> " : "  ") + line,
                style)).ToArray());
    }

    /// <summary>Normalizes line endings before caret navigation or command submission.</summary>
    private static string NormalizeInput(string value) => value.ReplaceLineEndings("\n");

    /// <summary>Scrolls the complete selected package review without leaving the central panel.</summary>
    private void MoveDetails(int delta, IReadOnlyList<FullscreenMenuItem> items)
    {
        if (!SelectedHasDetails(items))
        {
            _detailOffset = 0;
            return;
        }
        var lineCount = SafeDetails(items[_selectedItemIndex].Details!).Split('\n').Length;
        _detailOffset = delta == int.MaxValue
            ? Math.Max(0, lineCount - 1)
            : Math.Clamp(_detailOffset + delta, 0, Math.Max(0, lineCount - 1));
    }

    /// <summary>Reports whether the active command exposes a complete package review.</summary>
    private bool SelectedHasDetails(IReadOnlyList<FullscreenMenuItem> items) => items.Count > 0
        && !string.IsNullOrWhiteSpace(items[Math.Clamp(_selectedItemIndex, 0, items.Count - 1)].Details);

    /// <summary>Reports whether the active command owns an inline textbox or textarea.</summary>
    private bool SelectedHasInput(IReadOnlyList<FullscreenMenuItem> items) => items.Count > 0
        && items[Math.Clamp(_selectedItemIndex, 0, items.Count - 1)].InputLabel is not null;

    /// <summary>Keeps the selected central command inside its bounded viewport.</summary>
    private void EnsureItemVisible(int itemCount, int visibleRows)
    {
        _selectedItemIndex = Math.Clamp(_selectedItemIndex, 0, itemCount - 1);
        if (_selectedItemIndex < _itemOffset)
        {
            _itemOffset = _selectedItemIndex;
        }
        else if (_selectedItemIndex >= _itemOffset + visibleRows)
        {
            _itemOffset = _selectedItemIndex - visibleRows + 1;
        }
        _itemOffset = Math.Clamp(_itemOffset, 0, Math.Max(0, itemCount - visibleRows));
    }

    /// <summary>Computes a conservative central page movement amount after shared chrome and group details.</summary>
    private int VisibleItemRows() => Math.Max(1,
        (console.Profile.Height - FullscreenHeader.Height - FullscreenFooter.Height() - 8) / 2);

    /// <summary>Computes a conservative page size for the package review region.</summary>
    private int VisibleDetailRows() => DetailCapacity(
        console.Profile.Height - FullscreenHeader.Height - FullscreenFooter.Height());

    /// <summary>Reserves comparable space for commands and the selected package body.</summary>
    private static int DetailCapacity(int bodyRows) => Math.Max(2, (bodyRows - 8) / 2);

    /// <summary>Applies the shared emoji fallback and spacing rules to a sidebar group.</summary>
    private string GroupLabel(FullscreenMenuGroup group) =>
        TerminalTheme.IconPrefix(options, group.Icon, "-") + Safe(group.Label);

    /// <summary>Applies the shared emoji fallback and spacing rules to an inline command.</summary>
    private string ItemLabel(FullscreenMenuItem item) =>
        TerminalTheme.IconPrefix(options, item.Icon, "-") + Safe(item.Label);

    /// <summary>Flattens externally supplied labels and descriptions before writing them to the terminal.</summary>
    private static string Safe(string value) => new(value.ReplaceLineEndings(" ")
        .Select(character => char.IsControl(character) ? ' ' : character).ToArray());

    /// <summary>Preserves package-review line breaks while removing unsafe terminal controls.</summary>
    private static string SafeDetails(string value) => new(value.ReplaceLineEndings("\n")
        .Select(character => char.IsControl(character) && character != '\n' && character != '\t' ? ' ' : character)
        .ToArray());

    private enum MenuFocus
    {
        Groups,
        Commands,
        Editor,
        Details,
        Back
    }
}

// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public interface IMemoryManagerView
{
    MemoryManagerSelection Choose(IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals,
        string? feedback = null, bool feedbackIsError = false, bool selectProposals = false);
}

/// <summary>Edits saved and suggested memories inside one numbered master-detail workspace.</summary>
public sealed class MemoryManagerView : IMemoryManagerView
{
    private const string CreateKey = "create";
    private const string ProposalsKey = "proposals";
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly FullscreenSectionNavigator _navigator = new();
    private readonly FullscreenInput _inputReader;
    private readonly Dictionary<string, string> _drafts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dirty = new(StringComparer.Ordinal);
    private string? _selectedKey;
    private string? _submittedKey;
    private EditorFocus _focus;
    private int _actionIndex;
    private int _proposalIndex;
    private int _detailOffset;
    private int _lineCount;
    private int _visibleRows;
    private MemoryManagerAction? _pending;
    private bool _editing;
    private string _input = string.Empty;
    private int _caret;
    private string? _error;
    private FullscreenFrame? _lastFrame;

    /// <summary>Creates a passive inline editor using the shared terminal palette and input primitives.</summary>
    public MemoryManagerView(IAnsiConsole console, ILocalizationService text, IConsoleShellView shell)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _inputReader = new(console, text);
    }

    /// <summary>Returns one confirmed inline action without opening a child editor or proposal screen.</summary>
    public MemoryManagerSelection Choose(IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals,
        string? feedback = null, bool feedbackIsError = false, bool selectProposals = false)
    {
        ArgumentNullException.ThrowIfNull(memories);
        ArgumentNullException.ThrowIfNull(proposals);
        if (selectProposals)
        {
            _selectedKey = ProposalsKey;
        }
        ReconcileDrafts(memories, proposals, feedback, feedbackIsError);
        MemoryManagerSelection selection;
        try
        {
            selection = FullscreenForm.CanUse(_console)
                ? ChooseFullscreen(memories, proposals, feedback, feedbackIsError)
                : ChooseScrolling(memories, proposals, feedback, feedbackIsError);
        }
        catch (InteractiveFlowCanceledException)
        {
            selection = new(MemoryManagerAction.Close);
        }
        if (selection.Action == MemoryManagerAction.Close)
        {
            DiscardSessionDrafts();
        }
        return selection;
    }

    /// <summary>Discards unsaved editor state when the memory workspace is closed.</summary>
    private void DiscardSessionDrafts()
    {
        _drafts.Clear();
        _dirty.Clear();
        _submittedKey = null;
        _pending = null;
        _editing = false;
        _input = string.Empty;
        _caret = 0;
        _error = null;
        _detailOffset = 0;
    }

    /// <summary>Runs the setup-shaped editor in one alternate-buffer lifecycle.</summary>
    private MemoryManagerSelection ChooseFullscreen(IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals,
        string? feedback, bool feedbackIsError)
    {
        var entries = Entries(memories, proposals);
        var selected = entries.ToList().FindIndex(entry => string.Equals(entry.Key, _selectedKey, StringComparison.Ordinal));
        if (selected < 0)
        {
            selected = memories.Count > 0 ? 2 : 0;
        }
        _navigator.Reset(selected, entries.Count, focused: true);
        _selectedKey = entries[selected].Key;
        _focus = EditorFocus.Sections;
        _actionIndex = 0;
        _pending = null;
        _editing = false;
        _error = null;
        _detailOffset = 0;
        _lastFrame = null;
        _inputReader.Reset();
        EnsureDraft(entries[selected], memories, proposals);
        try
        {
            var result = new MemoryManagerSelection(MemoryManagerAction.Close);
            FullscreenViewport.Run(_console, () => result = RunLoop(memories, proposals, entries, feedback, feedbackIsError));
            return result;
        }
        finally
        {
            _inputReader.Reset();
            _editing = false;
            _input = string.Empty;
        }
    }

    /// <summary>Navigates numbered sections, the central editor, and contextual actions in one screen.</summary>
    private MemoryManagerSelection RunLoop(IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals,
        IReadOnlyList<NavigationEntry> entries, string? feedback, bool feedbackIsError)
    {
        while (true)
        {
            void Paint() => PaintScreen(memories, proposals, entries, feedback, feedbackIsError);
            Paint();
            var readKey = _inputReader.ReadKey(Paint, _navigator.PendingSelectionDeadline);
            if (readKey is null)
            {
                var before = _navigator.SelectedIndex;
                _navigator.CompletePendingNumber(entries.Count);
                if (_navigator.SelectedIndex != before)
                {
                    Activate(entries[_navigator.SelectedIndex], memories, proposals);
                }
                continue;
            }
            var key = readKey.Value;
            var entry = entries[_navigator.SelectedIndex];
            if (_editing)
            {
                Edit(entry, memories, proposals, key);
                continue;
            }
            if (key.Key == ConsoleKey.Escape)
            {
                if (_pending is not null)
                {
                    _pending = null;
                    _actionIndex = 0;
                }
                else if (_focus != EditorFocus.Sections)
                {
                    _focus = EditorFocus.Sections;
                }
                else
                {
                    return new(MemoryManagerAction.Close);
                }
                continue;
            }
            if (_console.Profile.Width < 60 || _console.Profile.Height < 20)
            {
                continue;
            }
            var previous = _navigator.SelectedIndex;
            if (_navigator.TrySelectNumber(key, entries.Count))
            {
                if (_navigator.SelectedIndex != previous)
                {
                    Activate(entries[_navigator.SelectedIndex], memories, proposals);
                }
                continue;
            }
            var actions = Actions(entry, proposals);
            _actionIndex = Math.Clamp(_actionIndex, 0, actions.Count - 1);
            switch (key.Key)
            {
                case ConsoleKey.F6:
                case ConsoleKey.Tab:
                    _focus = _focus switch
                    {
                        EditorFocus.Sections => EditorFocus.Editor,
                        EditorFocus.Editor => EditorFocus.Actions,
                        _ => EditorFocus.Sections
                    };
                    break;
                case ConsoleKey.Enter:
                    if (_focus == EditorFocus.Sections)
                    {
                        _focus = EditorFocus.Editor;
                    }
                    else if (_focus == EditorFocus.Editor)
                    {
                        BeginEdit(entry, memories, proposals);
                    }
                    else if (ApplyAction(actions[_actionIndex], entry, memories, proposals) is { } result)
                    {
                        return result;
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (_focus == EditorFocus.Sections)
                    {
                        _focus = EditorFocus.Editor;
                    }
                    else if (_focus == EditorFocus.Editor && entry.Kind == NavigationKind.Proposals)
                    {
                        ChangeProposal(1, proposals);
                    }
                    else if (_focus == EditorFocus.Actions)
                    {
                        _actionIndex = (_actionIndex + 1) % actions.Count;
                    }
                    break;
                case ConsoleKey.LeftArrow:
                    if (_focus == EditorFocus.Editor && entry.Kind == NavigationKind.Proposals)
                    {
                        ChangeProposal(-1, proposals);
                    }
                    else if (_focus == EditorFocus.Actions)
                    {
                        _actionIndex = (_actionIndex - 1 + actions.Count) % actions.Count;
                    }
                    else
                    {
                        _focus = EditorFocus.Sections;
                    }
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    var direction = key.Key == ConsoleKey.UpArrow ? -1 : 1;
                    if (_focus == EditorFocus.Sections)
                    {
                        MoveSection(direction, entries, memories, proposals);
                    }
                    else if (_focus == EditorFocus.Editor)
                    {
                        _detailOffset = Math.Clamp(_detailOffset + direction, 0, Math.Max(0, _lineCount - _visibleRows));
                    }
                    else
                    {
                        _actionIndex = (_actionIndex + direction + actions.Count) % actions.Count;
                    }
                    break;
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                    if (_focus == EditorFocus.Sections)
                    {
                        MoveSection(key.Key == ConsoleKey.PageUp ? -1 : 1, entries, memories, proposals);
                    }
                    else
                    {
                        var amount = Math.Max(1, _visibleRows);
                        _detailOffset = Math.Clamp(_detailOffset + (key.Key == ConsoleKey.PageUp ? -amount : amount),
                            0, Math.Max(0, _lineCount - _visibleRows));
                    }
                    break;
                case ConsoleKey.Home:
                case ConsoleKey.End:
                    if (_focus == EditorFocus.Sections)
                    {
                        _navigator.Select(key.Key == ConsoleKey.Home ? 0 : entries.Count - 1, entries.Count);
                        Activate(entries[_navigator.SelectedIndex], memories, proposals);
                    }
                    else
                    {
                        _detailOffset = key.Key == ConsoleKey.Home ? 0 : Math.Max(0, _lineCount - _visibleRows);
                    }
                    break;
            }
        }
    }

    /// <summary>Draws numbered sections, an inline textbox and text area, and one contextual action bar.</summary>
    private void PaintScreen(IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals,
        IReadOnlyList<NavigationEntry> entries, string? feedback, bool feedbackIsError)
    {
        var frame = FullscreenViewport.BeginFrame(_console, _lastFrame);
        _lastFrame = frame;
        if (frame.Width < 60 || frame.Height < 20)
        {
            _console.Write(new Text(_text.Text("Form.TooSmall"), Style.Parse(TerminalTheme.Warning)));
            return;
        }
        var height = frame.Height - 1;
        var width = frame.Width - 1;
        var bodyRows = height - FullscreenHeader.Height - FullscreenFooter.Height();
        var contentWidth = width - FullscreenWorkspace.SidebarWidth(frame.Width) - 4;
        var entry = entries[_navigator.SelectedIndex];
        _navigator.IsFocused = _focus == EditorFocus.Sections;
        var renderOptions = new RenderOptions(_console.Profile.Capabilities, new Size(width, height));
        var details = Details(entry, memories, proposals);
        var lines = Segment.SplitLines(details.Render(renderOptions, contentWidth)).ToArray();
        _lineCount = lines.Length;
        _visibleRows = Math.Max(1, bodyRows - 2);
        _detailOffset = Math.Clamp(_detailOffset, 0, Math.Max(0, _lineCount - _visibleRows));
        var body = Inset(new Rows(
            Line(PageTitle(entry, proposals), TerminalTheme.Accent),
            new Text(" "),
            new MemoryLines(lines.Skip(_detailOffset).Take(_visibleRows).ToArray())));
        var actions = Actions(entry, proposals);
        _actionIndex = Math.Clamp(_actionIndex, 0, actions.Count - 1);
        var buttons = new Grid();
        foreach (var unused in actions)
        {
            _ = unused;
            buttons.AddColumn();
        }
        buttons.AddRow(actions.Select((action, index) => FullscreenFooter.Button(
            _text.Text(action.LabelKey), action.Color, _focus == EditorFocus.Actions && index == _actionIndex)).ToArray());
        var notice = Notice(entry, proposals, feedback, feedbackIsError);
        var hintKey = _editing ? "MemoryManager.EditorKeys"
            : _focus == EditorFocus.Sections ? "Form.SectionsFooter"
            : _focus == EditorFocus.Editor ? "MemoryManager.DetailKeys"
            : "MemoryManager.ActionKeys";
        var footer = FullscreenFooter.Create(
            Line(notice.Text, notice.Color),
            buttons,
            FullscreenFooter.Shortcuts(_text.Text(hintKey)));
        _console.Write(FullscreenWorkspace.Create(_text.Text("Settings.Memories"), _shell.Options, frame.Width,
            body, _navigator.Render(entries, NavigationLabel, bodyRows, _text.Text("Form.Sections"),
                _console.Profile.Capabilities.Unicode), footer, FullscreenFooter.NoticeRows));
    }

    /// <summary>Builds stable create, suggestions, and saved-note entries for the numbered sidebar.</summary>
    private IReadOnlyList<NavigationEntry> Entries(IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals) =>
    [
        new(NavigationKind.Create, CreateKey, _text.Text("MemoryManager.Create"), "✨"),
        new(NavigationKind.Proposals, ProposalsKey, _text.Text("Lab.ReviewButton") + $" ({proposals.Proposals.Count})", "💭"),
        .. memories.Select(memory => new NavigationEntry(NavigationKind.Memory, MemoryKey(memory.Id),
            PreviewLabel(memory.Text), "📚", memory.Id))
    ];

    /// <summary>Applies shared icons and sanitization to one numbered sidebar entry.</summary>
    private string NavigationLabel(NavigationEntry entry) =>
        TerminalTheme.IconPrefix(_shell.Options, entry.Icon, "-") + SafeSingleLine(entry.Label);

    /// <summary>Resets page-local focus and scrolling after a numbered section changes.</summary>
    private void Activate(NavigationEntry entry, IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals)
    {
        _selectedKey = entry.Key;
        _focus = EditorFocus.Sections;
        _actionIndex = 0;
        _pending = null;
        _editing = false;
        _error = null;
        _detailOffset = 0;
        EnsureDraft(entry, memories, proposals);
    }

    /// <summary>Moves the shared numbered navigator and activates the newly selected page.</summary>
    private void MoveSection(int delta, IReadOnlyList<NavigationEntry> entries, IReadOnlyList<PersistentMemory> memories,
        MemoryProposalWorkspace proposals)
    {
        var before = _navigator.SelectedIndex;
        _navigator.Move(delta, entries.Count);
        if (_navigator.SelectedIndex != before)
        {
            Activate(entries[_navigator.SelectedIndex], memories, proposals);
        }
    }

    /// <summary>Initializes an editable draft without overwriting unsaved text retained during navigation.</summary>
    private void EnsureDraft(NavigationEntry entry, IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals)
    {
        if (entry.Kind == NavigationKind.Create)
        {
            _drafts.TryAdd(CreateKey, string.Empty);
            return;
        }
        if (entry.Kind == NavigationKind.Memory)
        {
            var memory = memories.Single(item => string.Equals(item.Id, entry.Id, StringComparison.Ordinal));
            _drafts.TryAdd(entry.Key, NormalizeEditorText(memory.Text));
            return;
        }
        if (CurrentProposal(proposals) is { } proposal)
        {
            _drafts.TryAdd(ProposalKey(proposal.Id), NormalizeEditorText(proposal.Text));
        }
    }

    /// <summary>Reconciles successful submissions with refreshed storage while retaining drafts after validation errors.</summary>
    private void ReconcileDrafts(IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals,
        string? feedback, bool feedbackIsError)
    {
        if (_submittedKey is not null && feedback is not null && !feedbackIsError)
        {
            _drafts.Remove(_submittedKey);
            _dirty.Remove(_submittedKey);
        }
        _submittedKey = null;
        foreach (var memory in memories)
        {
            var key = MemoryKey(memory.Id);
            if (!_dirty.Contains(key))
            {
                _drafts[key] = NormalizeEditorText(memory.Text);
            }
        }
        var validProposals = proposals.Proposals.Select(proposal => ProposalKey(proposal.Id)).ToHashSet(StringComparer.Ordinal);
        foreach (var key in _drafts.Keys.Where(key => key.StartsWith("proposal:", StringComparison.Ordinal) && !validProposals.Contains(key)).ToArray())
        {
            _drafts.Remove(key);
            _dirty.Remove(key);
        }
        _proposalIndex = Math.Clamp(_proposalIndex, 0, Math.Max(0, proposals.Proposals.Count - 1));
    }

    /// <summary>Returns contextual actions while destructive decisions remain in an inline confirmation state.</summary>
    private IReadOnlyList<EditorAction> Actions(NavigationEntry entry, MemoryProposalWorkspace proposals)
    {
        if (_pending is not null)
        {
            return
            [
                new(EditorCommand.Cancel, "Lab.Cancel", TerminalTheme.Warning),
                new(EditorCommand.Confirm, "Lab.Confirm", TerminalTheme.Error)
            ];
        }
        if (entry.Kind == NavigationKind.Create)
        {
            return
            [
                new(EditorCommand.Save, "Form.Save", TerminalTheme.Success),
                new(EditorCommand.Close, "Form.Cancel", TerminalTheme.Warning)
            ];
        }
        if (entry.Kind == NavigationKind.Memory)
        {
            return
            [
                new(EditorCommand.Save, "Form.Save", TerminalTheme.Success),
                new(EditorCommand.Delete, "MemoryManager.Delete", TerminalTheme.Error),
                new(EditorCommand.Close, "Form.Cancel", TerminalTheme.Warning)
            ];
        }
        var proposal = CurrentProposal(proposals);
        if (!proposals.Enabled || proposal is null)
        {
            return [new(EditorCommand.Close, "Form.Cancel", TerminalTheme.Warning)];
        }
        var actions = new List<EditorAction>();
        if (proposal.Operation != "flag")
        {
            actions.Add(new(EditorCommand.Approve, "Lab.Approve", TerminalTheme.Success));
        }
        actions.Add(new(EditorCommand.Reject, "Lab.Reject", TerminalTheme.Error));
        actions.Add(new(EditorCommand.Close, "Form.Cancel", TerminalTheme.Warning));
        return actions;
    }

    /// <summary>Applies a central action or enters its explicit inline confirmation state.</summary>
    private MemoryManagerSelection? ApplyAction(EditorAction action, NavigationEntry entry,
        IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals)
    {
        _error = null;
        switch (action.Command)
        {
            case EditorCommand.Cancel:
                _pending = null;
                _actionIndex = 0;
                return null;
            case EditorCommand.Close:
                return new(MemoryManagerAction.Close);
            case EditorCommand.Delete:
                _pending = MemoryManagerAction.Delete;
                _actionIndex = 0;
                return null;
            case EditorCommand.Approve:
                _pending = MemoryManagerAction.ApproveProposal;
                _actionIndex = 0;
                return null;
            case EditorCommand.Reject:
                _pending = MemoryManagerAction.RejectProposal;
                _actionIndex = 0;
                return null;
            case EditorCommand.Save:
                var draft = CurrentDraft(entry, memories, proposals);
                if (ValidateNote(draft) is { } error)
                {
                    _error = error;
                    return null;
                }
                _submittedKey = entry.Key;
                return new(entry.Kind == NavigationKind.Create ? MemoryManagerAction.Create : MemoryManagerAction.Edit,
                    entry.Id, draft);
            case EditorCommand.Confirm:
                if (_pending == MemoryManagerAction.Delete)
                {
                    return new(MemoryManagerAction.Delete, entry.Id);
                }
                var proposal = CurrentProposal(proposals) ?? throw new InvalidOperationException(_text.Text("Lab.Invalid"));
                var result = new MemoryManagerSelection(_pending
                    ?? throw new InvalidOperationException(_text.Text("Lab.Invalid")), proposal.Id,
                    CurrentDraft(entry, memories, proposals));
                _submittedKey = ProposalKey(proposal.Id);
                return result;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    /// <summary>Starts direct editing for a saved note, new note, or editable proposal.</summary>
    private void BeginEdit(NavigationEntry entry, IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals)
    {
        if (!CanEdit(entry, proposals))
        {
            _focus = EditorFocus.Actions;
            return;
        }
        _input = CurrentDraft(entry, memories, proposals);
        _caret = _input.Length;
        _editing = true;
        _error = null;
    }

    /// <summary>Edits multiline text in place; Ctrl+Enter accepts the field and Escape discards the current edit.</summary>
    private void Edit(NavigationEntry entry, IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals,
        ConsoleKeyInfo key)
    {
        _ = memories;
        _error = null;
        if (key.Key == ConsoleKey.Escape)
        {
            _editing = false;
            _input = string.Empty;
            return;
        }
        if (key.Key == ConsoleKey.Enter && (key.Modifiers & ConsoleModifiers.Control) != 0)
        {
            if (ValidateEditorText(entry, _input) is { } error)
            {
                _error = error;
                return;
            }
            var keyName = DraftKey(entry, proposals);
            _drafts[keyName] = _input;
            _dirty.Add(keyName);
            _editing = false;
            _input = string.Empty;
            _detailOffset = 0;
            return;
        }
        if (key.Key == ConsoleKey.Enter)
        {
            Insert("\n");
        }
        else if (key.Key == ConsoleKey.U && (key.Modifiers & ConsoleModifiers.Control) != 0)
        {
            _input = string.Empty;
            _caret = 0;
        }
        else if (key.Key == ConsoleKey.Home)
        {
            _caret = _input.LastIndexOf('\n', Math.Max(0, _caret - 1)) + 1;
        }
        else if (key.Key == ConsoleKey.End)
        {
            var end = _input.IndexOf('\n', _caret);
            _caret = end < 0 ? _input.Length : end;
        }
        else if (key.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow)
        {
            MoveCaretVertically(key.Key == ConsoleKey.UpArrow ? -1 : 1);
        }
        else if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.Backspace && _caret > 0)
        {
            var previous = PreviousElement(_input, _caret);
            if (key.Key == ConsoleKey.Backspace)
            {
                _input = _input.Remove(previous, _caret - previous);
            }
            _caret = previous;
        }
        else if (key.Key is ConsoleKey.RightArrow or ConsoleKey.Delete && _caret < _input.Length)
        {
            var next = NextElement(_input, _caret);
            if (key.Key == ConsoleKey.Delete)
            {
                _input = _input.Remove(_caret, next - _caret);
            }
            else
            {
                _caret = next;
            }
        }
        else if (!char.IsControl(key.KeyChar) && (key.Modifiers & ConsoleModifiers.Control) == 0)
        {
            Insert(key.KeyChar.ToString());
        }
    }

    /// <summary>Inserts bounded editor text without splitting the current caret position.</summary>
    private void Insert(string value)
    {
        if (_input.Length + value.Length > PersistentMemoryService.MaximumCharacters)
        {
            _error = _text.Text("Memory.TooLong", PersistentMemoryService.MaximumCharacters);
            return;
        }
        _input = _input.Insert(_caret, value);
        _caret += value.Length;
    }

    /// <summary>Moves the caret to the closest column on the adjacent logical line.</summary>
    private void MoveCaretVertically(int delta)
    {
        var lineStart = _input.LastIndexOf('\n', Math.Max(0, _caret - 1)) + 1;
        var column = _caret - lineStart;
        if (delta < 0)
        {
            if (lineStart == 0)
            {
                return;
            }
            var previousEnd = lineStart - 1;
            var previousStart = _input.LastIndexOf('\n', Math.Max(0, previousEnd - 1)) + 1;
            _caret = Math.Min(previousStart + column, previousEnd);
            return;
        }
        var currentEnd = _input.IndexOf('\n', _caret);
        if (currentEnd < 0)
        {
            return;
        }
        var nextStart = currentEnd + 1;
        var nextEnd = _input.IndexOf('\n', nextStart);
        _caret = Math.Min(nextStart + column, nextEnd < 0 ? _input.Length : nextEnd);
    }

    /// <summary>Finds the preceding Unicode text element for cursor movement and deletion.</summary>
    private static int PreviousElement(string value, int index) =>
        StringInfo.ParseCombiningCharacters(value).LastOrDefault(start => start < index);

    /// <summary>Finds the following Unicode text element without splitting a composed character.</summary>
    private static int NextElement(string value, int index) =>
        StringInfo.ParseCombiningCharacters(value).FirstOrDefault(start => start > index, value.Length);

    /// <summary>Changes the proposal shown in the central editor without leaving the suggestions section.</summary>
    private void ChangeProposal(int delta, MemoryProposalWorkspace proposals)
    {
        if (proposals.Proposals.Count == 0)
        {
            return;
        }
        _proposalIndex = (_proposalIndex + delta + proposals.Proposals.Count) % proposals.Proposals.Count;
        _pending = null;
        _actionIndex = 0;
        _detailOffset = 0;
        _error = null;
        var proposal = proposals.Proposals[_proposalIndex];
        _drafts.TryAdd(ProposalKey(proposal.Id), NormalizeEditorText(proposal.Text));
    }

    /// <summary>Builds the editable note or complete proposal review shown in the main panel.</summary>
    private IRenderable Details(NavigationEntry entry, IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals)
    {
        if (entry.Kind == NavigationKind.Proposals)
        {
            return ProposalDetails(proposals);
        }
        var rows = new List<IRenderable>();
        if (entry.Kind == NavigationKind.Memory)
        {
            var memory = memories.Single(item => string.Equals(item.Id, entry.Id, StringComparison.Ordinal));
            var metadata = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn(new GridColumn().LeftAligned());
            AddPair(metadata, "ID", memory.Id);
            AddPair(metadata, _text.Text("MemoryManager.Updated"), memory.UpdatedAt.ToLocalTime().ToString("g", _text.Culture));
            rows.Add(metadata);
        }
        var draft = _editing ? _input : CurrentDraft(entry, memories, proposals);
        rows.Add(Line((_focus == EditorFocus.Editor ? "> " : string.Empty) + _text.Text("Memory.Note"), TerminalTheme.Accent));
        rows.Add(new Text(" "));
        rows.Add(new EditorInput(width => EditorValue(draft, width),
            Style.Parse((_focus == EditorFocus.Editor ? "bold underline " : string.Empty) + TerminalTheme.FieldValue)));
        rows.Add(new Text(" "));
        rows.Add(new Text(SafeText(draft), Style.Parse(TerminalTheme.Primary)));
        return new Rows(rows);
    }

    /// <summary>Shows operation, before and after text, rationale, and exact local evidence for one suggestion.</summary>
    private IRenderable ProposalDetails(MemoryProposalWorkspace workspace)
    {
        if (!workspace.Enabled)
        {
            return new Text(_text.Text("Lab.Disabled"), Style.Parse(TerminalTheme.Primary));
        }
        var proposal = CurrentProposal(workspace);
        if (proposal is null)
        {
            return new Text(_text.Text("Lab.None"), Style.Parse(TerminalTheme.Primary));
        }
        var reviewed = _editing ? _input : _drafts.GetValueOrDefault(ProposalKey(proposal.Id), NormalizeEditorText(proposal.Text));
        var before = proposal.Targets.Count == 0 ? _text.Text("Lab.NoPrevious")
            : string.Join("\n\n", proposal.Targets.Select(target => target.Id + " · "
                + target.UpdatedAt.ToString("u", CultureInfo.InvariantCulture) + "\n" + target.Text));
        var sourceText = proposal.SourceIds.Count == 0
            ? string.Join("\n", proposal.Targets.Select(target => target.Id))
            : string.Join("\n\n", proposal.SourceIds.Select(id => workspace.Evidence
                .SingleOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal)) is { } observation
                    ? ObservationText(observation)
                    : id));
        var metadata = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn(new GridColumn().LeftAligned());
        AddPair(metadata, _text.Text("Lab.Operation"), _text.Text("Lab.Operation." + proposal.Operation));
        AddPair(metadata, _text.Text("Lab.Kind"), _text.Text("Lab.Kind." + proposal.Kind));
        var rows = new List<IRenderable>
        {
            metadata,
            Line(_text.Text("Lab.Before"), TerminalTheme.Accent),
            new Text(SafeText(before), Style.Parse(TerminalTheme.Primary)),
            new Text(" "),
            Line((_focus == EditorFocus.Editor ? "> " : string.Empty) + _text.Text("Lab.After"), TerminalTheme.Accent),
            new EditorInput(width => EditorValue(proposal.Operation == "archive" ? _text.Text("Lab.Archived")
                : proposal.Operation == "flag" ? _text.Text("Lab.Unchanged") : reviewed, width),
                Style.Parse((CanEditProposal(proposal) && _focus == EditorFocus.Editor ? "bold underline " : string.Empty)
                    + TerminalTheme.FieldValue)),
            new Text(" "),
            new Text(SafeText(proposal.Operation is "archive" or "flag" ? proposal.Text : reviewed), Style.Parse(TerminalTheme.Primary)),
            new Text(" "),
            Line(_text.Text("Lab.Reason"), TerminalTheme.Accent),
            new Text(SafeText(proposal.Rationale), Style.Parse(TerminalTheme.Primary)),
            new Text(" "),
            Line(_text.Text("Lab.Sources"), TerminalTheme.Accent),
            new Text(SafeText(sourceText), Style.Parse(TerminalTheme.Primary))
        };
        if (proposal.Operation is "merge" or "archive")
        {
            rows.Add(new Text(" "));
            rows.Add(new Text(_text.Text("Lab.ForgetNotice"), Style.Parse(TerminalTheme.Warning)));
        }
        if (proposal.Operation == "flag")
        {
            rows.Add(new Text(" "));
            rows.Add(new Text(_text.Text("Lab.FlagNotice"), Style.Parse(TerminalTheme.Warning)));
        }
        return new Rows(rows);
    }

    /// <summary>Resolves the page heading for the selected numbered section.</summary>
    private string PageTitle(NavigationEntry entry, MemoryProposalWorkspace proposals) => entry.Kind switch
    {
        NavigationKind.Create => _text.Text("MemoryManager.Create"),
        NavigationKind.Proposals => _text.Text("Lab.Proposals") + $" ({proposals.Proposals.Count})",
        _ => _text.Text("Memory.Note")
    };

    /// <summary>Chooses a stable footer notice from validation, confirmation, persistence, or page guidance.</summary>
    private (string Text, string Color) Notice(NavigationEntry entry, MemoryProposalWorkspace proposals,
        string? feedback, bool feedbackIsError)
    {
        if (_error is not null)
        {
            return (_error, TerminalTheme.Error);
        }
        if (_pending is not null)
        {
            var warning = _pending == MemoryManagerAction.Delete
                ? _text.Text("Lab.ForgetNotice") + " " + _text.Text("MemoryManager.DeleteConfirm")
                : _text.Text(_pending == MemoryManagerAction.ApproveProposal ? "Lab.Approve" : "Lab.Reject");
            return (warning, TerminalTheme.Warning);
        }
        if (feedback is not null)
        {
            return (feedback, feedbackIsError ? TerminalTheme.Error : TerminalTheme.Success);
        }
        if (entry.Kind == NavigationKind.Proposals && !proposals.Enabled)
        {
            return (_text.Text("Lab.Disabled"), TerminalTheme.Muted);
        }
        if (entry.Kind == NavigationKind.Proposals && proposals.Proposals.Count == 0)
        {
            return (_text.Text("Lab.None"), TerminalTheme.Muted);
        }
        return (_text.Text(entry.Kind == NavigationKind.Proposals ? "Lab.Proposals" : "MemoryManager.NoteHelp"), TerminalTheme.Muted);
    }

    /// <summary>Returns the current draft for the selected note or proposal.</summary>
    private string CurrentDraft(NavigationEntry entry, IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals)
    {
        EnsureDraft(entry, memories, proposals);
        return _drafts.GetValueOrDefault(DraftKey(entry, proposals), string.Empty);
    }

    /// <summary>Maps a selected editor page to its stable draft key.</summary>
    private string DraftKey(NavigationEntry entry, MemoryProposalWorkspace proposals) => entry.Kind switch
    {
        NavigationKind.Create => CreateKey,
        NavigationKind.Memory => entry.Key,
        _ => ProposalKey(CurrentProposal(proposals)?.Id ?? string.Empty)
    };

    /// <summary>Allows text changes only for new notes, saved notes, and add or merge suggestions.</summary>
    private bool CanEdit(NavigationEntry entry, MemoryProposalWorkspace proposals) =>
        entry.Kind != NavigationKind.Proposals || CurrentProposal(proposals) is { } proposal && CanEditProposal(proposal);

    /// <summary>Limits proposal text editing to operations supported by the existing review workflow.</summary>
    private static bool CanEditProposal(MemoryProposal proposal) => proposal.Operation is "add" or "merge";

    /// <summary>Gets the currently selected suggestion without manufacturing an empty proposal.</summary>
    private MemoryProposal? CurrentProposal(MemoryProposalWorkspace proposals) => proposals.Proposals.Count == 0
        ? null
        : proposals.Proposals[Math.Clamp(_proposalIndex, 0, proposals.Proposals.Count - 1)];

    /// <summary>Checks local note shape before authoritative persistence validation.</summary>
    private string? ValidateNote(string value) => string.IsNullOrWhiteSpace(value) ? _text.Text("Memory.Empty")
        : value.Trim().Length > PersistentMemoryService.MaximumCharacters
            ? _text.Text("Memory.TooLong", PersistentMemoryService.MaximumCharacters)
            : null;

    /// <summary>Validates editable central text according to the selected page.</summary>
    private string? ValidateEditorText(NavigationEntry entry, string value) => entry.Kind == NavigationKind.Proposals
        ? string.IsNullOrWhiteSpace(value) ? _text.Text("Lab.Invalid")
            : value.Trim().Length > PersistentMemoryService.MaximumCharacters
                ? _text.Text("Memory.TooLong", PersistentMemoryService.MaximumCharacters)
                : null
        : ValidateNote(value);

    /// <summary>Fits the active editor around its caret or flattens the passive textbox value.</summary>
    private string EditorValue(string value, int width)
    {
        if (!_editing)
        {
            return SafeSingleLine(value);
        }
        if (width < 3)
        {
            return "|";
        }
        var sourceCaret = Math.Clamp(_caret, 0, _input.Length);
        var beforeCaret = SafeSingleLine(_input[..sourceCaret]);
        var afterCaret = SafeSingleLine(_input[sourceCaret..]);
        var safe = beforeCaret + afterCaret;
        var caret = beforeCaret.Length;
        var budget = Math.Max(1, width - 3);
        var start = Math.Max(0, caret - budget / 2);
        var end = Math.Min(safe.Length, start + budget);
        start = Math.Max(0, end - budget);
        return (start > 0 ? "…" : string.Empty) + safe[start..Math.Min(caret, end)] + "|"
            + safe[Math.Min(caret, end)..end] + (end < safe.Length ? "…" : string.Empty);
    }

    /// <summary>Offers the same actions through bounded prompts when a fullscreen editor is unavailable.</summary>
    private MemoryManagerSelection ChooseScrolling(IReadOnlyList<PersistentMemory> memories, MemoryProposalWorkspace proposals,
        string? feedback, bool feedbackIsError)
    {
        if (feedback is not null)
        {
            _console.Write(new Text(feedback, Style.Parse(feedbackIsError ? TerminalTheme.Error : TerminalTheme.Success)));
            _console.WriteLine();
        }
        while (true)
        {
            TerminalTheme.WriteSection(_console, _text.Text("Settings.Memories"), _text.Text("MemoryManager.Help"));
            var choices = new List<MenuChoice>
            {
                new(CreateKey, _text.Text("MemoryManager.Create")),
                new(ProposalsKey, _text.Text("Lab.ReviewButton") + $" ({proposals.Proposals.Count})")
            };
            choices.AddRange(memories.Select(memory => new MenuChoice(MemoryKey(memory.Id), PreviewLabel(memory.Text))));
            choices.Add(new("close", _text.Text("Help.Browse.Close")));
            var selected = Prompt(_text.Text("MemoryManager.Choose"), choices);
            if (selected.Id == "close")
            {
                return new(MemoryManagerAction.Close);
            }
            if (selected.Id == CreateKey)
            {
                var value = ReadNoteScrolling(string.Empty);
                if (value is not null)
                {
                    return new(MemoryManagerAction.Create, Text: value);
                }
                continue;
            }
            if (selected.Id == ProposalsKey)
            {
                if (ChooseProposalScrolling(proposals) is { } proposalSelection)
                {
                    return proposalSelection;
                }
                continue;
            }
            var id = selected.Id["memory:".Length..];
            var memory = memories.Single(item => string.Equals(item.Id, id, StringComparison.Ordinal));
            _console.Write(Details(new(NavigationKind.Memory, selected.Id, memory.Text, "📚", id), memories, proposals));
            _console.WriteLine();
            var action = Prompt(_text.Text("MemoryManager.Choose"),
                [new("back", _text.Text("Form.Back")), new("edit", _text.Text("MemoryManager.Edit")), new("delete", _text.Text("MemoryManager.Delete"))]);
            if (action.Id == "edit")
            {
                var value = ReadNoteScrolling(NormalizeEditorText(memory.Text));
                if (value is not null)
                {
                    return new(MemoryManagerAction.Edit, memory.Id, value);
                }
            }
            else if (action.Id == "delete" && Prompt(_text.Text("MemoryManager.DeleteConfirm"),
                [new("cancel", _text.Text("Lab.Cancel")), new("confirm", _text.Text("Lab.Confirm"))]).Id == "confirm")
            {
                return new(MemoryManagerAction.Delete, memory.Id);
            }
        }
    }

    /// <summary>Reviews one suggestion in compact terminals while keeping approval and rejection explicit.</summary>
    private MemoryManagerSelection? ChooseProposalScrolling(MemoryProposalWorkspace workspace)
    {
        if (!workspace.Enabled || workspace.Proposals.Count == 0)
        {
            _console.Write(new Text(_text.Text(workspace.Enabled ? "Lab.None" : "Lab.Disabled"), Style.Parse(TerminalTheme.Muted)));
            _console.WriteLine();
            return null;
        }
        var choices = workspace.Proposals.Select(proposal => new MenuChoice(proposal.Id,
            _text.Text("Lab.Operation." + proposal.Operation) + " — " + PreviewLabel(proposal.Text)))
            .Prepend(new("back", _text.Text("Form.Back"))).ToArray();
        var selected = Prompt(_text.Text("Lab.Proposals"), choices);
        if (selected.Id == "back")
        {
            return null;
        }
        var proposal = workspace.Proposals.Single(item => string.Equals(item.Id, selected.Id, StringComparison.Ordinal));
        _proposalIndex = workspace.Proposals.ToList().IndexOf(proposal);
        _drafts.TryAdd(ProposalKey(proposal.Id), NormalizeEditorText(proposal.Text));
        _console.Write(ProposalDetails(workspace));
        _console.WriteLine();
        var actions = new List<MenuChoice> { new("back", _text.Text("Form.Back")), new("reject", _text.Text("Lab.Reject")) };
        if (CanEditProposal(proposal))
        {
            actions.Add(new("edit", _text.Text("Lab.Edit")));
        }
        if (proposal.Operation != "flag")
        {
            actions.Add(new("approve", _text.Text("Lab.Approve")));
        }
        var action = Prompt(_text.Text("MemoryManager.Choose"), actions);
        if (action.Id == "edit")
        {
            var edited = ReadNoteScrolling(_drafts[ProposalKey(proposal.Id)]);
            if (edited is null)
            {
                return null;
            }
            _drafts[ProposalKey(proposal.Id)] = edited;
            action = Prompt(_text.Text("MemoryManager.Choose"),
                [new("back", _text.Text("Form.Back")), new("reject", _text.Text("Lab.Reject")), new("approve", _text.Text("Lab.Approve"))]);
        }
        if (action.Id is not ("approve" or "reject"))
        {
            return null;
        }
        if (Prompt(_text.Text(action.Id == "approve" ? "Lab.Approve" : "Lab.Reject"),
            [new("cancel", _text.Text("Lab.Cancel")), new("confirm", _text.Text("Lab.Confirm"))]).Id != "confirm")
        {
            return null;
        }
        return new(action.Id == "approve" ? MemoryManagerAction.ApproveProposal : MemoryManagerAction.RejectProposal,
            proposal.Id, _drafts[ProposalKey(proposal.Id)]);
    }

    /// <summary>Reads one bounded replacement in compact terminals without interpreting markup.</summary>
    private string? ReadNoteScrolling(string current)
    {
        _console.Write(new Text(_text.Text("MemoryManager.ReplaceHelp"), Style.Parse(TerminalTheme.Muted)));
        _console.WriteLine();
        var value = _console.Prompt(new TextPrompt<string>(Markup.Escape(_text.Text("Memory.Note"))).AllowEmpty()
            .Validate(candidate => candidate.Length > PersistentMemoryService.MaximumCharacters
                ? ValidationResult.Error(Markup.Escape(_text.Text("Memory.TooLong", PersistentMemoryService.MaximumCharacters)))
                : ValidationResult.Success()));
        if (value.Length == 0)
        {
            return current.Length == 0 ? null : current;
        }
        return value;
    }

    /// <summary>Creates a localized, high-contrast selection menu without interpreting stored note markup.</summary>
    private MenuChoice Prompt(string title, IReadOnlyList<MenuChoice> choices) => _console.Prompt(new SelectionPrompt<MenuChoice>()
        .Title(Markup.Escape(title)).PageSize(Math.Clamp(_console.Profile.Height - 6, 3, 10))
        .MoreChoicesText(Markup.Escape(_text.Text("MemoryManager.More")))
        .HighlightStyle(Style.Parse($"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"))
        .UseConverter(choice => Markup.Escape(choice.Label)).AddChoices(choices));

    /// <summary>Keeps sidebar previews concise while the complete content remains in the editor.</summary>
    private static string PreviewLabel(string value)
    {
        var flattened = SafeSingleLine(value);
        return flattened.Length <= 32 ? flattened : flattened[..31] + "…";
    }

    /// <summary>Adds one right-aligned label and left-aligned value followed by a blank row.</summary>
    private static void AddPair(Grid grid, string label, string value)
    {
        grid.AddRow(new Text(label, Style.Parse(TerminalTheme.Muted)), new Text(SafeText(value), Style.Parse(TerminalTheme.FieldValue)));
        grid.AddRow(new Text(" "), new Text(" "));
    }

    /// <summary>Formats exact observation metadata and text for proposal evidence.</summary>
    private static string ObservationText(LearningObservation observation) =>
        observation.Id + " · " + observation.SessionId + " · " + observation.CreatedAt.ToString("u", CultureInfo.InvariantCulture)
        + "\n" + observation.Text;

    /// <summary>Builds a stable saved-memory draft key.</summary>
    private static string MemoryKey(string id) => "memory:" + id;

    /// <summary>Builds a stable suggested-memory draft key.</summary>
    private static string ProposalKey(string id) => "proposal:" + id;

    /// <summary>Uses one line-ending convention so caret movement never splits a newline pair.</summary>
    private static string NormalizeEditorText(string value) => value.ReplaceLineEndings("\n");

    /// <summary>Preserves line breaks while removing unsafe terminal controls.</summary>
    private static string SafeText(string value) => new(value.ReplaceLineEndings("\n")
        .Select(character => char.IsControl(character) && character != '\n' ? ' ' : character).ToArray());

    /// <summary>Flattens text for sidebar and textbox rows while removing unsafe controls.</summary>
    private static string SafeSingleLine(string value) => SafeText(value).ReplaceLineEndings(" ↵ ");

    /// <summary>Matches the shared horizontal content margins.</summary>
    private static Padder Inset(IRenderable content) => new(content, new Padding(2, 0, 2, 0));

    /// <summary>Creates a clipped single-row label with an optional focus background.</summary>
    private static MemoryLine Line(string value, string foreground, string? background = null) =>
        new(value, Style.Parse(background is null ? foreground : $"{foreground} on {background}"));

    private enum NavigationKind
    {
        Create,
        Proposals,
        Memory
    }

    private enum EditorFocus
    {
        Sections,
        Editor,
        Actions
    }

    private enum EditorCommand
    {
        Save,
        Delete,
        Approve,
        Reject,
        Cancel,
        Close,
        Confirm
    }

    private sealed record NavigationEntry(NavigationKind Kind, string Key, string Label, string Icon, string? Id = null);

    private sealed record EditorAction(EditorCommand Command, string LabelKey, string Color);

    private sealed record MenuChoice(string Id, string Label);

    /// <summary>Renders a bracketed textbox whose active value follows the caret window.</summary>
    private sealed class EditorInput(Func<int, string> value, Style style) : IRenderable
    {
        /// <summary>Accepts the full central editor width.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Max(0, maxWidth));

        /// <summary>Fits the current textbox value between visible brackets.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            if (maxWidth <= 0)
            {
                return [];
            }
            var content = "[ " + value(Math.Max(0, maxWidth - 4)) + " ]";
            return Segment.SplitOverflow(new Segment(content, style), Overflow.Ellipsis, maxWidth);
        }
    }

    /// <summary>Clips sidebar and heading labels without wrapping into neighboring controls.</summary>
    private sealed class MemoryLine(string value, Style style) : IRenderable
    {
        /// <summary>Accepts the assigned width without requiring text wrapping.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Min(new Segment(value).CellCount(), Math.Max(0, maxWidth)));

        /// <summary>Preserves complete terminal cells while indicating clipped text.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
            maxWidth > 0 ? Segment.SplitOverflow(new Segment(value, style), Overflow.Ellipsis, maxWidth) : [];
    }

    /// <summary>Shows the scrollable slice of the complete central editor content.</summary>
    private sealed class MemoryLines(IReadOnlyList<SegmentLine> lines) : IRenderable
    {
        /// <summary>Lets the shared workspace assign the editor width.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Max(0, maxWidth));

        /// <summary>Retains styles and line boundaries for the visible editor rows.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            foreach (var line in lines)
            {
                foreach (var segment in Segment.Truncate(line, maxWidth))
                {
                    yield return segment;
                }
                yield return Segment.LineBreak;
            }
        }
    }
}

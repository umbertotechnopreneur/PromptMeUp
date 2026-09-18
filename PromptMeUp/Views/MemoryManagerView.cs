// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public interface IMemoryManagerView
{
    MemoryManagerSelection Choose(IReadOnlyList<PersistentMemory> memories);
    MemoryDraft? Edit(PersistentMemory? memory, MemoryDraft? draft = null, string? error = null);
    bool ConfirmDelete(PersistentMemory memory);
}

/// <summary>Browses and edits local memory drafts without owning persistence or project discovery.</summary>
public sealed class MemoryManagerView : IMemoryManagerView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private string? _selectedId;
    private int _selected;
    private int _focus;
    private int _offset;
    private int _lineCount;
    private int _visibleRows;
    private (int Width, int Height, string Theme)? _lastFrame;

    /// <summary>Creates a passive memory workspace using the shared terminal palette and input.</summary>
    public MemoryManagerView(IAnsiConsole console, ILocalizationService text, IConsoleShellView shell)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    /// <summary>Returns one explicit action while preserving the selected note and terminal scrollback.</summary>
    public MemoryManagerSelection Choose(IReadOnlyList<PersistentMemory> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);
        try
        {
            return FullscreenForm.CanUse(_console) ? ChooseFullscreen(memories, false) : ChooseScrolling(memories);
        }
        catch (InteractiveFlowCanceledException)
        {
            return new(MemoryManagerAction.Close);
        }
    }

    /// <summary>Edits a scope and bounded note in a disposable draft, retaining existing line breaks.</summary>
    public MemoryDraft? Edit(PersistentMemory? memory, MemoryDraft? draft = null, string? error = null)
    {
        var note = draft?.Text ?? memory?.Text ?? string.Empty;
        var global = draft?.IsGlobal ?? memory?.IsGlobal ?? false;
        var fields = new FormField[]
        {
            new("scope", "Memory.Scope", () => global ? "global" : "project", value => { global = value == "global"; error = null; })
            {
                Choices = () => [new("project", _text.Text("Memory.Project")), new("global", _text.Text("Memory.Global"))],
                Help = () => error ?? _text.Text("MemoryManager.ScopeHelp"),
                HelpKey = "MemoryManager.ScopeHelp"
            },
            new("note", "Memory.Note", () => note, value => { note = value; error = null; })
            {
                MaxLength = PersistentMemoryService.MaximumCharacters,
                Validate = ValidateNote,
                Help = () => error ?? _text.Text("MemoryManager.NoteHelp"),
                HelpKey = "MemoryManager.NoteHelp"
            }
        };
        var titleKey = memory is null ? "MemoryManager.Create" : "MemoryManager.Edit";
        var page = new FormPage(titleKey, fields)
        {
            HelpKey = "MemoryManager.EditHelp",
            Overview = () => error is null ? new Text(SafeText(note), Style.Parse(TerminalTheme.Primary))
                : new Rows(new Text(error, Style.Parse(TerminalTheme.Error)), new Text(" "), new Text(SafeText(note), Style.Parse(TerminalTheme.Primary)))
        };
        try
        {
            var saved = FullscreenForm.CanUse(_console)
                ? new FullscreenForm(_console, _text, _shell.Options).Run("Settings.Memories", [page], () => ValidateNote(note))
                : EditScrolling(page, () => error);
            return saved ? new(note, global) : null;
        }
        catch (InteractiveFlowCanceledException)
        {
            return null;
        }
    }

    /// <summary>Requires an explicit delete choice and defaults to cancellation in every terminal mode.</summary>
    public bool ConfirmDelete(PersistentMemory memory)
    {
        ArgumentNullException.ThrowIfNull(memory);
        try
        {
            if (FullscreenForm.CanUse(_console))
            {
                return ChooseFullscreen([memory], true).Action == MemoryManagerAction.Delete;
            }
            TerminalTheme.WriteRule(_console, _text.Text("MemoryManager.Delete"), TerminalTheme.Warning);
            _console.Write(Details(memory));
            _console.WriteLine();
            return Prompt(_text.Text("MemoryManager.DeleteConfirm"),
                [new("cancel", _text.Text("Form.Cancel")), new("delete", _text.Text("MemoryManager.Delete"))]).Id == "delete";
        }
        catch (InteractiveFlowCanceledException)
        {
            return false;
        }
    }

    /// <summary>Runs one workspace inside its own alternate buffer and restores the cursor on every exit.</summary>
    private MemoryManagerSelection ChooseFullscreen(IReadOnlyList<PersistentMemory> memories, bool deleting)
    {
        _selected = Math.Max(0, memories.ToList().FindIndex(memory => memory.Id == _selectedId));
        _focus = deleting || memories.Count == 0 ? 2 : 0;
        _offset = 0;
        _lastFrame = null;
        var result = new MemoryManagerSelection(MemoryManagerAction.Close);
        _console.AlternateScreen(() =>
        {
            _console.Cursor.Hide();
            try
            {
                result = RunLoop(memories, deleting);
            }
            finally
            {
                _console.WriteAnsi(writer => writer.ResetStyle());
                _console.Cursor.Show();
            }
        });
        return result;
    }

    /// <summary>Navigates the note list, complete detail, and explicit action buttons without nested prompts.</summary>
    private MemoryManagerSelection RunLoop(IReadOnlyList<PersistentMemory> memories, bool deleting)
    {
        var actions = Actions(memories.Count > 0, deleting);
        while (true)
        {
            void Paint() => PaintScreen(memories, actions, deleting);
            Paint();
            var key = ReadKey(Paint);
            if (key.Key is ConsoleKey.Escape)
            {
                return new(MemoryManagerAction.Close);
            }
            if (_console.Profile.Width < 60 || _console.Profile.Height < 20)
            {
                continue;
            }
            var direction = key.Key is ConsoleKey.UpArrow or ConsoleKey.PageUp or ConsoleKey.LeftArrow ? -1 : 1;
            switch (key.Key)
            {
                case ConsoleKey.Tab:
                    _focus = (_focus + ((key.Modifiers & ConsoleModifiers.Shift) != 0 ? -1 : 1) + actions.Length + 2) % (actions.Length + 2);
                    break;
                case ConsoleKey.F6:
                    _focus = _focus == 0 ? 1 : 0;
                    break;
                case ConsoleKey.Enter:
                    if (_focus < 2)
                    {
                        _focus = _focus == 0 ? 1 : 2;
                        break;
                    }
                    return new(actions[_focus - 2], memories.Count == 0 ? null : memories[_selected].Id);
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                    _focus = _focus < 2 ? (direction < 0 ? 0 : 1) : 2 + (_focus - 2 + direction + actions.Length) % actions.Length;
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                    if (_focus == 0 && memories.Count > 0)
                    {
                        _selected = Math.Clamp(_selected + direction, 0, memories.Count - 1);
                        _selectedId = memories[_selected].Id;
                        _offset = 0;
                    }
                    else if (_focus == 1)
                    {
                        var amount = key.Key is ConsoleKey.PageUp or ConsoleKey.PageDown ? _visibleRows : 1;
                        _offset = Math.Clamp(_offset + direction * amount, 0, Math.Max(0, _lineCount - _visibleRows));
                    }
                    else
                    {
                        _focus = 2 + (_focus - 2 + direction + actions.Length) % actions.Length;
                    }
                    break;
                case ConsoleKey.Home:
                case ConsoleKey.End:
                    if (_focus == 0 && memories.Count > 0)
                    {
                        _selected = key.Key == ConsoleKey.Home ? 0 : memories.Count - 1;
                        _selectedId = memories[_selected].Id;
                        _offset = 0;
                    }
                    else if (_focus == 1)
                    {
                        _offset = key.Key == ConsoleKey.Home ? 0 : Math.Max(0, _lineCount - _visibleRows);
                    }
                    break;
            }
        }
    }

    /// <summary>Draws an open list and scrollable full note using the shared workspace header and footer.</summary>
    private void PaintScreen(IReadOnlyList<PersistentMemory> memories, MemoryManagerAction[] actions, bool deleting)
    {
        var frame = (_console.Profile.Width, _console.Profile.Height, TerminalTheme.Current.Id);
        _console.WriteAnsi(writer =>
        {
            if (_lastFrame != frame)
            {
                // This erase is confined to the disposable alternate buffer.
                writer.Background(Style.Parse(TerminalTheme.Background).Foreground);
                writer.EraseInDisplay(2);
            }
            writer.CursorHome();
        });
        _lastFrame = frame;
        if (_console.Profile.Width < 60 || _console.Profile.Height < 20)
        {
            _console.Write(new FormSurface(new Text(_text.Text("Form.TooSmall"), Style.Parse(TerminalTheme.Warning))));
            return;
        }
        var height = _console.Profile.Height - 1;
        var width = _console.Profile.Width - 1;
        var bodyRows = height - FullscreenHeader.Height - FullscreenFooter.Height();
        _visibleRows = bodyRows - 2;
        var contentWidth = width - FullscreenWorkspace.SidebarWidth(_console.Profile.Width) - 4;
        IRenderable details = memories.Count == 0 ? new Text(_text.Text("Memory.None"), Style.Parse(TerminalTheme.Primary)) : Details(memories[_selected]);
        if (deleting)
        {
            details = new Rows(details, new Text(" "), new Text(_text.Text("Lab.ForgetNotice"), Style.Parse(TerminalTheme.Warning)));
        }
        var renderOptions = new RenderOptions(_console.Profile.Capabilities, new Size(width, height));
        var lines = Segment.SplitLines(details.Render(renderOptions, contentWidth)).ToArray();
        _lineCount = lines.Length;
        _offset = Math.Clamp(_offset, 0, Math.Max(0, _lineCount - _visibleRows));
        var body = Inset(new Rows(Line($"{(_focus == 1 ? "> " : string.Empty)}{_text.Text("Memory.Note")}", TerminalTheme.Accent),
            new Text(" "), new MemoryLines(lines.Skip(_offset).Take(_visibleRows).ToArray())));
        var buttons = new Grid();
        foreach (var action in actions)
        {
            buttons.AddColumn();
        }
        buttons.AddRow(actions.Select((action, index) => FullscreenFooter.Button(
            ActionLabel(action, deleting), ActionColor(action), _focus == index + 2)).ToArray());
        var notice = deleting ? _text.Text("MemoryManager.DeleteConfirm")
            : _text.Text("Help.Browse.Range", _lineCount == 0 ? 0 : _offset + 1, Math.Min(_lineCount, _offset + _visibleRows), _lineCount);
        var hintKey = _focus switch { 0 => "MemoryManager.ListKeys", 1 => "MemoryManager.DetailKeys", _ => "MemoryManager.ActionKeys" };
        if (new Segment(_text.Text(hintKey)).CellCount() > width - 4)
        {
            hintKey += "Compact";
        }
        var footer = FullscreenFooter.Create(Line(notice, deleting ? TerminalTheme.Warning : TerminalTheme.Muted), buttons,
            FullscreenFooter.Shortcuts(_text.Text(hintKey)));
        _console.Write(FullscreenWorkspace.Create(_text.Text("Settings.Memories"), _shell.Options, _console.Profile.Width,
            body, Navigation(memories, bodyRows), footer, FullscreenFooter.NoticeRows));
    }

    /// <summary>Keeps the active scope and note preview visible within a bounded left navigation column.</summary>
    private IRenderable Navigation(IReadOnlyList<PersistentMemory> memories, int bodyRows)
    {
        var capacity = Math.Max(1, (bodyRows - 2) / 2);
        var offset = Math.Clamp(_selected - capacity + 1, 0, Math.Max(0, memories.Count - capacity));
        var rows = new List<IRenderable> { Line(_text.Text("Settings.Memories") + $" ({memories.Count})", TerminalTheme.Accent), new Text(" ") };
        for (var index = offset; index < Math.Min(memories.Count, offset + capacity); index++)
        {
            var memory = memories[index];
            var selected = index == _selected;
            rows.Add(Line($"{(selected ? ">" : " ")} {_text.Text(memory.IsGlobal ? "Memory.Global" : "Memory.Project")}",
                selected && _focus == 0 ? TerminalTheme.SelectionForeground : selected ? TerminalTheme.Accent : TerminalTheme.Muted,
                selected && _focus == 0 ? TerminalTheme.SelectionBackground : null));
            rows.Add(Line("  " + SafeText(memory.Text).ReplaceLineEndings(" "), TerminalTheme.Primary));
        }
        return Inset(new Rows(rows));
    }

    /// <summary>Displays full note text and identifying metadata without interpreting embedded markup or terminal controls.</summary>
    private IRenderable Details(PersistentMemory memory)
    {
        var grid = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn(new GridColumn().LeftAligned());
        AddPair(grid, _text.Text("Memory.Scope"), _text.Text(memory.IsGlobal ? "Memory.Global" : "Memory.Project"));
        AddPair(grid, "ID", memory.Id);
        AddPair(grid, _text.Text("MemoryManager.Updated"), memory.UpdatedAt.ToLocalTime().ToString("g", _text.Culture));
        return new Rows(grid, new Text(SafeText(memory.Text), Style.Parse(TerminalTheme.Primary)));
    }

    /// <summary>Offers the same memory actions in a scrolling menu for compact or non-ANSI terminals.</summary>
    private MemoryManagerSelection ChooseScrolling(IReadOnlyList<PersistentMemory> memories)
    {
        while (true)
        {
            TerminalTheme.WriteSection(_console, _text.Text("Settings.Memories"), _text.Text("MemoryManager.Help"));
            if (memories.Count == 0)
            {
                _console.Write(new Text(_text.Text("Memory.None"), Style.Parse(TerminalTheme.Primary)));
                _console.WriteLine();
            }
            var choices = memories.Select(memory => new MenuChoice(memory.Id, PreviewLabel(memory)))
                .Prepend(new("create", _text.Text("MemoryManager.Create")))
                .Append(new("proposals", _text.Text("Lab.Proposals")))
                .Append(new("close", _text.Text("Help.Browse.Close"))).ToArray();
            var selected = Prompt(_text.Text("MemoryManager.Choose"), choices);
            if (selected.Id is "create" or "close" or "proposals")
            {
                return new(selected.Id switch
                {
                    "create" => MemoryManagerAction.Create,
                    "proposals" => MemoryManagerAction.Proposals,
                    _ => MemoryManagerAction.Close
                });
            }
            var memory = memories.First(item => item.Id == selected.Id);
            _selectedId = memory.Id;
            TerminalTheme.WriteRule(_console, _text.Text("Memory.Note"));
            _console.Write(Details(memory));
            _console.WriteLine();
            var action = Prompt(_text.Text("MemoryManager.Choose"),
                [new("back", _text.Text("Form.Back")), new("edit", _text.Text("MemoryManager.Edit")), new("delete", _text.Text("MemoryManager.Delete"))]);
            if (action.Id != "back")
            {
                return new(action.Id == "edit" ? MemoryManagerAction.Edit : MemoryManagerAction.Delete, memory.Id);
            }
        }
    }

    /// <summary>Keeps compact menu previews to one row while the selected note retains its complete detail.</summary>
    private string PreviewLabel(PersistentMemory memory)
    {
        var scope = _text.Text(memory.IsGlobal ? "Memory.Global" : "Memory.Project");
        var width = Math.Max(1, _console.Profile.Width - new Segment(scope).CellCount() - 6);
        var preview = new Segment(SafeText(memory.Text).ReplaceLineEndings(" "));
        return $"{scope} · {string.Concat(Segment.SplitOverflow(preview, Overflow.Ellipsis, width).Select(segment => segment.Text))}";
    }

    /// <summary>Edits the same draft fields with two-column rows and an explicit save action in compact terminals.</summary>
    private bool EditScrolling(FormPage page, Func<string?> readError)
    {
        while (true)
        {
            TerminalTheme.WriteRule(_console, _text.Text(page.TitleKey));
            var grid = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn(new GridColumn().LeftAligned());
            foreach (var field in page.Fields)
            {
                AddPair(grid, _text.Text(field.LabelKey), field.Choices?.Invoke().First(choice => choice.Value == field.Read()).Label ?? field.Read());
            }
            _console.Write(grid);
            if (readError() is { } validationError)
            {
                _console.Write(new Text(validationError, Style.Parse(TerminalTheme.Error)));
                _console.WriteLine();
            }
            _console.Write(new Rule { Style = Style.Parse(TerminalTheme.Divider) });
            var selected = Prompt(_text.Text("MemoryManager.EditHelp"), page.Fields.Select(field => new MenuChoice(field.Key, _text.Text(field.LabelKey)))
                .Append(new("save", _text.Text("Form.Save"))).Append(new("cancel", _text.Text("Form.Cancel"))).ToArray());
            if (selected.Id == "cancel")
            {
                return false;
            }
            if (selected.Id == "save")
            {
                var error = ValidateNote(page.Fields[1].Read());
                if (error is null)
                {
                    return true;
                }
                _console.Write(new Text(error, Style.Parse(TerminalTheme.Error)));
                _console.WriteLine();
                continue;
            }
            var active = page.Fields.First(field => field.Key == selected.Id);
            if (active.Choices is { } choices)
            {
                active.Write(Prompt(_text.Text(active.LabelKey), choices().OrderBy(choice => choice.Value == active.Read() ? 0 : 1)
                    .Select(choice => new MenuChoice(choice.Value, choice.Label)).ToArray()).Id);
            }
            else
            {
                _console.Write(new Text(_text.Text("MemoryManager.ReplaceHelp"), Style.Parse(TerminalTheme.Muted)));
                _console.WriteLine();
                var value = _console.Prompt(new TextPrompt<string>(Markup.Escape(_text.Text(active.LabelKey))).AllowEmpty()
                    .Validate(candidate => candidate.Length > active.MaxLength
                        ? ValidationResult.Error(Markup.Escape(_text.Text("Memory.TooLong", active.MaxLength))) : ValidationResult.Success()));
                if (value.Length > 0)
                {
                    active.Write(value);
                }
            }
        }
    }

    /// <summary>Creates a localized, high-contrast selection menu without interpreting stored note markup.</summary>
    private MenuChoice Prompt(string title, IReadOnlyList<MenuChoice> choices) => _console.Prompt(new SelectionPrompt<MenuChoice>()
        .Title(Markup.Escape(title)).PageSize(Math.Clamp(_console.Profile.Height - 6, 3, 10))
        .MoreChoicesText(Markup.Escape(_text.Text("MemoryManager.More")))
        .HighlightStyle(Style.Parse($"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"))
        .UseConverter(choice => Markup.Escape(choice.Label)).AddChoices(choices));

    /// <summary>Checks only local draft shape while persistence applies authoritative privacy and scope validation.</summary>
    private string? ValidateNote(string value) => string.IsNullOrWhiteSpace(value) ? _text.Text("Memory.Empty")
        : value.Trim().Length > PersistentMemoryService.MaximumCharacters ? _text.Text("Memory.TooLong", PersistentMemoryService.MaximumCharacters) : null;

    /// <summary>Limits actions to valid selections and places cancellation first in delete confirmation.</summary>
    private static MemoryManagerAction[] Actions(bool hasMemory, bool deleting) => deleting
        ? [MemoryManagerAction.Close, MemoryManagerAction.Delete]
        : hasMemory ? [MemoryManagerAction.Create, MemoryManagerAction.Edit, MemoryManagerAction.Delete, MemoryManagerAction.Proposals, MemoryManagerAction.Close]
        : [MemoryManagerAction.Create, MemoryManagerAction.Proposals, MemoryManagerAction.Close];

    /// <summary>Resolves concise action labels while distinguishing close from cancel during confirmation.</summary>
    private string ActionLabel(MemoryManagerAction action, bool deleting) => _text.Text(action switch
    {
        MemoryManagerAction.Create => "MemoryManager.Create",
        MemoryManagerAction.Edit => "MemoryManager.Edit",
        MemoryManagerAction.Delete => "MemoryManager.Delete",
        MemoryManagerAction.Proposals => "Lab.ReviewButton",
        MemoryManagerAction.Close => deleting ? "Form.Cancel" : "Help.Browse.Close",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    });

    /// <summary>Uses semantic action colors from the shared high-contrast palette.</summary>
    private static string ActionColor(MemoryManagerAction action) => action switch
    {
        MemoryManagerAction.Create => TerminalTheme.Success,
        MemoryManagerAction.Edit => TerminalTheme.Info,
        MemoryManagerAction.Proposals => TerminalTheme.Info,
        MemoryManagerAction.Delete => TerminalTheme.Error,
        _ => TerminalTheme.Warning
    };

    /// <summary>Waits for cancellable input while repainting only when terminal dimensions change.</summary>
    private ConsoleKeyInfo ReadKey(Action repaint)
    {
        var pending = ReadKeyAsync();
        var dimensions = (_console.Profile.Width, _console.Profile.Height);
        while (!pending.IsCompleted)
        {
            Task.WhenAny(pending, Task.Delay(100)).GetAwaiter().GetResult();
            var current = (_console.Profile.Width, _console.Profile.Height);
            if (current != dimensions)
            {
                dimensions = current;
                repaint();
            }
        }
        return pending.GetAwaiter().GetResult() ?? throw new IOException(_text.Text("Form.EndOfInput"));
    }

    /// <summary>Uses the host input wrapper to preserve application shutdown and flow cancellation.</summary>
    private async Task<ConsoleKeyInfo?> ReadKeyAsync() =>
        await _console.Input.ReadKeyAsync(true, CancellationToken.None).ConfigureAwait(false);

    /// <summary>Adds one right-aligned label and left-aligned value followed by a blank row.</summary>
    private static void AddPair(Grid grid, string label, string value)
    {
        grid.AddRow(new Text(label, Style.Parse(TerminalTheme.Muted)), new Text(SafeText(value), Style.Parse(TerminalTheme.FieldValue)));
        grid.AddRow(new Text(" "), new Text(" "));
    }

    /// <summary>Preserves note line breaks while replacing other terminal control characters for display only.</summary>
    private static string SafeText(string value) => new(value.ReplaceLineEndings("\n")
        .Select(character => char.IsControl(character) && character != '\n' ? ' ' : character).ToArray());

    /// <summary>Matches the open workspace's shared horizontal content margins.</summary>
    private static Padder Inset(IRenderable content) => new(content, new Padding(2, 0, 2, 0));

    /// <summary>Creates a single-row label with optional focus background.</summary>
    private static MemoryLine Line(string value, string foreground, string? background = null) =>
        new(value, Style.Parse(background is null ? foreground : $"{foreground} on {background}"));

    private sealed record MenuChoice(string Id, string Label);

    /// <summary>Clips preview and chrome labels without wrapping into neighboring controls.</summary>
    private sealed class MemoryLine(string value, Style style) : IRenderable
    {
        /// <summary>Accepts the assigned width without requiring text wrapping.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Min(new Segment(value).CellCount(), Math.Max(0, maxWidth)));

        /// <summary>Preserves complete terminal cells while indicating clipped preview text.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
            maxWidth > 0 ? Segment.SplitOverflow(new Segment(value, style), Overflow.Ellipsis, maxWidth) : [];
    }

    /// <summary>Shows only the current viewport of the complete, already wrapped note.</summary>
    private sealed class MemoryLines(IReadOnlyList<SegmentLine> lines) : IRenderable
    {
        /// <summary>Lets the containing workspace assign the note column's available width.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Max(0, maxWidth));

        /// <summary>Retains note styles and line boundaries without losing content outside the scroll position.</summary>
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

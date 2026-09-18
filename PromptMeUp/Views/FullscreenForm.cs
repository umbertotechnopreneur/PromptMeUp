// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

internal sealed record FormChoice(string Value, string Label);

internal sealed record FormField(string Key, string LabelKey, Func<string> Read, Action<string> Write)
{
    public Func<IReadOnlyList<FormChoice>>? Choices { get; init; }
    public Func<string, string?>? Validate { get; init; }
    public Func<bool>? IsVisible { get; init; }
    public Func<string>? Display { get; init; }
    public Func<string>? Help { get; init; }
    public Func<string>? Label { get; init; }
    public Func<string>? ValueColor { get; init; }
    public Func<IRenderable>? Overview { get; init; }
    public bool Secret { get; init; }
    public bool DefaultToCurrentValue { get; init; } = true;
    public int MaxLength { get; init; } = 100_000;
    public string? HelpKey { get; init; }
}

internal sealed record FormPage(string TitleKey, IReadOnlyList<FormField> Fields)
{
    public Action? Open { get; init; }
    public string? HelpKey { get; init; }
    public Func<IRenderable>? Preview { get; init; }
    public int PreviewRows { get; init; }
    public Func<IRenderable>? Overview { get; init; }
}

/// <summary>Owns keyboard navigation and rendering for temporary, passive terminal forms.</summary>
internal sealed class FullscreenForm(IAnsiConsole console, ILocalizationService text, ConsoleRenderOptions? options = null)
{
    private const int RowsPerField = 2;
    // Shared chrome, section heading, and the reserved terminal row, excluding the variable notice height.
    private const int FixedBodyRows = FullscreenHeader.Height + FullscreenFooter.ActionsRows + FullscreenFooter.HintRows + 3;
    private readonly ConsoleRenderOptions _options = options ?? new(false, false);
    private int _page;
    private int _focus;
    private bool _allowSectionNavigation;
    private bool _sectionsFocused;
    private string? _error;
    private FormField? _editing;
    private string _input = string.Empty;
    private int _caret;
    private int _messageHeight = FullscreenFooter.NoticeRows;
    private int _overviewOffset;
    private int _overviewMaximumOffset;
    private Func<IRenderable>? _overviewSource;
    private Action? _pendingOpen;
    private (int Width, int Height, string Theme)? _lastFrame;

    internal int SelectedPageIndex => _page;

    /// <summary>Checks terminal capabilities before opting into a fullscreen form.</summary>
    internal static bool CanUse(IAnsiConsole console) =>
        console.Profile.Capabilities.Ansi && console.Profile.Capabilities.AlternateBuffer
        && console.Profile.Capabilities.Interactive && console.Profile.Out.IsTerminal
        && !Console.IsInputRedirected && !Console.IsOutputRedirected
        && console.Profile.Width >= 60 && console.Profile.Height >= 20;

    /// <summary>Returns an explicitly saved draft while always restoring the original terminal buffer.</summary>
    internal bool Run(
        string titleKey,
        IReadOnlyList<FormPage> pages,
        Func<string?>? validate = null,
        int initialPage = 0)
    {
        ArgumentNullException.ThrowIfNull(pages);
        if (pages.Count == 0)
        {
            throw new ArgumentException("A fullscreen form needs at least one page.", nameof(pages));
        }
        ArgumentOutOfRangeException.ThrowIfNegative(initialPage);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(initialPage, pages.Count);
        if (!CanUse(console))
        {
            throw new InvalidOperationException(text.Text("Form.Unavailable"));
        }

        _page = initialPage;
        _allowSectionNavigation = pages.Count > 1;
        _sectionsFocused = _allowSectionNavigation;
        var saved = false;
        try
        {
            do
            {
                _pendingOpen = null;
                _lastFrame = null;
                console.AlternateScreen(() =>
                {
                    console.Cursor.Hide();
                    try
                    {
                        saved = RunLoop(titleKey, pages, validate);
                    }
                    finally
                    {
                        console.WriteAnsi(writer => writer.ResetStyle());
                        console.Cursor.Show();
                    }
                });
                // Child pages own their alternate buffer after this form restores the main buffer.
                _pendingOpen?.Invoke();
            }
            while (_pendingOpen is not null);
        }
        finally
        {
            _input = string.Empty;
            _editing = null;
        }
        return saved;
    }

    /// <summary>Processes one focused control at a time without nesting Spectre prompts or Live displays.</summary>
    private bool RunLoop(string titleKey, IReadOnlyList<FormPage> pages, Func<string?>? validate)
    {
        while (true)
        {
            var visiblePages = pages.Where(page => page.Open is not null || page.Overview is not null
                || page.Preview is not null || page.Fields.Any(IsVisible)).ToArray();
            if (visiblePages.Length == 0)
            {
                throw new InvalidOperationException("The form has no visible pages.");
            }
            _page = Math.Clamp(_page, 0, visiblePages.Length - 1);
            var fields = visiblePages[_page].Fields.Where(IsVisible).ToArray();
            _focus = Math.Clamp(_focus, 0, fields.Length + 1);
            void Paint() => Render(titleKey, visiblePages, fields);
            Paint();
            var key = ReadKey(Paint);
            if (key.Key == ConsoleKey.Escape)
            {
                throw new InteractiveFlowCanceledException();
            }
            if (console.Profile.Width < 60 || console.Profile.Height < 20)
            {
                continue;
            }
            if (_editing is not null)
            {
                Edit(key);
                continue;
            }
            _error = null;
            if (_sectionsFocused && visiblePages[_page].Open is { } open
                && key.Key is ConsoleKey.Enter or ConsoleKey.RightArrow)
            {
                _pendingOpen = open;
                return false;
            }
            if ((visiblePages[_page].Overview is not null || (!_sectionsFocused && _focus < fields.Length && fields[_focus].Overview is not null))
                && (key.Modifiers & ConsoleModifiers.Control) != 0
                && key.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow)
            {
                _overviewOffset = Math.Clamp(_overviewOffset + (key.Key == ConsoleKey.UpArrow ? -1 : 1), 0, _overviewMaximumOffset);
                continue;
            }
            if (_allowSectionNavigation && visiblePages.Length > 1
                && HandleSectionNavigation(key, fields.Length, visiblePages.Length))
            {
                continue;
            }
            switch (key.Key)
            {
                case ConsoleKey.Tab:
                    MoveFocus((key.Modifiers & ConsoleModifiers.Shift) != 0 ? -1 : 1, fields.Length + 2);
                    break;
                case ConsoleKey.UpArrow:
                    MoveFocus(-1, fields.Length + 2);
                    break;
                case ConsoleKey.DownArrow:
                    MoveFocus(1, fields.Length + 2);
                    break;
                case ConsoleKey.PageUp:
                    ChangePage(-1, visiblePages.Length);
                    break;
                case ConsoleKey.PageDown:
                    ChangePage(1, visiblePages.Length);
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                    if (_focus < fields.Length)
                    {
                        CycleChoice(fields[_focus], key.Key == ConsoleKey.LeftArrow ? -1 : 1);
                    }
                    else
                    {
                        _focus = fields.Length + (1 - (_focus - fields.Length));
                    }
                    break;
                case ConsoleKey.Enter:
                    if (_focus < fields.Length)
                    {
                        BeginEdit(fields[_focus]);
                    }
                    else if (_focus == fields.Length)
                    {
                        _error = validate?.Invoke();
                        if (_error is null)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }
        }
    }

    /// <summary>Moves focus between the section list and fields without accepting unfinished edits or saving the draft.</summary>
    private bool HandleSectionNavigation(ConsoleKeyInfo key, int fieldCount, int pageCount)
    {
        var backwards = (key.Modifiers & ConsoleModifiers.Shift) != 0;
        if (_sectionsFocused)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.PageUp:
                    ChangePage(-1, pageCount);
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.PageDown:
                    ChangePage(1, pageCount);
                    break;
                case ConsoleKey.Home:
                    ChangePage(-pageCount, pageCount);
                    break;
                case ConsoleKey.End:
                    ChangePage(pageCount, pageCount);
                    break;
                case ConsoleKey.Enter:
                case ConsoleKey.RightArrow:
                case ConsoleKey.F6:
                    _sectionsFocused = false;
                    _focus = 0;
                    break;
                case ConsoleKey.Tab:
                    _sectionsFocused = false;
                    _focus = backwards ? fieldCount + 1 : 0;
                    break;
            }
            return true;
        }

        if (key.Key == ConsoleKey.F6
            || key.Key == ConsoleKey.LeftArrow && (key.Modifiers & ConsoleModifiers.Control) != 0
            || key.Key == ConsoleKey.Tab && (backwards ? _focus == 0 : _focus == fieldCount + 1))
        {
            _sectionsFocused = true;
            return true;
        }
        return false;
    }

    /// <summary>Moves keyboard focus cyclically through fields and navigation actions.</summary>
    private void MoveFocus(int delta, int count)
    {
        _focus = (_focus + delta + count) % count;
        _overviewOffset = 0;
    }

    /// <summary>Changes section while keeping the form draft intact.</summary>
    private void ChangePage(int delta, int count)
    {
        _page = Math.Clamp(_page + delta, 0, count - 1);
        _focus = 0;
        _overviewOffset = 0;
    }

    /// <summary>Applies one current choice without opening a separate sequential prompt.</summary>
    private void CycleChoice(FormField field, int delta)
    {
        if (field.Choices is null)
        {
            return;
        }
        var choices = field.Choices();
        if (choices.Count == 0)
        {
            throw new InvalidOperationException("A choice field needs at least one available value.");
        }
        var index = choices.ToList().FindIndex(choice => choice.Value == field.Read());
        var selected = choices[(Math.Max(0, index) + delta + choices.Count) % choices.Count];
        _error = field.Validate?.Invoke(selected.Value);
        if (_error is null)
        {
            field.Write(selected.Value);
        }
    }

    /// <summary>Starts bounded text editing or advances the focused choice.</summary>
    private void BeginEdit(FormField field)
    {
        if (field.Choices is not null)
        {
            CycleChoice(field, 1);
            return;
        }
        _editing = field;
        _input = field.Read();
        _caret = _input.Length;
    }

    /// <summary>Edits text locally, never echoing secret values or their lengths.</summary>
    private void Edit(ConsoleKeyInfo key)
    {
        var field = _editing!;
        _error = null;
        if (key.Key == ConsoleKey.Enter)
        {
            _error = field.Validate?.Invoke(_input);
            if (_error is null)
            {
                field.Write(_input);
                _editing = null;
                _input = string.Empty;
            }
        }
        else if (key.Key == ConsoleKey.U && (key.Modifiers & ConsoleModifiers.Control) != 0)
        {
            _input = string.Empty;
            _caret = 0;
        }
        else if (key.Key == ConsoleKey.Home)
        {
            _caret = 0;
        }
        else if (key.Key == ConsoleKey.End)
        {
            _caret = _input.Length;
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
            if (_input.Length >= field.MaxLength)
            {
                _error = text.Text("Form.InputTooLong", field.MaxLength);
            }
            else
            {
                _input = _input.Insert(_caret++, key.KeyChar.ToString());
            }
        }
    }

    /// <summary>Finds the preceding Unicode text element for cursor movement and deletion.</summary>
    private static int PreviousElement(string value, int index) =>
        StringInfo.ParseCombiningCharacters(value).LastOrDefault(start => start < index);

    /// <summary>Finds the following Unicode text element without splitting a composed character.</summary>
    private static int NextElement(string value, int index) =>
        StringInfo.ParseCombiningCharacters(value).FirstOrDefault(start => start > index, value.Length);

    /// <summary>Waits for input while repainting only when the terminal changes size.</summary>
    private ConsoleKeyInfo ReadKey(Action repaint)
    {
        var pending = ReadKeyAsync();
        var dimensions = (console.Profile.Width, console.Profile.Height);
        while (!pending.IsCompleted)
        {
            Task.WhenAny(pending, Task.Delay(100)).GetAwaiter().GetResult();
            var current = (console.Profile.Width, console.Profile.Height);
            if (current != dimensions)
            {
                dimensions = current;
                repaint();
            }
        }
        return pending.GetAwaiter().GetResult()
            ?? throw new IOException(text.Text("Form.EndOfInput"));
    }

    /// <summary>Uses the cancellation-aware console input provided by the application host.</summary>
    private async Task<ConsoleKeyInfo?> ReadKeyAsync() =>
        await console.Input.ReadKeyAsync(true, CancellationToken.None).ConfigureAwait(false);

    /// <summary>Draws a fixed viewport with sections, focus, contextual guidance and persistent actions.</summary>
    private void Render(string titleKey, IReadOnlyList<FormPage> pages, IReadOnlyList<FormField> fields)
    {
        var frame = (console.Profile.Width, console.Profile.Height, TerminalTheme.Current.Id);
        if (_lastFrame.HasValue && _lastFrame.Value.Theme != frame.Id)
        {
            _overviewOffset = 0;
        }
        console.WriteAnsi(writer =>
        {
            if (_lastFrame != frame)
            {
                // Only the disposable alternate buffer is erased, never the main screen or its history.
                writer.Background(Style.Parse(TerminalTheme.Background).Foreground);
                writer.EraseInDisplay(2);
            }
            writer.CursorHome();
        });
        _lastFrame = frame;
        if (frame.Width < 60 || frame.Height < 20)
        {
            console.Write(new Text(text.Text("Form.TooSmall"), Style.Parse(TerminalTheme.Warning)));
            return;
        }

        var sectionNavigation = _allowSectionNavigation && pages.Count > 1;
        var focused = !_sectionsFocused && _focus < fields.Count ? fields[_focus] : null;
        var overview = focused?.Overview ?? pages[_page].Overview;
        if (!ReferenceEquals(overview, _overviewSource))
        {
            _overviewOffset = 0;
            _overviewSource = overview;
        }
        var helpKey = pages[_page].Open is not null && !_sectionsFocused
            ? "Form.NavigationHelp"
            : pages[_page].HelpKey ?? (sectionNavigation ? "Form.NavigationHelp" : "Form.Help");
        var guidance = focused?.Help?.Invoke() ?? text.Text(focused?.HelpKey ?? helpKey);
        if (overview is not null)
        {
            guidance += " " + text.Text("Settings.OverviewScroll");
        }
        var hint = _error ?? (focused is null ? guidance : FieldLabel(focused, text) + ": " + guidance);
        var hintWidth = Math.Max(1, frame.Width - 5);
        var minimumBodyRows = RowsPerField + (overview is not null ? 3 : 0);
        _messageHeight = Math.Clamp((int)Math.Ceiling(Segment.CellCount([new Segment(hint)]) / (double)hintWidth) + 1,
            FullscreenFooter.NoticeRows, Math.Min(6, frame.Height - FixedBodyRows - minimumBodyRows));
        var section = SectionTitle(pages[_page]);
        var availableRows = BodyRows();
        var reservedOverviewRows = focused?.Overview is null ? 3 : Math.Max(3, (availableRows + 1) / 2);
        var fieldRows = overview is null ? availableRows
            : Math.Min(fields.Count * RowsPerField, Math.Max(RowsPerField, (availableRows - reservedOverviewRows) / RowsPerField * RowsPerField));
        var fieldBody = FieldsBody(fields, fieldRows);
        if (overview is not null)
        {
            var overviewWidth = frame.Width - 5 - (sectionNavigation ? FullscreenWorkspace.SidebarWidth(frame.Width) : 0);
            var overviewHeight = availableRows - Math.Min(fields.Count, fieldRows / RowsPerField) * RowsPerField;
            var renderOptions = new RenderOptions(console.Profile.Capabilities, new Size(frame.Width, frame.Height));
            var lines = Segment.SplitLines(overview().Render(renderOptions, overviewWidth)).ToArray();
            _overviewMaximumOffset = Math.Max(0, lines.Length - overviewHeight);
            _overviewOffset = Math.Clamp(_overviewOffset, 0, _overviewMaximumOffset);
            fieldBody = new Rows(fieldBody, new OverviewLines(lines.Skip(_overviewOffset).Take(overviewHeight).ToArray()));
        }
        if (pages[_page].Preview is { } preview && BodyRows() >= fields.Count * RowsPerField + pages[_page].PreviewRows)
        {
            fieldBody = new Rows(fieldBody, preview());
        }
        var content = Inset(new Rows(Styled(section, "bold " + TerminalTheme.Accent), new Text(" "), fieldBody));
        var footerKey = _editing is not null ? "Form.EditFooter"
            : pages[_page].Open is not null ? "Form.OpenFooter"
            : _sectionsFocused ? "Form.SectionsFooter" : sectionNavigation ? "Form.NavigationFooter" : "Form.Footer";
        if (Segment.CellCount([new Segment(text.Text(footerKey))]) > frame.Width - 5)
        {
            footerKey += "Compact";
        }
        var footer = FullscreenFooter.Create(
            new Text(SafeText(hint), Style.Parse(_error is null ? TerminalTheme.Muted : TerminalTheme.Error)),
            Actions(fields.Count),
            FullscreenFooter.Shortcuts(text.Text(footerKey)),
            _messageHeight);
        console.Write(FullscreenWorkspace.Create(text.Text(titleKey), _options, frame.Width, content,
            sectionNavigation ? SectionNavigation(pages) : null, footer, _messageHeight));
    }

    /// <summary>Shows the current section and keeps the focused item visible in both wide and compact layouts.</summary>
    private IRenderable SectionNavigation(IReadOnlyList<FormPage> pages)
    {
        var spacing = BodyRows() >= pages.Count * 2 ? 2 : 1;
        var capacity = Math.Max(1, (BodyRows() - (pages.Count * spacing > BodyRows() ? 1 : 0)) / spacing);
        var offset = Math.Clamp(_page - capacity + 1, 0, Math.Max(0, pages.Count - capacity));
        var rows = new List<IRenderable>();
        for (var index = offset; index < Math.Min(pages.Count, offset + capacity); index++)
        {
            var active = index == _page;
            var selected = active && _sectionsFocused;
            var marker = active ? ">" : " ";
            var label = marker + " " + SectionTitle(pages[index]);
            rows.Add(Styled(label, selected ? TerminalTheme.SelectionForeground : active ? TerminalTheme.Accent : TerminalTheme.Primary,
                selected ? TerminalTheme.SelectionBackground : null));
            if (spacing == 2)
            {
                rows.Add(new Text(" "));
            }
        }
        if (pages.Count > capacity)
        {
            var above = offset > 0 ? console.Profile.Capabilities.Unicode ? "↑ " : "^ " : string.Empty;
            var below = offset + capacity < pages.Count ? console.Profile.Capabilities.Unicode ? " ↓" : " v" : string.Empty;
            rows.Add(Styled(above + "..." + below, TerminalTheme.Info));
        }
        return Inset(new Rows(Styled(text.Text("Form.Sections"), TerminalTheme.Accent), new Text(" "), new Rows(rows)));
    }

    /// <summary>Adds meaningful setup section icons through the shared emoji and ASCII fallback helper.</summary>
    private string SectionTitle(FormPage page) => SectionTitle(page, text, _options);

    /// <summary>Shares localized section icons between fullscreen and scrolling settings.</summary>
    internal static string SectionTitle(FormPage page, ILocalizationService text, ConsoleRenderOptions options)
    {
        var icon = page.TitleKey switch
        {
            "Form.General" => "⚙️",
            "Setup.Keys" => "🔑",
            "Setup.Model" => "🧠",
            "Setup.Preferences" => "📝",
            "Form.Advanced" => "🛠️",
            "Theme.Title" => "🎨",
            "Settings.General" => "⚙️",
            "Settings.Ai" => "🧠",
            "Settings.Credentials" => "🔑",
            "Settings.Context" => "💬",
            "Settings.Commands" => "⚡",
            "Settings.Personalization" => "📝",
            "Settings.Theme" => "🎨",
            "About.MenuLabel" => "ℹ️",
            "Settings.Skills" => "🧩",
            "Settings.Learning" => "💭",
            "Settings.Memories" => "📚",
            "Settings.Privacy" => "🔒",
            _ => null
        };
        return (icon is null ? string.Empty : TerminalTheme.IconPrefix(options, icon, "-")) + text.Text(page.TitleKey);
    }

    /// <summary>Uses literal supplied labels for inspected packages and localized labels for ordinary fields.</summary>
    internal static string FieldLabel(FormField field, ILocalizationService text) => SafeText(field.Label?.Invoke() ?? text.Text(field.LabelKey));

    /// <summary>Renders spaced editable fields while keeping the complete selected field inside the viewport.</summary>
    private IRenderable FieldsBody(IReadOnlyList<FormField> fields, int availableRows)
    {
        var capacity = Math.Max(1, availableRows / RowsPerField);
        var offset = Math.Clamp(_focus - capacity + 1, 0, Math.Max(0, fields.Count - capacity));
        var rows = new List<IRenderable>();
        for (var index = offset; index < Math.Min(fields.Count, offset + capacity); index++)
        {
            var field = fields[index];
            var selected = !_sectionsFocused && index == _focus;
            rows.Add(FieldBlock(
                Styled($"{(selected ? ">" : " ")} {FieldLabel(field, text)}", selected ? TerminalTheme.Accent : TerminalTheme.Primary),
                width => Styled($"[ {FieldValue(field, width - 4)} ]",
                    (selected ? "bold underline " : string.Empty) + (field.ValueColor?.Invoke() ?? TerminalTheme.FieldValue))));
        }
        return new Rows(rows);
    }

    /// <summary>Reads a display value or fits the active editor inside the allocated value column.</summary>
    private string FieldValue(FormField field, int width)
    {
        if (ReferenceEquals(field, _editing))
        {
            return field.Secret ? text.Text("Form.SecretInput") : InputWindow(width);
        }
        if (field.Secret)
        {
            return field.Display?.Invoke() ?? text.Text(string.IsNullOrEmpty(field.Read()) ? "Form.SecretUnchanged" : "Form.SecretEntered");
        }
        return field.Display?.Invoke() ?? field.Choices?.Invoke().FirstOrDefault(choice => choice.Value == field.Read())?.Label ?? field.Read();
    }

    /// <summary>Pairs a right-aligned label with a left-aligned value and one blank row below the field.</summary>
    private static Rows FieldBlock(IRenderable label, Func<int, IRenderable> value) =>
        new(new FormPair(label, value), new Text(" "));

    /// <summary>Fits text around the caret into the value column after reserving its bracket padding.</summary>
    private string InputWindow(int width)
    {
        if (width < 3)
        {
            return "|";
        }
        var elements = StringInfo.ParseCombiningCharacters(_input);
        var caretElement = Array.BinarySearch(elements, Math.Clamp(_caret, 0, _input.Length));
        if (caretElement < 0)
        {
            caretElement = ~caretElement;
        }

        // Reserve the caret and both overflow markers before fitting whole graphemes by terminal cells.
        var budget = width - 3;
        var startElement = caretElement;
        var leftCells = 0;
        while (startElement > 0)
        {
            var cells = InputElementWidth(elements, startElement - 1);
            if (leftCells + cells > budget / 2)
            {
                break;
            }
            leftCells += cells;
            startElement--;
        }

        var endElement = caretElement;
        var rightCells = 0;
        while (endElement < elements.Length)
        {
            var cells = InputElementWidth(elements, endElement);
            if (leftCells + rightCells + cells > budget)
            {
                break;
            }
            rightCells += cells;
            endElement++;
        }

        if (endElement == elements.Length)
        {
            while (startElement > 0)
            {
                var cells = InputElementWidth(elements, startElement - 1);
                if (leftCells + rightCells + cells > budget)
                {
                    break;
                }
                leftCells += cells;
                startElement--;
            }
        }

        var start = InputElementOffset(elements, startElement);
        var caret = InputElementOffset(elements, caretElement);
        var end = InputElementOffset(elements, endElement);
        return (start > 0 ? "…" : string.Empty) + _input[start..caret] + "|" + _input[caret..end]
            + (end < _input.Length ? "…" : string.Empty);
    }

    /// <summary>Measures one complete text element after applying the form's control-character sanitization.</summary>
    private int InputElementWidth(int[] elements, int element)
    {
        var start = InputElementOffset(elements, element);
        var end = InputElementOffset(elements, element + 1);
        var visible = SafeText(_input[start..end]);
        return new Segment(visible).CellCount();
    }

    /// <summary>Maps a grapheme boundary or the final boundary to a valid UTF-16 offset.</summary>
    private int InputElementOffset(int[] elements, int element) =>
        element < elements.Length ? elements[element] : _input.Length;

    /// <summary>Builds semantic navigation buttons with contrasting focus colors and an explicit focus marker.</summary>
    private IRenderable Actions(int fieldCount)
    {
        var labels = new[] { "Form.Save", "Form.Cancel" };
        var row = new Grid();
        foreach (var label in labels)
        {
            row.AddColumn();
        }
        row.AddRow(labels.Select((label, index) =>
        {
            var selected = !_sectionsFocused && _focus == fieldCount + index;
            var icon = label == "Form.Save"
                ? TerminalTheme.IconPrefix(_options, "💾", "+")
                : TerminalTheme.IconPrefix(_options, "↩️", "x");
            return FullscreenFooter.Button(icon + text.Text(label), ActionColor(label), selected);
        }).ToArray());
        return row;
    }

    /// <summary>Assigns navigation, progression, saving, and cancellation their semantic theme colors.</summary>
    private static string ActionColor(string label) => label switch
    {
        "Form.Save" => TerminalTheme.Success,
        "Form.Cancel" => TerminalTheme.Warning,
        _ => throw new InvalidOperationException("Unsupported fullscreen form action.")
    };

    /// <summary>Gives all form regions consistent horizontal spacing without drawing a frame.</summary>
    private static Padder Inset(IRenderable content) => new(content, new Padding(2, 0, 2, 0));

    /// <summary>Calculates field rows after the open headings and footer, preserving one complete spaced field.</summary>
    private int BodyRows() => Math.Max(RowsPerField, console.Profile.Height - FixedBodyRows - _messageHeight);

    /// <summary>Checks field visibility against the current draft.</summary>
    private static bool IsVisible(FormField field) => field.IsVisible?.Invoke() != false;

    /// <summary>Creates one high-contrast row without interpreting user input as markup or control sequences.</summary>
    private static IRenderable Styled(string value, string color, string? background = null) =>
        new FormLine(SafeText(value),
            Style.Parse(background is null ? color : $"{color} on {background}"));

    /// <summary>Flattens line separators and removes terminal control characters before text rendering.</summary>
    private static string SafeText(string value) =>
        new(value.ReplaceLineEndings(" ").Select(character => char.IsControl(character) ? ' ' : character).ToArray());

    /// <summary>Shares exact label and value widths across field rows while preserving each cell's style.</summary>
    private sealed class FormPair(IRenderable label, Func<int, IRenderable> value) : IRenderable
    {
        /// <summary>Uses the full content width so every field's value starts in the same column.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth)
        {
            var width = Math.Max(0, maxWidth);
            return new Measurement(Math.Min(1, width), width);
        }

        /// <summary>Right-aligns the label before a fixed gutter and renders the value in its remaining cells.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var width = Math.Max(0, maxWidth);
            var gutter = Math.Min(2, Math.Max(0, width - 2));
            var labelWidth = Math.Min(32, (width - gutter) * 2 / 5);
            var valueWidth = width - labelWidth - gutter;
            var labelSegments = label.Render(options, labelWidth).ToArray();
            yield return new Segment(new string(' ', Math.Max(0, labelWidth - Segment.CellCount(labelSegments))));
            foreach (var segment in labelSegments)
            {
                yield return segment;
            }
            yield return new Segment(new string(' ', gutter));
            foreach (var segment in value(valueWidth).Render(options, valueWidth))
            {
                yield return segment;
            }
        }
    }

    /// <summary>Displays a scrollable slice of the general overview without changing its measured columns.</summary>
    private sealed class OverviewLines(IReadOnlyList<SegmentLine> lines) : IRenderable
    {
        /// <summary>Accepts the width of the settings content region.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, maxWidth);

        /// <summary>Preserves semantic colors and the line boundaries of the selected overview rows.</summary>
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

    /// <summary>Keeps a styled form label or value on one row, clipping only at complete Unicode text elements.</summary>
    private sealed class FormLine(string value, Style style) : IRenderable
    {
        private readonly int _width = new Segment(value).CellCount();

        /// <summary>Allows a row to shrink to its allocated cells without requesting wrapped lines.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth)
        {
            var width = Math.Min(_width, Math.Max(0, maxWidth));
            return new Measurement(Math.Min(1, width), width);
        }

        /// <summary>Emits exactly one styled segment with a visible overflow marker when needed.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            yield return new Segment(Fit(maxWidth, options.Capabilities.Unicode), style);
        }

        /// <summary>Fits a complete prefix by terminal-cell width without splitting wide or composed characters.</summary>
        private string Fit(int maxWidth, bool unicode)
        {
            if (maxWidth <= 0)
            {
                return string.Empty;
            }
            if (_width <= maxWidth)
            {
                return value;
            }

            var marker = unicode ? "…" : ".";
            var budget = maxWidth - new Segment(marker).CellCount();
            var elements = StringInfo.GetTextElementEnumerator(value);
            var cells = 0;
            var end = 0;
            while (elements.MoveNext())
            {
                var element = elements.GetTextElement();
                var width = new Segment(element).CellCount();
                if (cells + width > budget)
                {
                    break;
                }
                cells += width;
                end = elements.ElementIndex + element.Length;
            }
            return value[..end] + marker;
        }
    }
}

/// <summary>Applies the theme background to every layout cell and avoids the terminal's wrapping corner.</summary>
internal sealed class FormSurface(IRenderable content) : IRenderable
{
    /// <summary>Measures the surface within the terminal's available width.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) => content.Measure(options, Math.Max(1, maxWidth - 1));

    /// <summary>Renders a bounded viewport while preserving explicit selection colors.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var foreground = Style.Parse(TerminalTheme.Primary).Foreground;
        var background = Style.Parse(TerminalTheme.Background).Foreground;
        foreach (var segment in content.Render(options with { Height = Math.Max(1, options.ConsoleSize.Height - 1) }, Math.Max(1, maxWidth - 1)))
        {
            if (segment.IsLineBreak || segment.IsControlCode)
            {
                yield return segment;
                continue;
            }
            var current = segment.Style;
            yield return new Segment(segment.Text, new Style(
                current.Foreground != Color.Default ? current.Foreground : foreground,
                current.Background != Color.Default ? current.Background : background,
                current.Decoration), segment.Link);
        }
    }
}

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
    public bool Secret { get; init; }
    public int MaxLength { get; init; } = 100_000;
    public string? HelpKey { get; init; }
}

internal sealed record FormPage(string TitleKey, IReadOnlyList<FormField> Fields);

internal sealed record FormSummary(string Label, string Value);

/// <summary>Owns keyboard navigation and rendering for temporary, passive terminal forms.</summary>
internal sealed class FullscreenForm(IAnsiConsole console, ILocalizationService text)
{
    private const int RowsPerField = 2;
    // Header, section title, navigation, shortcuts, and the reserved terminal row.
    private const int FixedBodyRows = 9;
    private int _page;
    private int _focus;
    private bool _review;
    private int _reviewScroll;
    private string? _error;
    private FormField? _editing;
    private string _input = string.Empty;
    private int _caret;
    private int _messageHeight = 2;
    private (int Width, int Height, string Theme)? _lastFrame;

    /// <summary>Checks terminal capabilities before opting into a fullscreen form.</summary>
    internal static bool CanUse(IAnsiConsole console) =>
        console.Profile.Capabilities.Ansi && console.Profile.Capabilities.AlternateBuffer
        && console.Profile.Capabilities.Interactive && console.Profile.Out.IsTerminal
        && !Console.IsInputRedirected && !Console.IsOutputRedirected
        && console.Profile.Width >= 60 && console.Profile.Height >= 20;

    /// <summary>Returns a reviewed submission while always restoring the original terminal buffer.</summary>
    internal bool Run(
        string titleKey,
        IReadOnlyList<FormPage> pages,
        Func<IReadOnlyList<FormSummary>> summary,
        Func<string?>? validate = null)
    {
        ArgumentNullException.ThrowIfNull(pages);
        if (pages.Count == 0)
        {
            throw new ArgumentException("A fullscreen form needs at least one page.", nameof(pages));
        }
        if (!CanUse(console))
        {
            throw new InvalidOperationException(text.Text("Form.Unavailable"));
        }

        var saved = false;
        try
        {
            console.AlternateScreen(() =>
            {
                console.Cursor.Hide();
                try
                {
                    saved = RunLoop(titleKey, pages, summary, validate);
                }
                finally
                {
                    console.WriteAnsi(writer => writer.ResetStyle());
                    console.Cursor.Show();
                }
            });
        }
        finally
        {
            _input = string.Empty;
            _editing = null;
        }
        return saved;
    }

    /// <summary>Processes one focused control at a time without nesting Spectre prompts or Live displays.</summary>
    private bool RunLoop(string titleKey, IReadOnlyList<FormPage> pages,
        Func<IReadOnlyList<FormSummary>> summary, Func<string?>? validate)
    {
        while (true)
        {
            var visiblePages = pages.Where(page => page.Fields.Any(IsVisible)).ToArray();
            if (visiblePages.Length == 0)
            {
                throw new InvalidOperationException("The form has no visible fields.");
            }
            _page = Math.Clamp(_page, 0, visiblePages.Length - 1);
            var fields = visiblePages[_page].Fields.Where(IsVisible).ToArray();
            _focus = Math.Clamp(_focus, 0, _review ? 1 : fields.Length + 2);
            void Paint() => Render(titleKey, visiblePages, fields, summary());
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
            if (_review)
            {
                if (HandleReview(key, summary().Count, validate))
                {
                    return true;
                }
                continue;
            }
            _error = null;
            switch (key.Key)
            {
                case ConsoleKey.Tab:
                    MoveFocus((key.Modifiers & ConsoleModifiers.Shift) != 0 ? -1 : 1, fields.Length + 3);
                    break;
                case ConsoleKey.UpArrow:
                    MoveFocus(-1, fields.Length + 3);
                    break;
                case ConsoleKey.DownArrow:
                    MoveFocus(1, fields.Length + 3);
                    break;
                case ConsoleKey.PageUp:
                    ChangePage(-1, visiblePages.Length);
                    break;
                case ConsoleKey.PageDown:
                    ChangePage(1, visiblePages.Length);
                    break;
                case ConsoleKey.F10:
                    OpenReview();
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                    if (_focus < fields.Length)
                    {
                        CycleChoice(fields[_focus], key.Key == ConsoleKey.LeftArrow ? -1 : 1);
                    }
                    break;
                case ConsoleKey.Enter:
                    if (_focus < fields.Length)
                    {
                        BeginEdit(fields[_focus]);
                    }
                    else if (_focus == fields.Length)
                    {
                        ChangePage(-1, visiblePages.Length);
                    }
                    else if (_focus == fields.Length + 1 && _page < visiblePages.Length - 1)
                    {
                        ChangePage(1, visiblePages.Length);
                    }
                    else if (_focus == fields.Length + 2)
                    {
                        return false;
                    }
                    else
                    {
                        OpenReview();
                    }
                    break;
            }
        }
    }

    /// <summary>Requires an explicit Save action after displaying the review and validating the complete draft.</summary>
    private bool HandleReview(ConsoleKeyInfo key, int rows, Func<string?>? validate)
    {
        switch (key.Key)
        {
            case ConsoleKey.Tab:
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
                _focus = 1 - _focus;
                break;
            case ConsoleKey.PageUp:
                _reviewScroll = Math.Max(0, _reviewScroll - ReviewCapacity());
                break;
            case ConsoleKey.PageDown:
                _reviewScroll = Math.Min(Math.Max(0, rows - ReviewCapacity()), _reviewScroll + ReviewCapacity());
                break;
            case ConsoleKey.Enter:
                if (_focus == 0)
                {
                    _review = false;
                    _focus = 0;
                }
                else
                {
                    _error = validate?.Invoke();
                    return _error is null;
                }
                break;
        }
        return false;
    }

    /// <summary>Opens the review without making its Save action the default selection.</summary>
    private void OpenReview()
    {
        _review = true;
        _focus = 0;
        _reviewScroll = 0;
    }

    /// <summary>Moves keyboard focus cyclically through fields and navigation actions.</summary>
    private void MoveFocus(int delta, int count) => _focus = (_focus + delta + count) % count;

    /// <summary>Changes section while keeping the form draft intact.</summary>
    private void ChangePage(int delta, int count)
    {
        _page = Math.Clamp(_page + delta, 0, count - 1);
        _focus = 0;
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
    private void Render(string titleKey, IReadOnlyList<FormPage> pages, IReadOnlyList<FormField> fields,
        IReadOnlyList<FormSummary> summary)
    {
        var frame = (console.Profile.Width, console.Profile.Height, TerminalTheme.Current.Id);
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

        var focused = !_review && _focus < fields.Count ? fields[_focus] : null;
        var hint = _error ?? focused?.Help?.Invoke() ?? text.Text(focused?.HelpKey ?? "Form.Help");
        var hintWidth = Math.Max(1, frame.Width - 5);
        var minimumBodyRows = RowsPerField + (_review ? 1 : 0);
        _messageHeight = Math.Clamp((int)Math.Ceiling(Segment.CellCount([new Segment(hint)]) / (double)hintWidth) + 1,
            2, Math.Min(6, frame.Height - FixedBodyRows - minimumBodyRows));
        var section = _review ? text.Text("Form.Review") : text.Text(pages[_page].TitleKey);
        var header = new Grid().AddColumn().AddColumn(new GridColumn().RightAligned());
        header.AddRow(Styled("hm / PromptMeUp", TerminalTheme.Accent), Styled(text.Text(titleKey), TerminalTheme.Primary));
        var root = new Layout("root").SplitRows(
            new Layout("header", new Rows(Inset(header), new Text(" "))).Size(2),
            new Layout("body"),
            new Layout("message").Size(_messageHeight),
            new Layout("actions").Size(2),
            new Layout("footer").Size(2));
        var body = _review ? ReviewBody(summary) : FieldsBody(fields);
        var sectionTitle = _review ? section : $"{section}  {_page + 1}/{pages.Count}";
        var content = Inset(new Rows(Styled(sectionTitle, TerminalTheme.Accent), new Text(" "), body));
        if (!_review && frame.Width >= 100)
        {
            var navigation = new Rows(pages.Select((page, index) => Styled(
                $"{index + 1:00}  {text.Text(page.TitleKey)}", index == _page ? TerminalTheme.SelectionForeground : TerminalTheme.Muted,
                index == _page ? TerminalTheme.SelectionBackground : null)));
            root["body"].SplitColumns(new Layout("sections", Inset(navigation)).Size(25), new Layout("fields", content));
        }
        else
        {
            root["body"].Update(content);
        }
        root["message"].Update(new Rows(new Text(" "), Inset(new Text(SafeText(hint),
            Style.Parse(_error is null ? TerminalTheme.Muted : TerminalTheme.Error)))));
        root["actions"].Update(new Rows(
            new Rule { Style = Style.Parse(TerminalTheme.Divider) },
            Inset(Actions(fields.Count, pages.Count))));
        var footerKey = _editing is not null ? "Form.EditFooter" : _review ? "Form.ReviewFooter" : "Form.Footer";
        if (Segment.CellCount([new Segment(text.Text(footerKey))]) > frame.Width - 5)
        {
            footerKey += "Compact";
        }
        root["footer"].Update(new Rows(new Text(" "), Inset(Styled(text.Text(footerKey), TerminalTheme.Info))));
        console.Write(new FormSurface(root));
    }

    /// <summary>Renders spaced editable fields while keeping the complete selected field inside the viewport.</summary>
    private IRenderable FieldsBody(IReadOnlyList<FormField> fields)
    {
        var capacity = Math.Max(1, BodyRows() / RowsPerField);
        var offset = Math.Clamp(_focus - capacity + 1, 0, Math.Max(0, fields.Count - capacity));
        var rows = new List<IRenderable>();
        for (var index = offset; index < Math.Min(fields.Count, offset + capacity); index++)
        {
            var field = fields[index];
            var selected = index == _focus;
            rows.Add(FieldBlock(
                Styled($"{(selected ? ">" : " ")} {text.Text(field.LabelKey)}", TerminalTheme.Muted),
                width => Styled($"[ {FieldValue(field, width - 4)} ]",
                    selected ? TerminalTheme.SelectionForeground : TerminalTheme.Primary,
                    selected ? TerminalTheme.SelectionBackground : null)));
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

    /// <summary>Shows non-sensitive review rows and a visible scroll range.</summary>
    private IRenderable ReviewBody(IReadOnlyList<FormSummary> summary)
    {
        var capacity = ReviewCapacity();
        _reviewScroll = Math.Clamp(_reviewScroll, 0, Math.Max(0, summary.Count - capacity));
        var rows = new List<IRenderable>();
        foreach (var row in summary.Skip(_reviewScroll).Take(capacity))
        {
            rows.Add(FieldBlock(
                Styled(row.Label, TerminalTheme.Muted),
                _ => Styled(row.Value, TerminalTheme.Primary)));
        }
        rows.Add(Styled($"{_reviewScroll + 1}–{Math.Min(summary.Count, _reviewScroll + capacity)} / {summary.Count}", TerminalTheme.Info));
        return new Rows(rows);
    }

    /// <summary>Builds semantic navigation buttons with contrasting focus colors and an explicit focus marker.</summary>
    private IRenderable Actions(int fieldCount, int pageCount)
    {
        var labels = _review
            ? new[] { "Form.Back", "Form.Save" }
            : new[] { "Form.Back", _page < pageCount - 1 ? "Form.Next" : "Form.Review", "Form.Cancel" };
        var row = new Grid();
        foreach (var label in labels)
        {
            row.AddColumn();
        }
        row.AddRow(labels.Select((label, index) =>
        {
            var selected = _focus == (_review ? index : fieldCount + index);
            return Styled($"{(selected ? ">" : " ")} [ {text.Text(label)} ]",
                selected ? TerminalTheme.SelectionForeground : ActionColor(label),
                selected ? TerminalTheme.SelectionBackground : null);
        }).ToArray());
        return row;
    }

    /// <summary>Assigns navigation, progression, saving, and cancellation their semantic theme colors.</summary>
    private static string ActionColor(string label) => label switch
    {
        "Form.Back" => TerminalTheme.Info,
        "Form.Next" or "Form.Review" => TerminalTheme.Accent,
        "Form.Save" => TerminalTheme.Success,
        "Form.Cancel" => TerminalTheme.Warning,
        _ => throw new InvalidOperationException("Unsupported fullscreen form action.")
    };

    /// <summary>Gives all form regions consistent horizontal spacing without drawing a frame.</summary>
    private static Padder Inset(IRenderable content) => new(content, new Padding(2, 0, 2, 0));

    /// <summary>Calculates field rows after the open headings and footer, preserving one complete spaced field.</summary>
    private int BodyRows() => Math.Max(RowsPerField, console.Profile.Height - FixedBodyRows - _messageHeight);

    /// <summary>Fits complete spaced review fields while reserving one row for the scroll-range indicator.</summary>
    private int ReviewCapacity() => Math.Max(1, (BodyRows() - 1) / RowsPerField);

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

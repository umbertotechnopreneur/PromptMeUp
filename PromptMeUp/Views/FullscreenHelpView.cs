// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Pairs a public command with its localized description.</summary>
internal sealed record HelpEntry(string Command, string Description)
{
    public string? Example { get; init; }
    public IReadOnlyList<HelpArgument> Arguments { get; init; } = [];
}

/// <summary>Explains one literal token used in a concrete command example.</summary>
internal sealed record HelpArgument(string Token, string Description);

/// <summary>Groups help entries under full and compact localized section names.</summary>
internal sealed record HelpSection(string Icon, string Title, string NavigationLabel, IReadOnlyList<HelpEntry> Entries)
{
    public Action? Open { get; init; }
}

/// <summary>Browses command sections in a temporary open layout without changing the original terminal buffer.</summary>
internal sealed class FullscreenHelpView(IAnsiConsole console, ILocalizationService text, ConsoleRenderOptions? options = null)
{
    private const int HeadingRows = 2;
    private readonly ConsoleRenderOptions _options = options ?? new(false, false);
    private int _section;
    private int _offset;
    private int _lineCount;
    private int _visibleRows;
    private HelpFocus _focus = HelpFocus.Sections;
    private (int Width, int Height, string Theme)? _lastFrame;

    /// <summary>Uses fullscreen help only when both terminal input and the alternate buffer are available.</summary>
    internal static bool CanUse(IAnsiConsole console) =>
        console.Profile.Capabilities.Ansi && console.Profile.Capabilities.AlternateBuffer
        && console.Profile.Capabilities.Interactive && console.Profile.Out.IsTerminal
        && !Console.IsInputRedirected && !Console.IsOutputRedirected
        && console.Profile.Width >= 60 && console.Profile.Height >= 20;

    /// <summary>Restores the main screen and cursor on close, cancellation, or a rendering error.</summary>
    internal void Render(IReadOnlyList<HelpSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        if (sections.Count == 0)
        {
            throw new ArgumentException("Fullscreen help needs at least one section.", nameof(sections));
        }
        _focus = HelpFocus.Sections;
        _section = Math.Clamp(_section, 0, sections.Count - 1);
        _offset = 0;
        _lastFrame = null;
        try
        {
            Action? open;
            do
            {
                open = null;
                _lastFrame = null;
                console.AlternateScreen(() =>
                {
                    console.Cursor.Hide();
                    try
                    {
                        open = RunLoop(sections);
                    }
                    finally
                    {
                        console.WriteAnsi(writer => writer.ResetStyle());
                        console.Cursor.Show();
                    }
                });
                // Each screen owns its alternate buffer; the help selection survives the round trip.
                open?.Invoke();
            }
            while (open is not null);
        }
        catch (InteractiveFlowCanceledException)
        {
            // Escape is a normal close action for this read-only browser.
        }
    }

    /// <summary>Navigates descriptions and returns screen actions for opening outside the help buffer.</summary>
    private Action? RunLoop(IReadOnlyList<HelpSection> sections)
    {
        while (true)
        {
            void Paint() => PaintScreen(sections);
            Paint();
            var key = ReadKey(Paint);
            if (key.Key is ConsoleKey.Escape or ConsoleKey.Q)
            {
                return null;
            }
            if (console.Profile.Width < 60 || console.Profile.Height < 20)
            {
                continue;
            }
            if (key.Key == ConsoleKey.LeftArrow && (key.Modifiers & ConsoleModifiers.Control) != 0)
            {
                _focus = HelpFocus.Sections;
                continue;
            }
            switch (key.Key)
            {
                case ConsoleKey.F6:
                    _focus = _focus == HelpFocus.Sections ? HelpFocus.Commands : HelpFocus.Sections;
                    break;
                case ConsoleKey.Enter:
                    if (_focus == HelpFocus.Close)
                    {
                        return null;
                    }
                    if (sections[_section].Open is { } open)
                    {
                        return open;
                    }
                    _focus = HelpFocus.Commands;
                    break;
                case ConsoleKey.Tab:
                    var direction = (key.Modifiers & ConsoleModifiers.Shift) != 0 ? -1 : 1;
                    _focus = (HelpFocus)(((int)_focus + direction + 3) % 3);
                    break;
                case ConsoleKey.LeftArrow:
                    if (_focus != HelpFocus.Close)
                    {
                        _focus = HelpFocus.Sections;
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (_focus != HelpFocus.Close)
                    {
                        if (sections[_section].Open is { } openSection)
                        {
                            return openSection;
                        }
                        _focus = HelpFocus.Commands;
                    }
                    break;
                case ConsoleKey.UpArrow:
                    if (_focus == HelpFocus.Sections)
                    {
                        ChangeSection(-1, sections.Count);
                    }
                    else if (_focus == HelpFocus.Commands)
                    {
                        Scroll(-1);
                    }
                    else
                    {
                        _focus = HelpFocus.Commands;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (_focus == HelpFocus.Sections)
                    {
                        ChangeSection(1, sections.Count);
                    }
                    else if (_focus == HelpFocus.Commands)
                    {
                        Scroll(1);
                    }
                    else
                    {
                        _focus = HelpFocus.Sections;
                    }
                    break;
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                    var delta = key.Key == ConsoleKey.PageUp ? -1 : 1;
                    if (_focus == HelpFocus.Sections)
                    {
                        ChangeSection(delta, sections.Count);
                    }
                    else if (_focus == HelpFocus.Commands)
                    {
                        Scroll(delta * _visibleRows);
                    }
                    break;
                case ConsoleKey.Home:
                    if (_focus == HelpFocus.Sections)
                    {
                        _section = 0;
                    }
                    _offset = 0;
                    break;
                case ConsoleKey.End:
                    if (_focus == HelpFocus.Sections)
                    {
                        _section = sections.Count - 1;
                        _offset = 0;
                    }
                    else
                    {
                        _offset = Math.Max(0, _lineCount - _visibleRows);
                    }
                    break;
            }
        }
    }

    /// <summary>Starts each newly selected section at its first command.</summary>
    private void ChangeSection(int delta, int count)
    {
        _section = Math.Clamp(_section + delta, 0, count - 1);
        _offset = 0;
    }

    /// <summary>Moves through already wrapped command lines without passing the visible range.</summary>
    private void Scroll(int delta) => _offset = Math.Clamp(_offset + delta, 0, Math.Max(0, _lineCount - _visibleRows));

    /// <summary>Waits for one cancellable key and repaints only when the terminal size changes.</summary>
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

    /// <summary>Uses the host input wrapper so application shutdown still interrupts the help browser.</summary>
    private async Task<ConsoleKeyInfo?> ReadKeyAsync() =>
        await console.Input.ReadKeyAsync(true, CancellationToken.None).ConfigureAwait(false);

    /// <summary>Uses the setup viewport's margins, sidebar, action bar, and shared header around colored command examples.</summary>
    private void PaintScreen(IReadOnlyList<HelpSection> sections)
    {
        var frame = (console.Profile.Width, console.Profile.Height, TerminalTheme.Current.Id);
        console.WriteAnsi(writer =>
        {
            if (_lastFrame != frame)
            {
                // Only the disposable alternate buffer is erased, never the user's main screen or history.
                writer.Background(Style.Parse(TerminalTheme.Background).Foreground);
                writer.EraseInDisplay(2);
            }
            writer.CursorHome();
        });
        _lastFrame = frame;
        var width = Math.Max(1, console.Profile.Width - 1);
        var height = Math.Max(1, console.Profile.Height - 1);
        if (console.Profile.Width < 60 || console.Profile.Height < 20)
        {
            var small = new Layout("help", Inset(new Text(text.Text("Help.Browse.TooSmall"), Style.Parse(TerminalTheme.Warning))));
            console.Write(new FormSurface(small));
            return;
        }

        var bodyHeight = height - FullscreenHeader.Height - FullscreenFooter.Height();
        var contentWidth = width - FullscreenWorkspace.SidebarWidth(console.Profile.Width) - 4;
        _visibleRows = bodyHeight - HeadingRows;
        var active = sections[_section];
        var entries = new Rows(active.Entries.Select(RenderEntry));
        var renderOptions = new RenderOptions(console.Profile.Capabilities, new Size(width, height));
        var lines = Segment.SplitLines(((IRenderable)entries).Render(renderOptions, contentWidth)).ToList();
        // A final spacing row alone must not make otherwise visible help count as overflowing.
        while (lines.Count > 0 && lines[^1].All(segment => string.IsNullOrWhiteSpace(segment.Text)))
        {
            lines.RemoveAt(lines.Count - 1);
        }
        _lineCount = lines.Count;
        _offset = Math.Clamp(_offset, 0, Math.Max(0, _lineCount - _visibleRows));

        var heading = $"{(_focus == HelpFocus.Commands ? "> " : string.Empty)}{SectionTitle(active, compact: false)}";
        var content = Inset(new Rows(Line(heading, "bold " + TerminalTheme.Accent), new Text(" "),
            new HelpLines(lines.Skip(_offset).Take(_visibleRows).ToArray())));

        var range = text.Text("Help.Browse.Range", _lineCount == 0 ? 0 : _offset + 1,
            Math.Min(_lineCount, _offset + _visibleRows), _lineCount);
        IRenderable message = _lineCount > _visibleRows
            ? new HelpRange(range)
            : HelpCommandLine.CreateDescription(text.Text("Help.Usage"), "hm");
        if (active.Open is not null && _focus != HelpFocus.Close)
        {
            message = Line(text.Text("About.OpenHint"), TerminalTheme.Primary);
        }
        var closeSelected = _focus == HelpFocus.Close;
        var actions = new Grid().AddColumn();
        actions.AddRow(FullscreenFooter.Button(
            TerminalTheme.IconPrefix(_options, "↩️", "x") + text.Text("Help.Browse.Close"), TerminalTheme.Warning, closeSelected));
        var footerKey = _focus switch
        {
            _ when active.Open is not null && _focus != HelpFocus.Close => "Help.Browse.OpenKeys",
            HelpFocus.Sections => "Help.Browse.SectionKeys",
            HelpFocus.Commands => "Help.Browse.ScrollKeys",
            _ => "Help.Browse.CloseKeys"
        };
        if (new Segment(text.Text(footerKey)).CellCount() > width - 4)
        {
            footerKey += "Compact";
        }
        var footer = FullscreenFooter.Create(message, actions, FullscreenFooter.Shortcuts(text.Text(footerKey)));
        console.Write(FullscreenWorkspace.Create(text.Text("Help.Title"), _options, console.Profile.Width,
            content, SectionNavigation(sections, bodyHeight), footer, FullscreenFooter.NoticeRows));
    }

    /// <summary>Shows contiguous icon labels and keeps the selected section visible without number prefixes.</summary>
    private IRenderable SectionNavigation(IReadOnlyList<HelpSection> sections, int bodyHeight)
    {
        var availableRows = bodyHeight - HeadingRows;
        var spacing = availableRows >= sections.Count * 2 ? 2 : 1;
        var capacity = Math.Max(1, availableRows / spacing);
        var offset = Math.Clamp(_section - capacity + 1, 0, Math.Max(0, sections.Count - capacity));
        var navigation = new List<IRenderable>
        {
            Line(text.Text("Form.Sections"), TerminalTheme.Accent), new Text(" ")
        };
        for (var index = offset; index < Math.Min(sections.Count, offset + capacity); index++)
        {
            var active = index == _section;
            var selected = active && _focus == HelpFocus.Sections;
            navigation.Add(Line($"{(active ? ">" : " ")} {SectionTitle(sections[index], compact: true)}",
                selected ? TerminalTheme.SelectionForeground : active ? TerminalTheme.Accent : TerminalTheme.Primary,
                selected ? TerminalTheme.SelectionBackground : null));
            if (spacing == 2)
            {
                navigation.Add(new Text(" "));
            }
        }
        return Inset(new Rows(navigation));
    }

    /// <summary>Uses the same spaced semantic icons and plain-text fallback as setup sections.</summary>
    private string SectionTitle(HelpSection section, bool compact) =>
        TerminalTheme.IconPrefix(_options, section.Icon, "-") + (compact ? section.NavigationLabel : section.Title);

    /// <summary>Matches the two-column horizontal inset used throughout the setup form.</summary>
    private static Padder Inset(IRenderable content) => new(content, new Padding(2, 0, 2, 0));

    /// <summary>Creates a single-row label with consistent high-contrast colors.</summary>
    private static HelpLine Line(string value, string foreground, string? background = null) =>
        new(value, Style.Parse(background is null ? foreground : $"{foreground} on {background}"));

    /// <summary>Keeps the localized displayed-line range on one row and emphasizes only its numbers.</summary>
    private sealed class HelpRange(string value) : IRenderable
    {
        /// <summary>Measures the range without requesting more than the content column's width.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Min(new Segment(value).CellCount(), maxWidth));

        /// <summary>Preserves primary text and bold number styles when clipping the range to its available row.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var normal = Style.Parse(TerminalTheme.Primary);
            var bold = Style.Parse($"bold {TerminalTheme.Primary}");
            var segments = new List<Segment>();
            var start = 0;
            while (start < value.Length)
            {
                var numeric = char.IsDigit(value[start]);
                var end = start + 1;
                while (end < value.Length && char.IsDigit(value[end]) == numeric)
                {
                    end++;
                }
                segments.Add(new Segment(value[start..end], numeric ? bold : normal));
                start = end;
            }
            return maxWidth > 0 ? Segment.Truncate(segments, maxWidth) : [];
        }
    }

    /// <summary>Uses a concrete example while leaving the full original syntax available in static help.</summary>
    private static string CommandExample(HelpEntry entry) => entry.Example
        ?? (entry.Command.StartsWith("hm ", StringComparison.Ordinal) ? entry.Command : "hm " + entry.Command);

    /// <summary>Separates syntax, indented explanations, and concrete examples while keeping hm white.</summary>
    internal static IRenderable RenderEntry(HelpEntry entry)
    {
        var example = CommandExample(entry);
        var rows = new List<IRenderable>
        {
            HelpCommandLine.Create(entry.Command.StartsWith("hm ", StringComparison.Ordinal) ? entry.Command : "hm " + entry.Command),
            new Padder(HelpCommandLine.CreateDescription(entry.Description, example), new Padding(2, 0, 0, 0))
        };
        if (entry.Example is not null)
        {
            rows.Add(new Padder(HelpCommandLine.Create(example), new Padding(2, 1, 0, 0)));
        }
        rows.AddRange(entry.Arguments.Select(argument => new Padder(
            HelpCommandLine.CreateArgument(example, argument.Token, argument.Description), new Padding(4, 0, 0, 0))));
        rows.Add(new Text(" "));
        return new Rows(rows);
    }

    /// <summary>Renders only the selected complete terminal lines of a command grid.</summary>
    private sealed class HelpLines(IReadOnlyList<SegmentLine> lines) : IRenderable
    {
        /// <summary>Lets the surrounding layout assign the content column's available width.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, maxWidth);

        /// <summary>Preserves styles and explicit line boundaries without rewrapping the selected content.</summary>
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

    /// <summary>Keeps navigation and fixed-height chrome on one visual row even in a narrow terminal.</summary>
    private sealed class HelpLine(string value, Style style) : IRenderable
    {
        /// <summary>Measures a label without requesting more than the assigned width.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Min(new Segment(value).CellCount(), maxWidth));

        /// <summary>Truncates the entire label rather than letting individual words wrap into another region.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
            maxWidth > 0 ? Segment.SplitOverflow(new Segment(value, style), Overflow.Ellipsis, maxWidth) : [];
    }

    private enum HelpFocus
    {
        Sections,
        Commands,
        Close
    }
}

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
    public string? OpenHintKey { get; init; }
}

/// <summary>Browses command sections in a temporary open layout without changing the original terminal buffer.</summary>
internal sealed class FullscreenHelpView(IAnsiConsole console, ILocalizationService text, ConsoleRenderOptions? options = null)
{
    private const int HeadingRows = 2;
    private readonly ConsoleRenderOptions _options = options ?? new(false, false);
    private readonly FullscreenSectionNavigator _sectionNavigator = new();
    private readonly FullscreenInput _input = new(console, text);
    private int _section;
    private int _offset;
    private int _lineCount;
    private int _visibleRows;
    private HelpFocus _focus = HelpFocus.Sections;
    private FullscreenFrame? _lastFrame;

    /// <summary>Uses fullscreen help only when both terminal input and the alternate buffer are available.</summary>
    internal static bool CanUse(IAnsiConsole console) => FullscreenViewport.CanUse(console);

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
        _sectionNavigator.Reset(_section, sections.Count, focused: true);
        _offset = 0;
        _lastFrame = null;
        _input.Reset();
        try
        {
            Action? open;
            do
            {
                open = null;
                _lastFrame = null;
                FullscreenViewport.Run(console, () => open = RunLoop(sections));
                // Each screen owns its alternate buffer; the help selection survives the round trip.
                open?.Invoke();
            }
            while (open is not null);
        }
        catch (InteractiveFlowCanceledException)
        {
            // Escape is a normal close action for this read-only browser.
        }
        finally
        {
            _input.Reset();
        }
    }

    /// <summary>Navigates descriptions and returns screen actions for opening outside the help buffer.</summary>
    private Action? RunLoop(IReadOnlyList<HelpSection> sections)
    {
        while (true)
        {
            void Paint() => PaintScreen(sections);
            Paint();
            var readKey = _input.ReadKey(Paint, _sectionNavigator.PendingSelectionDeadline);
            if (readKey is null)
            {
                CompletePendingSectionNumber(sections.Count);
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
            if (key.Key == ConsoleKey.LeftArrow && (key.Modifiers & ConsoleModifiers.Control) != 0)
            {
                _focus = HelpFocus.Sections;
                continue;
            }
            if (_focus != HelpFocus.Close && TrySelectSectionNumber(key, sections.Count))
            {
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
                        _sectionNavigator.Select(_section, sections.Count);
                    }
                    _offset = 0;
                    break;
                case ConsoleKey.End:
                    if (_focus == HelpFocus.Sections)
                    {
                        _section = sections.Count - 1;
                        _sectionNavigator.Select(_section, sections.Count);
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
        _sectionNavigator.Move(delta, count);
        _section = _sectionNavigator.SelectedIndex;
        _offset = 0;
    }

    /// <summary>Delegates numeric help-section shortcuts to the same sidebar navigator used by settings and skills.</summary>
    private bool TrySelectSectionNumber(ConsoleKeyInfo key, int count)
    {
        var previous = _section;
        if (!_sectionNavigator.TrySelectNumber(key, count))
        {
            return false;
        }
        if (_sectionNavigator.SelectedIndex != previous)
        {
            ApplySelectedSection();
        }
        _focus = HelpFocus.Sections;
        return true;
    }

    /// <summary>Completes a pending first digit after the shared two-digit shortcut window expires.</summary>
    private void CompletePendingSectionNumber(int count)
    {
        var previous = _section;
        if (_sectionNavigator.CompletePendingNumber(count) && _sectionNavigator.SelectedIndex != previous)
        {
            ApplySelectedSection();
        }
    }

    /// <summary>Moves help content to the selected sidebar section and returns its command list to the top.</summary>
    private void ApplySelectedSection()
    {
        _section = _sectionNavigator.SelectedIndex;
        _offset = 0;
        _focus = HelpFocus.Sections;
    }

    /// <summary>Moves through already wrapped command lines without passing the visible range.</summary>
    private void Scroll(int delta) => _offset = Math.Clamp(_offset + delta, 0, Math.Max(0, _lineCount - _visibleRows));

    /// <summary>Uses the setup viewport's margins, sidebar, action bar, and shared header around colored command examples.</summary>
    private void PaintScreen(IReadOnlyList<HelpSection> sections)
    {
        var frame = FullscreenViewport.BeginFrame(console, _lastFrame);
        _lastFrame = frame;
        var width = Math.Max(1, frame.Width - 1);
        var height = Math.Max(1, frame.Height - 1);
        if (frame.Width < 60 || frame.Height < 20)
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
            message = Line(text.Text(active.OpenHintKey
                ?? throw new InvalidOperationException("An actionable help section needs opening guidance.")), TerminalTheme.Primary);
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

    /// <summary>Renders the selected help section through the shared dot-numbered fullscreen sidebar.</summary>
    private IRenderable SectionNavigation(IReadOnlyList<HelpSection> sections, int bodyHeight)
    {
        _sectionNavigator.IsFocused = _focus == HelpFocus.Sections;
        return _sectionNavigator.Render(sections, section => SectionTitle(section, compact: true), bodyHeight,
            text.Text("Help.Browse.Sections"), console.Profile.Capabilities.Unicode);
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

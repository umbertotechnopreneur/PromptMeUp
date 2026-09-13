// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Pairs a public command with its localized description.</summary>
internal sealed record HelpEntry(string Command, string Description);

/// <summary>Groups help entries under full and compact localized section names.</summary>
internal sealed record HelpSection(string Icon, string Title, string NavigationLabel, IReadOnlyList<HelpEntry> Entries);

/// <summary>Browses command sections in a temporary open layout without changing the original terminal buffer.</summary>
internal sealed class FullscreenHelpView(IAnsiConsole console, ILocalizationService text)
{
    private int _section;
    private int _offset;
    private int _lineCount;
    private int _visibleRows;

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
        try
        {
            console.AlternateScreen(() =>
            {
                console.Cursor.Hide();
                try
                {
                    RunLoop(sections);
                }
                finally
                {
                    console.WriteAnsi(writer => writer.ResetStyle());
                    console.Cursor.Show();
                }
            });
        }
        catch (InteractiveFlowCanceledException)
        {
            // Escape is a normal close action for this read-only browser.
        }
    }

    /// <summary>Navigates sections independently of scrolling their descriptions.</summary>
    private void RunLoop(IReadOnlyList<HelpSection> sections)
    {
        while (true)
        {
            void Paint() => PaintScreen(sections);
            Paint();
            var key = ReadKey(Paint);
            if (key.Key is ConsoleKey.Escape or ConsoleKey.Q)
            {
                return;
            }
            if (console.Profile.Width < 60 || console.Profile.Height < 20)
            {
                continue;
            }
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                case ConsoleKey.PageUp:
                    ChangeSection(-1, sections.Count);
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.PageDown:
                    ChangeSection(1, sections.Count);
                    break;
                case ConsoleKey.Tab:
                    ChangeSection((key.Modifiers & ConsoleModifiers.Shift) != 0 ? -1 : 1, sections.Count);
                    break;
                case ConsoleKey.UpArrow:
                    _offset = Math.Max(0, _offset - 1);
                    break;
                case ConsoleKey.DownArrow:
                    _offset = Math.Min(Math.Max(0, _lineCount - _visibleRows), _offset + 1);
                    break;
                case ConsoleKey.Home:
                    _offset = 0;
                    break;
                case ConsoleKey.End:
                    _offset = Math.Max(0, _lineCount - _visibleRows);
                    break;
            }
        }
    }

    /// <summary>Starts each newly selected section at its first command.</summary>
    private void ChangeSection(int delta, int count)
    {
        _section = (_section + delta + count) % count;
        _offset = 0;
    }

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

    /// <summary>Builds a bounded header, section list, scrollable command grid, and one footer separator.</summary>
    private void PaintScreen(IReadOnlyList<HelpSection> sections)
    {
        console.WriteAnsi(writer =>
        {
            writer.Background(Style.Parse(TerminalTheme.Background).Foreground);
            writer.EraseInDisplay(2);
            writer.CursorHome();
        });
        var width = Math.Max(1, console.Profile.Width - 1);
        var height = Math.Max(1, console.Profile.Height - 1);
        if (console.Profile.Width < 60 || console.Profile.Height < 20)
        {
            var small = new Layout("help", new Text(text.Text("Help.Browse.TooSmall"), Style.Parse(TerminalTheme.Warning)));
            console.Write(new FormSurface(small));
            return;
        }

        const int headerRows = 3;
        const int footerRows = 3;
        const int sectionHeadingRows = 3;
        var navigationWidth = Math.Clamp(width / 4, 18, 28);
        var contentWidth = width - navigationWidth - 2;
        _visibleRows = height - headerRows - footerRows - sectionHeadingRows;
        var active = sections[_section];
        var grid = new Grid().AddColumn();
        foreach (var entry in active.Entries)
        {
            grid.AddRow(new Text(entry.Command, Style.Parse($"bold {TerminalTheme.Accent}")).Overflow(Overflow.Fold));
            grid.AddRow(new Text(entry.Description, Style.Parse(TerminalTheme.Primary)).Overflow(Overflow.Fold));
            grid.AddRow(new Text(" "));
        }
        var options = new RenderOptions(console.Profile.Capabilities, new Size(width, height));
        var lines = Segment.SplitLines(((IRenderable)grid).Render(options, contentWidth));
        _lineCount = lines.Count;
        _offset = Math.Clamp(_offset, 0, Math.Max(0, _lineCount - _visibleRows));

        var navigation = new List<IRenderable>
        {
            Line(text.Text("Help.Browse.Sections"), TerminalTheme.Info), new Text(" ")
        };
        for (var index = 0; index < sections.Count; index++)
        {
            var selected = index == _section;
            navigation.Add(Line($"{(selected ? ">" : " ")} {index + 1} {sections[index].NavigationLabel}",
                selected ? TerminalTheme.SelectionForeground : TerminalTheme.Primary,
                selected ? TerminalTheme.SelectionBackground : null));
            navigation.Add(new Text(" "));
        }

        var root = new Layout("help").SplitRows(
            new Layout("header", new Rows(
                Line($"hm / PromptMeUp  ·  {text.Text("Help.Title")}", TerminalTheme.Accent),
                Line(text.Text("Help.Usage"), TerminalTheme.Muted), new Text(" "))).Size(headerRows),
            new Layout("body"),
            new Layout("footer", new Rows(
                new Rule { Style = Style.Parse(TerminalTheme.Divider) },
                Line(text.Text("Help.Browse.SectionKeys"), TerminalTheme.Info),
                Line(text.Text("Help.Browse.ScrollKeys"), TerminalTheme.Info))).Size(footerRows));
        root["body"].SplitColumns(
            new Layout("sections", new Rows(navigation)).Size(navigationWidth),
            new Layout("gap", Text.Empty).Size(2),
            new Layout("content").SplitRows(
                new Layout("section", new Rows(
                    Line(active.Title, TerminalTheme.Primary),
                    Line(text.Text("Help.Browse.Range", _offset + 1, Math.Min(_lineCount, _offset + _visibleRows), _lineCount), TerminalTheme.Muted),
                    new Text(" "))).Size(sectionHeadingRows),
                new Layout("commands", new HelpLines(lines.Skip(_offset).Take(_visibleRows).ToArray()))));
        console.Write(new FormSurface(root));
    }

    /// <summary>Creates a single-row label with consistent high-contrast colors.</summary>
    private static HelpLine Line(string value, string foreground, string? background = null) =>
        new(value, Style.Parse(background is null ? foreground : $"{foreground} on {background}"));

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
}

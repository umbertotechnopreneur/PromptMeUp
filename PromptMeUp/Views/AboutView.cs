// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public interface IAboutView
{
    void Render();
}

/// <summary>Presents the project artwork and information in an adaptive, passive terminal page.</summary>
public sealed class AboutView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell,
    BuildInformation buildInformation) : IAboutView
{
    private const string RepositoryUrl = "https://github.com/umbertotechnopreneur/PromptMeUp";
    private const string AuthorUrl = "https://umbertogiacobbi.biz";
    private int _offset;
    private int _lineCount;
    private int _visibleRows;
    private (int Width, int Height, string Theme)? _lastFrame;

    /// <summary>Uses a disposable fullscreen buffer when supported and ordinary scrolling output otherwise.</summary>
    public void Render()
    {
        if (!FullscreenHelpView.CanUse(console))
        {
            TerminalTheme.WriteRule(console, text.Text("About.Title"));
            console.Write(CreateContent(console.Profile.Width));
            console.WriteLine();
            console.WriteLine();
            return;
        }

        _offset = 0;
        _lastFrame = null;
        try
        {
            console.AlternateScreen(() =>
            {
                console.Cursor.Hide();
                try
                {
                    RunLoop();
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
            // Escape closes this read-only page without canceling the surrounding application.
        }
    }

    /// <summary>Combines the invariant artwork with localized product details and literal project links.</summary>
    private IRenderable CreateContent(int width)
    {
        var details = new Grid();
        details.AddColumn(new GridColumn().RightAligned());
        details.AddColumn(new GridColumn().LeftAligned());
        var narrowDetails = new List<IRenderable>();
        AddDetail(details, narrowDetails, "Footer.Version", buildInformation.Version);
        AddDetail(details, narrowDetails, "About.BuildDate",
            $"{buildInformation.BuiltAtLocal.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)} ({buildInformation.TimeZone})");
        AddDetail(details, narrowDetails, "About.BuildMachine", buildInformation.MachineName);
        AddDetail(details, narrowDetails, "About.GitCommit", buildInformation.GitCommit);
        AddDetail(details, narrowDetails, "About.Author", "Umberto Giacobbi");
        AddDetail(details, narrowDetails, "About.License", "MIT");
        AddDetail(details, narrowDetails, "About.Platforms", "Windows / Linux / macOS");
        return new Rows(
            Align.Center(new HelpMeBanner()),
            new Text(" "),
            new Text(text.Text("About.Description"), Style.Parse(TerminalTheme.Primary)),
            new Text(" "),
            width >= 30 ? details : new Rows(narrowDetails),
            new Text(text.Text("About.Repository"), Style.Parse(TerminalTheme.Muted)),
            new Markup($"[underline {TerminalTheme.Info} link={RepositoryUrl}]{RepositoryUrl}[/]"),
            new Text(" "),
            new Text(text.Text("About.Website"), Style.Parse(TerminalTheme.Muted)),
            new Markup($"[underline {TerminalTheme.Info} link={AuthorUrl}]{AuthorUrl}[/]"));
    }

    /// <summary>Separates each right-aligned label and left-aligned value with one trailing blank row.</summary>
    private void AddDetail(Grid grid, List<IRenderable> narrowRows, string labelKey, string value)
    {
        var label = new Text(text.Text(labelKey) + ":", Style.Parse(TerminalTheme.Muted));
        var renderedValue = new Text(value, Style.Parse(TerminalTheme.Primary));
        grid.AddRow(label, renderedValue);
        grid.AddEmptyRow();
        narrowRows.AddRange([label, renderedValue, new Text(" ")]);
    }

    /// <summary>Scrolls the project information while leaving a permanently focused close action available.</summary>
    private void RunLoop()
    {
        while (true)
        {
            PaintScreen();
            var key = ReadKey();
            if (key.Key is ConsoleKey.Enter or ConsoleKey.Escape or ConsoleKey.Q)
            {
                return;
            }
            var maximum = Math.Max(0, _lineCount - _visibleRows);
            _offset = key.Key switch
            {
                ConsoleKey.UpArrow => Math.Max(0, _offset - 1),
                ConsoleKey.DownArrow => Math.Min(maximum, _offset + 1),
                ConsoleKey.PageUp => Math.Max(0, _offset - _visibleRows),
                ConsoleKey.PageDown => Math.Min(maximum, _offset + _visibleRows),
                ConsoleKey.Home => 0,
                ConsoleKey.End => maximum,
                _ => _offset
            };
        }
    }

    /// <summary>Keeps shutdown cancellable and repaints the viewport when it is resized while waiting for input.</summary>
    private ConsoleKeyInfo ReadKey()
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
                PaintScreen();
            }
        }
        return pending.GetAwaiter().GetResult()
            ?? throw new IOException(text.Text("Form.EndOfInput"));
    }

    /// <summary>Reads through the host wrapper so Escape and application cancellation retain their normal behavior.</summary>
    private async Task<ConsoleKeyInfo?> ReadKeyAsync() =>
        await console.Input.ReadKeyAsync(true, CancellationToken.None).ConfigureAwait(false);

    /// <summary>Fits styled content between the shared fullscreen header and footer without erasing terminal history.</summary>
    private void PaintScreen()
    {
        var frame = (console.Profile.Width, console.Profile.Height, TerminalTheme.Current.Id);
        console.WriteAnsi(writer =>
        {
            if (_lastFrame != frame)
            {
                // This erase is confined to the temporary alternate buffer.
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
            console.Write(new FormSurface(new Layout("about",
                new Text(text.Text("About.TooSmall"), Style.Parse(TerminalTheme.Warning)))));
            return;
        }

        _visibleRows = height - FullscreenHeader.Height - FullscreenFooter.Height();
        var renderOptions = new RenderOptions(console.Profile.Capabilities, new Size(width, height));
        var lines = Segment.SplitLines(CreateContent(width - 4).Render(renderOptions, width - 4)).ToList();
        _lineCount = lines.Count;
        _offset = Math.Clamp(_offset, 0, Math.Max(0, _lineCount - _visibleRows));
        var body = new Padder(new VisibleLines(lines.Skip(_offset).Take(_visibleRows).ToArray()), new Padding(2, 0, 2, 0));
        var close = new Text($"> [ {TerminalTheme.IconPrefix(shell.Options, "↩️", "<")}{text.Text("Help.Browse.Close")} ]",
            Style.Parse($"bold {TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"));
        var overflowing = _lineCount > _visibleRows;
        var notice = overflowing
            ? new Text(text.Text("Help.Browse.Range", _offset + 1, Math.Min(_lineCount, _offset + _visibleRows), _lineCount),
                Style.Parse(TerminalTheme.Primary))
            : Text.Empty;
        var hint = text.Text(overflowing ? "About.ScrollHint" : "About.CloseHint");
        if (overflowing && new Segment(hint).CellCount() > width - 4)
        {
            hint = text.Text("About.ScrollHintCompact");
        }
        var footer = FullscreenFooter.Create(notice, close, FullscreenFooter.Shortcuts(hint));
        var root = new Layout("about").SplitRows(
            new Layout("header", FullscreenHeader.Create(text.Text("About.Title"), shell.Options)).Size(FullscreenHeader.Height),
            new Layout("body", body),
            new Layout("footer", footer).Size(FullscreenFooter.Height()));
        console.Write(new FormSurface(root));
    }

    /// <summary>Preserves the already wrapped text, letter shapes, and link styles within the visible body rows.</summary>
    private sealed class VisibleLines(IReadOnlyList<SegmentLine> lines) : IRenderable
    {
        /// <summary>Accepts the content width assigned by the shared fullscreen margins.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Max(0, maxWidth));

        /// <summary>Emits only complete measured lines while preserving every terminal-cell boundary.</summary>
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

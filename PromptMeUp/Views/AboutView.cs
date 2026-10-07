// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public interface IAboutView
{
    void Render();
    IRenderable CreateContent(bool renderInstallationCard = false);
}

/// <summary>Presents the project artwork and information in an adaptive, passive terminal page.</summary>
public sealed class AboutView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell,
    BuildInformation buildInformation) : IAboutView
{
    private int _offset;
    private int _lineCount;
    private int _visibleRows;
    private FullscreenFrame? _lastFrame;
    private readonly FullscreenInput _input = new(console, text);

    /// <summary>Uses a disposable fullscreen buffer when supported and ordinary scrolling output otherwise.</summary>
    public void Render()
    {
        if (!FullscreenViewport.CanUse(console))
        {
            TerminalTheme.WriteRule(console, text.Text("About.Title"));
            console.Write(CreateContent());
            console.WriteLine();
            console.WriteLine();
            return;
        }

        _offset = 0;
        _lastFrame = null;
        _input.Reset();
        try
        {
            FullscreenViewport.Run(console, RunLoop);
        }
        catch (InteractiveFlowCanceledException)
        {
            // Escape closes this read-only page without canceling the surrounding application.
        }
        finally
        {
            _input.Reset();
        }
    }

    /// <summary>Provides shared product content, with an optional installation card for first-run setup.</summary>
    /// <param name="renderInstallationCard">Whether to emphasize installation details during first-run setup.</param>
    public IRenderable CreateContent(bool renderInstallationCard = false) => new Rows(
        new VibeWareBrand(console, new ProductBanner(text)),
        new Text(" "),
        new Text(text.Text("About.Description"), Style.Parse(TerminalTheme.Primary)),
        new Text(" "),
        new InstallationInfo(text, buildInformation, renderCard: renderInstallationCard));

    /// <summary>Scrolls the project information while leaving a permanently focused close action available.</summary>
    private void RunLoop()
    {
        while (true)
        {
            PaintScreen();
            var key = _input.ReadKey(PaintScreen) ?? throw new IOException(text.Text("Form.EndOfInput"));
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

    /// <summary>Fits styled content between the shared fullscreen header and footer without erasing terminal history.</summary>
    private void PaintScreen()
    {
        _lastFrame = FullscreenViewport.BeginFrame(console, _lastFrame);
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
        var lines = Segment.SplitLines(CreateContent().Render(renderOptions, width - 4)).ToList();
        _lineCount = lines.Count;
        _offset = Math.Clamp(_offset, 0, Math.Max(0, _lineCount - _visibleRows));
        var body = new Padder(new VisibleLines(lines.Skip(_offset).Take(_visibleRows).ToArray()), new Padding(2, 0, 2, 0));
        var close = TerminalActionBar.Create(
        [
            new TerminalAction(TerminalTheme.IconPrefix(shell.Options, "↩️", "<") + text.Text("Help.Browse.Close"),
                TerminalTheme.Warning, Selected: true)
        ]);
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

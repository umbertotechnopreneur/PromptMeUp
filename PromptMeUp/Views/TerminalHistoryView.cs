// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Displays retained turns and expanded results in a disposable read-only viewport.</summary>
internal static class TerminalHistoryView
{
    /// <summary>Identifies shortcuts shared by prompt docks and inline conversation menus.</summary>
    internal static bool IsShortcut(ConsoleKeyInfo key) => key.Key == ConsoleKey.F2
        || key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key is ConsoleKey.PageUp or ConsoleKey.PageDown;

    /// <summary>Opens the requested turn and restores the main buffer and any caller-owned draft on return.</summary>
    internal static void Show(IAnsiConsole console, ILocalizationService text, ConsoleKeyInfo shortcut,
        TerminalInputReader reader)
    {
        var history = TerminalSession.For(console).History;
        if (!FullscreenViewport.CanUse(console) || history.Turns.Count == 0) return;
        if (shortcut.Key == ConsoleKey.F2)
        {
            if (!history.SelectLatestDetails()) return;
        }
        else history.Move(shortcut.Key == ConsoleKey.PageDown ? 1 : -1);

        var input = new FullscreenInput(console, text, reader);
        var offset = 0;
        var lineCount = 0;
        FullscreenFrame? frame = null;
        TerminalTurn? renderedTurn = null;
        FullscreenFrame? renderedFrame = null;
        SegmentLine[] lines = [];

        void Paint()
        {
            frame = FullscreenViewport.BeginFrame(console, frame);
            var width = Math.Max(1, console.Profile.Width - 1);
            var height = Math.Max(1, console.Profile.Height - 1);
            var turn = history.Turns[history.SelectedIndex!.Value];
            if (renderedTurn != turn || renderedFrame != frame)
            {
                var options = new RenderOptions(console.Profile.Capabilities, new Size(width, height));
                lines = Segment.SplitLines(turn.Content.Render(options, width)).ToArray();
                renderedTurn = turn;
                renderedFrame = frame;
            }
            lineCount = lines.Length;
            var bodyRows = Math.Max(1, height - 4);
            offset = Math.Clamp(offset, 0, Math.Max(0, lineCount - bodyRows));
            var heading = text.Text("Terminal.TurnPosition", turn.Number, history.Turns[^1].Number)
                + " · " + text.Text("Terminal.Role." + turn.Kind);
            if (!string.IsNullOrWhiteSpace(turn.Title)) heading += " · " + turn.Title;
            WriteRow(console, new Markup($"[bold {TerminalTheme.Accent}]{Markup.Escape(TerminalText.Clip(heading, width))}[/]"), width);
            WriteRow(console, new ThemeSeparator(width: width), width);
            for (var row = 0; row < bodyRows; row++)
            {
                WriteRow(console, new SegmentRow(offset + row < lines.Length ? lines[offset + row] : []), width);
            }
            WriteRow(console, new ThemeSeparator(width: width), width);
            WriteRow(console, new Text(TerminalText.Clip(text.Text("Terminal.HistoryKeys"), width), Style.Parse(TerminalTheme.Info)), width, last: true);
        }

        try
        {
            FullscreenViewport.Run(console, () =>
            {
                while (true)
                {
                    Paint();
                    var key = input.ReadKey(Paint);
                    if (key is not { } pressed) continue;
                    if (pressed.Key is ConsoleKey.Escape or ConsoleKey.Enter or ConsoleKey.F2) return;
                    var page = Math.Max(1, console.Profile.Height - 5);
                    if (pressed.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow || IsShortcut(pressed))
                    {
                        history.Move(pressed.Key is ConsoleKey.RightArrow or ConsoleKey.PageDown ? 1 : -1);
                        offset = 0;
                    }
                    else offset = pressed.Key switch
                    {
                        ConsoleKey.UpArrow => Math.Max(0, offset - 1),
                        ConsoleKey.DownArrow => Math.Min(lineCount - 1, offset + 1),
                        ConsoleKey.PageUp => Math.Max(0, offset - page),
                        ConsoleKey.PageDown => Math.Min(lineCount - 1, offset + page),
                        ConsoleKey.Home => 0,
                        ConsoleKey.End => Math.Max(0, lineCount - page),
                        _ => offset
                    };
                }
            });
        }
        catch (InteractiveFlowCanceledException)
        {
            // Escape closes only this disposable viewer; the caller retains its input and selection.
        }
        finally
        {
            input.Reset();
        }
    }

    /// <summary>Replaces one alternate-buffer row, including the unused cells of a previous longer line.</summary>
    private static void WriteRow(IAnsiConsole console, IRenderable content, int width, bool last = false)
    {
        console.WriteAnsi(writer =>
        {
            writer.Background(Style.Parse(TerminalTheme.Background).Foreground);
            writer.EraseInLine(2);
        });
        console.Write(new SegmentRow(Segment.Truncate(content.GetSegments(console), width)));
        if (!last) console.WriteLine();
    }

    /// <summary>Preserves existing segment styles without reparsing provider output as terminal markup.</summary>
    private sealed class SegmentRow(IEnumerable<Segment> segments) : IRenderable
    {
        /// <summary>Accepts exactly one available row.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Max(0, maxWidth));

        /// <summary>Emits the visible slice of one prewrapped content row.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
            Segment.Truncate(segments.Where(segment => !segment.IsLineBreak && !segment.IsControlCode), Math.Max(0, maxWidth))
                .Select(segment => new Segment(segment.Text, new Style(
                    segment.Style.Foreground == Color.Default ? Style.Parse(TerminalTheme.Primary).Foreground : segment.Style.Foreground,
                    Style.Parse(TerminalTheme.Background).Foreground, segment.Style.Decoration), segment.Link));
    }
}

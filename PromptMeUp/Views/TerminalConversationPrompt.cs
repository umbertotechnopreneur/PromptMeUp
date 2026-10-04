// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Shares inline decision menus with a docked session strip and the conversation's history shortcuts.</summary>
internal static class TerminalConversationPrompt
{
    /// <summary>Returns an explicit menu choice while retaining selection when details or previous turns are inspected.</summary>
    /// <param name="console">The console used to display and read the menu.</param>
    /// <param name="text">The localized menu text.</param>
    /// <param name="choices">The available menu choices.</param>
    /// <param name="title">The heading displayed above the choices.</param>
    /// <param name="numbered">Whether number keys select choices immediately.</param>
    /// <param name="echoSelection">Whether the selected label remains in the conversation.</param>
    /// <exception cref="ArgumentException">Thrown when no choices are provided.</exception>
    internal static T Select<T>(IAnsiConsole console, ILocalizationService text,
        IReadOnlyList<TerminalMenuChoice<T>> choices, string title, bool numbered = false, bool echoSelection = true)
    {
        if (choices.Count == 0) throw new ArgumentException("A conversation menu needs choices.", nameof(choices));
        if (!console.Profile.Capabilities.Interactive || !console.Profile.Capabilities.Ansi
            || !console.Profile.Out.IsTerminal || Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return TerminalChoiceMenu.Select(console, choices, title);
        }
        return SelectInteractive(console, text, choices, title, numbered, echoSelection);
    }

    /// <summary>Reads an inline decision without holding a live renderer while input is pending.</summary>
    /// <param name="console">The console used to display and read the menu.</param>
    /// <param name="text">The localized menu text.</param>
    /// <param name="choices">The available menu choices.</param>
    /// <param name="title">The heading displayed above the choices.</param>
    /// <param name="numbered">Whether number keys select choices immediately.</param>
    /// <param name="echoSelection">Whether the selected label remains in the conversation.</param>
    /// <exception cref="InteractiveFlowCanceledException">Thrown when the user cancels the menu.</exception>
    internal static T SelectInteractive<T>(IAnsiConsole console, ILocalizationService text,
        IReadOnlyList<TerminalMenuChoice<T>> choices, string title, bool numbered = false, bool echoSelection = true)
    {
        using var state = new TerminalStateScope(console, text, TerminalActivityState.NeedsInput);
        using var paste = new TerminalPasteScope(console);
        var reader = new TerminalInputReader(console.Input, 1024, win32Encoding: OperatingSystem.IsWindows());
        var input = new FullscreenInput(console, text, reader);
        var session = TerminalSession.For(console);
        var selected = 0;

        IReadOnlyList<IRenderable> Render()
        {
            var width = Math.Max(1, console.Profile.Width - 1);
            var rows = new List<IRenderable>();
            if (console.Profile.Height >= 10)
            {
                rows.Add(new ThemeSeparator(width: width));
                rows.AddRange(new TerminalSessionStrip(text, session.LastStatus, session).Rows(width, showBreakdown: false)
                    .Select(row => new Markup(row)));
            }
            rows.Add(new Markup($"[bold {TerminalTheme.Info}]{Markup.Escape(TerminalText.Clip(title, width))}[/]"));
            rows.Add(new Text(" "));
            var visible = Math.Clamp((console.Profile.Height - rows.Count - 3) / 2, 1, Math.Min(7, choices.Count));
            var first = Math.Clamp(selected - visible / 2, 0, choices.Count - visible);
            for (var index = first; index < first + visible; index++)
            {
                if (index > first) rows.Add(new Text(" "));
                var choice = choices[index];
                var label = (numbered ? $"{index}  " : string.Empty) + choice.Label;
                if (!string.IsNullOrWhiteSpace(choice.Detail)) label += " · " + choice.Detail;
                var style = index == selected
                    ? $"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"
                    : TerminalChoiceMenu.Color(choice.Tone);
                rows.Add(new Markup($"[{style}]{Markup.Escape(TerminalText.Clip((index == selected ? "> " : "  ") + label, width))}[/]"));
            }
            rows.Add(new Text(" "));
            var hint = text.Text("Terminal.ChoiceKeys", selected + 1, choices.Count);
            rows.Add(new Text(TerminalText.Clip(hint, width), Style.Parse(TerminalTheme.Muted)));
            if (FullscreenViewport.CanUse(console) && session.History.Turns.Count > 0)
                rows.Add(new Text(TerminalText.Clip(text.Text("Terminal.NavigationKeys"), width), Style.Parse(TerminalTheme.Muted)));
            return rows;
        }

        TerminalPromptDock.Align(console, reservedRows: Math.Min(console.Profile.Height - 1, choices.Count * 2 + 6));
        var paintedRows = 0;
        var paintedSize = (Width: console.Profile.Width, Height: console.Profile.Height);

        void EraseMenu()
        {
            if (paintedRows == 0) return;
            if (paintedSize != (console.Profile.Width, console.Profile.Height))
            {
                console.WriteLine();
                paintedRows = 0;
                return;
            }
            console.WriteAnsi(writer =>
            {
                writer.CursorUp(paintedRows);
                writer.Write("\r");
                for (var row = 0; row < paintedRows; row++)
                {
                    writer.EraseInLine(2);
                    if (row + 1 < paintedRows) writer.CursorDown(1);
                }
                if (paintedRows > 1) writer.CursorUp(paintedRows - 1);
            });
            paintedRows = 0;
        }

        void PaintMenu()
        {
            EraseMenu();
            paintedSize = (console.Profile.Width, console.Profile.Height);
            foreach (var row in Render())
            {
                console.Write(row);
                console.WriteLine();
                paintedRows++;
            }
        }

        console.Cursor.Hide();
        try
        {
            PaintMenu();
            while (true)
            {
                var pressed = input.ReadKey(PaintMenu);
                if (pressed is not { } key) continue;
                if (TerminalHistoryView.IsShortcut(key))
                {
                    EraseMenu();
                    TerminalHistoryView.Show(console, text, key, reader);
                    TerminalPromptDock.Align(console, reservedRows: Math.Min(console.Profile.Height - 1, choices.Count * 2 + 6));
                    PaintMenu();
                    continue;
                }
                if (key.Key == ConsoleKey.Escape) throw new InteractiveFlowCanceledException();
                if (numbered && choices.Count <= 10 && key.Modifiers == 0
                    && key.KeyChar is >= '0' and <= '9' && key.KeyChar - '0' < choices.Count)
                {
                    selected = key.KeyChar - '0';
                    break;
                }
                if (key.Key == ConsoleKey.Enter && key.Modifiers == 0) break;
                selected = key.Key switch
                {
                    ConsoleKey.UpArrow => (selected + choices.Count - 1) % choices.Count,
                    ConsoleKey.DownArrow => (selected + 1) % choices.Count,
                    ConsoleKey.Tab => (selected + (key.Modifiers.HasFlag(ConsoleModifiers.Shift) ? choices.Count - 1 : 1)) % choices.Count,
                    ConsoleKey.Home => 0,
                    ConsoleKey.End => choices.Count - 1,
                    _ => selected
                };
                PaintMenu();
            }
            EraseMenu();
            if (echoSelection)
            {
                console.Write(new Text(choices[selected].Label, Style.Parse(TerminalTheme.Info)));
                console.WriteLine();
                console.WriteLine();
            }
            return choices[selected].Value;
        }
        finally
        {
            EraseMenu();
            console.Cursor.Show();
        }
    }

    /// <summary>Keeps approval explicitly default-negative while sharing the inline decision and history controls.</summary>
    internal static bool Confirm(IAnsiConsole console, ILocalizationService text, string title) =>
        Select<bool>(console, text,
        [
            new(false, text.Text("Common.No"), Tone: TerminalMenuTone.Muted),
            new(true, text.Text("Common.Yes"), Tone: TerminalMenuTone.Caution)
        ], title);
}

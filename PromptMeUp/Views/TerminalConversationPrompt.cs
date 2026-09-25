// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Shares inline decision menus with a docked session strip and the conversation's history shortcuts.</summary>
internal static class TerminalConversationPrompt
{
    /// <summary>Returns an explicit menu choice while retaining selection when details or previous turns are inspected.</summary>
    internal static T Select<T>(IAnsiConsole console, ILocalizationService text,
        IReadOnlyList<TerminalMenuChoice<T>> choices, string title, bool numbered = false)
    {
        if (choices.Count == 0) throw new ArgumentException("A conversation menu needs choices.", nameof(choices));
        if (!console.Profile.Capabilities.Interactive || !console.Profile.Capabilities.Ansi
            || !console.Profile.Out.IsTerminal || Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return TerminalChoiceMenu.Select(console, choices, title);
        }
        using var state = new TerminalStateScope(console, text, TerminalActivityState.NeedsInput);
        using var paste = new TerminalPasteScope(console);
        var reader = new TerminalInputReader(console.Input, 1024, win32Encoding: OperatingSystem.IsWindows());
        var input = new FullscreenInput(console, text, reader);
        var session = TerminalSession.For(console);
        var selected = 0;

        IRenderable Render()
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
            var visible = Math.Clamp(console.Profile.Height - rows.Count - 4, 1, Math.Min(7, choices.Count));
            var first = Math.Clamp(selected - visible / 2, 0, choices.Count - visible);
            for (var index = first; index < first + visible; index++)
            {
                var choice = choices[index];
                var label = (numbered ? $"{index}  " : string.Empty) + choice.Label;
                if (!string.IsNullOrWhiteSpace(choice.Detail)) label += " · " + choice.Detail;
                var style = index == selected
                    ? $"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"
                    : TerminalChoiceMenu.Color(choice.Tone);
                rows.Add(new Markup($"[{style}]{Markup.Escape(TerminalText.Clip((index == selected ? "> " : "  ") + label, width))}[/]"));
            }
            var hint = text.Text("Terminal.ChoiceKeys", selected + 1, choices.Count);
            if (FullscreenViewport.CanUse(console) && session.History.Turns.Count > 0)
                hint += " · " + text.Text("Terminal.NavigationKeys");
            rows.Add(new Text(TerminalText.Clip(hint, width), Style.Parse(TerminalTheme.Muted)));
            return new Rows(rows);
        }

        TerminalPromptDock.Align(console, reservedRows: Math.Min(console.Profile.Height - 1, choices.Count + 5));
        while (true)
        {
            var action = console.Live(Render()).AutoClear(true).Start(context =>
            {
                while (true)
                {
                    var key = input.ReadKey(() => context.UpdateTarget(Render()));
                    if (key is not { } pressed) continue;
                    if (TerminalHistoryView.IsShortcut(pressed)) return pressed;
                    if (pressed.Key == ConsoleKey.Enter) return pressed;
                    if (pressed.Key == ConsoleKey.Escape) throw new InteractiveFlowCanceledException();
                    if (numbered && choices.Count <= 10 && pressed.Modifiers == 0
                        && pressed.KeyChar is >= '0' and <= '9' && pressed.KeyChar - '0' < choices.Count)
                    {
                        selected = pressed.KeyChar - '0';
                        return new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
                    }
                    selected = pressed.Key switch
                    {
                        ConsoleKey.UpArrow => (selected + choices.Count - 1) % choices.Count,
                        ConsoleKey.DownArrow => (selected + 1) % choices.Count,
                        ConsoleKey.Tab => (selected + (pressed.Modifiers.HasFlag(ConsoleModifiers.Shift) ? choices.Count - 1 : 1)) % choices.Count,
                        ConsoleKey.Home => 0,
                        ConsoleKey.End => choices.Count - 1,
                        _ => selected
                    };
                    context.UpdateTarget(Render());
                }
            });
            if (TerminalHistoryView.IsShortcut(action))
            {
                TerminalHistoryView.Show(console, text, action, reader);
                continue;
            }
            console.Write(new Text(choices[selected].Label, Style.Parse(TerminalTheme.Info)));
            console.WriteLine();
            return choices[selected].Value;
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

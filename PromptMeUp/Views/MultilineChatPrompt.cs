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


using System.Globalization;
using System.Text;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Edits a small scrolling input area with paste boundaries and an explicit keyboard submission.</summary>
internal sealed class MultilineChatPrompt(IAnsiConsole console, ILocalizationService text)
{
    private int _paintedRows;
    private (int Width, int Height) _paintedSize;
    private string _label = string.Empty;
    private ShellRuntimeStatus? _status;

    /// <summary>Collects bounded pasted or typed text, preserving line breaks until a separate Enter key submits it.</summary>
    /// <param name="label">The label displayed beside the active input line.</param>
    /// <param name="maximumCharacters">The maximum number of characters accepted in the draft.</param>
    /// <param name="status">The runtime status displayed above the input.</param>
    /// <param name="showHint">Whether to show the full editing guide.</param>
    /// <exception cref="ArgumentOutOfRangeException">The character limit is not positive.</exception>
    /// <exception cref="InvalidOperationException">The terminal does not support paste-aware input.</exception>
    internal string Read(string label, int maximumCharacters, ShellRuntimeStatus? status = null, bool showHint = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCharacters);
        using var pasteMode = new TerminalPasteScope(console);
        if (!pasteMode.Enabled)
        {
            throw new InvalidOperationException(text.Text("Chat.PasteUnavailable"));
        }
        var buffer = new ChatInputBuffer(maximumCharacters);
        var reader = new TerminalInputReader(console.Input, maximumCharacters, win32Encoding: OperatingSystem.IsWindows());
        _label = label;
        _status = status;
        var session = TerminalSession.For(console);
        session.LastStatus = status;
        using var state = new TerminalStateScope(console, text, TerminalActivityState.Ready);
        var hint = text.Text(
            showHint ? "Chat.MultilineHint" : "Chat.InputShortHint",
            KeyPrefix("Enter"), KeyPrefix("Newline"), KeyPrefix("Arrows"), KeyPrefix("Escape"));
        console.MarkupLine($"  [{TerminalTheme.Muted}]{hint}[/]");
        TerminalPromptDock.Align(console, reservedRows: ReservedRows());
        console.Cursor.Hide();
        try
        {
            string? error = null;
            while (true)
            {
                Paint(buffer, error, maximumCharacters);
                var input = reader.Read();
                error = null;
                if (input.PasteTooLong || input.Paste is { } pasted && !buffer.Insert(pasted))
                {
                    error = text.Text("Chat.InputTooLong", maximumCharacters);
                }
                else if (input.Key is { } key)
                {
                    if (TerminalHistoryView.IsShortcut(key) && FullscreenViewport.CanUse(console))
                    {
                        EraseDraft();
                        TerminalHistoryView.Show(console, text, key, reader);
                        console.Cursor.Hide();
                        TerminalPromptDock.Align(console, reservedRows: ReservedRows());
                        continue;
                    }
                    if (key.Key == ConsoleKey.Enter && key.Modifiers == 0)
                    {
                        if (string.IsNullOrWhiteSpace(buffer.Text))
                        {
                            continue;
                        }
                        EraseDraft();
                        TerminalTurnHeader.Write(console, text, TerminalTurnKind.User);
                        ConversationText.Write(console, new Text(SafeDisplay(buffer.Text), Style.Parse(TerminalTheme.Primary)));
                        session.History.Add(TerminalTurnKind.User, text.Text("Terminal.Role.User"),
                            new Text(TerminalText.Safe(buffer.Text), Style.Parse(TerminalTheme.Primary)), buffer.Text.Length);
                        return buffer.Text;
                    }
                    if (!buffer.Edit(key))
                    {
                        error = text.Text("Chat.InputTooLong", maximumCharacters);
                    }
                }
            }
        }
        finally
        {
            console.Cursor.Show();
            _label = string.Empty;
            _status = null;
        }
    }

    /// <summary>Highlights readable localized key names separately from their short action descriptions.</summary>
    private string KeyPrefix(string key) =>
        $"[bold {TerminalTheme.Info}]{Markup.Escape(text.Text("Chat.Key." + key))}[/] ";

    /// <summary>Redraws only owned input rows; resizing starts a fresh area without touching earlier scrollback.</summary>
    /// <param name="buffer">The editable draft and its caret.</param>
    /// <param name="error">An optional input error.</param>
    /// <param name="maximumCharacters">The draft's character limit.</param>
    private void Paint(ChatInputBuffer buffer, string? error, int maximumCharacters)
    {
        var size = (console.Profile.Width, console.Profile.Height);
        if (_paintedRows > 0 && _paintedSize == size)
        {
            EraseDraft();
        }
        else if (_paintedRows > 0)
        {
            console.WriteLine();
            _paintedRows = 0;
            TerminalPromptDock.Align(console, reservedRows: ReservedRows());
        }
        var width = Math.Max(1, size.Width - 1 - TerminalSession.For(console).ChatIndent);
        var bar = new TerminalPromptBar(text, _status,
            showBorders: size.Height >= 8, showStatus: size.Height >= 4,
            showBreakdown: size.Height >= 8, session: TerminalSession.For(console));
        var header = bar.Header(width).ToList();
        if (NavigationHint(width) is { } navigation) header.Add(navigation);
        var footer = bar.Footer(width);
        foreach (var row in header)
        {
            WriteRow(row);
        }
        var lines = buffer.Text.Split('\n');
        var nearLimit = (long)buffer.Text.Length * 5 >= (long)maximumCharacters * 4;
        var showStatus = size.Height > 1 && (lines.Length > 1 || nearLimit || error is not null);
        var inputRows = Math.Min(lines.Length,
            Math.Clamp(size.Height - header.Count - footer.Count - (showStatus ? 1 : 0) - 1, 1, 6));
        var rowCount = inputRows + (showStatus ? 1 : 0);
        var current = buffer.Text.AsSpan(0, buffer.Cursor).Count('\n');
        var lineStart = buffer.Cursor == 0 ? 0 : buffer.Text.LastIndexOf('\n', buffer.Cursor - 1) + 1;
        var first = Math.Clamp(current - inputRows / 2, 0, Math.Max(0, lines.Length - inputRows));
        for (var offset = 0; offset < inputRows; offset++)
        {
            var index = first + offset;
            var active = index == current;
            var prompt = active && index == 0 ? _label : active ? "›" : string.Empty;
            var prefix = active ? "[bold " + TerminalTheme.Accent + "]" + Markup.Escape(prompt) + "[/] " : "  ";
            var line = index < lines.Length ? lines[index] : string.Empty;
            var prefixWidth = active ? new Segment(prompt + " ").CellCount() : 2;
            var markup = prefix + FormatLine(line, active ? buffer.Cursor - lineStart : null, Math.Max(1, width - prefixWidth));
            WriteRow(width > prefixWidth ? markup : FormatLine(line, null, width));
        }
        if (showStatus)
        {
            var status = error ?? (nearLimit
                ? text.Text("Chat.InputCount", buffer.Text.Length, maximumCharacters, maximumCharacters - buffer.Text.Length)
                : text.Text("Chat.InputLine", current + 1, lines.Length));
            var statusColor = error is not null ? TerminalTheme.Error : nearLimit ? TerminalTheme.Warning : TerminalTheme.Muted;
            WriteRow($"[{statusColor}]{Markup.Escape(Clip(status, width))}[/]");
        }
        foreach (var row in footer)
        {
            WriteRow(row);
        }
        _paintedRows = header.Count + rowCount + footer.Count;
        _paintedSize = size;
    }

    /// <summary>Reserves exactly the responsive strip, initial input row and optional bottom divider.</summary>
    private int ReservedRows()
    {
        var height = console.Profile.Height;
        var bar = new TerminalPromptBar(text, _status, height >= 8, height >= 4, height >= 8,
            TerminalSession.For(console));
        var width = Math.Max(1, console.Profile.Width - 1 - TerminalSession.For(console).ChatIndent);
        return bar.Header(width).Count + bar.Footer(width).Count + 1 + (NavigationHint(width) is null ? 0 : 1);
    }

    /// <summary>Shows the retained turn position and discovery keys only when the viewer is usable.</summary>
    private string? NavigationHint(int width)
    {
        var history = TerminalSession.For(console).History;
        if (history.Turns.Count == 0 || !FullscreenViewport.CanUse(console)) return null;
        var index = history.SelectedIndex ?? history.Turns.Count - 1;
        var label = text.Text("Terminal.TurnPosition", history.Turns[index].Number, history.Turns[^1].Number)
            + " · " + text.Text("Terminal.NavigationKeys");
        return $"[{TerminalTheme.Muted}]{Markup.Escape(TerminalText.Clip(label, width))}[/]";
    }

    /// <summary>Clears one owned row before writing its already width-bounded contents.</summary>
    /// <param name="markup">The styled contents of the input row.</param>
    private void WriteRow(string markup)
    {
        console.WriteAnsi(writer => writer.EraseInLine(2));
        console.MarkupLine(new string(' ', TerminalSession.For(console).ChatIndent) + markup);
    }

    /// <summary>Removes the editing viewport before printing the accepted text into normal terminal history.</summary>
    private void EraseDraft()
    {
        if (_paintedSize != (console.Profile.Width, console.Profile.Height))
        {
            console.WriteLine();
            _paintedRows = 0;
            return;
        }
        console.WriteAnsi(writer =>
        {
            writer.CursorUp(_paintedRows);
            writer.Write("\r");
            for (var row = 0; row < _paintedRows; row++)
            {
                writer.EraseInLine(2);
                if (row + 1 < _paintedRows)
                {
                    writer.CursorDown(1);
                }
            }
            if (_paintedRows > 1)
            {
                writer.CursorUp(_paintedRows - 1);
            }
        });
        _paintedRows = 0;
    }

    /// <summary>Clips a logical line around its caret without splitting Unicode characters or writing terminal controls.</summary>
    private static string FormatLine(string line, int? caret, int width)
    {
        var starts = StringInfo.ParseCombiningCharacters(line);
        var elements = starts.Select((start, index) => SafeDisplay(line[start..(index + 1 < starts.Length ? starts[index + 1] : line.Length)])).ToList();
        var cursor = caret.HasValue ? Array.FindIndex(starts, start => start >= caret.Value) : -1;
        if (caret == line.Length)
        {
            cursor = elements.Count;
            elements.Add(" ");
        }
        var first = 0;
        var beforeCursor = cursor < 0 ? 0 : elements.Take(cursor).Sum(CellWidth);
        while (first < cursor && beforeCursor > Math.Max(0, width / 2 - 1))
        {
            beforeCursor -= CellWidth(elements[first++]);
        }
        var result = new StringBuilder();
        var used = 0;
        if (first > 0 && width > 1)
        {
            result.Append($"[{TerminalTheme.Muted}]…[/]");
            used++;
        }
        for (var index = first; index < elements.Count; index++)
        {
            var element = elements[index];
            var cells = CellWidth(element);
            if (used + cells > width - (index + 1 < elements.Count ? 1 : 0))
            {
                if (used < width)
                {
                    result.Append($"[{TerminalTheme.Muted}]…[/]");
                }
                break;
            }
            var style = index == cursor
                ? $"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}"
                : TerminalTheme.Primary;
            result.Append($"[{style}]{Markup.Escape(element)}[/]");
            used += cells;
        }
        return result.ToString();
    }

    /// <summary>Measures printable text in terminal cells for safe input-area redraws.</summary>
    private static int CellWidth(string value) => new Segment(value).CellCount();

    /// <summary>Clips status text to a single row without breaking Unicode text elements.</summary>
    private static string Clip(string value, int width)
    {
        var result = new StringBuilder();
        var enumerator = StringInfo.GetTextElementEnumerator(SafeDisplay(value));
        var used = 0;
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            used += CellWidth(element);
            if (used > width)
            {
                break;
            }
            result.Append(element);
        }
        return result.ToString();
    }

    /// <summary>Displays pasted control characters visibly while retaining original input data in the draft.</summary>
    private static string SafeDisplay(string value) => string.Concat(value.Select(character => character switch
    {
        '\n' => "\n",
        '\t' => "    ",
        _ when char.IsControl(character) => $"<{(int)character:X2}>",
        _ => character.ToString()
    }));
}

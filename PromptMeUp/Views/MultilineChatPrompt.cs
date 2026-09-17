// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Edits a small scrolling input area with paste boundaries and an explicit keyboard submission.</summary>
internal sealed class MultilineChatPrompt(IAnsiConsole console, ILocalizationService text)
{
    private int _paintedRows;
    private (int Width, int Height) _paintedSize;

    /// <summary>Collects bounded pasted or typed text, preserving line breaks until a separate Enter key submits it.</summary>
    internal string Read(string label, int maximumCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCharacters);
        using var pasteMode = new TerminalPasteScope(console);
        if (!pasteMode.Enabled)
        {
            throw new InvalidOperationException(text.Text("Chat.PasteUnavailable"));
        }
        var buffer = new ChatInputBuffer(maximumCharacters);
        var reader = new TerminalInputReader(console.Input, maximumCharacters);
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Chat.MultilineHint"))}[/]");
        console.MarkupLine($"[bold {TerminalTheme.Accent}]{Markup.Escape(label)}[/]");
        console.Cursor.Hide();
        try
        {
            string? error = null;
            while (true)
            {
                Paint(buffer, error);
                var input = reader.Read();
                error = null;
                if (input.PasteTooLong || input.Paste is { } pasted && !buffer.Insert(pasted))
                {
                    error = text.Text("Chat.InputTooLong", maximumCharacters);
                }
                else if (input.Key is { } key)
                {
                    if (key.Key == ConsoleKey.Enter && key.Modifiers == 0)
                    {
                        EraseDraft();
                        console.Write(new Text(SafeDisplay(buffer.Text), Style.Parse(TerminalTheme.Primary)));
                        console.WriteLine();
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
        }
    }

    /// <summary>Redraws only owned input rows; resizing starts a fresh area without touching earlier scrollback.</summary>
    private void Paint(ChatInputBuffer buffer, string? error)
    {
        var size = (console.Profile.Width, console.Profile.Height);
        if (_paintedRows > 0 && _paintedSize == size)
        {
            console.WriteAnsi(writer => { writer.CursorUp(_paintedRows); writer.Write("\r"); });
        }
        else if (_paintedRows > 0)
        {
            console.WriteLine();
        }
        var width = Math.Max(1, size.Width - 1);
        var rowCount = Math.Clamp(size.Height - 2, 1, 8);
        var inputRows = Math.Max(1, rowCount - 1);
        var lines = buffer.Text.Split('\n');
        var current = buffer.Text.AsSpan(0, buffer.Cursor).Count('\n');
        var lineStart = buffer.Cursor == 0 ? 0 : buffer.Text.LastIndexOf('\n', buffer.Cursor - 1) + 1;
        var first = Math.Clamp(current - inputRows / 2, 0, Math.Max(0, lines.Length - inputRows));
        for (var offset = 0; offset < inputRows; offset++)
        {
            var index = first + offset;
            var prefix = index == current ? "› " : "  ";
            var line = index < lines.Length ? lines[index] : string.Empty;
            var markup = $"[bold {TerminalTheme.Accent}]{prefix}[/]"
                + FormatLine(line, index == current ? buffer.Cursor - lineStart : null, Math.Max(1, width - 2));
            WriteRow(width >= 3 ? markup : FormatLine(line, null, width));
        }
        if (rowCount > 1)
        {
            var status = error ?? text.Text("Chat.InputLine", current + 1, lines.Length);
            WriteRow($"[{(error is null ? TerminalTheme.Muted : TerminalTheme.Error)}]{Markup.Escape(Clip(status, width))}[/]");
        }
        _paintedRows = rowCount;
        _paintedSize = size;
    }

    /// <summary>Clears one owned row before writing its already width-bounded contents.</summary>
    private void WriteRow(string markup)
    {
        console.WriteAnsi(writer => writer.EraseInLine(2));
        console.MarkupLine(markup);
    }

    /// <summary>Removes the editing viewport before printing the accepted text into normal terminal history.</summary>
    private void EraseDraft()
    {
        if (_paintedSize != (console.Profile.Width, console.Profile.Height))
        {
            console.WriteLine();
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

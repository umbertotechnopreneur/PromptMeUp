// SPDX-License-Identifier: MIT

using System.Globalization;

namespace PromptMeUp.Views;

/// <summary>Keeps editable multiline input bounded without executing pasted keyboard shortcuts.</summary>
internal sealed class ChatInputBuffer(int maximumCharacters)
{
    public string Text { get; private set; } = string.Empty;
    public int Cursor { get; private set; }

    /// <summary>Inserts a complete text fragment atomically or rejects it without losing the existing draft.</summary>
    internal bool Insert(string value)
    {
        if (value.Length > maximumCharacters - Text.Length)
        {
            return false;
        }
        Text = Text.Insert(Cursor, value);
        Cursor += value.Length;
        if (Cursor < Text.Length)
        {
            // Inserting before a combining mark can merge it with the new text element.
            Cursor = StringInfo.ParseCombiningCharacters(Text).FirstOrDefault(index => index >= Cursor, Text.Length);
        }
        return true;
    }

    /// <summary>Edits one keyboard action while moving and deleting complete Unicode text elements.</summary>
    internal bool Edit(ConsoleKeyInfo key)
    {
        var control = (key.Modifiers & ConsoleModifiers.Control) != 0;
        switch (key.Key)
        {
            case ConsoleKey.Backspace when Cursor > 0:
                var previous = Previous(Cursor);
                Text = Text.Remove(previous, Cursor - previous);
                Cursor = previous;
                break;
            case ConsoleKey.Delete when Cursor < Text.Length:
                Text = Text.Remove(Cursor, Next(Cursor) - Cursor);
                break;
            case ConsoleKey.LeftArrow:
                Cursor = Previous(Cursor);
                break;
            case ConsoleKey.RightArrow:
                Cursor = Next(Cursor);
                break;
            case ConsoleKey.Home:
                Cursor = control ? 0 : LineStart(Cursor);
                break;
            case ConsoleKey.End:
                Cursor = control ? Text.Length : LineEnd(Cursor);
                break;
            case ConsoleKey.UpArrow:
                MoveLine(-1);
                break;
            case ConsoleKey.DownArrow:
                MoveLine(1);
                break;
            case ConsoleKey.Enter:
                return Insert("\n");
            case ConsoleKey.Tab:
                return Insert("\t");
            default:
                if (!char.IsControl(key.KeyChar) && !control)
                {
                    return Insert(key.KeyChar.ToString());
                }
                break;
        }
        return true;
    }

    /// <summary>Moves to the neighboring logical line without landing inside a composed character.</summary>
    private void MoveLine(int direction)
    {
        var start = LineStart(Cursor);
        var end = LineEnd(Cursor);
        if (direction < 0 && start == 0 || direction > 0 && end == Text.Length)
        {
            return;
        }
        var targetStart = direction < 0 ? LineStart(start - 1) : end + 1;
        var target = Math.Min(targetStart + Cursor - start, LineEnd(targetStart));
        Cursor = target == Text.Length ? target
            : StringInfo.ParseCombiningCharacters(Text).LastOrDefault(index => index <= target);
    }

    /// <summary>Finds the first character of the logical line containing a position.</summary>
    private int LineStart(int position) => position == 0 ? 0 : Text.LastIndexOf('\n', position - 1) + 1;

    /// <summary>Finds the line break or end of text following a position.</summary>
    private int LineEnd(int position)
    {
        var end = Text.IndexOf('\n', position);
        return end < 0 ? Text.Length : end;
    }

    /// <summary>Finds the preceding complete Unicode text element.</summary>
    private int Previous(int position) => StringInfo.ParseCombiningCharacters(Text).LastOrDefault(index => index < position);

    /// <summary>Finds the next complete Unicode text element or the end of the draft.</summary>
    private int Next(int position) => StringInfo.ParseCombiningCharacters(Text).FirstOrDefault(index => index > position, Text.Length);
}

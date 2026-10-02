// SPDX-License-Identifier: MIT

using System.Globalization;

namespace PromptMeUp.Views;

/// <summary>Owns the draft and Unicode-safe caret used by fullscreen text fields.</summary>
internal sealed class TerminalEditorBuffer
{
    public string Text { get; private set; } = string.Empty;
    public int Caret { get; private set; }

    /// <summary>Replaces the draft when focus enters a field.</summary>
    public void SetText(string value)
    {
        Text = value.ReplaceLineEndings("\n");
        Caret = Text.Length;
    }

    /// <summary>Clears the current edit without changing the caller's saved draft.</summary>
    public void Clear()
    {
        Text = string.Empty;
        Caret = 0;
    }

    /// <summary>Handles one editing key and returns false only when insertion exceeds the field limit.</summary>
    public bool Apply(ConsoleKeyInfo key, bool multiline, int maximumCharacters)
    {
        var control = (key.Modifiers & ConsoleModifiers.Control) != 0;
        switch (key.Key)
        {
            case ConsoleKey.U when control:
                Clear();
                break;
            case ConsoleKey.Home:
                Caret = multiline && !control ? LineStart(Caret) : 0;
                break;
            case ConsoleKey.End:
                Caret = multiline && !control ? LineEnd(Caret) : Text.Length;
                break;
            case ConsoleKey.UpArrow when multiline:
                MoveLine(-1);
                break;
            case ConsoleKey.DownArrow when multiline:
                MoveLine(1);
                break;
            case ConsoleKey.LeftArrow:
                Caret = TerminalTextElements.Previous(Text, Caret);
                break;
            case ConsoleKey.RightArrow:
                Caret = TerminalTextElements.Next(Text, Caret);
                break;
            case ConsoleKey.Backspace when Caret > 0:
                var previous = TerminalTextElements.Previous(Text, Caret);
                Text = Text.Remove(previous, Caret - previous);
                Caret = previous;
                break;
            case ConsoleKey.Delete when Caret < Text.Length:
                Text = Text.Remove(Caret, TerminalTextElements.Next(Text, Caret) - Caret);
                break;
            case ConsoleKey.Enter when multiline:
                return Insert("\n", maximumCharacters);
            default:
                if (!char.IsControl(key.KeyChar) && !control)
                {
                    return Insert(key.KeyChar.ToString(), maximumCharacters);
                }
                break;
        }
        return true;
    }

    /// <summary>Inserts a whole fragment or leaves the draft unchanged when it does not fit.</summary>
    private bool Insert(string value, int maximumCharacters)
    {
        if (value.Length > maximumCharacters - Text.Length)
        {
            return false;
        }
        Text = Text.Insert(Caret, value);
        Caret += value.Length;
        if (Caret < Text.Length)
        {
            Caret = StringInfo.ParseCombiningCharacters(Text).FirstOrDefault(index => index >= Caret, Text.Length);
        }
        return true;
    }

    /// <summary>Keeps the same visual column when moving between lines of composed characters.</summary>
    private void MoveLine(int direction)
    {
        var start = LineStart(Caret);
        var end = LineEnd(Caret);
        if (direction < 0 && start == 0 || direction > 0 && end == Text.Length)
        {
            return;
        }

        var column = StringInfo.ParseCombiningCharacters(Text[start..Caret]).Length;
        var targetStart = direction < 0 ? LineStart(start - 1) : end + 1;
        var targetEnd = LineEnd(targetStart);
        // A character can occupy several UTF-16 positions, but the cursor stops only once.
        var elements = StringInfo.ParseCombiningCharacters(Text[targetStart..targetEnd]);
        Caret = column < elements.Length ? targetStart + elements[column] : targetEnd;
    }

    /// <summary>Finds the first character after the preceding newline.</summary>
    private int LineStart(int position) => position == 0 ? 0 : Text.LastIndexOf('\n', position - 1) + 1;

    /// <summary>Finds the next newline or the end of the draft.</summary>
    private int LineEnd(int position)
    {
        var end = Text.IndexOf('\n', position);
        return end < 0 ? Text.Length : end;
    }
}

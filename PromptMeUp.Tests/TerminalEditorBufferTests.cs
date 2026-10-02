// SPDX-License-Identifier: MIT

using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class TerminalEditorBufferTests
{
    /// <summary>Vertical movement lands between complete characters, even when a line contains an emoji.</summary>
    [Fact]
    public void VerticalMovement_PreservesGraphemeColumn()
    {
        var editor = new TerminalEditorBuffer();
        editor.SetText("a😀b\nxy");

        editor.Apply(Key(ConsoleKey.UpArrow), multiline: true, 100);
        Assert.Equal(3, editor.Caret);
        editor.Apply(Key(ConsoleKey.DownArrow), multiline: true, 100);
        Assert.Equal(editor.Text.Length, editor.Caret);
    }

    /// <summary>Inserting before a combining mark keeps the caret outside the newly joined character.</summary>
    [Fact]
    public void InsertBeforeCombiningMark_MovesCaretToBoundary()
    {
        var editor = new TerminalEditorBuffer();
        editor.SetText("\u0301");
        editor.Apply(Key(ConsoleKey.Home), multiline: false, 100);

        Assert.True(editor.Apply(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false), multiline: false, 100));
        Assert.Equal("e\u0301", editor.Text);
        Assert.Equal(editor.Text.Length, editor.Caret);
    }

    /// <summary>A rejected newline leaves both text and caret untouched.</summary>
    [Fact]
    public void BoundedMultilineInsertion_IsAtomic()
    {
        var editor = new TerminalEditorBuffer();
        editor.SetText("abcd");

        Assert.False(editor.Apply(Key(ConsoleKey.Enter), multiline: true, 4));
        Assert.Equal("abcd", editor.Text);
        Assert.Equal(4, editor.Caret);
    }

    /// <summary>Creates a navigation key without relying on a real terminal.</summary>
    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);
}

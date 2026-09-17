// SPDX-License-Identifier: MIT

using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class ChatInputBufferTests
{
    /// <summary>Verifies rejected paste leaves both the existing draft and insertion position unchanged.</summary>
    [Fact]
    public void Insert_OversizedPaste_RejectsAtomically()
    {
        var buffer = new ChatInputBuffer(6);
        Assert.True(buffer.Insert("abcd"));
        buffer.Edit(Key(ConsoleKey.LeftArrow));
        buffer.Edit(Key(ConsoleKey.LeftArrow));

        Assert.False(buffer.Insert("XYZ"));

        Assert.Equal("abcd", buffer.Text);
        Assert.Equal(2, buffer.Cursor);
        Assert.True(buffer.Insert("XY"));
        Assert.Equal("abXYcd", buffer.Text);
    }

    /// <summary>Verifies pasted newlines and trailing whitespace remain literal editable text.</summary>
    [Fact]
    public void Insert_MultilinePaste_PreservesBlankAndTrailingLines()
    {
        const string value = "first\n\nlast \t\n";
        var buffer = new ChatInputBuffer(100);

        Assert.True(buffer.Insert(value));

        Assert.Equal(value, buffer.Text);
        Assert.Equal(value.Length, buffer.Cursor);
    }

    /// <summary>Verifies inserting before a combining mark leaves the caret after the merged grapheme.</summary>
    [Fact]
    public void Insert_BeforeCombiningMark_MaintainsGraphemeBoundary()
    {
        var buffer = new ChatInputBuffer(100);
        buffer.Insert("\u0301x");
        buffer.Edit(Key(ConsoleKey.Home));

        Assert.True(buffer.Insert("e"));

        Assert.Equal("e\u0301x", buffer.Text);
        Assert.Equal(2, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.Backspace));
        Assert.Equal("x", buffer.Text);
        Assert.Equal(0, buffer.Cursor);
    }

    /// <summary>Verifies Backspace removes a complete composed or supplementary Unicode character.</summary>
    [Theory]
    [InlineData("e\u0301")]
    [InlineData("\U0001F600")]
    [InlineData("\U0001F469\u200D\U0001F4BB")]
    public void Edit_Backspace_RemovesCompleteGrapheme(string grapheme)
    {
        var buffer = new ChatInputBuffer(100);
        buffer.Insert("prefix" + grapheme);

        buffer.Edit(Key(ConsoleKey.Backspace));

        Assert.Equal("prefix", buffer.Text);
        Assert.Equal("prefix".Length, buffer.Cursor);
    }

    /// <summary>Verifies Delete removes a whole grapheme at the caret without leaving combining fragments.</summary>
    [Theory]
    [InlineData("e\u0301")]
    [InlineData("\U0001F600")]
    [InlineData("\U0001F469\u200D\U0001F4BB")]
    public void Edit_Delete_RemovesCompleteGrapheme(string grapheme)
    {
        var buffer = new ChatInputBuffer(100);
        buffer.Insert(grapheme + "suffix");
        buffer.Edit(Key(ConsoleKey.Home));

        buffer.Edit(Key(ConsoleKey.Delete));

        Assert.Equal("suffix", buffer.Text);
        Assert.Equal(0, buffer.Cursor);
    }

    /// <summary>Verifies horizontal movement crosses whole graphemes and treats a newline as one boundary.</summary>
    [Fact]
    public void Edit_HorizontalArrows_TraverseGraphemesAndNewlines()
    {
        var buffer = new ChatInputBuffer(100);
        buffer.Insert("e\u0301\n\U0001F600");

        buffer.Edit(Key(ConsoleKey.LeftArrow));
        Assert.Equal(3, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.LeftArrow));
        Assert.Equal(2, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.LeftArrow));
        Assert.Equal(0, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.RightArrow));
        Assert.Equal(2, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.RightArrow));
        Assert.Equal(3, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.RightArrow));
        Assert.Equal(5, buffer.Cursor);
    }

    /// <summary>Verifies vertical navigation reaches empty lines and the final trailing line.</summary>
    [Fact]
    public void Edit_VerticalArrows_TraverseBlankAndTrailingLines()
    {
        var buffer = new ChatInputBuffer(100);
        buffer.Insert("first\n\nlast\n");

        buffer.Edit(Key(ConsoleKey.UpArrow));
        Assert.Equal(7, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.UpArrow));
        Assert.Equal(6, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.UpArrow));
        Assert.Equal(0, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.DownArrow));
        Assert.Equal(6, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.DownArrow));
        Assert.Equal(7, buffer.Cursor);
        buffer.Edit(Key(ConsoleKey.DownArrow));
        Assert.Equal(buffer.Text.Length, buffer.Cursor);
    }

    /// <summary>Verifies vertical movement never places the caret between a base character and its accent.</summary>
    [Fact]
    public void Edit_VerticalMovementIntoCombiningElement_UsesBoundary()
    {
        var buffer = new ChatInputBuffer(100);
        buffer.Insert("e\u0301z\nx");

        buffer.Edit(Key(ConsoleKey.UpArrow));

        Assert.Equal(0, buffer.Cursor);
    }

    /// <summary>Verifies Shift+Enter inserts at the caret and rejects a newline atomically at the size limit.</summary>
    [Theory]
    [InlineData(5, true, "ab\ncd", 3)]
    [InlineData(4, false, "abcd", 2)]
    public void Edit_ShiftEnter_InsertsBoundedLineBreak(int limit, bool accepted, string expected, int cursor)
    {
        var buffer = new ChatInputBuffer(limit);
        buffer.Insert("abcd");
        buffer.Edit(Key(ConsoleKey.LeftArrow));
        buffer.Edit(Key(ConsoleKey.LeftArrow));

        Assert.Equal(accepted, buffer.Edit(new ConsoleKeyInfo('\r', ConsoleKey.Enter, true, false, false)));
        Assert.Equal(expected, buffer.Text);
        Assert.Equal(cursor, buffer.Cursor);
    }

    /// <summary>Creates a keyboard action without adding printable characters to the draft.</summary>
    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);
}

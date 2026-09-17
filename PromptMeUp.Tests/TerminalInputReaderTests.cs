// SPDX-License-Identifier: MIT

using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class TerminalInputReaderTests
{
    /// <summary>Verifies pasted line endings, blank lines, and trailing whitespace remain input until a later Enter.</summary>
    [Fact]
    public void Read_MultilinePaste_PreservesTextBeforeSeparateEnter()
    {
        var reader = CreateReader("\u001b[200~first\r\n\rsecond\n \t\r\n\u001b[201~\r");

        var paste = reader.Read();
        var enter = reader.Read();

        Assert.Equal("first\n\nsecond\n \t\n", paste.Paste);
        Assert.Null(paste.Key);
        Assert.False(paste.PasteTooLong);
        Assert.Equal(ConsoleKey.Enter, enter.Key?.Key);
        Assert.Null(enter.Paste);
    }

    /// <summary>Verifies literal Escape characters and partial delimiter matches survive inside pasted content.</summary>
    [Fact]
    public void Read_PasteWithFalseTerminatorPrefixes_PreservesLiteralCharacters()
    {
        const string content = "\u001b[20x\u001b\u001b[201z\u001b[200~\nlast";
        var reader = CreateReader($"\u001b[200~{content}\u001b[201~");

        Assert.Equal(content, reader.Read().Paste);
    }

    /// <summary>Verifies delimiter characters can arrive on separate asynchronous reads.</summary>
    [Fact]
    public void Read_AsynchronousCharacterBoundaries_CollectsCompletePaste()
    {
        var input = new ScriptedConsoleInput(Keys("\u001b[200~a\r\nb\u001b[201~"), yieldBeforeRead: true);
        var reader = new TerminalInputReader(input, 100);

        Assert.Equal("a\nb", reader.Read().Paste);
    }

    /// <summary>Verifies Unix Alt-prefix key parsing does not hide the beginning or end of a paste.</summary>
    [Fact]
    public void Read_AltBracketPrefixes_RecognizesPasteBoundaries()
    {
        var keys = new[] { new ConsoleKeyInfo('[', ConsoleKey.Oem4, false, true, false) }
            .Concat(Keys("200~line\r\n"))
            .Concat([new ConsoleKeyInfo('[', ConsoleKey.Oem4, false, true, false)])
            .Concat(Keys("201~"));
        var reader = new TerminalInputReader(new ScriptedConsoleInput(keys), 100);

        Assert.Equal("line\n", reader.Read().Paste);
    }

    /// <summary>Verifies Escape characters folded into Alt keys remain literal within pasted text.</summary>
    [Fact]
    public void Read_AltCharacterInsidePaste_RestoresLiteralEscape()
    {
        var keys = Keys("\u001b[200~")
            .Concat([new ConsoleKeyInfo('x', ConsoleKey.X, false, true, false)])
            .Concat(Keys("\u001b[201~"));
        var reader = new TerminalInputReader(new ScriptedConsoleInput(keys), 100);

        Assert.Equal("\u001bx", reader.Read().Paste);
    }

    /// <summary>Verifies oversized paste is rejected as a whole and drained before another deliberate key.</summary>
    [Fact]
    public void Read_OversizedPaste_DrainsWithoutExposingPastedEnter()
    {
        var reader = CreateReader("\u001b[200~abcd\r\nefg\r\n\u001b[201~x\r", maximumCharacters: 3);

        var paste = reader.Read();
        var next = reader.Read();
        var enter = reader.Read();

        Assert.True(paste.PasteTooLong);
        Assert.Null(paste.Paste);
        Assert.Null(paste.Key);
        Assert.Equal('x', next.Key?.KeyChar);
        Assert.Equal(ConsoleKey.Enter, enter.Key?.Key);
    }

    /// <summary>Verifies the size limit counts normalized text rather than counting CRLF twice.</summary>
    [Fact]
    public void Read_PasteAtNormalizedLimit_AcceptsEntirePaste()
    {
        var reader = CreateReader("\u001b[200~a\r\nb\r\n\u001b[201~", maximumCharacters: 4);

        var paste = reader.Read();

        Assert.Equal("a\nb\n", paste.Paste);
        Assert.False(paste.PasteTooLong);
    }

    /// <summary>Verifies an empty paste produces a harmless empty text event.</summary>
    [Fact]
    public void Read_EmptyPaste_ReturnsEmptyText()
    {
        var reader = CreateReader("\u001b[200~\u001b[201~");

        Assert.Equal(string.Empty, reader.Read().Paste);
    }

    /// <summary>Verifies standalone Escape cancels once its bounded prefix wait expires.</summary>
    [Fact]
    public void Read_StandaloneEscape_CancelsCurrentFlow()
    {
        var input = new ScriptedConsoleInput(Keys("\u001b"), waitAtEnd: true);
        var reader = new TerminalInputReader(input, 100);

        Assert.Throws<InteractiveFlowCanceledException>(() => reader.Read());
    }

    /// <summary>Verifies input ending inside a paste never returns incomplete text or a submission action.</summary>
    [Fact]
    public void Read_UnterminatedPaste_CancelsCurrentFlow()
    {
        var reader = CreateReader("\u001b[200~first\nsecond");

        Assert.Throws<InteractiveFlowCanceledException>(() => reader.Read());
    }

    /// <summary>Verifies exhausted input cancels even when it ends inside a terminal control prefix.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("\u001b[")]
    [InlineData("\u001b[20")]
    public void Read_ExhaustedInput_CancelsCurrentFlow(string text)
    {
        var reader = CreateReader(text);

        Assert.Throws<InteractiveFlowCanceledException>(() => reader.Read());
    }

    /// <summary>Verifies bracketed paste bypasses the general Escape wrapper without cancelling the editor.</summary>
    [Fact]
    public void Read_EscapeAwareInput_RecognizesPaste()
    {
        var input = new EscapeAwareConsoleInput(
            new ScriptedConsoleInput(Keys("\u001b[200~first\nsecond\u001b[201~")),
            CancellationToken.None);
        var reader = new TerminalInputReader(input, 100);

        Assert.Equal("first\nsecond", reader.Read().Paste);
    }

    /// <summary>Verifies virtual-terminal character records retain ordinary keyboard editing actions.</summary>
    [Theory]
    [InlineData('\r', ConsoleKey.Enter)]
    [InlineData('\n', ConsoleKey.Enter)]
    [InlineData('\b', ConsoleKey.Backspace)]
    [InlineData('\u007f', ConsoleKey.Backspace)]
    [InlineData('\t', ConsoleKey.Tab)]
    public void Read_RawControlCharacter_RestoresKeyIdentity(char character, ConsoleKey expected)
    {
        var reader = CreateReader(character.ToString());

        Assert.Equal(expected, reader.Read().Key?.Key);
    }

    /// <summary>Verifies common CSI and SS3 navigation sequences retain their key and modifier identity.</summary>
    [Theory]
    [InlineData("\u001b[A", ConsoleKey.UpArrow, (ConsoleModifiers)0)]
    [InlineData("\u001b[B", ConsoleKey.DownArrow, (ConsoleModifiers)0)]
    [InlineData("\u001bOC", ConsoleKey.RightArrow, (ConsoleModifiers)0)]
    [InlineData("\u001bOD", ConsoleKey.LeftArrow, (ConsoleModifiers)0)]
    [InlineData("\u001b[H", ConsoleKey.Home, (ConsoleModifiers)0)]
    [InlineData("\u001b[4~", ConsoleKey.End, (ConsoleModifiers)0)]
    [InlineData("\u001b[3~", ConsoleKey.Delete, (ConsoleModifiers)0)]
    [InlineData("\u001b[1;5D", ConsoleKey.LeftArrow, ConsoleModifiers.Control)]
    [InlineData("\u001b[1;6C", ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)]
    [InlineData("\u001b[Z", ConsoleKey.Tab, ConsoleModifiers.Shift)]
    [InlineData("\u001b[13;2u", ConsoleKey.Enter, ConsoleModifiers.Shift)]
    [InlineData("\u001b[27;2;13~", ConsoleKey.Enter, ConsoleModifiers.Shift)]
    public void Read_NavigationSequence_RestoresKeyAndModifiers(string sequence, ConsoleKey expected, ConsoleModifiers modifiers)
    {
        var reader = CreateReader(sequence);

        var key = reader.Read().Key;

        Assert.Equal(expected, key?.Key);
        Assert.Equal(modifiers, key?.Modifiers);
    }

    /// <summary>Verifies Windows Shift+Enter remains distinct from the following unmodified submit key.</summary>
    [Fact]
    public void Read_Win32ShiftEnter_PreservesModifierAndSkipsKeyRelease()
    {
        var reader = new TerminalInputReader(new ScriptedConsoleInput(Keys(
            "\u001b[16;42;0;1;16;1_\u001b[13;28;13;1;16;1_\u001b[13;28;13;0;16;1_\u001b[13;28;13;1;0;1_")), 100, win32Encoding: true);

        var newline = reader.Read().Key;
        var submit = reader.Read().Key;

        Assert.Equal(ConsoleKey.Enter, newline?.Key);
        Assert.Equal(ConsoleModifiers.Shift, newline?.Modifiers);
        Assert.Equal(ConsoleKey.Enter, submit?.Key);
        Assert.Equal((ConsoleModifiers)0, submit?.Modifiers);
    }

    /// <summary>Verifies Windows-encoded paste markers and newlines stay literal until a later keyboard submission.</summary>
    [Fact]
    public void Read_Win32EncodedPaste_PreservesBoundaries()
    {
        var encoded = string.Concat("\u001b[200~first\r\nsecond\u001b[201~".Select(character => $"\u001b[0;0;{(int)character};1;0;1_"));
        var reader = new TerminalInputReader(new ScriptedConsoleInput(Keys(encoded + "\u001b[13;28;13;1;0;1_")), 100, win32Encoding: true);

        Assert.Equal("first\nsecond", reader.Read().Paste);
        Assert.Equal(ConsoleKey.Enter, reader.Read().Key?.Key);
    }

    /// <summary>Verifies a raw paste cannot turn text resembling Windows key records into keyboard actions.</summary>
    [Fact]
    public void Read_Win32ModeRawPaste_PreservesLiteralKeySyntax()
    {
        const string content = "\u001b[13;28;13;1;16;1_";
        var reader = new TerminalInputReader(new ScriptedConsoleInput(Keys(
            "\u001b[200~" + content + "\u001b[201~\u001b[13;28;13;1;16;1_")), 100, win32Encoding: true);

        Assert.Equal(content, reader.Read().Paste);
        Assert.Equal(ConsoleModifiers.Shift, reader.Read().Key?.Modifiers);
    }

    /// <summary>Verifies repeated Windows text keys are retained and encoded Escape still cancels.</summary>
    [Fact]
    public void Read_Win32RepeatedKeyAndEscape_PreservesEditingAndCancellation()
    {
        var reader = new TerminalInputReader(new ScriptedConsoleInput(Keys(
            "\u001b[65;30;97;1;0;2_\u001b[27;1;27;1;0;1_")), 100, win32Encoding: true);

        Assert.Equal('a', reader.Read().Key?.KeyChar);
        Assert.Equal('a', reader.Read().Key?.KeyChar);
        Assert.Throws<InteractiveFlowCanceledException>(() => reader.Read());
    }

    /// <summary>Verifies unknown terminal controls are ignored rather than inserted into the prompt.</summary>
    [Fact]
    public void Read_UnknownSequence_ReturnsEmptyEvent()
    {
        var reader = CreateReader("\u001b[99~x");

        Assert.Equal(new TerminalInputEvent(), reader.Read());
        Assert.Equal('x', reader.Read().Key?.KeyChar);
    }

    /// <summary>Creates a reader over raw character records with a bounded paste size.</summary>
    private static TerminalInputReader CreateReader(string text, int maximumCharacters = 100) =>
        new(new ScriptedConsoleInput(Keys(text)), maximumCharacters);

    /// <summary>Encodes text as virtual-terminal records without relying on platform keyboard parsing.</summary>
    private static IEnumerable<ConsoleKeyInfo> Keys(string text) =>
        text.Select(character => new ConsoleKeyInfo(character, (ConsoleKey)0, false, false, false));

    private sealed class ScriptedConsoleInput : IAnsiConsoleInput
    {
        private readonly Queue<ConsoleKeyInfo> _keys;
        private readonly bool _yieldBeforeRead;
        private readonly bool _waitAtEnd;

        /// <summary>Creates deterministic raw input with optional asynchronous boundaries or pending input.</summary>
        public ScriptedConsoleInput(IEnumerable<ConsoleKeyInfo> keys, bool yieldBeforeRead = false, bool waitAtEnd = false)
        {
            _keys = new Queue<ConsoleKeyInfo>(keys);
            _yieldBeforeRead = yieldBeforeRead;
            _waitAtEnd = waitAtEnd;
        }

        /// <summary>Reports whether the next scripted record can be consumed immediately.</summary>
        public bool IsKeyAvailable() => _keys.Count > 0;

        /// <summary>Reads one scripted record synchronously.</summary>
        public ConsoleKeyInfo? ReadKey(bool intercept) =>
            ReadKeyAsync(intercept, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>Reads one record while respecting cancellation for a simulated idle terminal.</summary>
        public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_yieldBeforeRead)
            {
                await Task.Yield();
            }

            if (_keys.TryDequeue(out var key))
            {
                return key;
            }

            if (_waitAtEnd)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }
    }
}

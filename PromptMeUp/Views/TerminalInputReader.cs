// SPDX-License-Identifier: MIT

using System.Text;
using Spectre.Console;

namespace PromptMeUp.Views;

internal sealed record TerminalInputEvent(ConsoleKeyInfo? Key = null, string? Paste = null, bool PasteTooLong = false);

internal sealed class TerminalInputReader
{
    private const string PasteEnd = "\u001b[201~";
    private readonly IAnsiConsoleInput _input;
    private readonly int _maximumPasteCharacters;

    /// <summary>Creates a reader that separates terminal paste blocks from deliberate keyboard actions.</summary>
    internal TerminalInputReader(IAnsiConsoleInput input, int maximumPasteCharacters)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPasteCharacters);
        _maximumPasteCharacters = maximumPasteCharacters;
    }

    /// <summary>Reads one keyboard action or one complete paste without submitting pasted line endings.</summary>
    internal TerminalInputEvent Read() => ReadAsync().GetAwaiter().GetResult();

    /// <summary>Recognizes raw terminal control prefixes before interpreting ordinary keys.</summary>
    private async Task<TerminalInputEvent> ReadAsync()
    {
        var key = await ReadRawAsync(CancellationToken.None).ConfigureAwait(false)
            ?? throw new InteractiveFlowCanceledException();
        var sequenceKind = '\0';
        if (key.Key == ConsoleKey.Escape || key.KeyChar == '\u001b')
        {
            var next = await ReadPrefixKeyAsync().ConfigureAwait(false);
            if (next?.KeyChar is not ('[' or 'O'))
            {
                throw new InteractiveFlowCanceledException();
            }

            sequenceKind = next.Value.KeyChar;
        }
        else if ((key.Modifiers & ConsoleModifiers.Alt) != 0 && key.KeyChar is '[' or 'O')
        {
            // Unix console parsing can fold the first Escape and following character into one Alt key.
            sequenceKind = key.KeyChar;
        }

        if (sequenceKind == '\0')
        {
            return new TerminalInputEvent(Key: NormalizeKey(key));
        }

        var sequence = new StringBuilder();
        while (sequence.Length < 32)
        {
            var next = await ReadPrefixKeyAsync().ConfigureAwait(false);
            if (next is null)
            {
                return new TerminalInputEvent();
            }

            var character = next.Value.KeyChar;
            if (character is < ' ' or > '~')
            {
                return new TerminalInputEvent();
            }

            sequence.Append(character);
            if (character is >= '@' and <= '~')
            {
                if (sequenceKind == '[' && sequence.ToString() == "200~")
                {
                    return await ReadPasteAsync().ConfigureAwait(false);
                }

                return new TerminalInputEvent(Key: DecodeControlSequence(sequenceKind, sequence.ToString()));
            }
        }

        return new TerminalInputEvent();
    }

    /// <summary>Reads without the general Escape cancellation wrapper while retaining application shutdown.</summary>
    private Task<ConsoleKeyInfo?> ReadRawAsync(CancellationToken cancellationToken) =>
        _input is EscapeAwareConsoleInput wrapped
            ? wrapped.ReadRawKeyAsync(intercept: true, cancellationToken)
            : _input.ReadKeyAsync(intercept: true, cancellationToken);

    /// <summary>Bounds the wait for a terminal prefix so a standalone Escape still cancels promptly.</summary>
    private async Task<ConsoleKeyInfo?> ReadPrefixKeyAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        try
        {
            return await ReadRawAsync(timeout.Token).ConfigureAwait(false)
                ?? throw new InteractiveFlowCanceledException();
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>Collects one bounded paste, preserving whitespace and draining rejected oversized content.</summary>
    private async Task<TerminalInputEvent> ReadPasteAsync()
    {
        var text = new StringBuilder(Math.Min(_maximumPasteCharacters, 1024));
        var candidate = new StringBuilder(PasteEnd.Length);
        var previousWasCarriageReturn = false;
        var tooLong = false;
        while (true)
        {
            var key = await ReadRawAsync(CancellationToken.None).ConfigureAwait(false)
                ?? throw new InteractiveFlowCanceledException();
            foreach (var character in GetPasteCharacters(key))
            {
                candidate.Append(character);
                while (!PasteEnd.StartsWith(candidate.ToString(), StringComparison.Ordinal))
                {
                    var literal = candidate[0];
                    candidate.Remove(0, 1);
                    if (literal == '\n' && previousWasCarriageReturn)
                    {
                        previousWasCarriageReturn = false;
                        continue;
                    }

                    previousWasCarriageReturn = literal == '\r';
                    if (text.Length == _maximumPasteCharacters)
                    {
                        tooLong = true;
                    }
                    else if (!tooLong)
                    {
                        text.Append(literal == '\r' ? '\n' : literal);
                    }
                }

                if (candidate.Length == PasteEnd.Length)
                {
                    return tooLong
                        ? new TerminalInputEvent(PasteTooLong: true)
                        : new TerminalInputEvent(Paste: text.ToString());
                }
            }
        }
    }

    /// <summary>Restores Escape prefixes folded into Alt keys while reading literal pasted text.</summary>
    private static string GetPasteCharacters(ConsoleKeyInfo key)
    {
        if (key.KeyChar != '\0')
        {
            return (key.Modifiers & ConsoleModifiers.Alt) != 0
                ? string.Concat('\u001b', key.KeyChar)
                : key.KeyChar.ToString();
        }

        return key.Key switch
        {
            ConsoleKey.Enter => "\r",
            ConsoleKey.Tab => "\t",
            ConsoleKey.Escape => "\u001b",
            ConsoleKey.Backspace => "\b",
            ConsoleKey.UpArrow => "\u001b[A",
            ConsoleKey.DownArrow => "\u001b[B",
            ConsoleKey.RightArrow => "\u001b[C",
            ConsoleKey.LeftArrow => "\u001b[D",
            ConsoleKey.Home => "\u001b[H",
            ConsoleKey.End => "\u001b[F",
            ConsoleKey.Delete => "\u001b[3~",
            _ => string.Empty
        };
    }

    /// <summary>Restores normal keyboard identities for Windows virtual-terminal character records.</summary>
    private static ConsoleKeyInfo NormalizeKey(ConsoleKeyInfo key)
    {
        if (key.Key != 0)
        {
            return key;
        }

        var normalized = key.KeyChar switch
        {
            '\r' or '\n' => ConsoleKey.Enter,
            '\b' or '\u007f' => ConsoleKey.Backspace,
            '\t' => ConsoleKey.Tab,
            >= '\u0001' and <= '\u001a' => ConsoleKey.A + key.KeyChar - 1,
            _ => key.Key
        };
        return new ConsoleKeyInfo(
            key.KeyChar,
            normalized,
            (key.Modifiers & ConsoleModifiers.Shift) != 0,
            (key.Modifiers & ConsoleModifiers.Alt) != 0,
            (key.Modifiers & ConsoleModifiers.Control) != 0
                || (key.KeyChar is >= '\u0001' and <= '\u001a' && normalized is not (ConsoleKey.Enter or ConsoleKey.Tab or ConsoleKey.Backspace)));
    }

    /// <summary>Decodes common CSI and SS3 editing keys without treating terminal syntax as prompt text.</summary>
    private static ConsoleKeyInfo? DecodeControlSequence(char kind, string sequence)
    {
        var final = sequence[^1];
        var parameters = sequence[..^1].Split(';');
        var modifier = 1;
        if (parameters.Length > 2 || (parameters.Length == 2
                && (!int.TryParse(parameters[1], out modifier) || modifier is < 1 or > 8)))
        {
            return null;
        }

        var key = final switch
        {
            'A' => ConsoleKey.UpArrow,
            'B' => ConsoleKey.DownArrow,
            'C' => ConsoleKey.RightArrow,
            'D' => ConsoleKey.LeftArrow,
            'H' => ConsoleKey.Home,
            'F' => ConsoleKey.End,
            'Z' when kind == '[' => ConsoleKey.Tab,
            'P' when kind == 'O' => ConsoleKey.F1,
            'Q' when kind == 'O' => ConsoleKey.F2,
            'R' when kind == 'O' => ConsoleKey.F3,
            'S' when kind == 'O' => ConsoleKey.F4,
            '~' when kind == '[' => parameters[0] switch
            {
                "1" or "7" => ConsoleKey.Home,
                "4" or "8" => ConsoleKey.End,
                "2" => ConsoleKey.Insert,
                "3" => ConsoleKey.Delete,
                "5" => ConsoleKey.PageUp,
                "6" => ConsoleKey.PageDown,
                "11" => ConsoleKey.F1,
                "12" => ConsoleKey.F2,
                "13" => ConsoleKey.F3,
                "14" => ConsoleKey.F4,
                _ => (ConsoleKey)0
            },
            _ => (ConsoleKey)0
        };
        if (key == 0 || (final != '~' && parameters[0] is not ("" or "1")))
        {
            return null;
        }

        var flags = modifier - 1;
        return new ConsoleKeyInfo(
            key == ConsoleKey.Tab ? '\t' : '\0',
            key,
            (flags & 1) != 0 || final == 'Z',
            (flags & 2) != 0,
            (flags & 4) != 0);
    }
}

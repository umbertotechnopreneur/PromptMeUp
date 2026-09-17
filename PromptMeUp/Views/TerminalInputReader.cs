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
    private readonly bool _win32Encoding;
    private readonly Queue<ConsoleKeyInfo> _pendingKeys = new();
    private bool _literalPaste;

    /// <summary>Creates a reader that separates terminal paste blocks from deliberate keyboard actions.</summary>
    internal TerminalInputReader(IAnsiConsoleInput input, int maximumPasteCharacters, bool win32Encoding = false)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPasteCharacters);
        _maximumPasteCharacters = maximumPasteCharacters;
        _win32Encoding = win32Encoding;
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
    private Task<ConsoleKeyInfo?> ReadInputAsync(CancellationToken cancellationToken) =>
        _input is EscapeAwareConsoleInput wrapped
            ? wrapped.ReadRawKeyAsync(intercept: true, cancellationToken)
            : _input.ReadKeyAsync(intercept: true, cancellationToken);

    /// <summary>Unwraps Windows key records before paste parsing so modifiers and encoded paste delimiters survive.</summary>
    private async Task<ConsoleKeyInfo?> ReadRawAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (_pendingKeys.TryDequeue(out var pending))
            {
                return pending;
            }
            var first = await ReadInputAsync(cancellationToken).ConfigureAwait(false);
            if (!_win32Encoding || _literalPaste || first?.KeyChar != '\u001b')
            {
                return first;
            }

            var prefix = new List<ConsoleKeyInfo>();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(150));
            try
            {
                while (prefix.Count < 64)
                {
                    var next = await ReadInputAsync(timeout.Token).ConfigureAwait(false);
                    if (next is null)
                    {
                        break;
                    }
                    prefix.Add(next.Value);
                    if (prefix.Count == 1 ? next.Value.KeyChar != '[' : next.Value.KeyChar is >= '@' and <= '~')
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // A standalone Escape still belongs to the normal cancellation path.
            }
            var sequence = new string(prefix.Select(key => key.KeyChar).ToArray());
            if (sequence.StartsWith('[') && sequence.EndsWith('_'))
            {
                var (key, repeats) = DecodeWin32Key(sequence[1..^1]);
                if (key is { } decoded)
                {
                    for (var repeat = 1; repeat < repeats; repeat++)
                    {
                        _pendingKeys.Enqueue(decoded);
                    }
                    return decoded;
                }
                continue;
            }

            // Raw bracketed paste remains literal even if it contains text resembling key records.
            _literalPaste = sequence == "[200~";
            foreach (var key in prefix)
            {
                _pendingKeys.Enqueue(key);
            }
            return first;
        }
    }

    /// <summary>Decodes a bounded Windows key-down record and ignores key-up and modifier-only events.</summary>
    private static (ConsoleKeyInfo? Key, int Repeats) DecodeWin32Key(string sequence)
    {
        var fields = sequence.Split(';');
        if (fields.Length != 6)
        {
            return (null, 0);
        }
        var values = new int[6];
        values[5] = 1;
        for (var index = 0; index < fields.Length; index++)
        {
            if (fields[index].Length != 0 && (!int.TryParse(fields[index], out values[index]) || values[index] < 0 || values[index] > ushort.MaxValue))
            {
                return (null, 0);
            }
        }
        if (values[0] > 255 || values[0] is 16 or 17 or 18 || values[3] != 1 || values[5] == 0)
        {
            return (null, 0);
        }
        var modifiers = values[4];
        return (new ConsoleKeyInfo((char)values[2], (ConsoleKey)values[0],
            (modifiers & 16) != 0, (modifiers & 3) != 0, (modifiers & 12) != 0), values[5]);
    }

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
                    _literalPaste = false;
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
        if (kind == '[' && (final == 'u' && parameters.Length == 2 && parameters[0] == "13"
            || final == '~' && parameters.Length == 3 && parameters[0] == "27" && parameters[2] == "13"))
        {
            if (parameters.Length is < 2 or > 3 || !int.TryParse(parameters[1], out var enterModifier) || enterModifier is < 1 or > 8)
            {
                return null;
            }
            var enterFlags = enterModifier - 1;
            return new ConsoleKeyInfo('\r', ConsoleKey.Enter, (enterFlags & 1) != 0, (enterFlags & 2) != 0, (enterFlags & 4) != 0);
        }
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

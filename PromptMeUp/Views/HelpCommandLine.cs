// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Colors complete help examples without interpreting their text as terminal markup.</summary>
internal static class HelpCommandLine
{
    /// <summary>Renders a lowercase white command, accented switches, green strings, and informative arguments.</summary>
    internal static IRenderable Create(string example)
    {
        var paragraph = new Paragraph { Overflow = Overflow.Fold };
        foreach (var token in Tokenize(example))
        {
            Append(paragraph, token.Value, token.Color);
        }
        return paragraph;
    }

    /// <summary>Colors exact example tokens mentioned in prose while leaving unrelated words as primary text.</summary>
    internal static IRenderable CreateDescription(string description, string example)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        var paragraph = new Paragraph { Overflow = Overflow.Fold };
        AppendDescription(paragraph, description, Tokenize(example));
        return paragraph;
    }

    /// <summary>Explains an explicit example argument using the same color as its command-line occurrence.</summary>
    internal static IRenderable CreateArgument(string example, string token, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        var tokens = Tokenize(example);
        var argument = tokens.FirstOrDefault(item => item.IsParameter && item.Value == token)
            ?? throw new ArgumentException("The argument must occur exactly in the help example.", nameof(token));
        var paragraph = new Paragraph { Overflow = Overflow.Fold };
        Append(paragraph, argument.Value, argument.Color);
        Append(paragraph, ": ", TerminalTheme.Primary);
        AppendDescription(paragraph, description, tokens);
        return paragraph;
    }

    /// <summary>Retains every example token and whitespace span while assigning its semantic display color.</summary>
    private static IReadOnlyList<CommandToken> Tokenize(string example)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(example);
        var start = 0;
        while (start < example.Length && char.IsWhiteSpace(example[start]))
        {
            start++;
        }
        var end = start;
        while (end < example.Length && !char.IsWhiteSpace(example[end]))
        {
            end++;
        }
        if (!example.AsSpan(start, end - start).Equals("hm", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A help example must begin with the hm command.", nameof(example));
        }

        var tokens = new List<CommandToken>
        {
            new(example[..start], TerminalTheme.Primary, false),
            new("hm", "white", false)
        };
        var index = end;
        while (index < example.Length)
        {
            start = index;
            string color;
            var parameter = true;
            if (char.IsWhiteSpace(example[index]))
            {
                while (index < example.Length && char.IsWhiteSpace(example[index]))
                {
                    index++;
                }
                color = TerminalTheme.Primary;
                parameter = false;
            }
            else if (example[index] is '\'' or '"')
            {
                index = QuotedEnd(example, index);
                color = TerminalTheme.Success;
            }
            else
            {
                while (index < example.Length && !char.IsWhiteSpace(example[index]) && example[index] is not ('\'' or '"'))
                {
                    index++;
                }
                color = example[start] == '-' ? TerminalTheme.Accent : TerminalTheme.Info;
            }
            tokens.Add(new CommandToken(example[start..index], color, parameter));
        }
        return tokens;
    }

    /// <summary>Matches longer known tokens first and preserves all unmatched prose, punctuation, and spacing.</summary>
    private static void AppendDescription(Paragraph paragraph, string description, IReadOnlyList<CommandToken> tokens)
    {
        var candidates = tokens.Where(token => token.Value == "hm"
                || token.IsParameter && (token.Value[0] is '-' or '\'' or '"' || token.Value.Length >= 4))
            .DistinctBy(token => token.Value, StringComparer.Ordinal)
            .OrderByDescending(token => token.Value.Length)
            .ToArray();
        var plainStart = 0;
        var index = 0;
        while (index < description.Length)
        {
            var match = candidates.FirstOrDefault(token => Matches(description, index, token.Value));
            if (match is null)
            {
                index++;
                continue;
            }
            Append(paragraph, description[plainStart..index], TerminalTheme.Primary);
            Append(paragraph, match.Value, match.Color);
            index += match.Value.Length;
            plainStart = index;
        }
        Append(paragraph, description[plainStart..], TerminalTheme.Primary);
    }

    /// <summary>Requires literal token matches at word boundaries, excluding longer switches and path extensions.</summary>
    private static bool Matches(string description, int index, string token)
    {
        var end = index + token.Length;
        return end <= description.Length
            && description.AsSpan(index, token.Length).SequenceEqual(token)
            && (index == 0 || !IsTokenCharacter(description[index - 1]))
            && (end == description.Length || !IsTokenCharacter(description[end]))
            && !(end + 1 < description.Length && description[end] == '.' && IsTokenCharacter(description[end + 1]));
    }

    /// <summary>Recognizes characters that make a matched word part of a larger argument or identifier.</summary>
    private static bool IsTokenCharacter(char character) =>
        char.IsLetterOrDigit(character) || character is '_' or '-' or '/' or '\\'
        || char.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

    /// <summary>Stores a literal example span and its shared command, description, and argument-note color.</summary>
    private sealed record CommandToken(string Value, string Color, bool IsParameter);

    /// <summary>Finds a quoted string boundary while preserving PowerShell escapes and doubled quote characters.</summary>
    private static int QuotedEnd(string example, int start)
    {
        var quote = example[start];
        var index = start + 1;
        while (index < example.Length)
        {
            if (quote == '"' && example[index] == '`' && index + 1 < example.Length)
            {
                index += 2;
            }
            else if (example[index] == quote)
            {
                if (index + 1 < example.Length && example[index + 1] == quote)
                {
                    index += 2;
                }
                else
                {
                    return index + 1;
                }
            }
            else
            {
                index++;
            }
        }
        return index;
    }

    /// <summary>Appends literal styled text, exposing terminal control codes instead of executing them.</summary>
    private static void Append(Paragraph paragraph, string value, string color)
    {
        var visible = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsControl(character) && character is not ('\r' or '\n' or '\t'))
            {
                visible.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            }
            else
            {
                visible.Append(character);
            }
        }
        paragraph.Append(visible.ToString(), Style.Parse(color));
    }
}

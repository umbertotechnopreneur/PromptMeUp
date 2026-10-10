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
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Provides safe display text and terminal-cell sizing for shared inline controls.</summary>
internal static class TerminalText
{
    /// <summary>Removes terminal control characters while retaining readable lines and expanded tabs.</summary>
    internal static string Safe(string value) => new(value.ReplaceLineEndings("\n")
        .Replace("\t", "    ", StringComparison.Ordinal)
        .Where(character => character == '\n' || !char.IsControl(character)).ToArray());

    /// <summary>Measures visible cells instead of UTF-16 code units.</summary>
    internal static int Width(string value) => new Segment(value).CellCount();

    /// <summary>Fits a single line without splitting a Unicode text element or overflowing the ellipsis.</summary>
    internal static string Clip(string value, int width)
    {
        if (width <= 0) return string.Empty;
        value = Safe(value).Replace('\n', ' ');
        if (Width(value) <= width) return value;
        var result = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(value);
        var used = 0;
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            var cells = Width(element);
            if (used + cells > width - 1) break;
            result.Append(element);
            used += cells;
        }
        return result.Append('…').ToString();
    }

    /// <summary>Shortens a single line in the middle while preserving complete Unicode text elements.</summary>
    /// <param name="value">The text to display without terminal control characters.</param>
    /// <param name="maxCharacters">The maximum displayed text elements, including the ellipsis.</param>
    internal static string ClipMiddle(string value, int maxCharacters)
    {
        if (maxCharacters <= 0) return string.Empty;
        value = Safe(value).Replace('\n', ' ');
        var elements = StringInfo.ParseCombiningCharacters(value);
        if (elements.Length <= maxCharacters) return value;
        if (maxCharacters == 1) return "…";

        var prefixCharacters = maxCharacters / 2;
        var suffixCharacters = maxCharacters - prefixCharacters - 1;
        var prefix = value[..elements[prefixCharacters]];
        var suffix = suffixCharacters == 0 ? string.Empty : value[elements[^suffixCharacters]..];
        return prefix + "…" + suffix;
    }
}

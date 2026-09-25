// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Provides safe display text and terminal-cell sizing for shared inline controls.</summary>
internal static class TerminalText
{
    /// <summary>Removes terminal control sequences while retaining readable lines and expanded tabs.</summary>
    internal static string Safe(string value) => new(value.Replace("\r\n", "\n", StringComparison.Ordinal)
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
}

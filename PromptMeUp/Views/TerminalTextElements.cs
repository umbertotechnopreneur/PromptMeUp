// SPDX-License-Identifier: MIT

namespace PromptMeUp.Views;

/// <summary>Provides Unicode-safe text-element boundaries for terminal editors.</summary>
internal static class TerminalTextElements
{
    /// <summary>Finds the preceding Unicode text element for cursor movement and deletion.</summary>
    public static int Previous(string value, int index) =>
        System.Globalization.StringInfo.ParseCombiningCharacters(value).LastOrDefault(start => start < index);

    /// <summary>Finds the following Unicode text element without splitting a composed character.</summary>
    public static int Next(string value, int index) =>
        System.Globalization.StringInfo.ParseCombiningCharacters(value).FirstOrDefault(start => start > index, value.Length);
}

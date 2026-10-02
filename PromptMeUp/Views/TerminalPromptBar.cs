// SPDX-License-Identifier: MIT

using PromptMeUp.Services;

namespace PromptMeUp.Views;

/// <summary>Composes the reusable session strip with optional prompt separators.</summary>
internal sealed class TerminalPromptBar(
    ILocalizationService text,
    ShellRuntimeStatus? status,
    bool showBorders = true,
    bool showStatus = true,
    bool showBreakdown = true,
    TerminalSession? session = null)
{
    /// <summary>Returns complete status rows without splitting a metric label from its value.</summary>
    internal IReadOnlyList<string> Header(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        var rows = new List<string>();
        if (showBorders) rows.Add(ThemeSeparator.Markup(width));
        if (showStatus) rows.AddRange(new TerminalSessionStrip(text, status, session).Rows(width, showBreakdown));
        return rows;
    }

    /// <summary>Returns an optional bottom separator without enclosing the conversation in a card.</summary>
    internal IReadOnlyList<string> Footer(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        return showBorders ? [ThemeSeparator.Markup(width)] : [];
    }
}

// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Contains the semantic colors used by terminal pages and shared output.</summary>
public sealed record TerminalThemeColors(
    string Background,
    string Primary,
    string Muted,
    string Accent,
    string Info,
    string Divider,
    string Success,
    string Warning,
    string Error,
    string SelectionBackground,
    string SelectionForeground);

/// <summary>Describes one versioned terminal theme with a stable persisted identifier.</summary>
public sealed record TerminalThemeDefinition(int Version, string Id, string Name, TerminalThemeColors Colors)
{
    public static TerminalThemeDefinition Default { get; } = new(
        1,
        "cyan",
        "Cyan",
        new TerminalThemeColors(
            "#081820", "#F4FAFF", "#BDCED8", "#80DEEA", "#69D2FF", "#7B9DAC",
            "#7CE6A2", "#FFD479", "#FF9B9B", "#80DEEA", "#081820"));
}

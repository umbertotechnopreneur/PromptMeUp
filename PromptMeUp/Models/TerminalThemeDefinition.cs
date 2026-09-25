// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace PromptMeUp.Models;

/// <summary>Contains the semantic colors used by terminal pages and shared output.</summary>
public sealed record TerminalThemeColors(
    string Background,
    string Primary,
    string Muted,
    string Accent,
    string AccentSecondary,
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
    public string? Author { get; init; }

    public string? Website { get; init; }

    public string? Description { get; init; }

    [JsonIgnore]
    public string? SourcePath { get; init; }

    public static TerminalThemeDefinition Default { get; } = new(
        4,
        "cyan",
        "Cyan",
        new TerminalThemeColors(
            "#081820", "#F4FAFF", "#BDCED8", "#4EDAE9", "#F47CB7", "#43C9FF", "#94B9C8",
            "#7CE6A2", "#FFD479", "#FF9B9B", "#4EDAE9", "#081820"))
    {
        Author = "PromptMeUp contributors",
        Website = "https://github.com/umbertotechnopreneur/PromptMeUp",
        Description = "Bright cyan and blue accents on a deep blue background."
    };
}

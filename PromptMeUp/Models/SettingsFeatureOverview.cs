// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Reports global preferences, effectively enabled usable skills, and all inspected catalog packages.</summary>
public sealed record SettingsFeatureOverview(SkillsAndMemorySettings Settings, int EnabledSkillCount, int SkillCount)
{
    /// <summary>Contains each inspected package and its stored approval independently of the global master switch.</summary>
    public IReadOnlyList<SettingsSkillState> Skills { get; init; } = [];

    /// <summary>Marks unreadable catalog counts; zero values are placeholders and must not be displayed as measured totals.</summary>
    public bool CatalogUnavailable { get; init; }
}

/// <summary>Reports whether one inspected package matches the global retained approval.</summary>
public sealed record SettingsSkillState(SkillDefinition Skill, bool Enabled)
{
    /// <summary>Preserves the exact stored approval, including missing, disabled, and outdated fingerprints.</summary>
    public string? ApprovalFingerprint { get; init; }
}

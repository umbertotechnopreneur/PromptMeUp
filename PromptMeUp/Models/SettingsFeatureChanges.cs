// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Collects project feature edits and explicit privacy acknowledgements for one atomic settings save.</summary>
public sealed record SettingsFeatureChanges(SkillsAndMemorySettings Expected, SkillsAndMemorySettings Settings,
    IReadOnlyList<SettingsSkillChange> Skills, bool CaptureConsent = false, bool ClearLearningConsent = false);

/// <summary>Describes a package approval edit relative to the exact package and approval shown in settings.</summary>
public sealed record SettingsSkillChange(SkillDefinition Skill, bool ExpectedEnabled, bool Enabled)
{
    /// <summary>Matches the stored approval seen when editing began; null and an empty disabled value are distinct.</summary>
    public string? ExpectedApprovalFingerprint { get; init; }
}

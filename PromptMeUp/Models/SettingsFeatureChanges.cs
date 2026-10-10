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

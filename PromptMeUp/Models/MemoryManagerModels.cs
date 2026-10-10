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

/// <summary>Identifies one local saved-memory management action.</summary>
public enum MemoryManagerAction
{
    Create,
    Edit,
    Delete,
    ApproveProposal,
    RejectProposal,
    Close
}

/// <summary>Contains one confirmed editor action, its exact item identifier, and reviewed text when required.</summary>
public sealed record MemoryManagerSelection(MemoryManagerAction Action, string? Id = null, string? Text = null);

/// <summary>Supplies the inline memory editor with current suggestions and their complete local evidence.</summary>
public sealed record MemoryProposalWorkspace(
    bool Enabled,
    IReadOnlyList<MemoryProposal> Proposals,
    IReadOnlyList<LearningObservation> Evidence)
{
    public static MemoryProposalWorkspace Disabled { get; } = new(false, [], []);
}

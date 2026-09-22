// SPDX-License-Identifier: MIT

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

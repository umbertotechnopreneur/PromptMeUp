// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Identifies one local saved-memory management action.</summary>
public enum MemoryManagerAction
{
    Create,
    Edit,
    Delete,
    Proposals,
    Close
}

/// <summary>Contains the chosen action and an exact saved-memory identifier when required.</summary>
public sealed record MemoryManagerSelection(MemoryManagerAction Action, string? Id = null);

/// <summary>Contains the note and destination scope reviewed in the memory editor.</summary>
public sealed record MemoryDraft(string Text, bool IsGlobal);

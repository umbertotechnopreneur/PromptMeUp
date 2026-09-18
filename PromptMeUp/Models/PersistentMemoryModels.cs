// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Represents one explicit, credential-free global note; IsGlobal remains for source compatibility.</summary>
public sealed record PersistentMemory(string Id, string Text, bool IsGlobal, DateTimeOffset UpdatedAt);

/// <summary>Contains locally selected notes and their estimated content token cost.</summary>
public sealed record MemorySelection(IReadOnlyList<PersistentMemory> Memories, long EstimatedTokens);

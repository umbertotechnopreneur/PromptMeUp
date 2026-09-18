// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Binds a reflection request to the complete evidence shown before sharing it with OpenAI.</summary>
public sealed record MemoryReflectionBatch(bool Dream, string Request,
    IReadOnlyList<LearningObservation> Observations, IReadOnlyList<PersistentMemory> Memories, int OmittedCount);

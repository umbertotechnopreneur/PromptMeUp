// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record ConversationMemorySnapshot(
    IReadOnlyList<ChatMessage> Messages,
    int TurnCount,
    int PrunedMessages,
    long EstimatedTokens,
    long TokenBudget);

public sealed record ConversationMemoryUpdate(int PrunedMessages, ConversationMemorySnapshot Snapshot);

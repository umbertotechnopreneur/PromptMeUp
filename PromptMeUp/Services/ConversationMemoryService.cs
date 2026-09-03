// SPDX-License-Identifier: MIT

using System.Text;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IConversationMemoryService
{
    ConversationMemory Create(AppSettings settings);
}

public sealed class ConversationMemoryService : IConversationMemoryService
{
    /// <summary>Creates one isolated in-memory sliding window for a short AI work session.</summary>
    public ConversationMemory Create(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new ConversationMemory(settings);
    }
}

public sealed class ConversationMemory
{
    private const long InstructionTokenReserve = 2_048;
    private readonly AppSettings _settings;
    private readonly long _tokenBudget;
    private readonly List<ChatMessage> _messages = [];
    private int _totalPrunedMessages;

    /// <summary>Creates a bounded memory using the configured turn, message, and context limits.</summary>
    internal ConversationMemory(AppSettings settings)
    {
        _settings = settings;
        var contextWindow = AiModelCatalog.Resolve(settings.Model).ContextWindowTokens;
        var configuredBudget = checked(contextWindow * settings.MaxContextPercent / 100);
        _tokenBudget = Math.Max(1, configuredBudget - InstructionTokenReserve);
    }

    /// <summary>Adds one user, assistant, or tool-output message and prunes the oldest complete turns when needed.</summary>
    public ConversationMemoryUpdate Add(string role, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var normalizedRole = NormalizeRole(role);
        if (normalizedRole == "user" && content.Length > _settings.MaxMessageCharacters)
        {
            throw new ConversationLimitException(
                $"Message length {content.Length:N0} exceeds the configured {_settings.MaxMessageCharacters:N0}-character limit.");
        }

        if (normalizedRole == "user" && EstimateMessages([new ChatMessage(normalizedRole, content)]) > _tokenBudget)
        {
            throw new ConversationLimitException("The message exceeds the configured context token budget.");
        }

        // Assistant output has its own provider/body limits; user-input limits must never discard a completed answer.
        _messages.Add(new ChatMessage(normalizedRole, content));
        var pruned = PruneToLimits();
        _totalPrunedMessages += pruned;
        return new ConversationMemoryUpdate(pruned, Snapshot());
    }

    /// <summary>Clears the active context while leaving the persistent session ledger untouched.</summary>
    public void Clear()
    {
        _totalPrunedMessages += _messages.Count;
        _messages.Clear();
    }

    /// <summary>Returns an immutable view of the exact messages that would be sent on the next request.</summary>
    public ConversationMemorySnapshot Snapshot() => new(
        _messages.ToArray(),
        _messages.Count(message => message.Role == "user"),
        _totalPrunedMessages,
        EstimateMessages(_messages),
        _tokenBudget);

    /// <summary>Removes oldest turn groups until both the turn count and estimated token budget are satisfied.</summary>
    private int PruneToLimits()
    {
        var removed = 0;
        while ((_messages.Count(message => message.Role == "user") > _settings.MaxConversationTurns
                || EstimateMessages(_messages) > _tokenBudget)
               && _messages.Count > 0)
        {
            var nextTurn = _messages.FindIndex(1, message => message.Role == "user");
            var count = nextTurn > 0 ? nextTurn : _messages.Count;
            _messages.RemoveRange(0, count);
            removed += count;
        }

        return removed;
    }

    /// <summary>Estimates serialized message tokens using the same lightweight UTF-8 heuristic as preflight display.</summary>
    private static long EstimateMessages(IEnumerable<ChatMessage> messages) =>
        messages.Sum(message => Math.Max(1, (long)Math.Ceiling(Encoding.UTF8.GetByteCount(message.Content) / 4d)) + 4);

    /// <summary>Restricts local memory roles to user and assistant.</summary>
    private static string NormalizeRole(string role) => role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
        ? "assistant"
        : "user";
}

public sealed class ConversationLimitException : Exception
{
    /// <summary>Creates a user-visible bounded-memory validation error.</summary>
    public ConversationLimitException(string message)
        : base(message)
    {
    }
}

// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record ChatMessage(string Role, string Content);

public sealed record AiUsageMetrics(
    long InputTokens,
    long CachedInputTokens,
    long CacheWriteTokens,
    long OutputTokens,
    long ReasoningTokens,
    long TotalTokens);

public sealed record AiCostBreakdown(
    decimal InputUsd,
    decimal CachedInputUsd,
    decimal CacheWriteUsd,
    decimal OutputUsd,
    decimal TotalUsd);

public sealed record AiContextUsage(
    long InputTokens,
    long OutputTokens,
    long SystemInstructionTokens,
    long ConversationTokens,
    long LatestUserPromptTokens,
    long ContextWindowTokens,
    bool IsInputEstimate);

public sealed record SuggestedCommand(string Label, string Command);

public sealed record AiResponse(
    string Id,
    string Model,
    string Text,
    AiUsageMetrics Usage,
    AiContextUsage ContextUsage,
    AiCostBreakdown? CostBreakdown,
    decimal? EstimatedCostUsd,
    int HttpStatusCode,
    long ElapsedMilliseconds,
    string? ProviderRequestId)
{
    public IReadOnlyList<SuggestedCommand> SuggestedCommands { get; init; } = [];
}

public sealed record AiRequestLog(
    string Id,
    string ConversationId,
    string PromptId,
    DateTimeOffset OccurredAt,
    DateTimeOffset? CompletedAt,
    string EndpointHost,
    string RequestedModel,
    string? ReturnedModel,
    string UserPrompt,
    string? AssistantResponse,
    AiUsageMetrics Usage,
    decimal? EstimatedCostUsd,
    int? HttpStatusCode,
    long? ElapsedMilliseconds,
    string? ProviderResponseId,
    string? ProviderRequestId,
    bool Succeeded,
    string? FailureCode);

public sealed record PromptDefinition(
    string Id,
    int Version,
    string Description,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Texts,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>Returns localized prompt text with English as the explicit resource fallback.</summary>
    public string ResolveText(string language) =>
        Texts.TryGetValue(language, out var localized)
            ? localized
            : Texts.TryGetValue("en", out var english)
                ? english
                : throw new InvalidDataException($"Prompt '{Id}' has no text for '{language}' or English fallback.");
}

public sealed record ConnectionTestResult(AiResponse Response);

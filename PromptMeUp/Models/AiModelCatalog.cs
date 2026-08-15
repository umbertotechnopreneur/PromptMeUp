// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record AiModelDescriptor(
    string Id,
    string DisplayName,
    IReadOnlyList<string> ReasoningEfforts,
    long ContextWindowTokens);

public static class AiModelCatalog
{
    private static readonly string[] FullEfforts = ["none", "low", "medium", "high", "xhigh"];
    private static readonly string[] FrontierEfforts = ["none", "low", "medium", "high", "xhigh", "max"];

    public static IReadOnlyList<AiModelDescriptor> Models { get; } =
    [
        new("gpt-5.6-sol", "GPT-5.6 Sol", FrontierEfforts, 1_050_000),
        new("gpt-5.6-terra", "GPT-5.6 Terra", FrontierEfforts, 1_050_000),
        new("gpt-5.6-luna", "GPT-5.6 Luna", FrontierEfforts, 1_050_000),
        new("gpt-5.5", "GPT-5.5", FullEfforts, 1_050_000),
        new("gpt-5.4", "GPT-5.4", FullEfforts, 1_050_000),
        new("gpt-5.4-mini", "GPT-5.4 mini", FullEfforts, 400_000),
        new("gpt-5.4-nano", "GPT-5.4 nano", FullEfforts, 400_000)
    ];

    /// <summary>Resolves a supported model identifier or rejects stale configuration.</summary>
    public static AiModelDescriptor Resolve(string id) =>
        Models.FirstOrDefault(model => string.Equals(model.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Unsupported model '{id}'.");
}

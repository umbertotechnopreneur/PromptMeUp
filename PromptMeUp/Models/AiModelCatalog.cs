// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record AiModelDescriptor(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<string> ReasoningEfforts,
    bool SupportsImageInput,
    long ContextWindowTokens,
    long MaximumOutputTokens);

public static class AiModelCatalog
{
    private static readonly string[] FullEfforts = ["none", "low", "medium", "high", "xhigh"];
    private static readonly string[] FrontierEfforts = ["none", "low", "medium", "high", "xhigh", "max"];

    public static IReadOnlyList<AiModelDescriptor> Models { get; } =
    [
        new("gpt-5.6-sol", "GPT-5.6 Sol", "Frontier model for demanding professional work and reasoning.", FrontierEfforts, true, 1_050_000, 128_000),
        new("gpt-5.6-terra", "GPT-5.6 Terra", "Balanced intelligence, latency, and cost for everyday work.", FrontierEfforts, true, 1_050_000, 128_000),
        new("gpt-5.6-luna", "GPT-5.6 Luna", "Cost-sensitive model for fast, high-volume workloads.", FrontierEfforts, true, 1_050_000, 128_000),
        new("gpt-5.5", "GPT-5.5", "Previous frontier model for complex professional work.", FullEfforts, true, 1_050_000, 128_000),
        new("gpt-5.4", "GPT-5.4", "Affordable model for coding and professional work.", FullEfforts, true, 1_050_000, 128_000),
        new("gpt-5.4-mini", "GPT-5.4 mini", "Efficient model for focused and high-volume tasks.", FullEfforts, true, 400_000, 128_000),
        new("gpt-5.4-nano", "GPT-5.4 nano", "Low-cost model for extraction, ranking, and simple tasks.", FullEfforts, true, 400_000, 128_000)
    ];

    /// <summary>Resolves a supported model identifier or rejects stale configuration.</summary>
    public static AiModelDescriptor Resolve(string id) =>
        Models.FirstOrDefault(model => string.Equals(model.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Unsupported model '{id}'.");
}

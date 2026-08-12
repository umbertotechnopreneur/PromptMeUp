// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IAiCostCalculator
{
    decimal Calculate(AiUsageMetrics usage, AiModelPrice price);

    AiCostBreakdown CalculateBreakdown(AiUsageMetrics usage, AiModelPrice price);
}

public sealed class AiCostCalculator : IAiCostCalculator
{
    /// <summary>Returns the combined locally estimated USD amount for one provider usage record.</summary>
    public decimal Calculate(AiUsageMetrics usage, AiModelPrice price)
    {
        ArgumentNullException.ThrowIfNull(usage);
        ArgumentNullException.ThrowIfNull(price);
        return CalculateBreakdown(usage, price).TotalUsd;
    }

    /// <summary>Separates regular input, cached input, and output cost for the fixed runtime status bar.</summary>
    public AiCostBreakdown CalculateBreakdown(AiUsageMetrics usage, AiModelPrice price)
    {
        ArgumentNullException.ThrowIfNull(usage);
        ArgumentNullException.ThrowIfNull(price);
        var cachedTokens = Math.Min(usage.InputTokens, usage.CachedInputTokens);
        var cacheWriteTokens = Math.Min(usage.InputTokens - cachedTokens, usage.CacheWriteTokens);
        var regularInputTokens = Math.Max(0, usage.InputTokens - cachedTokens - cacheWriteTokens);
        var cachedInputPrice = price.CachedInputUsdPerMillionTokens ?? price.InputUsdPerMillionTokens;
        var cacheWritePrice = price.CacheWriteUsdPerMillionTokens ?? price.InputUsdPerMillionTokens;
        var input = Round(regularInputTokens * price.InputUsdPerMillionTokens / 1_000_000m);
        var cached = Round(cachedTokens * cachedInputPrice / 1_000_000m);
        var cacheWrite = Round(cacheWriteTokens * cacheWritePrice / 1_000_000m);
        var output = Round(usage.OutputTokens * price.OutputUsdPerMillionTokens / 1_000_000m);
        return new AiCostBreakdown(input, cached, cacheWrite, output, Round(input + cached + cacheWrite + output));
    }

    /// <summary>Normalizes local USD estimates to eight fractional digits.</summary>
    private static decimal Round(decimal value) => decimal.Round(value, 8, MidpointRounding.AwayFromZero);
}

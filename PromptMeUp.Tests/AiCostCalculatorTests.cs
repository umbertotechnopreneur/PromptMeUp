// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class AiCostCalculatorTests
{
    /// <summary>Verifies separate standard, cached, cache-write, and output token pricing.</summary>
    [Fact]
    public void CalculateBreakdown_MixedUsage_UsesEachPricingBand()
    {
        var usage = new AiUsageMetrics(1_000_000, 200_000, 100_000, 500_000, 0, 1_500_000);
        var price = new AiModelPrice(
            "openai",
            "test-model",
            "standard",
            "short",
            "USD",
            2m,
            0.5m,
            2.5m,
            8m,
            "https://example.test/pricing",
            DateTimeOffset.UtcNow);

        var result = new AiCostCalculator().CalculateBreakdown(usage, price);

        Assert.Equal(1.4m, result.InputUsd);
        Assert.Equal(0.1m, result.CachedInputUsd);
        Assert.Equal(0.25m, result.CacheWriteUsd);
        Assert.Equal(4m, result.OutputUsd);
        Assert.Equal(5.75m, result.TotalUsd);
    }
}

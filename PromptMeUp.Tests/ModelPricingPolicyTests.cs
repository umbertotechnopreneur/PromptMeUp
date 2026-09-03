// SPDX-License-Identifier: MIT

using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ModelPricingPolicyTests
{
    /// <summary>Verifies model-specific long-context thresholds, dated aliases, and mini/nano exceptions.</summary>
    [Theory]
    [InlineData("gpt-5.6-sol", 272000, "gpt-5.6-sol", "short")]
    [InlineData("gpt-5.6-sol", 272001, "gpt-5.6-sol", "long")]
    [InlineData("gpt-5.6-terra", 400000, "gpt-5.6-terra", "long")]
    [InlineData("gpt-5.6-luna", 400000, "gpt-5.6-luna", "long")]
    [InlineData("gpt-5.5", 272001, "gpt-5.5", "long")]
    [InlineData("gpt-5.4", 272001, "gpt-5.4", "long")]
    [InlineData("gpt-5.4-mini", 400000, "gpt-5.4-mini", "short")]
    [InlineData("gpt-5.4-nano", 400000, "gpt-5.4-nano", "short")]
    [InlineData("gpt-5.6-terra-2026-09-01", 272001, "gpt-5.6-terra", "long")]
    public void Resolve_KnownModel_SelectsDocumentedBand(string model, long tokens, string expectedModel, string band)
    {
        var result = ModelPricingPolicy.Resolve(model, tokens);

        Assert.NotNull(result);
        Assert.Equal(expectedModel, result.Value.Model);
        Assert.Equal(band, result.Value.ContextWindow);
    }

    /// <summary>Verifies unsupported aliases never silently inherit another model family's pricing.</summary>
    [Theory]
    [InlineData("unknown-model")]
    [InlineData("gpt-5.4-pro")]
    [InlineData("gpt-5.6-terra-preview")]
    [InlineData("gpt-5.6-terra-2026-99-99")]
    public void Resolve_UnknownModel_ReturnsNoEstimate(string model) => Assert.Null(ModelPricingPolicy.Resolve(model, 400_000));

    /// <summary>Verifies the database never substitutes a short rate when the required long rate is missing.</summary>
    [Fact]
    public async Task FindModelPriceAsync_MissingLongBand_ReturnsNoEstimate()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short")], default);

        Assert.NotNull(await fixture.Database.FindModelPriceAsync("openai", "gpt-5.6-terra", 272_000, default));
        Assert.Null(await fixture.Database.FindModelPriceAsync("openai", "gpt-5.6-terra", 272_001, default));
    }
}

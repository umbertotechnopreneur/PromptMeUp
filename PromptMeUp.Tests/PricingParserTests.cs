// SPDX-License-Identifier: MIT

using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class PricingParserTests
{
    /// <summary>Verifies the official nine-column Standard pricing shape, including cache writes and long context.</summary>
    [Fact]
    public void ParseStandardPricingData_NineColumnTable_NormalizesBothContextBands()
    {
        const string markdown = """
            # Pricing

            ### Standard pricing data

            | Model | Input | Cached input | Cache write | Output | Input (long) | Cached input (long) | Cache write (long) | Output (long) |
            | --- | --- | --- | --- | --- | --- | --- | --- | --- |
            | `gpt-example` | $2.00 | $0.20 | $2.50 | $8.00 | $4.00 | $0.40 | $5.00 | $16.00 |

            ### Next section
            """;
        var retrievedAt = new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

        var rows = OpenAiPricingMarkdownParser.ParseStandardPricingData(markdown, retrievedAt, "https://example.test/pricing");

        Assert.Equal(2, rows.Count);
        Assert.Equal("short", rows[0].ContextWindow);
        Assert.Equal(2.50m, rows[0].CacheWriteUsdPerMillionTokens);
        Assert.Equal("long", rows[1].ContextWindow);
        Assert.Equal(16m, rows[1].OutputUsdPerMillionTokens);
    }
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Tests;

public sealed class SessionCostTests
{
    /// <summary>Verifies all priced calls, including a billed failure, contribute once and other sessions remain separate.</summary>
    [Fact]
    public async Task GetSessionCostAsync_PricedCalls_SumsOnlyRequestedSession()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("guide-session", 0.012345m), default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("guide-session", 0.006789m), default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("guide-session", 0.004321m, succeeded: false), default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("other-session", 50m), default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("other-session", null), default);

        var cost = await fixture.Database.GetSessionCostAsync("guide-session", default);

        Assert.Equal(0.023455m, cost);
    }

    /// <summary>Verifies any unpriced successful or billed failed call makes the session total unavailable.</summary>
    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 12)]
    [InlineData(false, 12)]
    public async Task GetSessionCostAsync_UnpricedChargedCall_ReturnsUnknown(bool succeeded, long totalTokens)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("guide-session", 0.01m), default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("guide-session", null, succeeded, totalTokens), default);

        var cost = await fixture.Database.GetSessionCostAsync("guide-session", default);

        Assert.Null(cost);
    }

    /// <summary>Verifies a failure without usage cannot obscure a known session total or invent a charge.</summary>
    [Fact]
    public async Task GetSessionCostAsync_NonbillableFailure_PreservesKnownCost()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("guide-session", null, succeeded: false, totalTokens: 0), default);

        Assert.Equal(0m, await fixture.Database.GetSessionCostAsync("guide-session", default));

        await fixture.Database.AppendAiRequestAsync(CreateRequest("guide-session", 0.012345m), default);

        Assert.Equal(0.012345m, await fixture.Database.GetSessionCostAsync("guide-session", default));
    }

    /// <summary>Verifies an empty session has zero cost even when another session has unpriced requests.</summary>
    [Fact]
    public async Task GetSessionCostAsync_NoCalls_ReturnsZero()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.AppendAiRequestAsync(CreateRequest("other-session", null), default);

        Assert.Equal(0m, await fixture.Database.GetSessionCostAsync("empty-session", default));
    }

    /// <summary>Builds a synthetic request ledger row with explicit charge, outcome, and usage for session aggregation.</summary>
    private static AiRequestLog CreateRequest(string sessionId, decimal? cost, bool succeeded = true, long totalTokens = 12)
    {
        var occurredAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        return new AiRequestLog(
            Guid.NewGuid().ToString("N"), sessionId, "query-system", occurredAt, occurredAt,
            "example.invalid", "gpt-5.6-terra", "gpt-5.6-terra", "Synthetic question", succeeded ? "Synthetic answer" : null,
            new AiUsageMetrics(totalTokens, 0, 0, 0, 0, totalTokens), cost,
            succeeded || totalTokens > 0 ? 200 : 503, 1, null, null, succeeded, succeeded ? null : "synthetic_error");
    }
}

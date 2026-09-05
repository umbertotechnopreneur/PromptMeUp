// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class RequestSummaryRegressionTests
{
    /// <summary>Verifies the production summary query performs a bounded search through the existing time index.</summary>
    [Fact]
    public async Task GetAiRequestSummaryAsync_QueryPlan_UsesBoundedTimeIndex()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = fixture.Paths.DatabasePath,
            Pooling = false
        }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN " + SqliteDatabaseService.AiRequestSummarySql;
        command.Parameters.AddWithValue("$month", 0);
        command.Parameters.AddWithValue("$today", 100);
        command.Parameters.AddWithValue("$tomorrow", 200);

        await using var reader = await command.ExecuteReaderAsync();
        var plan = new List<string>();
        while (await reader.ReadAsync())
        {
            plan.Add(reader.GetString(3));
        }

        Assert.Contains(plan, detail => detail.Contains("SEARCH ai_requests USING INDEX ix_ai_requests_occurred", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, detail => detail.Contains("SCAN ai_requests", StringComparison.Ordinal));
    }

    /// <summary>Verifies month and day boundaries, failed requests, and unknown costs preserve the expected totals.</summary>
    [Fact]
    public async Task GetAiRequestSummaryAsync_MixedHistory_PreservesPeriodTotals()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var today = DateTime.Today;
        var month = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Local);
        await fixture.Database.AppendAiRequestAsync(Request(month.AddSeconds(-1), 99m), default);
        await fixture.Database.AppendAiRequestAsync(Request(month, 2m), default);
        await fixture.Database.AppendAiRequestAsync(Request(today.AddHours(1), 3m), default);
        await fixture.Database.AppendAiRequestAsync(Request(today.AddHours(2), null), default);
        await fixture.Database.AppendAiRequestAsync(Request(today.AddHours(3), 900m, succeeded: false), default);

        var summary = await fixture.Database.GetAiRequestSummaryAsync(default);

        var expectedRequests = today.Day == 1 ? 4 : 3;
        Assert.Equal(905m, summary.EstimatedCostCurrentMonthUsd);
        Assert.Equal(today.Day == 1 ? 905m : 903m, summary.EstimatedCostTodayUsd);
        Assert.Equal(expectedRequests, summary.RequestsToday);
        Assert.Equal(expectedRequests * 10L, summary.InputTokensToday);
        Assert.Equal(expectedRequests * 2L, summary.OutputTokensToday);
        Assert.Equal(expectedRequests * 12L, summary.TotalTokensToday);
    }

    /// <summary>Builds one synthetic ledger row with deterministic usage and the specified period and outcome.</summary>
    private static AiRequestLog Request(DateTime occurred, decimal? cost, bool succeeded = true) => new(
        Guid.NewGuid().ToString("N"), "summary-test", "query-system", new DateTimeOffset(occurred), new DateTimeOffset(occurred),
        "example.invalid", "gpt-5.6-terra", "gpt-5.6-terra", "Synthetic question", "Synthetic answer",
        new AiUsageMetrics(10, 0, 0, 2, 0, 12), cost, 200, 1, null, null, succeeded, succeeded ? null : "synthetic_error");
}

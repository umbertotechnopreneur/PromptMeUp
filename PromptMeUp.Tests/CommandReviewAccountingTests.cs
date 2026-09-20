// SPDX-License-Identifier: MIT

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class CommandReviewAccountingTests
{
    /// <summary>Counts successful and rejected AI review contracts in the parent flow without closing or relabeling it.</summary>
    [Theory]
    [InlineData(15, true)]
    [InlineData(-1, false)]
    public async Task Review_RecordsCostAndUsageInOwningFlow(int score, bool accepted)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short")], default);
        await fixture.Audit.StartSessionAsync("owner-flow", "chat", AppSettings.Default, null, default);
        var statusBefore = await fixture.ScalarAsync("SELECT status FROM ai_sessions WHERE id = 'owner-flow';");
        var body = JsonNode.Parse(RegressionFixture.ResponseJson(inputTokens: 25))!;
        body["output"]![0]!["content"]![0]!["text"] =
            $$"""{"score":{{score}},"level":"low","description_markdown":"Read-only review."}""";
        using var http = new HttpClient(new SyntheticHttpHandler(() => new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(body.ToJsonString())
        }));
        var reviewer = new CommandRiskAssessmentService(fixture.CreateOpenAi(http, promptId: "command-risk"),
            fixture.Secrets, new SensitiveDataRedactor(), NullLogger<CommandRiskAssessmentService>.Instance);

        var assessment = await reviewer.AssessAsync("owner-flow", "Get-Location", true, AppSettings.Default, "en", default);

        Assert.Equal(accepted, assessment.UsedAi);
        Assert.Equal(accepted, assessment.CanRunDirect);
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_sessions;"));
        Assert.Equal(statusBefore, await fixture.ScalarAsync("SELECT status FROM ai_sessions WHERE id = 'owner-flow';"));
        Assert.Equal("chat", await fixture.ScalarAsync("SELECT kind FROM ai_sessions WHERE id = 'owner-flow';"));
        Assert.Equal("owner-flow", await fixture.ScalarAsync("SELECT conversation_id FROM ai_requests WHERE prompt_id = 'command-risk';"));
        var accounting = await fixture.Database.GetSessionAccountingAsync("owner-flow", default);
        Assert.Equal(25, accounting.Usage.InputTokens);
        Assert.Equal(1, accounting.Usage.OutputTokens);
        Assert.Equal(0.000025m, accounting.EstimatedCostUsd);
    }
}

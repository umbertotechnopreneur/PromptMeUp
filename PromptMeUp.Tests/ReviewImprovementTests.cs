// SPDX-License-Identifier: MIT

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ReviewImprovementTests
{
    /// <summary>Rejects recognizable credential arguments consistently before dispatch or transport.</summary>
    [Theory]
    [InlineData("--query")]
    [InlineData("--diagnose")]
    [InlineData("--plan")]
    [InlineData("--script")]
    public void Parse_CredentialQuery_RejectsEveryFeature(string option)
    {
        var text = new LocalizationService();
        var result = new CommandLineParser(text).Parse([option, "Explain password=synthetic-value"]);
        Assert.False(result.Succeeded);
        Assert.Equal(text.Text("Input.SecretArgument"), result.Error);
    }

    /// <summary>Rejects credential-bearing positional and inline query forms as well as explicit query values.</summary>
    [Fact]
    public void Parse_AlternateQueryForms_RejectsCredentials()
    {
        var parser = new CommandLineParser(new LocalizationService());
        Assert.False(parser.Parse(["password=synthetic-value"]).Succeeded);
        Assert.False(parser.Parse(["--query=password=synthetic-value"]).Succeeded);
    }

    /// <summary>Redacts recognizable credentials before serializing any conversation message for transport.</summary>
    [Fact]
    public async Task SendAsync_InteractiveCredential_RedactsProviderBody()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var handler = new CapturingHandler();
        using var http = new HttpClient(handler);
        await fixture.CreateOpenAi(http).SendAsync("query-system", "privacy-input",
            [new ChatMessage("user", "password=synthetic-value")], AppSettings.Default, "en", default);
        Assert.DoesNotContain("synthetic-value", handler.Body, StringComparison.Ordinal);
        Assert.Contains("redacted-credential", handler.Body, StringComparison.Ordinal);
    }

    /// <summary>A failed optional price query cannot discard successful provider content or token accounting.</summary>
    [Fact]
    public async Task SendAsync_PriceLookupFails_PreservesResponseAndUsage()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var database = TestProxy.Create<IDatabaseService>((method, args) => method.Name == "FindModelPriceAsync"
            ? Task.FromException<AiModelPrice?>(new SqliteException("Synthetic price failure", 5))
            : method.Invoke(fixture.Database, args));
        using var http = new HttpClient(new SyntheticHttpHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(RegressionFixture.ResponseJson()) }));
        var response = await fixture.CreateOpenAi(http, database: database).SendAsync("query-system", "price-failure",
            [new ChatMessage("user", "Hello")], AppSettings.Default, "en", default);
        Assert.Equal("Answer", response.Text);
        Assert.Equal(21, response.Usage.TotalTokens);
        Assert.Null(response.EstimatedCostUsd);
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT success FROM ai_requests;"));
        Assert.Equal(21L, await fixture.ScalarAsync("SELECT total_tokens FROM ai_requests;"));
    }

    /// <summary>Incomplete or invalid answers retain actual usage, response identifiers, and cost in the ledger and totals.</summary>
    [Theory]
    [InlineData(false, "invalid_chat_response")]
    [InlineData(true, "incomplete_response")]
    public async Task SendAsync_InvalidAnswer_PreservesAccounting(bool incomplete, string expectedError)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short")], default);
        var envelope = JsonNode.Parse(RegressionFixture.ResponseJson())!;
        envelope["output"]![0]!["content"]![0]!["text"] = "{";
        envelope["status"] = incomplete ? "incomplete" : "completed";
        using var http = new HttpClient(new SyntheticHttpHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(envelope.ToJsonString()) }));
        var error = await Assert.ThrowsAsync<OpenAiRequestException>(() => fixture.CreateOpenAi(http).SendAsync("query-system", "invalid-answer",
            [new ChatMessage("user", "Hello")], AppSettings.Default, "en", default));
        Assert.Equal(expectedError, error.ErrorCode);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT success FROM ai_requests;"));
        Assert.Equal(21L, await fixture.ScalarAsync("SELECT total_tokens FROM ai_requests;"));
        Assert.Equal("synthetic-response", await fixture.ScalarAsync("SELECT provider_response_id FROM ai_requests;"));
        var summary = await fixture.Database.GetAiRequestSummaryAsync(default);
        Assert.Equal(1, summary.RequestsToday);
        Assert.Equal(21, summary.TotalTokensToday);
        Assert.Equal(0.000020m, summary.EstimatedCostTodayUsd);
    }

    /// <summary>Session cleanup does not replace the result of completed work with a secondary storage failure.</summary>
    [Fact]
    public async Task CloseSessionAsync_DatabaseFails_DoesNotThrow()
    {
        var database = TestProxy.Create<IDatabaseService>((_, _) => Task.FromException(new SqliteException("Synthetic close failure", 5)));
        await new ActivityAuditService(database, new SensitiveDataRedactor()).CloseSessionAsync("test", "completed", default);
    }

    /// <summary>Exercises actual localized risk rendering instead of checking only catalog key counts.</summary>
    [Theory]
    [InlineData("en", "Local review")]
    [InlineData("it", "Valutazione locale")]
    [InlineData("fr", "Évaluation locale")]
    [InlineData("de", "Lokale Bewertung")]
    [InlineData("es", "Evaluación local")]
    [InlineData("vi", "Đánh giá cục bộ")]
    public void AssessLocally_AllLanguages_RenderLocalizedExplanations(string language, string heading)
    {
        foreach (var command in new[] { "Get-Date", "Remove-Item sample -Recurse", "Set-Content sample value", "curl example.invalid", "unknown-command" })
        {
            var result = CommandRiskAssessmentService.AssessLocally(command, language);
            Assert.Contains(heading, result.DescriptionMarkdown, StringComparison.Ordinal);
            Assert.DoesNotContain("{0}", result.DescriptionMarkdown, StringComparison.Ordinal);
            if (language != "en")
            {
                Assert.DoesNotContain("The command", result.DescriptionMarkdown, StringComparison.Ordinal);
            }
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        /// <summary>Captures only synthetic request data and returns an in-memory provider response.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(RegressionFixture.ResponseJson(), Encoding.UTF8) };
        }
    }
}

// SPDX-License-Identifier: MIT

using System.Net;
using System.Text;
using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Tests;

public sealed class PrivacyFlowRegressionTests
{
    /// <summary>Verifies structural credential detection protects both SQLite ledgers without removing token accounting.</summary>
    [Fact]
    public async Task Audit_NestedCredentialProperties_RedactsBothLedgersAndPreservesUsage()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Audit.StartSessionAsync("privacy-structure", "chat", AppSettings.Default, null, default);
        var payload = new
        {
            accessToken = "synthetic-access-value",
            tokenType = "Bearer",
            Credentials = new[]
            {
                new { SecretAccessKey = "synthetic-secret-value", SessionToken = "synthetic-session-value" }
            },
            usage = new AiUsageMetrics(20, 4, 0, 1, 0, 21),
            safe = "keep"
        };

        await fixture.Audit.AppendSessionEventAsync("privacy-structure", "credential-output", payload, default);
        await fixture.Audit.RecordAsync("credential-output", "completed", "privacy-structure", payload, default);

        foreach (var query in new[]
        {
            "SELECT payload_json FROM ai_session_events;",
            "SELECT payload_json FROM activity_audit;"
        })
        {
            var stored = Assert.IsType<string>(await fixture.ScalarAsync(query));
            Assert.DoesNotContain("synthetic-", stored, StringComparison.Ordinal);
            using var json = JsonDocument.Parse(stored);
            var root = json.RootElement;
            Assert.Equal("Bearer", root.GetProperty("tokenType").GetString());
            Assert.Equal("keep", root.GetProperty("safe").GetString());
            Assert.Equal(payload.usage, root.GetProperty("usage").Deserialize<AiUsageMetrics>());
            Assert.Contains("redacted", root.GetProperty("accessToken").GetString(), StringComparison.Ordinal);
            Assert.Contains("redacted", root.GetProperty("Credentials")[0].GetProperty("SecretAccessKey").GetString(), StringComparison.Ordinal);
            Assert.Contains("redacted", root.GetProperty("Credentials")[0].GetProperty("SessionToken").GetString(), StringComparison.Ordinal);
        }
    }

    /// <summary>Verifies actual provider serialization and prompt persistence redact reviewed formats while response usage remains typed.</summary>
    [Fact]
    public async Task SendAsync_ReviewedCredentialFormats_RedactsTransportAndRetainsAccounting()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var handler = new CapturingHandler();
        using var http = new HttpClient(handler);
        const string input = """
            {"accessToken":"synthetic-access-syntheticSuffixSentinel","tokenType":"Bearer","inputTokens":31,"safe":"keep"}
            PASSWORD='synthetic-single''syntheticSuffixSentinel'; safe=keep
            PASSWORD="synthetic-double`"syntheticSuffixSentinel"; safe=keep
            PASSWORD="synthetic-incomplete head syntheticSuffixSentinel
            """;

        var response = await fixture.CreateOpenAi(http).SendAsync("query-system", "privacy-provider",
            [new ChatMessage("user", input)], AppSettings.Default, "en", default);

        using var request = JsonDocument.Parse(handler.Body);
        var providerInput = request.RootElement.GetProperty("input")[0].GetProperty("content").GetString()!;
        Assert.DoesNotContain("synthetic-", providerInput, StringComparison.Ordinal);
        Assert.DoesNotContain("syntheticSuffixSentinel", providerInput, StringComparison.Ordinal);
        using var retainedJson = JsonDocument.Parse(providerInput.Split('\n')[0]);
        Assert.Equal("Bearer", retainedJson.RootElement.GetProperty("tokenType").GetString());
        Assert.Equal(31, retainedJson.RootElement.GetProperty("inputTokens").GetInt32());
        Assert.Equal("keep", retainedJson.RootElement.GetProperty("safe").GetString());

        var storedPrompt = Assert.IsType<string>(await fixture.ScalarAsync(
            "SELECT payload_json FROM ai_session_events WHERE event_type = 'prompt';"));
        Assert.DoesNotContain("synthetic-", storedPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("syntheticSuffixSentinel", storedPrompt, StringComparison.Ordinal);
        using var promptJson = JsonDocument.Parse(storedPrompt);
        Assert.Equal(providerInput, promptJson.RootElement.GetProperty("content").GetString());
        var storedResponse = Assert.IsType<string>(await fixture.ScalarAsync(
            "SELECT payload_json FROM ai_session_events WHERE event_type = 'response';"));
        using var responseJson = JsonDocument.Parse(storedResponse);
        Assert.Equal(response.Usage, responseJson.RootElement.GetProperty("usage").Deserialize<AiUsageMetrics>());
        Assert.Equal(20L, await fixture.ScalarAsync("SELECT input_tokens FROM ai_requests;"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT output_tokens FROM ai_requests;"));
        Assert.Equal(21L, await fixture.ScalarAsync("SELECT total_tokens FROM ai_requests;"));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        /// <summary>Captures synthetic provider input and returns an in-memory response without network access.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RegressionFixture.ResponseJson(), Encoding.UTF8)
            };
        }
    }
}

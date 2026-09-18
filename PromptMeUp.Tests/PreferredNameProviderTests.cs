// SPDX-License-Identifier: MIT

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Services.OpenAi;

namespace PromptMeUp.Tests;

public sealed class PreferredNameProviderTests
{
    /// <summary>Sends the normalized literal name in localized chat and query instructions with either caching mode and matching estimates.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public async Task Send_ConfiguredName_UsesLocalizedEscapedContextAndBudget(string language)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var prompts = Prompts(fixture);
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var provider = Provider(fixture, http, prompts);
        const string name = "Zoë \"Mây\" <Admin>";
        var context = (await prompts.GetAsync("preferred-name-context", default)).ResolveText(language)
            .Replace("{preferred_name}", JsonSerializer.Serialize(name), StringComparison.Ordinal);
        ChatMessage[] messages = [new("user", "Explain this example.")];

        foreach (var promptId in new[] { "chat-system", "query-system" })
        {
            foreach (var caching in new[] { false, true })
            {
                var settings = AppSettings.Default with { PreferredName = "  " + name + "  ", Language = language, PromptCachingEnabled = caching };
                var estimate = await provider.EstimateContextAsync(promptId, messages, settings, language, default);

                await provider.SendAsync(promptId, Guid.NewGuid().ToString("N"), messages, settings, language, default);

                using var request = JsonDocument.Parse(handler.Bodies[^1]);
                var instructions = Instructions(request.RootElement);
                Assert.Contains(context, instructions);
                Assert.DoesNotContain("{preferred_name}", instructions);
                Assert.DoesNotContain("<Admin>", instructions);
                Assert.Equal(2, instructions.Split("<preferred-name>", StringSplitOptions.None).Length);
                var expected = OpenAiRequestBuilder.EstimateContext(instructions, messages, settings.Model);
                Assert.Equal(expected.InputTokens, estimate.InputTokens);
                Assert.Equal(expected.SystemInstructionTokens, estimate.SystemInstructionTokens);
                var input = request.RootElement.GetProperty("input").EnumerateArray().Last();
                Assert.Equal(messages[0].Content, input.GetProperty("content").GetString());
                if (caching)
                {
                    Assert.DoesNotContain(name, request.RootElement.GetProperty("prompt_cache_key").GetString());
                }
            }
        }
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE user_prompt LIKE '%Zoë%' OR assistant_response LIKE '%Zoë%';"));
    }

    /// <summary>Changing or clearing the preference changes subsequent payloads without guessing a name when the field is empty.</summary>
    [Fact]
    public async Task Send_ChangedAndClearedName_OnlySharesCurrentExplicitPreference()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var provider = Provider(fixture, http, Prompts(fixture));
        var session = Guid.NewGuid().ToString("N");
        foreach (var name in new[] { "", "Example One", "Example Two", "   " })
        {
            await provider.SendAsync("chat-system", session, [new("user", "Hello")],
                AppSettings.Default with { PreferredName = name, PromptCachingEnabled = false }, "en", default);
        }

        using var unset = JsonDocument.Parse(handler.Bodies[0]);
        using var first = JsonDocument.Parse(handler.Bodies[1]);
        using var changed = JsonDocument.Parse(handler.Bodies[2]);
        using var cleared = JsonDocument.Parse(handler.Bodies[3]);
        Assert.DoesNotContain("<preferred-name>", Instructions(unset.RootElement));
        Assert.Contains("Example One", Instructions(first.RootElement));
        Assert.Contains("Example Two", Instructions(changed.RootElement));
        Assert.DoesNotContain("Example One", Instructions(changed.RootElement));
        Assert.Equal(Instructions(unset.RootElement), Instructions(cleared.RootElement));
    }

    /// <summary>Does not load or append the name preference to internal classification, command review, maintenance, or connection prompts.</summary>
    [Theory]
    [InlineData("chat-display-intent")]
    [InlineData("command-risk")]
    [InlineData("memory-dream")]
    [InlineData("memory-heartbeat")]
    [InlineData("connection-test")]
    public async Task Estimate_InternalPrompt_DoesNotLoadNameGuidance(string promptId)
    {
        using var fixture = new RegressionFixture();
        var realPrompts = Prompts(fixture);
        var readIds = new List<string>();
        var prompts = TestProxy.Create<IPromptCatalogService>((method, args) =>
        {
            Assert.Equal(nameof(IPromptCatalogService.GetAsync), method.Name);
            var id = (string)args[0]!;
            readIds.Add(id);
            return realPrompts.GetAsync(id, (CancellationToken)args[1]!);
        });
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var provider = Provider(fixture, http, prompts);
        ChatMessage[] messages = [new("user", "Synthetic input")];
        var baseline = await provider.EstimateContextAsync(promptId, messages, AppSettings.Default, "en", default);

        var named = await provider.EstimateContextAsync(promptId, messages, AppSettings.Default with { PreferredName = "Private Nickname" }, "en", default);

        Assert.Equal(baseline, named);
        Assert.DoesNotContain("preferred-name-context", readIds);
        Assert.Empty(handler.Bodies);
        var prompt = await realPrompts.GetAsync(promptId, default);
        Assert.DoesNotContain("Private Nickname", OpenAiRequestBuilder.BuildInstructions(prompt,
            AppSettings.Default with { PreferredName = "Private Nickname" }, "en"));
    }

    /// <summary>Rejects unsafe or credential-bearing names before requests or audit persistence even when settings bypass the database.</summary>
    [Theory]
    [InlineData("Name\nOther line")]
    [InlineData("Name\u202EOther")]
    [InlineData("password=synthetic-value")]
    public async Task Send_InvalidName_FailsBeforeNetworkOrAudit(string name)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var provider = Provider(fixture, http, Prompts(fixture));
        var settings = AppSettings.Default with { PreferredName = name };

        var error = await Assert.ThrowsAsync<ArgumentException>(() => provider.SendAsync("chat-system", Guid.NewGuid().ToString("N"),
            [new("user", "Hello")], settings, "en", default));

        Assert.DoesNotContain(name, error.Message);
        Assert.Empty(handler.Bodies);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests;"));
    }

    /// <summary>Requires a versioned resource rather than silently dropping a configured name from a conversation.</summary>
    [Fact]
    public async Task BuildInstructions_ConfiguredNameWithoutResource_FailsFast()
    {
        using var fixture = new RegressionFixture();
        var prompt = await Prompts(fixture).GetAsync("chat-system", default);
        Assert.Throws<InvalidOperationException>(() => OpenAiRequestBuilder.BuildInstructions(prompt,
            AppSettings.Default with { PreferredName = "Example" }, "en"));
    }

    /// <summary>Reads the same instruction text from ordinary and explicit-cache Responses payloads.</summary>
    private static string Instructions(JsonElement request) => request.TryGetProperty("instructions", out var instructions)
        ? instructions.GetString()!
        : request.GetProperty("input")[0].GetProperty("content")[0].GetProperty("text").GetString()!;

    /// <summary>Uses packaged YAML resources so all six translations and conditional name guidance are exercised.</summary>
    private static YamlPromptCatalogService Prompts(RegressionFixture fixture) =>
        new(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);

    /// <summary>Creates the real provider service with synthetic credentials, runtime facts, and a local HTTP handler.</summary>
    private static OpenAiService Provider(RegressionFixture fixture, HttpClient http, IPromptCatalogService prompts) =>
        new(http, fixture.Secrets, prompts,
            TestProxy.Create<IRuntimeContextService>((_, _) => new RuntimeContext("~", "test", "PowerShell 7", "test", "test", "test")),
            fixture.Database, new AiCostCalculator(), fixture.Audit, new SensitiveDataRedactor(),
            new PromptInjectionProtectionService(), NullLogger<OpenAiService>.Instance);

    /// <summary>Supplies the current structured reply contract, including an empty guide-topic collection.</summary>
    private static string ResponseJson() => JsonSerializer.Serialize(new
    {
        id = "synthetic-name-response",
        model = "gpt-5.6-terra",
        status = "completed",
        output = new[]
        {
            new
            {
                type = "message",
                content = new[]
                {
                    new
                    {
                        type = "output_text",
                        text = JsonSerializer.Serialize(new
                        {
                            answer_markdown = "Synthetic answer.", commands = Array.Empty<object>(), guide_topics = Array.Empty<string>()
                        })
                    }
                }
            }
        },
        usage = new { input_tokens = 20, output_tokens = 1, total_tokens = 21 }
    });

    private sealed class RecordingHandler : HttpMessageHandler
    {
        internal List<string> Bodies { get; } = [];

        /// <summary>Captures outbound payloads and returns a synthetic response without network access.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ResponseJson()) };
        }
    }
}

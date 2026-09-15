// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Services.OpenAi;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PromptMeUp.Tests;

public sealed class OpenAiServiceRegressionTests
{
    /// <summary>Verifies that delayed body delivery shares the request deadline and records a stable failure.</summary>
    [Fact]
    public async Task SendAsync_SlowBody_TimesOutBeforeBodyArrives()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var http = CreateHttp(new DelayedHttpContent(RegressionFixture.ResponseJson(), TimeSpan.FromSeconds(5)));
        http.Timeout = TimeSpan.FromMilliseconds(150);
        var stopwatch = Stopwatch.StartNew();

        var error = await Assert.ThrowsAsync<OpenAiRequestException>(() => SendAsync(fixture.CreateOpenAi(http)));

        Assert.Equal("responses_api_timeout", error.ErrorCode);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), stopwatch.Elapsed.ToString());
        Assert.Equal("responses_api_timeout", await fixture.ScalarAsync("SELECT failure_code FROM ai_requests;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT success FROM ai_requests;"));
    }

    /// <summary>Verifies that recorded response duration includes body delivery rather than only headers.</summary>
    [Fact]
    public async Task SendAsync_DelayedSuccessfulBody_RecordsCompleteDuration()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var http = CreateHttp(new DelayedHttpContent(RegressionFixture.ResponseJson(), TimeSpan.FromMilliseconds(250)));

        var response = await SendAsync(fixture.CreateOpenAi(http));

        Assert.True(response.ElapsedMilliseconds >= 200, response.ElapsedMilliseconds.ToString());
        Assert.Equal(response.ElapsedMilliseconds, await fixture.ScalarAsync("SELECT elapsed_ms FROM ai_requests;"));
    }

    /// <summary>Verifies that user cancellation remains cancellation instead of becoming a timeout failure.</summary>
    [Fact]
    public async Task SendAsync_CallerCancelsBody_PropagatesCancellation()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var http = CreateHttp(new DelayedHttpContent(RegressionFixture.ResponseJson(), TimeSpan.FromSeconds(5)));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SendAsync(fixture.CreateOpenAi(http), cancellation.Token));

        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests;"));
    }

    /// <summary>Verifies that chunked oversized bodies are rejected before unlimited buffering.</summary>
    [Fact]
    public async Task SendAsync_OversizedBody_RejectsBoundedBufferOverflow()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var http = CreateHttp(new DelayedHttpContent(new string('x', 2 * 1024 * 1024 + 1), TimeSpan.Zero));

        await Assert.ThrowsAsync<HttpRequestException>(() => SendAsync(fixture.CreateOpenAi(http)));

        Assert.Equal(0L, await fixture.ScalarAsync("SELECT success FROM ai_requests;"));
    }

    /// <summary>Verifies actual provider usage chooses the long rate and persists the resulting estimate.</summary>
    [Fact]
    public async Task SendAsync_LongContext_UsesLongBandForActualInput()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short"), RegressionFixture.Price("long", 2m)], default);
        using var http = CreateHttp(new StringContent(RegressionFixture.ResponseJson(inputTokens: 400_000)));

        var response = await SendAsync(fixture.CreateOpenAi(http));

        Assert.Equal(0.8m, response.EstimatedCostUsd);
        Assert.Equal(800_000L, await fixture.ScalarAsync("SELECT estimated_cost_microusd FROM ai_requests;"));
    }

    /// <summary>Verifies unknown returned models never inherit the requested model's potentially different price.</summary>
    [Fact]
    public async Task SendAsync_UnknownReturnedModel_LeavesCostUnknown()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short")], default);
        using var http = CreateHttp(new StringContent(RegressionFixture.ResponseJson(model: "unknown-model")));

        var response = await SendAsync(fixture.CreateOpenAi(http));

        Assert.Null(response.EstimatedCostUsd);
        Assert.Equal(DBNull.Value, await fixture.ScalarAsync("SELECT estimated_cost_microusd FROM ai_requests;"));
    }

    /// <summary>Verifies a provider credential echo cannot reach Serilog through an optional review exception.</summary>
    [Fact]
    public async Task AssessAsync_ProviderEchoesCredential_PersistentLogExcludesExceptionDetails()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var marker = "synthetic-" + Guid.NewGuid().ToString("N");
        using var http = new HttpClient(new SyntheticHttpHandler(() => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { error = new { message = "password=" + marker } }))
        }));
        var sink = new CaptureSink();
        using var serilog = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Sink(sink).CreateLogger();
        using var loggers = LoggerFactory.Create(builder => builder.AddSerilog(serilog));
        var service = new CommandRiskAssessmentService(fixture.CreateOpenAi(http, loggers.CreateLogger<OpenAiService>()),
            fixture.Secrets, new SensitiveDataRedactor(), loggers.CreateLogger<CommandRiskAssessmentService>());

        var assessment = await service.AssessAsync("Get-Date", true, AppSettings.Default, "en", default);

        Assert.False(assessment.UsedAi);
        Assert.Contains(sink.Events, item => item.Level == LogEventLevel.Warning);
        Assert.All(sink.Events, item =>
        {
            Assert.Null(item.Exception);
            Assert.DoesNotContain(marker, item.RenderMessage(), StringComparison.Ordinal);
        });
        Assert.Contains(sink.Events, item => item.RenderMessage().Contains(nameof(OpenAiRequestException), StringComparison.Ordinal));
    }

    /// <summary>Verifies direct provider calls reject a credential-bearing preamble before transport or audit.</summary>
    [Fact]
    public async Task SendAsync_CredentialPreamble_RejectsBeforeProviderRequest()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var http = new HttpClient(new SyntheticHttpHandler(() => throw new InvalidOperationException("Transport must not run.")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.CreateOpenAi(http).SendAsync("query-system", "test",
            [new ChatMessage("user", "Hello")], AppSettings.Default with { CustomInstruction = "password=synthetic-value" }, "en", default));

        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_sessions;"));
    }

    /// <summary>Verifies guide-aware estimation matches the redacted, populated system and message payload sent to the provider.</summary>
    [Fact]
    public async Task EstimateContextAsync_WithGuide_MatchesProviderPayload()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var handler = new GuideHttpHandler([GuideResponseJson([], 20)]);
        using var http = new HttpClient(handler);
        var service = CreateGuideService(fixture, http);
        var settings = AppSettings.Default with { PromptCachingEnabled = false };
        var guide = CreateGuide();
        ChatMessage[] messages = [new("user", "Review password=synthetic-value and explain memories.")];

        var estimate = await service.EstimateContextAsync("query-system", messages, settings, "en", default, guide);
        var response = await service.SendAsync("query-system", "guide-payload", messages, settings, "en", default, guide);
        using var body = JsonDocument.Parse(Assert.Single(handler.Requests));
        var instructions = body.RootElement.GetProperty("instructions").GetString()!;
        var sentMessages = body.RootElement.GetProperty("input").EnumerateArray()
            .Select(item => new ChatMessage(item.GetProperty("role").GetString()!, item.GetProperty("content").GetString()!)).ToArray();
        var sentEstimate = OpenAiRequestBuilder.EstimateContext(instructions, sentMessages, settings.Model, guide);

        Assert.Equal(sentEstimate.InputTokens, estimate.InputTokens);
        Assert.Equal(sentEstimate.SystemInstructionTokens, estimate.SystemInstructionTokens);
        Assert.Equal(sentEstimate.UserMessageTokens, estimate.UserMessageTokens);
        Assert.Equal(guide.Tokens, response.ContextUsage.GuideTokens);
        Assert.Contains(guide.Text, instructions, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-value", sentMessages[0].Content, StringComparison.Ordinal);
    }

    /// <summary>Verifies a guide request and its answer each retain their own persisted usage and cost despite the empty intermediate answer.</summary>
    [Fact]
    public async Task SendAsync_GuideThenAnswer_PersistsBothProviderCalls()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short")], default);
        var handler = new GuideHttpHandler([GuideResponseJson(["memories"], 20), GuideResponseJson([], 40)]);
        using var http = new HttpClient(handler);
        var service = CreateGuideService(fixture, http);
        ChatMessage[] messages = [new("user", "How do memories work?")];

        var first = await service.SendAsync("query-system", "guide-accounting", messages, AppSettings.Default, "en", default);
        var answer = await service.SendAsync("query-system", "guide-accounting", messages, AppSettings.Default, "en", default, CreateGuide());

        Assert.Empty(first.Text);
        Assert.Equal(["memories"], first.GuideTopics);
        Assert.Equal("Answer", answer.Text);
        Assert.Equal(40, answer.Usage.InputTokens);
        Assert.Equal(2L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
        Assert.Equal(60L, await fixture.ScalarAsync("SELECT SUM(input_tokens) FROM ai_requests;"));
        Assert.Equal(60L, await fixture.ScalarAsync("SELECT SUM(estimated_cost_microusd) FROM ai_requests;"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_session_events WHERE event_type = 'guide-request';"));
    }

    /// <summary>Verifies invalid guide topics still preserve billed provider usage while the response fails closed.</summary>
    [Fact]
    public async Task SendAsync_InvalidGuideRequest_PersistsFailureAccounting()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short")], default);
        using var http = new HttpClient(new GuideHttpHandler([GuideResponseJson(["unknown"], 20)]));

        var exception = await Assert.ThrowsAsync<OpenAiRequestException>(() =>
            SendAsync(CreateGuideService(fixture, http)));

        Assert.Equal("invalid_chat_response", exception.ErrorCode);
        Assert.Equal(20L, await fixture.ScalarAsync("SELECT input_tokens FROM ai_requests;"));
        Assert.Equal(20L, await fixture.ScalarAsync("SELECT estimated_cost_microusd FROM ai_requests;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT success FROM ai_requests;"));
    }

    /// <summary>Verifies guide instructions cannot bypass the same complete input budget used for ordinary requests.</summary>
    [Fact]
    public async Task SendAsync_GuideExceedsBudget_RejectsBeforeTransportOrAudit()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var handler = new GuideHttpHandler([]);
        using var http = new HttpClient(handler);
        var service = CreateGuideService(fixture, http);
        var guide = CreateGuide();
        ChatMessage[] messages = [new("user", "How do memories work?")];
        var estimate = await service.EstimateContextAsync("query-system", messages, AppSettings.Default, "en", default, guide);
        var settings = AppSettings.Default with { ContextTokenBudget = checked((int)estimate.InputTokens - 1) };

        await Assert.ThrowsAsync<ConversationLimitException>(() =>
            service.SendAsync("query-system", "guide-budget", messages, settings, "en", default, guide));

        Assert.Empty(handler.Requests);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_sessions;"));
    }

    /// <summary>Creates a bounded synthetic chapter with the exact token metadata expected by instruction assembly.</summary>
    private static AppGuideContext CreateGuide()
    {
        const string text = "<app-guide>Memories are local notes selected for relevant requests.</app-guide>";
        return new AppGuideContext(["memories"], text, ContextTokenEstimator.Text(text));
    }

    /// <summary>Constructs a guide-aware provider client backed by the isolated fixture and synthetic local collaborators.</summary>
    private static OpenAiService CreateGuideService(RegressionFixture fixture, HttpClient http) => new(
        http, fixture.Secrets,
        TestProxy.Create<IPromptCatalogService>((_, _) => Task.FromResult(new PromptDefinition(
            "query-system", 2, "Synthetic guide prompt", [],
            new Dictionary<string, string> { ["en"] = "Answer or request a built-in guide chapter." },
            new Dictionary<string, string> { ["response-format"] = "promptmeup-console-response-v2" }))),
        TestProxy.Create<IRuntimeContextService>((_, _) => new RuntimeContext("~", "test", "PowerShell 7", "test", "test", "test")),
        fixture.Database, new AiCostCalculator(), fixture.Audit, new SensitiveDataRedactor(), new PromptInjectionProtectionService(),
        NullLogger<OpenAiService>.Instance);

    /// <summary>Creates a synthetic v2 response with independent provider accounting for either retrieval or a final answer.</summary>
    private static string GuideResponseJson(IReadOnlyList<string> topics, long inputTokens) => JsonSerializer.Serialize(new
    {
        id = "synthetic-guide-response",
        model = "gpt-5.6-terra",
        status = "completed",
        output = new[]
        {
            new
            {
                content = new[]
                {
                    new
                    {
                        type = "output_text",
                        text = JsonSerializer.Serialize(new { answer_markdown = topics.Count > 0 ? string.Empty : "Answer", commands = Array.Empty<object>(), guide_topics = topics })
                    }
                }
            }
        },
        usage = new { input_tokens = inputTokens, output_tokens = 1, total_tokens = inputTokens + 1 }
    });

    /// <summary>Creates a successful synthetic transport with a bounded default timeout.</summary>
    private static HttpClient CreateHttp(HttpContent content) => new(new SyntheticHttpHandler(() =>
        new HttpResponseMessage(HttpStatusCode.OK) { Content = content }))
    { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Sends one short synthetic query through the real provider service.</summary>
    private static Task<AiResponse> SendAsync(OpenAiService service, CancellationToken cancellationToken = default) =>
        service.SendAsync("query-system", "regression-session", [new ChatMessage("user", "Hello")], AppSettings.Default, "en", cancellationToken);

    private sealed class GuideHttpHandler(IEnumerable<string> responses) : HttpMessageHandler
    {
        private readonly Queue<string> _responses = new(responses);
        public List<string> Requests { get; } = [];

        /// <summary>Captures each provider-visible request and returns the next in-memory response without networking.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_responses.Dequeue()) };
        }
    }

    private sealed class CaptureSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        /// <summary>Captures exactly what the persistent Serilog pipeline would receive.</summary>
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}

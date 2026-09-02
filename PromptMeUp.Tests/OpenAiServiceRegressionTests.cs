// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
using PromptMeUp.Services;
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

    /// <summary>Creates a successful synthetic transport with a bounded default timeout.</summary>
    private static HttpClient CreateHttp(HttpContent content) => new(new SyntheticHttpHandler(() =>
        new HttpResponseMessage(HttpStatusCode.OK) { Content = content }))
    { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Sends one short synthetic query through the real provider service.</summary>
    private static Task<AiResponse> SendAsync(OpenAiService service, CancellationToken cancellationToken = default) =>
        service.SendAsync("query-system", "regression-session", [new ChatMessage("user", "Hello")], AppSettings.Default, "en", cancellationToken);

    private sealed class CaptureSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        /// <summary>Captures exactly what the persistent Serilog pipeline would receive.</summary>
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}

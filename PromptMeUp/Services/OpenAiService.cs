// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
using PromptMeUp.Services.OpenAi;

namespace PromptMeUp.Services;

public interface IOpenAiService
{
    Task<AiResponse> SendAsync(
        string promptId,
        string conversationId,
        IReadOnlyList<ChatMessage> messages,
        AppSettings settings,
        string language,
        CancellationToken cancellationToken);

    Task<ConnectionTestResult> TestConnectionAsync(
        AppSettings settings,
        string language,
        CancellationToken cancellationToken);

    Task<AiContextUsage> EstimateContextAsync(
        string promptId,
        IReadOnlyList<ChatMessage> messages,
        AppSettings settings,
        string language,
        CancellationToken cancellationToken);

    Task<CommandRiskAssessment> AssessCommandAsync(
        string command,
        AppSettings settings,
        string language,
        CancellationToken cancellationToken);
}

public sealed class OpenAiService : IOpenAiService
{
    private const string Provider = "openai";
    private readonly HttpClient _http;
    private readonly IEnvironmentSecretService _secrets;
    private readonly IPromptCatalogService _prompts;
    private readonly IRuntimeContextService _runtimeContext;
    private readonly IDatabaseService _database;
    private readonly IAiCostCalculator _costCalculator;
    private readonly IActivityAuditService _audit;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly ILogger<OpenAiService> _logger;

    /// <summary>Creates the Responses API client and its local persistence collaborators.</summary>
    public OpenAiService(
        HttpClient http,
        IEnvironmentSecretService secrets,
        IPromptCatalogService prompts,
        IRuntimeContextService runtimeContext,
        IDatabaseService database,
        IAiCostCalculator costCalculator,
        IActivityAuditService audit,
        ISensitiveDataRedactor redactor,
        ILogger<OpenAiService> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _costCalculator = costCalculator ?? throw new ArgumentNullException(nameof(costCalculator));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Sends one bounded multi-turn conversation through the configured OpenAI Responses endpoint.</summary>
    public async Task<AiResponse> SendAsync(
        string promptId,
        string conversationId,
        IReadOnlyList<ChatMessage> messages,
        AppSettings settings,
        string language,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(settings);
        if (messages.Count == 0 || messages.Any(message => string.IsNullOrWhiteSpace(message.Content)))
        {
            throw new ArgumentException("At least one non-empty conversation message is required.", nameof(messages));
        }

        var prompt = await _prompts.GetAsync(promptId, cancellationToken).ConfigureAwait(false);
        var instructions = OpenAiRequestBuilder.BuildInstructions(
            prompt,
            settings,
            language,
            _runtimeContext.GetCurrent());
        return await SendCoreAsync(
            prompt,
            conversationId,
            messages,
            instructions,
            settings,
            OpenAiRequestBuilder.ResolveMaxOutputTokens(prompt, settings.OutputDetail),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs the localized YAML connection probe and returns its expected short phrase.</summary>
    public async Task<ConnectionTestResult> TestConnectionAsync(
        AppSettings settings,
        string language,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var prompt = await _prompts.GetAsync("connection-test", cancellationToken).ConfigureAwait(false);
        var expectedKey = $"expected.{SupportedLanguages.Normalize(language)}";
        var expected = prompt.Metadata.TryGetValue(expectedKey, out var localizedExpected)
            ? localizedExpected
            : prompt.Metadata["expected.en"];
        var sessionId = Guid.NewGuid().ToString("N");
        var status = "failed";
        try
        {
            var response = await SendCoreAsync(
                prompt,
                sessionId,
                [new ChatMessage("user", prompt.ResolveText(language))],
                prompt.ResolveText(language),
                settings,
                OpenAiRequestBuilder.ResolveMaxOutputTokens(prompt, settings.OutputDetail),
                cancellationToken).ConfigureAwait(false);
            if (!string.Equals(response.Text.Trim(), expected.Trim(), StringComparison.Ordinal))
            {
                throw new OpenAiRequestException(
                    "The connection test returned an unexpected response.",
                    "connection_test_mismatch",
                    response.HttpStatusCode);
            }
            status = "completed";
            return new ConnectionTestResult(response);
        }
        finally
        {
            await TryCloseSessionAsync(sessionId, status).ConfigureAwait(false);
        }
    }

    /// <summary>Estimates the populated YAML instruction and conversation context before any network call.</summary>
    public async Task<AiContextUsage> EstimateContextAsync(
        string promptId,
        IReadOnlyList<ChatMessage> messages,
        AppSettings settings,
        string language,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(settings);
        var prompt = await _prompts.GetAsync(promptId, cancellationToken).ConfigureAwait(false);
        return OpenAiRequestBuilder.EstimateContext(
            OpenAiRequestBuilder.BuildInstructions(
                prompt,
                settings,
                language,
                _runtimeContext.GetCurrent()),
            messages,
            settings.Model);
    }

    /// <summary>Requests an advisory AI risk review for a command; final authorization remains local and manual.</summary>
    public async Task<CommandRiskAssessment> AssessCommandAsync(
        string command,
        AppSettings settings,
        string language,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(settings);
        var prompt = await _prompts.GetAsync("command-risk", cancellationToken).ConfigureAwait(false);
        var sessionId = Guid.NewGuid().ToString("N");
        var status = "failed";
        try
        {
            var response = await SendCoreAsync(
                prompt,
                sessionId,
                [new ChatMessage("user", command)],
                prompt.ResolveText(language),
                settings,
                500,
                cancellationToken).ConfigureAwait(false);
            status = "completed";
            return OpenAiResponseParser.ParseRiskAssessment(response.Text);
        }
        finally
        {
            await TryCloseSessionAsync(sessionId, status).ConfigureAwait(false);
        }
    }

    /// <summary>Performs one HTTP request, parses usage, estimates price, and records both success and failure.</summary>
    private async Task<AiResponse> SendCoreAsync(
        PromptDefinition prompt,
        string conversationId,
        IReadOnlyList<ChatMessage> messages,
        string instructions,
        AppSettings settings,
        int maxOutputTokens,
        CancellationToken cancellationToken)
    {
        var key = _secrets.Load(settings.ApiKeyVariable);
        if (!_secrets.LooksLikeOpenAiKey(key))
        {
            throw new OpenAiRequestException(
                $"Environment variable {settings.ApiKeyVariable} is missing or invalid.",
                "api_key_missing",
                null);
        }

        var requestLogId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var endpoint = new Uri(settings.Endpoint, UriKind.Absolute);
        var latestUserText = messages.Last(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)).Content;
        var estimatedContext = OpenAiRequestBuilder.EstimateContext(instructions, messages, settings.Model);
        var configuredContextLimit = checked(estimatedContext.ContextWindowTokens * settings.MaxContextPercent / 100);
        if (estimatedContext.InputTokens > configuredContextLimit)
        {
            throw new ConversationLimitException(
                $"Estimated input context {estimatedContext.InputTokens:N0} exceeds the configured {settings.MaxContextPercent}% limit ({configuredContextLimit:N0} tokens). Clear the chat or reduce the prompt.");
        }
        var clientRequestId = Guid.NewGuid().ToString();
        HttpResponseMessage? response = null;
        string? providerRequestId = null;

        try
        {
            await _audit.StartSessionAsync(
                conversationId,
                prompt.Id,
                settings,
                new { promptId = prompt.Id, promptVersion = prompt.Version, caching = settings.PromptCachingEnabled },
                cancellationToken).ConfigureAwait(false);
            await _audit.AppendSessionEventAsync(
                conversationId,
                "prompt",
                new
                {
                    role = "user",
                    content = latestUserText,
                    instructions,
                    messages,
                    promptId = prompt.Id,
                    requestedModel = settings.Model,
                    context = estimatedContext
                },
                cancellationToken).ConfigureAwait(false);
            var requestBody = OpenAiRequestBuilder.BuildBody(prompt, settings, messages, instructions, maxOutputTokens);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            request.Headers.TryAddWithoutValidation("X-Client-Request-Id", clientRequestId);

            _logger.LogInformation(
                "OpenAI request started. PromptId={PromptId}, Model={Model}, ConversationId={ConversationId}, ClientRequestId={ClientRequestId}",
                prompt.Id,
                settings.Model,
                conversationId,
                clientRequestId);
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            providerRequestId = ReadHeader(response, "x-request-id");
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var providerError = OpenAiResponseParser.ReadApiError(responseJson)
                    ?? $"OpenAI returned HTTP {(int)response.StatusCode}.";
                throw new OpenAiRequestException(providerError, "responses_api_failed", (int)response.StatusCode);
            }

            var parsed = OpenAiResponseParser.ParseResponse(
                responseJson,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                providerRequestId,
                IsStructuredAssistantPrompt(prompt));
            var price = await ResolvePriceAsync(parsed.Model, settings.Model, cancellationToken).ConfigureAwait(false);
            var final = parsed with
            {
                ContextUsage = estimatedContext with
                {
                    InputTokens = parsed.Usage.InputTokens,
                    OutputTokens = parsed.Usage.OutputTokens,
                    IsInputEstimate = false
                },
                CostBreakdown = price is null ? null : _costCalculator.CalculateBreakdown(parsed.Usage, price),
                EstimatedCostUsd = price is null ? null : _costCalculator.Calculate(parsed.Usage, price)
            };
            await PersistAsync(
                new AiRequestLog(
                    requestLogId,
                    conversationId,
                    prompt.Id,
                    occurredAt,
                    DateTimeOffset.UtcNow,
                    endpoint.Host,
                    settings.Model,
                    final.Model,
                    _redactor.Redact(latestUserText),
                    _redactor.Redact(final.Text),
                    final.Usage,
                    final.EstimatedCostUsd,
                    final.HttpStatusCode,
                    final.ElapsedMilliseconds,
                    final.Id,
                    final.ProviderRequestId,
                    true,
                    null),
                cancellationToken).ConfigureAwait(false);
            await TryAppendSessionEventAsync(
                conversationId,
                "response",
                new
                {
                    role = "assistant",
                    content = final.Text,
                    responseId = final.Id,
                    model = final.Model,
                    usage = final.Usage,
                    context = final.ContextUsage,
                    cost = final.CostBreakdown,
                    suggestedCommands = final.SuggestedCommands.Select(command => new
                    {
                        label = _redactor.Redact(command.Label),
                        command = _redactor.Redact(command.Command)
                    }).ToArray(),
                    providerRequestId = final.ProviderRequestId
                }).ConfigureAwait(false);
            _logger.LogInformation(
                "OpenAI request completed. PromptId={PromptId}, Model={Model}, HttpStatus={HttpStatus}, TotalTokens={TotalTokens}, ElapsedMs={ElapsedMs}, ProviderRequestId={ProviderRequestId}",
                prompt.Id,
                final.Model,
                final.HttpStatusCode,
                final.Usage.TotalTokens,
                final.ElapsedMilliseconds,
                final.ProviderRequestId);
            return final;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var statusCode = exception is OpenAiRequestException openAiException
                ? openAiException.StatusCode
                : response is null
                    ? null
                    : (int)response.StatusCode;
            var failureCode = exception is OpenAiRequestException requestException
                ? requestException.ErrorCode
                : exception.GetType().Name;
            await PersistAsync(
                new AiRequestLog(
                    requestLogId,
                    conversationId,
                    prompt.Id,
                    occurredAt,
                    DateTimeOffset.UtcNow,
                    endpoint.Host,
                    settings.Model,
                    null,
                    _redactor.Redact(latestUserText),
                    null,
                    OpenAiResponseParser.EmptyUsage,
                    null,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    null,
                    providerRequestId,
                    false,
                    failureCode),
                CancellationToken.None).ConfigureAwait(false);
            await TryAppendSessionEventAsync(
                conversationId,
                "error",
                new
                {
                    promptId = prompt.Id,
                    failureCode,
                    httpStatus = statusCode,
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds
                }).ConfigureAwait(false);
            _logger.LogWarning(
                "OpenAI request failed. PromptId={PromptId}, Model={Model}, FailureCode={FailureCode}, HttpStatus={HttpStatus}, ElapsedMs={ElapsedMs}",
                prompt.Id,
                settings.Model,
                failureCode,
                statusCode,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <summary>Uses the returned model first, then the configured model, for local price estimation.</summary>
    private async Task<AiModelPrice?> ResolvePriceAsync(
        string returnedModel,
        string requestedModel,
        CancellationToken cancellationToken) =>
        await _database.FindModelPriceAsync(Provider, returnedModel, cancellationToken).ConfigureAwait(false)
        ?? await _database.FindModelPriceAsync(Provider, requestedModel, cancellationToken).ConfigureAwait(false);

    /// <summary>Persists telemetry without allowing a database error to conceal the provider result.</summary>
    private async Task PersistAsync(AiRequestLog request, CancellationToken cancellationToken)
    {
        try
        {
            await _database.AppendAiRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "AI request persistence failed. RequestLogId={RequestLogId}", request.Id);
        }
    }

    /// <summary>Appends a session event without allowing audit storage failure to replace the provider outcome.</summary>
    private async Task TryAppendSessionEventAsync(string sessionId, string eventType, object payload)
    {
        try
        {
            await _audit.AppendSessionEventAsync(sessionId, eventType, payload, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "AI session event persistence failed. SessionId={SessionId}, EventType={EventType}", sessionId, eventType);
        }
    }

    /// <summary>Closes a short internal test or risk-review session without masking its primary result.</summary>
    private async Task TryCloseSessionAsync(string sessionId, string status)
    {
        try
        {
            await _audit.CloseSessionAsync(sessionId, status, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "AI session close failed. SessionId={SessionId}, Status={Status}", sessionId, status);
        }
    }

    /// <summary>Reads a provider response header without assuming it is present.</summary>
    private static string? ReadHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    /// <summary>Identifies assistant prompts that request the typed user-facing response envelope.</summary>
    private static bool IsStructuredAssistantPrompt(PromptDefinition prompt) =>
        string.Equals(prompt.Id, "chat-system", StringComparison.OrdinalIgnoreCase)
        || string.Equals(prompt.Id, "query-system", StringComparison.OrdinalIgnoreCase);

}

public sealed class OpenAiRequestException : Exception
{
    /// <summary>Creates a provider exception that carries a stable local code and optional HTTP status.</summary>
    public OpenAiRequestException(string message, string errorCode, int? statusCode, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public string ErrorCode { get; }

    public int? StatusCode { get; }
}

// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;

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
    private const long MinimumExplicitCachePrefixTokens = 1_024;
    private readonly HttpClient _http;
    private readonly IEnvironmentSecretService _secrets;
    private readonly IPromptCatalogService _prompts;
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
        IDatabaseService database,
        IAiCostCalculator costCalculator,
        IActivityAuditService audit,
        ISensitiveDataRedactor redactor,
        ILogger<OpenAiService> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
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
        var instructions = BuildInstructions(prompt, settings, language);
        return await SendCoreAsync(
            prompt,
            conversationId,
            messages,
            instructions,
            settings,
            ResolveMaxOutputTokens(prompt, settings.OutputDetail),
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
                ResolveMaxOutputTokens(prompt, settings.OutputDetail),
                cancellationToken).ConfigureAwait(false);
            if (!string.Equals(response.Text.Trim(), expected.Trim(), StringComparison.Ordinal))
            {
                throw new OpenAiRequestException(
                    "The connection test returned an unexpected response.",
                    "connection_test_mismatch",
                    response.HttpStatusCode);
            }
            status = "completed";
            return new ConnectionTestResult(response, expected);
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
        return EstimateContext(BuildInstructions(prompt, settings, language), messages, settings.Model);
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
            return ParseRiskAssessment(response.Text);
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
        var estimatedContext = EstimateContext(instructions, messages, settings.Model);
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
            var requestBody = BuildRequestBody(prompt, settings, messages, instructions, maxOutputTokens);
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
                var providerError = ReadApiError(responseJson) ?? $"OpenAI returned HTTP {(int)response.StatusCode}.";
                throw new OpenAiRequestException(providerError, "responses_api_failed", (int)response.StatusCode);
            }

            var parsed = ParseResponse(responseJson, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, providerRequestId);
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
                    EmptyUsage,
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

    /// <summary>Builds a provider payload without placing secrets in the serializable object.</summary>
    private static IReadOnlyDictionary<string, object> BuildRequestBody(
        PromptDefinition prompt,
        AppSettings settings,
        IReadOnlyList<ChatMessage> messages,
        string instructions,
        int maxOutputTokens)
    {
        var body = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["model"] = settings.Model,
            ["reasoning"] = new { effort = settings.ReasoningEffort },
            ["text"] = new { verbosity = ResolveVerbosity(settings.OutputDetail) },
            ["max_output_tokens"] = maxOutputTokens,
            ["store"] = false
        };

        if (settings.PromptCachingEnabled
            && IsGpt56(settings.Model)
            && EstimateTokens(instructions) >= MinimumExplicitCachePrefixTokens)
        {
            // GPT-5.6 explicit mode caches only the stable YAML instruction, not the changing conversation suffix.
            body["input"] = BuildExplicitCacheInput(instructions, messages);
            body["prompt_cache_key"] = BuildPromptCacheKey(prompt, settings.Model, instructions);
            body["prompt_cache_options"] = new { mode = "explicit", ttl = "30m" };
        }
        else
        {
            body["instructions"] = instructions;
            body["input"] = messages.Select(message => new
            {
                role = NormalizeRole(message.Role),
                content = message.Content
            }).ToArray();
            if (settings.PromptCachingEnabled)
            {
                body["prompt_cache_key"] = BuildPromptCacheKey(prompt, settings.Model, instructions);
                if (settings.Model == "gpt-5.5")
                {
                    body["prompt_cache_retention"] = "24h";
                }
            }
        }

        return body;
    }

    /// <summary>Places an explicit cache breakpoint after the stable instruction for GPT-5.6 requests.</summary>
    private static IReadOnlyList<object> BuildExplicitCacheInput(
        string instructions,
        IReadOnlyList<ChatMessage> messages)
    {
        var input = new List<object>
        {
            new
            {
                role = "developer",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
                        text = instructions,
                        prompt_cache_breakpoint = new { mode = "explicit" }
                    }
                }
            }
        };
        input.AddRange(messages.Select(message => (object)new
        {
            role = NormalizeRole(message.Role),
            content = message.Content
        }));
        return input;
    }

    /// <summary>Creates a stable routing key without embedding instruction text or user data.</summary>
    private static string BuildPromptCacheKey(PromptDefinition prompt, string model, string instructions)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(instructions)))
            .ToLowerInvariant()[..16];
        return $"promptmeup:{model}:{prompt.Id}:v{prompt.Version}:{hash}";
    }

    /// <summary>Combines the immutable YAML instruction with approved user preferences and optional locale context.</summary>
    private static string BuildInstructions(PromptDefinition prompt, AppSettings settings, string language)
    {
        var builder = new StringBuilder(prompt.ResolveText(language));
        if (string.Equals(prompt.Id, "chat-system", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(settings.CustomInstruction))
            {
                builder.AppendLine().AppendLine().Append(settings.CustomInstruction.Trim());
            }

            if (settings.IncludeWindowsLocation)
            {
                // Only coarse machine locale is included; precise location is neither requested nor inferred.
                builder.AppendLine()
                    .AppendLine()
                    .Append("Windows locale context: culture=")
                    .Append(System.Globalization.CultureInfo.CurrentCulture.Name)
                    .Append(", timezone=")
                    .Append(TimeZoneInfo.Local.Id)
                    .Append('.');
            }
        }

        return builder.ToString();
    }

    /// <summary>Builds a lightweight preflight estimate from the populated instruction and bounded message list.</summary>
    private static AiContextUsage EstimateContext(
        string instructions,
        IReadOnlyList<ChatMessage> messages,
        string model)
    {
        var instructionTokens = EstimateTokens(instructions);
        var conversationTokens = messages.Sum(message => EstimateTokens(message.Content) + 4L);
        var latestPromptTokens = messages.LastOrDefault(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)) is { } latest
            ? EstimateTokens(latest.Content)
            : 0;
        return new AiContextUsage(
            instructionTokens + conversationTokens + 8,
            0,
            instructionTokens,
            conversationTokens,
            latestPromptTokens,
            AiModelCatalog.Resolve(model).ContextWindowTokens,
            true);
    }

    /// <summary>Approximates text tokens from UTF-8 payload size until provider usage supplies the exact count.</summary>
    private static long EstimateTokens(string text) => string.IsNullOrEmpty(text)
        ? 0
        : Math.Max(1, (long)Math.Ceiling(Encoding.UTF8.GetByteCount(text) / 4d));

    /// <summary>Parses the stable response fields and tolerates non-text output items.</summary>
    private static AiResponse ParseResponse(
        string json,
        int statusCode,
        long elapsedMilliseconds,
        string? providerRequestId)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var text = ReadOutputText(root);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new OpenAiRequestException("OpenAI returned no text output.", "empty_response", statusCode);
        }

        var usage = root.TryGetProperty("usage", out var usageElement)
            ? ParseUsage(usageElement)
            : EmptyUsage;
        return new AiResponse(
            ReadOptionalString(root, "id") ?? Guid.NewGuid().ToString("N"),
            ReadOptionalString(root, "model") ?? "unknown",
            text.Trim(),
            usage,
            new AiContextUsage(usage.InputTokens, usage.OutputTokens, 0, usage.InputTokens, 0, 0, false),
            null,
            null,
            statusCode,
            elapsedMilliseconds,
            providerRequestId);
    }

    /// <summary>Concatenates output_text entries from every assistant output message.</summary>
    private static string ReadOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (string.Equals(ReadOptionalString(part, "type"), "output_text", StringComparison.Ordinal)
                    && ReadOptionalString(part, "text") is { Length: > 0 } value)
                {
                    parts.Add(value);
                }
            }
        }

        return string.Join(Environment.NewLine, parts);
    }

    /// <summary>Extracts token counters from the Responses API usage object.</summary>
    private static AiUsageMetrics ParseUsage(JsonElement usage)
    {
        var input = ReadInt64(usage, "input_tokens");
        var output = ReadInt64(usage, "output_tokens");
        var total = ReadInt64(usage, "total_tokens");
        var cached = usage.TryGetProperty("input_tokens_details", out var inputDetails)
            ? ReadInt64(inputDetails, "cached_tokens")
            : 0;
        var cacheWrite = usage.TryGetProperty("input_tokens_details", out inputDetails)
            ? ReadInt64(inputDetails, "cache_write_tokens")
            : 0;
        var reasoning = usage.TryGetProperty("output_tokens_details", out var outputDetails)
            ? ReadInt64(outputDetails, "reasoning_tokens")
            : 0;
        return new AiUsageMetrics(input, cached, cacheWrite, output, reasoning, total == 0 ? input + output : total);
    }

    /// <summary>Parses the deliberately small JSON contract returned by the risk-review prompt.</summary>
    private static CommandRiskAssessment ParseRiskAssessment(string text)
    {
        var json = StripCodeFence(text);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var score = Math.Clamp(root.GetProperty("score").GetInt32(), 0, 100);
            var levelText = root.GetProperty("level").GetString() ?? string.Empty;
            var description = root.GetProperty("description_markdown").GetString();
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new JsonException("Risk description is empty.");
            }

            var level = levelText.ToLowerInvariant() switch
            {
                "low" => CommandRiskLevel.Low,
                "medium" => CommandRiskLevel.Medium,
                "high" => CommandRiskLevel.High,
                "critical" => CommandRiskLevel.Critical,
                _ => ScoreToLevel(score)
            };
            return new CommandRiskAssessment(score, level, description.Trim(), true, null);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new OpenAiRequestException("The AI command review returned an invalid structure.", "invalid_risk_review", null, exception);
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

    /// <summary>Maps output-detail preference to the Responses API verbosity vocabulary.</summary>
    private static string ResolveVerbosity(string detail) => detail switch
    {
        "compact" => "low",
        "detailed" => "high",
        _ => "medium"
    };

    /// <summary>Chooses a bounded response budget and honors a smaller YAML diagnostic limit.</summary>
    private static int ResolveMaxOutputTokens(PromptDefinition prompt, string detail)
    {
        var configured = detail switch
        {
            "compact" => 900,
            "detailed" => 3000,
            _ => 1800
        };
        return prompt.Metadata.TryGetValue("max-output-tokens", out var text)
               && int.TryParse(text, out var promptLimit)
               && promptLimit is > 0 and <= 16_384
            ? Math.Min(configured, promptLimit)
            : configured;
    }

    /// <summary>Restricts conversation roles to those accepted by the provider.</summary>
    private static string NormalizeRole(string role) => role.ToLowerInvariant() switch
    {
        "assistant" => "assistant",
        "developer" => "developer",
        _ => "user"
    };

    /// <summary>Identifies the model family that supports explicit prompt-cache breakpoints.</summary>
    private static bool IsGpt56(string model) => model.StartsWith("gpt-5.6", StringComparison.OrdinalIgnoreCase);

    /// <summary>Maps a numeric advisory score to its display level.</summary>
    private static CommandRiskLevel ScoreToLevel(int score) => score switch
    {
        >= 85 => CommandRiskLevel.Critical,
        >= 60 => CommandRiskLevel.High,
        >= 30 => CommandRiskLevel.Medium,
        _ => CommandRiskLevel.Low
    };

    /// <summary>Removes an optional JSON code fence without interpreting other Markdown.</summary>
    private static string StripCodeFence(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLine = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstLine >= 0 && lastFence > firstLine
            ? trimmed[(firstLine + 1)..lastFence].Trim()
            : trimmed;
    }

    /// <summary>Reads a scalar token counter and treats absent fields as zero.</summary>
    private static long ReadInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? Math.Max(0, value)
            : 0;

    /// <summary>Reads a nullable JSON string.</summary>
    private static string? ReadOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    /// <summary>Reads a provider response header without assuming it is present.</summary>
    private static string? ReadHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    /// <summary>Extracts the standard provider error message while ignoring malformed envelopes.</summary>
    private static string? ReadApiError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.GetProperty("error").GetProperty("message").GetString();
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static AiUsageMetrics EmptyUsage { get; } = new(0, 0, 0, 0, 0, 0);
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

// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services.Sqlite;

namespace PromptMeUp.Services;

public interface IDatabaseService
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken);

    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken);

    Task AppendAiRequestAsync(AiRequestLog request, CancellationToken cancellationToken);

    Task ReplaceModelPricesAsync(string provider, IReadOnlyList<AiModelPrice> prices, CancellationToken cancellationToken);

    Task<IReadOnlyList<AiModelPrice>> ListModelPricesAsync(string provider, CancellationToken cancellationToken);

    Task<AiModelPrice?> FindModelPriceAsync(string provider, string model, long inputTokens, CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLatestModelPriceSyncAsync(string provider, CancellationToken cancellationToken);

    Task ReplaceOrganizationCostsAsync(DateTimeOffset from, DateTimeOffset to, IReadOnlyList<OrganizationCost> costs, CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLastOrganizationCostSyncAsync(CancellationToken cancellationToken);

    Task<AiRequestSummary> GetAiRequestSummaryAsync(CancellationToken cancellationToken);

    Task<decimal?> GetOrganizationCostCurrentMonthAsync(CancellationToken cancellationToken);

    Task EnsureAiSessionAsync(AiSessionRecord session, CancellationToken cancellationToken);

    Task CloseAiSessionAsync(string sessionId, string status, DateTimeOffset endedAt, CancellationToken cancellationToken);

    Task AppendAiSessionEventAsync(AiSessionEventRecord sessionEvent, CancellationToken cancellationToken);

    Task AppendActivityAuditAsync(ActivityAuditRecord audit, CancellationToken cancellationToken);
}

public sealed class SqliteDatabaseService : IDatabaseService
{
    internal const string AiRequestSummarySql = """
        SELECT
            COALESCE(SUM(CASE WHEN occurred_unix >= $today AND occurred_unix < $tomorrow THEN estimated_cost_microusd ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN occurred_unix >= $month THEN estimated_cost_microusd ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN (success = 1 OR total_tokens > 0) AND occurred_unix >= $today AND occurred_unix < $tomorrow THEN 1 ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN occurred_unix >= $today AND occurred_unix < $tomorrow THEN input_tokens ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN occurred_unix >= $today AND occurred_unix < $tomorrow THEN output_tokens ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN occurred_unix >= $today AND occurred_unix < $tomorrow THEN total_tokens ELSE 0 END), 0)
        FROM ai_requests
        WHERE occurred_unix >= $month;
        """;

    private readonly string _connectionString;
    private readonly ILogger<SqliteDatabaseService> _logger;
    private readonly IPromptInjectionProtectionService _promptProtection;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Creates the serialized SQLite gateway for local settings, usage, sessions, and audit data.</summary>
    public SqliteDatabaseService(
        AppPaths paths,
        ILogger<SqliteDatabaseService> logger,
        IPromptInjectionProtectionService promptProtection,
        ISensitiveDataRedactor redactor)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _promptProtection = promptProtection ?? throw new ArgumentNullException(nameof(promptProtection));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
    }

    /// <summary>Creates the versioned schema and singleton default settings row.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            var currentVersion = await ReadSchemaVersionAsync(connection, cancellationToken).ConfigureAwait(false);
            if (currentVersion is < 0 or > SqliteSchema.Version)
            {
                throw new InvalidOperationException($"Unsupported PromptMeUp database schema version '{currentVersion}'.");
            }

            await EnableWriteAheadLoggingAsync(connection, cancellationToken).ConfigureAwait(false);

            await EnsureCurrentSchemaAsync(
                connection,
                setVersion: currentVersion == 0,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("SQLite database initialized. SchemaVersion={SchemaVersion}", SqliteSchema.Version);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Loads the validated singleton application settings record.</summary>
    public async Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT setup_completed, language, ai_enabled, model, reasoning_effort, output_detail,
                   custom_instruction, include_windows_location, review_commands_with_ai,
                   prompt_caching_enabled, max_conversation_turns, max_message_characters,
                   max_context_percent, max_command_output_characters, command_timeout_seconds,
                   endpoint, api_key_variable, admin_key_variable, updated_unix
            FROM app_settings
            WHERE id = 1;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("PromptMeUp settings row is missing.");
        }

        var settings = new AppSettings(
            reader.GetInt32(0) == 1,
            reader.GetString(1),
            reader.GetInt32(2) == 1,
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetInt32(7) == 1,
            reader.GetInt32(8) == 1,
            reader.GetInt32(9) == 1,
            reader.GetInt32(10),
            reader.GetInt32(11),
            reader.GetInt32(12),
            reader.GetInt32(13),
            reader.GetInt32(14),
            reader.GetString(15),
            reader.GetString(16),
            reader.GetString(17),
            DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(18)));
        var normalizedPreamble = _promptProtection.Protect(settings.CustomInstruction).SanitizedText;
        var safePreamble = _redactor.Redact(normalizedPreamble);
        if (!string.Equals(normalizedPreamble, safePreamble, StringComparison.Ordinal))
        {
            settings = settings with { CustomInstruction = safePreamble, UpdatedAt = DateTimeOffset.UtcNow };
            await reader.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
            await SaveSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Recognizable credentials were removed from a legacy configured preamble.");
        }
        ValidateSettings(settings);
        return settings;
    }

    /// <summary>Validates and atomically updates the singleton application settings record.</summary>
    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateSettings(settings);
        var protectedPreamble = _promptProtection.Protect(settings.CustomInstruction);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE app_settings
                SET setup_completed = $setupCompleted,
                    language = $language,
                    ai_enabled = $aiEnabled,
                    model = $model,
                    reasoning_effort = $reasoning,
                    output_detail = $detail,
                    custom_instruction = $customInstruction,
                    include_windows_location = $includeLocation,
                    review_commands_with_ai = $reviewCommandsWithAi,
                    prompt_caching_enabled = $promptCachingEnabled,
                    max_conversation_turns = $maxConversationTurns,
                    max_message_characters = $maxMessageCharacters,
                    max_context_percent = $maxContextPercent,
                    max_command_output_characters = $maxCommandOutputCharacters,
                    command_timeout_seconds = $commandTimeoutSeconds,
                    endpoint = $endpoint,
                    api_key_variable = $apiKeyVariable,
                    admin_key_variable = $adminKeyVariable,
                    updated_unix = $updated
                WHERE id = 1;
                """;
            command.Parameters.AddWithValue("$setupCompleted", settings.SetupCompleted ? 1 : 0);
            command.Parameters.AddWithValue("$language", settings.Language);
            command.Parameters.AddWithValue("$aiEnabled", settings.AiEnabled ? 1 : 0);
            command.Parameters.AddWithValue("$model", settings.Model);
            command.Parameters.AddWithValue("$reasoning", settings.ReasoningEffort);
            command.Parameters.AddWithValue("$detail", settings.OutputDetail);
            command.Parameters.AddWithValue("$customInstruction", protectedPreamble.SanitizedText);
            command.Parameters.AddWithValue("$includeLocation", settings.IncludeWindowsLocation ? 1 : 0);
            command.Parameters.AddWithValue("$reviewCommandsWithAi", settings.ReviewCommandsWithAi ? 1 : 0);
            command.Parameters.AddWithValue("$promptCachingEnabled", settings.PromptCachingEnabled ? 1 : 0);
            command.Parameters.AddWithValue("$maxConversationTurns", settings.MaxConversationTurns);
            command.Parameters.AddWithValue("$maxMessageCharacters", settings.MaxMessageCharacters);
            command.Parameters.AddWithValue("$maxContextPercent", settings.MaxContextPercent);
            command.Parameters.AddWithValue("$maxCommandOutputCharacters", settings.MaxCommandOutputCharacters);
            command.Parameters.AddWithValue("$commandTimeoutSeconds", settings.CommandTimeoutSeconds);
            command.Parameters.AddWithValue("$endpoint", settings.Endpoint);
            command.Parameters.AddWithValue("$apiKeyVariable", settings.ApiKeyVariable);
            command.Parameters.AddWithValue("$adminKeyVariable", settings.AdminKeyVariable);
            command.Parameters.AddWithValue("$updated", settings.UpdatedAt.ToUnixTimeSeconds());
            if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new InvalidOperationException("PromptMeUp settings update did not affect the expected row.");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Appends normalized AI request usage, response metadata, and redacted content.</summary>
    public async Task AppendAiRequestAsync(AiRequestLog request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ai_requests (
                    id, conversation_id, prompt_id, occurred_unix, completed_unix, endpoint_host,
                    requested_model, returned_model, user_prompt, assistant_response, input_tokens,
                    cached_input_tokens, cache_write_tokens, output_tokens, reasoning_tokens, total_tokens,
                    estimated_cost_microusd, http_status, elapsed_ms, provider_response_id,
                    provider_request_id, success, failure_code)
                VALUES (
                    $id, $conversationId, $promptId, $occurred, $completed, $endpointHost,
                    $requestedModel, $returnedModel, $userPrompt, $assistantResponse, $inputTokens,
                    $cachedInputTokens, $cacheWriteTokens, $outputTokens, $reasoningTokens, $totalTokens,
                    $estimatedCost, $httpStatus, $elapsed, $providerResponseId,
                    $providerRequestId, $success, $failureCode);
                """;
            command.Parameters.AddWithValue("$id", request.Id);
            command.Parameters.AddWithValue("$conversationId", request.ConversationId);
            command.Parameters.AddWithValue("$promptId", request.PromptId);
            command.Parameters.AddWithValue("$occurred", request.OccurredAt.ToUnixTimeSeconds());
            AddNullable(command, "$completed", request.CompletedAt?.ToUnixTimeSeconds());
            command.Parameters.AddWithValue("$endpointHost", request.EndpointHost);
            command.Parameters.AddWithValue("$requestedModel", request.RequestedModel);
            AddNullable(command, "$returnedModel", request.ReturnedModel);
            command.Parameters.AddWithValue("$userPrompt", request.UserPrompt);
            AddNullable(command, "$assistantResponse", request.AssistantResponse);
            command.Parameters.AddWithValue("$inputTokens", request.Usage.InputTokens);
            command.Parameters.AddWithValue("$cachedInputTokens", request.Usage.CachedInputTokens);
            command.Parameters.AddWithValue("$cacheWriteTokens", request.Usage.CacheWriteTokens);
            command.Parameters.AddWithValue("$outputTokens", request.Usage.OutputTokens);
            command.Parameters.AddWithValue("$reasoningTokens", request.Usage.ReasoningTokens);
            command.Parameters.AddWithValue("$totalTokens", request.Usage.TotalTokens);
            AddNullable(command, "$estimatedCost", ToMicroUsd(request.EstimatedCostUsd));
            AddNullable(command, "$httpStatus", request.HttpStatusCode);
            AddNullable(command, "$elapsed", request.ElapsedMilliseconds);
            AddNullable(command, "$providerResponseId", request.ProviderResponseId);
            AddNullable(command, "$providerRequestId", request.ProviderRequestId);
            command.Parameters.AddWithValue("$success", request.Succeeded ? 1 : 0);
            AddNullable(command, "$failureCode", request.FailureCode);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Replaces one provider price snapshot in a single transaction.</summary>
    public async Task ReplaceModelPricesAsync(string provider, IReadOnlyList<AiModelPrice> prices, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider) || prices.Count == 0)
        {
            throw new ArgumentException("A provider and at least one price row are required.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = connection.BeginTransaction();
            await using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM ai_model_pricing WHERE provider = $provider;";
                delete.Parameters.AddWithValue("$provider", provider.ToLowerInvariant());
                await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var price in prices)
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO ai_model_pricing (
                        provider, model, service_tier, context_window, currency,
                        input_microusd_per_million, cached_input_microusd_per_million,
                        cache_write_microusd_per_million, output_microusd_per_million,
                        source_url, retrieved_unix)
                    VALUES (
                        $provider, $model, $serviceTier, $contextWindow, $currency,
                        $input, $cachedInput, $cacheWrite, $output, $sourceUrl, $retrieved);
                    """;
                insert.Parameters.AddWithValue("$provider", price.Provider);
                insert.Parameters.AddWithValue("$model", price.Model);
                insert.Parameters.AddWithValue("$serviceTier", price.ServiceTier);
                insert.Parameters.AddWithValue("$contextWindow", price.ContextWindow);
                insert.Parameters.AddWithValue("$currency", price.Currency);
                insert.Parameters.AddWithValue("$input", ToMicroUsd(price.InputUsdPerMillionTokens)!.Value);
                AddNullable(insert, "$cachedInput", ToMicroUsd(price.CachedInputUsdPerMillionTokens));
                AddNullable(insert, "$cacheWrite", ToMicroUsd(price.CacheWriteUsdPerMillionTokens));
                insert.Parameters.AddWithValue("$output", ToMicroUsd(price.OutputUsdPerMillionTokens)!.Value);
                insert.Parameters.AddWithValue("$sourceUrl", price.SourceUrl);
                insert.Parameters.AddWithValue("$retrieved", price.RetrievedAt.ToUnixTimeSeconds());
                await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Lists the cached standard short-context prices for one provider.</summary>
    public async Task<IReadOnlyList<AiModelPrice>> ListModelPricesAsync(string provider, CancellationToken cancellationToken)
    {
        var prices = new List<AiModelPrice>();
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT provider, model, service_tier, context_window, currency,
                   input_microusd_per_million, cached_input_microusd_per_million,
                   cache_write_microusd_per_million, output_microusd_per_million,
                   source_url, retrieved_unix
            FROM ai_model_pricing
            WHERE provider = $provider
              AND service_tier = 'standard'
              AND context_window = 'short'
            ORDER BY model COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$provider", provider.ToLowerInvariant());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            prices.Add(ReadPrice(reader));
        }

        return prices;
    }

    /// <summary>Finds the applicable cached standard rate, leaving unknown models or missing bands unpriced.</summary>
    public async Task<AiModelPrice?> FindModelPriceAsync(string provider, string model, long inputTokens, CancellationToken cancellationToken)
    {
        var selection = ModelPricingPolicy.Resolve(model, inputTokens);
        if (selection is null)
        {
            return null;
        }

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT provider, model, service_tier, context_window, currency,
                   input_microusd_per_million, cached_input_microusd_per_million,
                   cache_write_microusd_per_million, output_microusd_per_million,
                   source_url, retrieved_unix
            FROM ai_model_pricing
            WHERE provider = $provider
              AND model = $model COLLATE NOCASE
              AND service_tier = 'standard'
              AND context_window = $contextWindow
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$provider", provider.ToLowerInvariant());
        command.Parameters.AddWithValue("$model", selection.Value.Model);
        command.Parameters.AddWithValue("$contextWindow", selection.Value.ContextWindow);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadPrice(reader) : null;
    }

    /// <summary>Returns the newest cached price retrieval timestamp for daily-refresh planning.</summary>
    public async Task<DateTimeOffset?> GetLatestModelPriceSyncAsync(string provider, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX(retrieved_unix) FROM ai_model_pricing WHERE provider = $provider;";
        command.Parameters.AddWithValue("$provider", provider.ToLowerInvariant());
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is null or DBNull ? null : DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value));
    }

    /// <summary>Replaces organization-cost buckets in the requested interval and advances sync state.</summary>
    public async Task ReplaceOrganizationCostsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyList<OrganizationCost> costs,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = connection.BeginTransaction();
            await using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM organization_costs WHERE bucket_start_unix >= $from AND bucket_start_unix < $to;";
                delete.Parameters.AddWithValue("$from", from.ToUnixTimeSeconds());
                delete.Parameters.AddWithValue("$to", to.ToUnixTimeSeconds());
                await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var cost in costs)
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO organization_costs (
                        id, bucket_start_unix, bucket_end_unix, amount_microusd, currency,
                        line_item, project_id, retrieved_unix)
                    VALUES ($id, $start, $end, $amount, $currency, $lineItem, $projectId, $retrieved);
                    """;
                insert.Parameters.AddWithValue("$id", cost.Id);
                insert.Parameters.AddWithValue("$start", cost.BucketStart.ToUnixTimeSeconds());
                insert.Parameters.AddWithValue("$end", cost.BucketEnd.ToUnixTimeSeconds());
                insert.Parameters.AddWithValue("$amount", ToMicroUsd(cost.Amount)!.Value);
                insert.Parameters.AddWithValue("$currency", cost.Currency);
                AddNullable(insert, "$lineItem", cost.LineItem);
                AddNullable(insert, "$projectId", cost.ProjectId);
                insert.Parameters.AddWithValue("$retrieved", cost.RetrievedAt.ToUnixTimeSeconds());
                await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await SetSyncStateAsync(connection, transaction, "organization_costs", DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Returns the last successful organization-cost synchronization timestamp.</summary>
    public async Task<DateTimeOffset?> GetLastOrganizationCostSyncAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM sync_state WHERE name = 'organization_costs';";
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is string text && long.TryParse(text, out var unix)
            ? DateTimeOffset.FromUnixTimeSeconds(unix)
            : null;
    }

    /// <summary>Aggregates successful local AI usage for today and the current local month.</summary>
    public async Task<AiRequestSummary> GetAiRequestSummaryAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var todayUtc = StartOfLocalDayUtc(today);
        var tomorrowUtc = StartOfLocalDayUtc(today.AddDays(1));
        var monthUtc = StartOfLocalDayUtc(monthStart);

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = AiRequestSummarySql;
        command.Parameters.AddWithValue("$today", todayUtc.ToUnixTimeSeconds());
        command.Parameters.AddWithValue("$tomorrow", tomorrowUtc.ToUnixTimeSeconds());
        command.Parameters.AddWithValue("$month", monthUtc.ToUnixTimeSeconds());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return new AiRequestSummary(
            FromMicroUsd(reader.GetInt64(0)),
            FromMicroUsd(reader.GetInt64(1)),
            checked((int)reader.GetInt64(2)),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5));
    }

    /// <summary>Aggregates downloaded organization cost buckets for the current local month.</summary>
    public async Task<decimal?> GetOrganizationCostCurrentMonthAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var monthStart = StartOfLocalDayUtc(new DateOnly(today.Year, today.Month, 1));
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SUM(amount_microusd) FROM organization_costs WHERE bucket_start_unix >= $month AND currency = 'usd';";
        command.Parameters.AddWithValue("$month", monthStart.ToUnixTimeSeconds());
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is null or DBNull ? null : FromMicroUsd(Convert.ToInt64(value));
    }

    /// <summary>Creates an AI work session once and preserves its original start metadata on repeated calls.</summary>
    public async Task EnsureAiSessionAsync(AiSessionRecord session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateJson(session.MetadataJson, nameof(session.MetadataJson));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT OR IGNORE INTO ai_sessions (
                    id, started_unix, ended_unix, language, model, kind, status, metadata_json)
                VALUES ($id, $started, $ended, $language, $model, $kind, $status, $metadata);
                """;
            command.Parameters.AddWithValue("$id", session.Id);
            command.Parameters.AddWithValue("$started", session.StartedAt.ToUnixTimeMilliseconds());
            AddNullable(command, "$ended", session.EndedAt?.ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$language", session.Language);
            command.Parameters.AddWithValue("$model", session.Model);
            command.Parameters.AddWithValue("$kind", session.Kind);
            command.Parameters.AddWithValue("$status", session.Status);
            command.Parameters.AddWithValue("$metadata", session.MetadataJson);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Closes one existing AI session with an explicit terminal status.</summary>
    public async Task CloseAiSessionAsync(
        string sessionId,
        string status,
        DateTimeOffset endedAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE ai_sessions
                SET ended_unix = $ended, status = $status
                WHERE id = $id AND ended_unix IS NULL;
                """;
            command.Parameters.AddWithValue("$ended", endedAt.ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$status", status);
            command.Parameters.AddWithValue("$id", sessionId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Appends one ordered, JSON-backed prompt, response, command, or error event to a session.</summary>
    public async Task AppendAiSessionEventAsync(AiSessionEventRecord sessionEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionEvent);
        ValidateJson(sessionEvent.PayloadJson, nameof(sessionEvent.PayloadJson));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ai_session_events (
                    id, session_id, sequence, occurred_unix, event_type, payload_json)
                VALUES (
                    $id, $sessionId,
                    (SELECT COALESCE(MAX(sequence), 0) + 1 FROM ai_session_events WHERE session_id = $sessionId),
                    $occurred, $eventType, $payload);
                """;
            command.Parameters.AddWithValue("$id", sessionEvent.Id);
            command.Parameters.AddWithValue("$sessionId", sessionEvent.SessionId);
            command.Parameters.AddWithValue("$occurred", sessionEvent.OccurredAt.ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$eventType", sessionEvent.EventType);
            command.Parameters.AddWithValue("$payload", sessionEvent.PayloadJson);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Appends one flexible user-activity audit entry with a validated JSON payload.</summary>
    public async Task AppendActivityAuditAsync(ActivityAuditRecord audit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audit);
        ValidateJson(audit.PayloadJson, nameof(audit.PayloadJson));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO activity_audit (
                    id, occurred_unix, session_id, activity_type, outcome, payload_json)
                VALUES ($id, $occurred, $sessionId, $activityType, $outcome, $payload);
                """;
            command.Parameters.AddWithValue("$id", audit.Id);
            command.Parameters.AddWithValue("$occurred", audit.OccurredAt.ToUnixTimeMilliseconds());
            AddNullable(command, "$sessionId", audit.SessionId);
            command.Parameters.AddWithValue("$activityType", audit.ActivityType);
            command.Parameters.AddWithValue("$outcome", audit.Outcome);
            command.Parameters.AddWithValue("$payload", audit.PayloadJson);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Opens one pooled connection with foreign keys and a bounded busy timeout enabled.</summary>
    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <summary>Reads the schema marker before any initialization DDL can alter the database.</summary>
    private static async Task<int> ReadSchemaVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Reasserts write-ahead logging for every supported database version.</summary>
    private static async Task EnableWriteAheadLoggingAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqliteSchema.EnableWriteAheadLoggingSql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Atomically reapplies idempotent schema objects, seeds settings, and versions a new database.</summary>
    private static async Task EnsureCurrentSchemaAsync(
        SqliteConnection connection,
        bool setVersion,
        CancellationToken cancellationToken)
    {
        await using var transaction = connection.BeginTransaction();
        try
        {
            await CreateSchemaAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            await EnsureDefaultSettingsAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            if (setVersion)
            {
                await SetSchemaVersionAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Creates all tables and indexes for a previously unversioned database.</summary>
    private static async Task CreateSchemaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = SqliteSchema.CreateSql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Marks a successfully initialized database with the current schema version.</summary>
    private static async Task SetSchemaVersionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA user_version = {SqliteSchema.Version};";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Inserts the platform-language default settings exactly once.</summary>
    private static async Task EnsureDefaultSettingsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        var settings = AppSettings.Default;
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO app_settings (
                id, setup_completed, language, ai_enabled, model, reasoning_effort,
                output_detail, custom_instruction, include_windows_location, review_commands_with_ai,
                prompt_caching_enabled, max_conversation_turns, max_message_characters,
                max_context_percent, max_command_output_characters, command_timeout_seconds, endpoint,
                api_key_variable, admin_key_variable, updated_unix)
            VALUES (
                1, 0, $language, 1, $model, $reasoning, $detail, '', 0, 1,
                1, $maxConversationTurns, $maxMessageCharacters, $maxContextPercent,
                $maxCommandOutputCharacters, $commandTimeoutSeconds, $endpoint,
                $apiKeyVariable, $adminKeyVariable, $updated);
            """;
        command.Parameters.AddWithValue("$language", SupportedLanguages.ResolveSystemLanguage());
        command.Parameters.AddWithValue("$model", settings.Model);
        command.Parameters.AddWithValue("$reasoning", settings.ReasoningEffort);
        command.Parameters.AddWithValue("$detail", settings.OutputDetail);
        command.Parameters.AddWithValue("$maxConversationTurns", settings.MaxConversationTurns);
        command.Parameters.AddWithValue("$maxMessageCharacters", settings.MaxMessageCharacters);
        command.Parameters.AddWithValue("$maxContextPercent", settings.MaxContextPercent);
        command.Parameters.AddWithValue("$maxCommandOutputCharacters", settings.MaxCommandOutputCharacters);
        command.Parameters.AddWithValue("$commandTimeoutSeconds", settings.CommandTimeoutSeconds);
        command.Parameters.AddWithValue("$endpoint", settings.Endpoint);
        command.Parameters.AddWithValue("$apiKeyVariable", settings.ApiKeyVariable);
        command.Parameters.AddWithValue("$adminKeyVariable", settings.AdminKeyVariable);
        command.Parameters.AddWithValue("$updated", settings.UpdatedAt.ToUnixTimeSeconds());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Upserts one named synchronization timestamp inside the caller transaction.</summary>
    private static async Task SetSyncStateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string name,
        DateTimeOffset value,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO sync_state (name, value)
            VALUES ($name, $value)
            ON CONFLICT(name) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$value", value.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Maps one normalized database row to a model-price record.</summary>
    private static AiModelPrice ReadPrice(SqliteDataReader reader) => new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        FromMicroUsd(reader.GetInt64(5)),
        reader.IsDBNull(6) ? null : FromMicroUsd(reader.GetInt64(6)),
        reader.IsDBNull(7) ? null : FromMicroUsd(reader.GetInt64(7)),
        FromMicroUsd(reader.GetInt64(8)),
        reader.GetString(9),
        DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(10)));

    /// <summary>Rejects unsupported settings before persistence can create an unusable runtime state.</summary>
    private void ValidateSettings(AppSettings settings)
    {
        var protectedPreamble = _promptProtection.Protect(settings.CustomInstruction);
        if (!string.Equals(protectedPreamble.SanitizedText, _redactor.Redact(protectedPreamble.SanitizedText), StringComparison.Ordinal))
        {
            throw new ArgumentException("The configured AI preamble must not contain credentials.", nameof(settings));
        }
        if (!SupportedLanguages.IsSupported(settings.Language)
            || string.IsNullOrWhiteSpace(settings.Model)
            || !AiModelCatalog.Models.Any(model => model.Id == settings.Model)
            || !AiModelCatalog.Resolve(settings.Model).ReasoningEfforts.Contains(settings.ReasoningEffort)
            || settings.OutputDetail is not ("compact" or "balanced" or "detailed")
            || !OpenAiEndpointPolicy.IsAllowed(settings.Endpoint)
            || string.IsNullOrWhiteSpace(settings.ApiKeyVariable)
            || string.IsNullOrWhiteSpace(settings.AdminKeyVariable)
            || !protectedPreamble.IsSafe
            || !protectedPreamble.IsWithinWordLimit
            || settings.MaxConversationTurns is < 2 or > 50
            || settings.MaxMessageCharacters is < 500 or > 100_000
            || settings.MaxContextPercent is < 10 or > 95
            || settings.MaxCommandOutputCharacters is < 1_000 or > 32_768
            || settings.CommandTimeoutSeconds is < 5 or > 300)
        {
            throw new ArgumentException("Application settings contain unsupported values.", nameof(settings));
        }
    }

    /// <summary>Rejects malformed JSON before it reaches JSON-constrained audit tables.</summary>
    private static void ValidateJson(string json, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("A non-empty JSON payload is required.", parameterName);
        }

        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new ArgumentException("The payload is not valid JSON.", parameterName, exception);
        }
    }

    /// <summary>Converts a local calendar boundary to the UTC instant stored in SQLite.</summary>
    private static DateTimeOffset StartOfLocalDayUtc(DateOnly date)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, TimeZoneInfo.Local));
    }

    /// <summary>Converts a nullable USD amount to exact integer microdollars.</summary>
    private static long? ToMicroUsd(decimal? value) => value.HasValue
        ? checked((long)decimal.Round(value.Value * 1_000_000m, 0, MidpointRounding.AwayFromZero))
        : null;

    /// <summary>Converts exact integer microdollars back to a decimal USD amount.</summary>
    private static decimal FromMicroUsd(long value) => value / 1_000_000m;

    /// <summary>Adds either a scalar value or database null to a SQLite command.</summary>
    private static void AddNullable(SqliteCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
}

// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IActivityAuditService
{
    Task StartSessionAsync(
        string sessionId,
        string kind,
        AppSettings settings,
        object? metadata,
        CancellationToken cancellationToken);

    Task CloseSessionAsync(
        string sessionId,
        string status,
        CancellationToken cancellationToken);

    Task AppendSessionEventAsync(
        string sessionId,
        string eventType,
        object payload,
        CancellationToken cancellationToken);

    Task RecordAsync(
        string activityType,
        string outcome,
        string? sessionId,
        object payload,
        CancellationToken cancellationToken);
}

public sealed class ActivityAuditService : IActivityAuditService
{
    private readonly IDatabaseService _database;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly ILogger<ActivityAuditService> _logger;

    /// <summary>Creates the JSON-backed activity and session ledger.</summary>
    public ActivityAuditService(IDatabaseService database, ISensitiveDataRedactor redactor, ILogger<ActivityAuditService>? logger = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _logger = logger ?? NullLogger<ActivityAuditService>.Instance;
    }

    /// <summary>Creates an idempotent AI work-session header.</summary>
    public Task StartSessionAsync(
        string sessionId,
        string kind,
        AppSettings settings,
        object? metadata,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentNullException.ThrowIfNull(settings);
        return _database.EnsureAiSessionAsync(
            new AiSessionRecord(
                sessionId,
                DateTimeOffset.UtcNow,
                null,
                settings.Language,
                settings.Model,
                kind,
                "active",
                SerializeSafe(metadata ?? new { })),
            cancellationToken);
    }

    /// <summary>Closes a session without allowing a secondary database failure to replace its primary outcome.</summary>
    public async Task CloseSessionAsync(string sessionId, string status, CancellationToken cancellationToken)
    {
        try
        {
            await _database.CloseAiSessionAsync(sessionId, status, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError("Session close failed. SessionId={SessionId}, Status={Status}, ErrorType={ErrorType}", sessionId, status, exception.GetType().Name);
        }
    }

    /// <summary>Appends a prompt, response, command, pruning, or error event to the ordered session ledger.</summary>
    public Task AppendSessionEventAsync(
        string sessionId,
        string eventType,
        object payload,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(payload);
        return _database.AppendAiSessionEventAsync(
            new AiSessionEventRecord(
                Guid.NewGuid().ToString("N"),
                sessionId,
                DateTimeOffset.UtcNow,
                eventType,
                SerializeSafe(payload)),
            cancellationToken);
    }

    /// <summary>Appends one flexible user-activity audit event with sensitive property names redacted.</summary>
    public Task RecordAsync(
        string activityType,
        string outcome,
        string? sessionId,
        object payload,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        ArgumentNullException.ThrowIfNull(payload);
        return _database.AppendActivityAuditAsync(
            new ActivityAuditRecord(
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow,
                sessionId,
                activityType,
                outcome,
                SerializeSafe(payload)),
            cancellationToken);
    }

    /// <summary>Serializes arbitrary audit data and redacts values whose property names indicate credentials.</summary>
    private string SerializeSafe(object payload)
    {
        var node = JsonSerializer.SerializeToNode(payload)
            ?? throw new InvalidOperationException("Audit payload serialization returned no JSON node.");
        RedactSecrets(node);
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>Walks JSON recursively and replaces credential-like properties without altering ordinary token metrics.</summary>
    private void RedactSecrets(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToArray())
            {
                if (IsSecretProperty(property.Key))
                {
                    jsonObject[property.Key] = "[redacted]";
                }
                else if (property.Value is JsonValue value
                         && value.TryGetValue<string>(out var text))
                {
                    jsonObject[property.Key] = _redactor.Redact(text);
                }
                else if (property.Value is not null)
                {
                    RedactSecrets(property.Value);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            for (var index = 0; index < jsonArray.Count; index++)
            {
                if (jsonArray[index] is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    jsonArray[index] = _redactor.Redact(text);
                }
                else if (jsonArray[index] is { } item)
                {
                    RedactSecrets(item);
                }
            }
        }
    }

    /// <summary>Identifies credential fields while allowing usage properties such as token counts.</summary>
    private static bool IsSecretProperty(string name)
    {
        var normalized = name.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return normalized.Contains("apikey", StringComparison.Ordinal)
               || normalized.Contains("adminkey", StringComparison.Ordinal)
               || normalized.Contains("password", StringComparison.Ordinal)
               || normalized.Contains("authorization", StringComparison.Ordinal)
               || normalized.EndsWith("secret", StringComparison.Ordinal);
    }
}

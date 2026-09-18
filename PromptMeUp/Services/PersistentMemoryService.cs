// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Stores explicit global notes and selects a small local context without provider calls.</summary>
public sealed partial class PersistentMemoryService
{
    public const int MaximumCharacters = 1_000;
    public const int MaximumMemoriesPerScope = 100;
    public const int MaximumMemories = 5;
    public const int MaximumContentTokens = 650;
    private const string GlobalScope = "global";

    private readonly string _connectionString;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly ILocalizationService _text;
    private readonly ILogger<PersistentMemoryService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Connects explicit memories to the initialized local database and shared privacy controls.</summary>
    public PersistentMemoryService(
        AppPaths paths,
        ISensitiveDataRedactor redactor,
        ILocalizationService text,
        ILogger<PersistentMemoryService> logger)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            ForeignKeys = true,
            DefaultTimeout = 5
        }.ToString();
    }

    /// <summary>Persists a bounded, credential-free global note or refreshes a match; the legacy global parameter is ignored.</summary>
    public async Task<PersistentMemory> RememberAsync(string text, bool global, CancellationToken cancellationToken)
    {
        var note = ValidateInput(text);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = connection.BeginTransaction();
            await using var existing = connection.CreateCommand();
            existing.Transaction = transaction;
            existing.CommandText = "SELECT id FROM persistent_memories WHERE body = $body ORDER BY updated_unix DESC, id ASC LIMIT 1;";
            existing.Parameters.AddWithValue("$body", note);
            var existingId = await existing.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
            if (existingId is not null && !Guid.TryParseExact(existingId, "N", out _))
            {
                throw new InvalidDataException(_text.Text("Memory.Invalid"));
            }
            await using var count = connection.CreateCommand();
            count.Transaction = transaction;
            count.CommandText = "SELECT COUNT(*) FROM persistent_memories;";
            var storedCount = Convert.ToInt64(await count.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            if (existingId is null && storedCount >= MaximumMemoriesPerScope)
            {
                throw new MemoryValidationException(_text.Text("Memory.Limit", MaximumMemoriesPerScope));
            }
            var id = existingId ?? Guid.NewGuid().ToString("N");
            var updated = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await using var save = connection.CreateCommand();
            save.Transaction = transaction;
            save.CommandText = """
                INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
                VALUES ($id, $scope, $body, $updated)
                ON CONFLICT (id) DO UPDATE SET scope_key = excluded.scope_key, updated_unix = excluded.updated_unix;
                """;
            save.Parameters.AddWithValue("$id", id);
            save.Parameters.AddWithValue("$scope", GlobalScope);
            save.Parameters.AddWithValue("$body", note);
            save.Parameters.AddWithValue("$updated", updated.ToUnixTimeSeconds());
            await save.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Explicit global memory saved.");
            return new PersistentMemory(id, note, true, updated);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Updates one global note without replacing another identifier; the legacy global parameter is ignored.</summary>
    public async Task<PersistentMemory> UpdateAsync(string id, string text, bool global, CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(id, "N", out _))
        {
            throw new MemoryValidationException(_text.Text("Memory.InvalidId"));
        }
        var note = ValidateInput(text);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = connection.BeginTransaction();
            await using var existing = connection.CreateCommand();
            existing.Transaction = transaction;
            existing.CommandText = "SELECT body FROM persistent_memories WHERE id = $id;";
            existing.Parameters.AddWithValue("$id", id);
            var originalBody = await existing.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string
                ?? throw new MemoryValidationException(_text.Text("Memory.NotFound"));

            await using var duplicate = connection.CreateCommand();
            duplicate.Transaction = transaction;
            duplicate.CommandText = "SELECT COUNT(*) FROM persistent_memories WHERE body = $body AND id <> $id;";
            duplicate.Parameters.AddWithValue("$body", note);
            duplicate.Parameters.AddWithValue("$id", id);
            if (!string.Equals(originalBody, note, StringComparison.Ordinal)
                && Convert.ToInt64(await duplicate.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0)
            {
                throw new MemoryValidationException(_text.Text("Memory.Duplicate"));
            }

            var updated = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await using var save = connection.CreateCommand();
            save.Transaction = transaction;
            save.CommandText = """
                UPDATE persistent_memories SET scope_key = $scope, body = $body, updated_unix = $updated
                WHERE id = $id;
                """;
            save.Parameters.AddWithValue("$scope", GlobalScope);
            save.Parameters.AddWithValue("$body", note);
            save.Parameters.AddWithValue("$updated", updated.ToUnixTimeSeconds());
            save.Parameters.AddWithValue("$id", id);
            if (await save.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new MemoryValidationException(_text.Text("Memory.NotFound"));
            }
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Explicit global memory updated.");
            return new PersistentMemory(id, note, true, updated);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Lists every saved global note, including migrated collections above the creation limit, with current redaction.</summary>
    public async Task<IReadOnlyList<PersistentMemory>> ListAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = connection.BeginTransaction();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                SELECT id, body, updated_unix
                FROM persistent_memories
                ORDER BY updated_unix DESC, id ASC;
                """;
            var memories = new List<PersistentMemory>();
            var repairs = new List<PersistentMemory>();
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var id = reader.GetString(0);
                    var body = reader.GetString(1);
                    var updatedUnix = reader.GetInt64(2);
                    if (!Guid.TryParseExact(id, "N", out _) || string.IsNullOrWhiteSpace(body)
                        || body.Length > MaximumCharacters || updatedUnix < 0
                        || updatedUnix > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
                    {
                        throw new InvalidDataException(_text.Text("Memory.Invalid"));
                    }
                    var safeBody = _redactor.Redact(body);
                    if (string.IsNullOrWhiteSpace(safeBody) || safeBody.Length > MaximumCharacters)
                    {
                        throw new InvalidDataException(_text.Text("Memory.Invalid"));
                    }
                    var memory = new PersistentMemory(id, safeBody, true,
                        DateTimeOffset.FromUnixTimeSeconds(updatedUnix));
                    memories.Add(memory);
                    if (!string.Equals(body, safeBody, StringComparison.Ordinal))
                    {
                        repairs.Add(memory);
                    }
                }
            }
            foreach (var memory in repairs)
            {
                await using var repair = connection.CreateCommand();
                repair.Transaction = transaction;
                repair.CommandText = "UPDATE persistent_memories SET body = $body WHERE id = $id;";
                repair.Parameters.AddWithValue("$body", memory.Text);
                repair.Parameters.AddWithValue("$id", memory.Id);
                await repair.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            if (repairs.Count > 0)
            {
                _logger.LogWarning("Recognizable credentials removed from {MemoryCount} persisted memories.", repairs.Count);
            }
            return memories;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Deletes a global note by ID, optionally requiring its reviewed content and timestamp to remain unchanged.</summary>
    public async Task<bool> ForgetAsync(string id, CancellationToken cancellationToken, PersistentMemory? expected = null)
    {
        if (!Guid.TryParseExact(id, "N", out _) || (expected is not null && expected.Id != id))
        {
            throw new MemoryValidationException(_text.Text("Memory.InvalidId"));
        }
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = connection.BeginTransaction();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                DELETE FROM persistent_memories WHERE id = $id
                AND ($check = 0 OR (body = $body AND updated_unix = $updated));
                """;
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$check", expected is null ? 0 : 1);
            command.Parameters.AddWithValue("$body", expected?.Text ?? string.Empty);
            command.Parameters.AddWithValue("$updated", expected?.UpdatedAt.ToUnixTimeSeconds() ?? 0);
            var removed = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
            if (removed)
            {
                await ExperimentalStore.ClearLearningAsync(connection, transaction, GlobalScope, cancellationToken).ConfigureAwait(false);
                await ExperimentalStore.AdvanceRevisionAsync(connection, transaction, GlobalScope, cancellationToken).ConfigureAwait(false);
            }
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Explicit memory deletion completed. Removed={Removed}", removed);
            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Ranks all saved notes by lexical relevance and recency within a fixed estimated content budget.</summary>
    public async Task<MemorySelection> SelectAsync(string query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var terms = ExtractTerms(_redactor.Redact(query));
        var memories = await ListAsync(cancellationToken).ConfigureAwait(false);
        var candidates = memories.Select(memory =>
        {
            var safe = memory with { Text = _redactor.Redact(memory.Text) };
            return (Memory: safe, Overlap: ExtractTerms(safe.Text).Count(terms.Contains));
        })
            .OrderByDescending(candidate => candidate.Overlap)
            .ThenByDescending(candidate => candidate.Memory.UpdatedAt)
            .ThenBy(candidate => candidate.Memory.Id, StringComparer.Ordinal);
        var selected = new List<PersistentMemory>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long estimatedTokens = 0;
        foreach (var candidate in candidates)
        {
            var cost = ContextTokenEstimator.Text(candidate.Memory.Text) + 4;
            if (estimatedTokens + cost > MaximumContentTokens || !seen.Add(candidate.Memory.Text))
            {
                continue;
            }
            selected.Add(candidate.Memory);
            estimatedTokens += cost;
            if (selected.Count == MaximumMemories)
            {
                break;
            }
        }
        return new MemorySelection(selected, estimatedTokens);
    }

    /// <summary>Rejects empty, oversized, or recognizable credential-bearing notes before persistence.</summary>
    private string ValidateInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new MemoryValidationException(_text.Text("Memory.Empty"));
        }
        var note = value.Trim();
        if (note.Length > MaximumCharacters)
        {
            throw new MemoryValidationException(_text.Text("Memory.TooLong", MaximumCharacters));
        }
        if (!string.Equals(note, _redactor.Redact(note), StringComparison.Ordinal))
        {
            throw new MemoryValidationException(_text.Text("Memory.Secret"));
        }
        return note;
    }

    /// <summary>Opens a bounded-wait connection without creating a missing database or bypassing schema initialization.</summary>
    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Hashes the nearest Git root or working directory so project isolation never persists local paths.</summary>
    internal static string ResolveProjectScope()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        var root = current;
        for (DirectoryInfo? candidate = current; candidate is not null; candidate = candidate.Parent)
        {
            var marker = Path.Combine(candidate.FullName, ".git");
            if (Directory.Exists(marker) || File.Exists(marker))
            {
                root = candidate;
                break;
            }
        }
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root.FullName));
        if (OperatingSystem.IsWindows())
        {
            normalized = normalized.ToUpperInvariant();
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    /// <summary>Extracts distinct Unicode word terms for deterministic local relevance ranking.</summary>
    private static HashSet<string> ExtractTerms(string value) =>
        WordPattern().Matches(value).Select(match => match.Value.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);

    /// <summary>Matches words across all supported languages without interpreting user input as a pattern.</summary>
    [GeneratedRegex(@"[\p{L}\p{N}]{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}

/// <summary>Identifies an expected memory input error that the interactive conversation can recover from.</summary>
public sealed class MemoryValidationException : Exception
{
    /// <summary>Creates a localized validation error without retaining the rejected note or identifier.</summary>
    public MemoryValidationException(string message)
        : base(message)
    {
    }
}

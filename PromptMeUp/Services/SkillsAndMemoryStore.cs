// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using System.Text.Json;
using Microsoft.Data.Sqlite;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Stores global skills and memory preferences without enabling any feature by default.</summary>
public sealed partial class SkillsAndMemoryStore(AppPaths paths, ISensitiveDataRedactor redactor, ILocalizationService text)
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Loads explicit global preferences, preserving disabled defaults.</summary>
    public async Task<SkillsAndMemorySettings> SettingsAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        return await ReadSettingsAsync(connection, null, PersistentMemoryService.GlobalScope, ct).ConfigureAwait(false);
    }

    /// <summary>Persists preferences only if the reviewed snapshot is current, purging observations when capture is switched off.</summary>
    public async Task SaveSettingsAsync(SkillsAndMemorySettings settings, SkillsAndMemorySettings expected, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(expected);
        var scope = PersistentMemoryService.GlobalScope;
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        var previous = await ReadSettingsAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        if (previous != expected)
        {
            throw InvalidLearning();
        }
        await WritePreferenceAsync(connection, transaction, scope, "settings", JsonSerializer.Serialize(settings, Json), ct).ConfigureAwait(false);
        if (previous.Enabled != settings.Enabled || previous.CaptureObservations != settings.CaptureObservations)
        {
            await AdvanceRevisionAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        }
        if (!settings.Enabled || (previous.CaptureObservations && !settings.CaptureObservations))
        {
            await ClearLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reads a bounded global preference by a stable internal key.</summary>
    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        ValidatePreference(key, string.Empty);
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM skills_and_memory_settings WHERE scope_key = $scope AND name = $name;";
        command.Parameters.AddWithValue("$scope", PersistentMemoryService.GlobalScope);
        command.Parameters.AddWithValue("$name", key);
        return await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
    }

    /// <summary>Reads only the requested package approvals in one database round trip.</summary>
    /// <param name="skillNames">At most 128 package names from the current catalog.</param>
    /// <param name="cancellationToken">Cancels the approval lookup.</param>
    /// <exception cref="ArgumentNullException">The name list is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Too many package names were supplied.</exception>
    /// <exception cref="InvalidOperationException">A preference key is invalid.</exception>
    /// <exception cref="SqliteException">The database could not be read.</exception>
    /// <exception cref="OperationCanceledException">The lookup was cancelled.</exception>
    internal async Task<IReadOnlyDictionary<string, string>> GetSkillApprovalsAsync(
        IReadOnlyList<string> skillNames, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(skillNames);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(skillNames.Count, 128);
        cancellationToken.ThrowIfCancellationRequested();

        var approvals = new Dictionary<string, string>(StringComparer.Ordinal);
        if (skillNames.Count == 0)
        {
            return approvals;
        }

        var keys = skillNames.Select(name => "skill:" + name).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var key in keys)
        {
            ValidatePreference(key, string.Empty);
        }

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        var parameters = new string[keys.Length];
        for (var index = 0; index < keys.Length; index++)
        {
            parameters[index] = "$skill" + index;
            command.Parameters.AddWithValue(parameters[index], keys[index]);
        }
        command.CommandText = "SELECT name, value FROM skills_and_memory_settings "
            + "WHERE scope_key = $scope AND name IN (" + string.Join(", ", parameters) + ");";
        command.Parameters.AddWithValue("$scope", PersistentMemoryService.GlobalScope);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            approvals.Add(reader.GetString(0)["skill:".Length..], reader.GetString(1));
        }
        return approvals;
    }

    /// <summary>Saves a small preference; values cannot contain recognizable credentials.</summary>
    public async Task SetAsync(string key, string value, CancellationToken ct)
    {
        ValidatePreference(key, value);
        if (key == "settings")
        {
            throw InvalidLearning();
        }
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await WritePreferenceAsync(connection, null, PersistentMemoryService.GlobalScope, key, value, ct).ConfigureAwait(false);
    }

    /// <summary>Rejects malformed or credential-bearing preference names and values before persistence.</summary>
    private void ValidatePreference(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 120 || value is null || value.Length > 4096
            || key != redactor.Redact(key) || value != redactor.Redact(value))
        {
            throw InvalidLearning();
        }
    }

    /// <summary>Writes a validated preference inside the caller's optional transaction.</summary>
    private static async Task WritePreferenceAsync(SqliteConnection connection, SqliteTransaction? transaction,
        string scope, string key, string value, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO skills_and_memory_settings(scope_key, name, value) VALUES($scope, $name, $value)
            ON CONFLICT(scope_key, name) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$name", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reads opt-in state from the same snapshot as a dependent write.</summary>
    private async Task<SkillsAndMemorySettings> ReadSettingsAsync(SqliteConnection connection, SqliteTransaction? transaction,
        string scope, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT value FROM skills_and_memory_settings WHERE scope_key = $scope AND name = 'settings';";
        command.Parameters.AddWithValue("$scope", scope);
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
        return value is null ? new() : JsonSerializer.Deserialize<SkillsAndMemorySettings>(value, Json) ?? throw InvalidLearning();
    }

    /// <summary>Snapshots global purge epochs before capturing input or requesting reflection.</summary>
    public async Task<string> RevisionAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        return await ReadRevisionAsync(connection, null, PersistentMemoryService.GlobalScope, ct).ConfigureAwait(false);
    }

    /// <summary>Reads the global purge epoch in one SQLite snapshot before dependent work.</summary>
    private static async Task<string> ReadRevisionAsync(SqliteConnection connection, SqliteTransaction? transaction,
        string scope, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT $scope || ':' || COALESCE((SELECT revision FROM learning_revisions WHERE scope_key = $scope), '');
            """;
        command.Parameters.AddWithValue("$scope", scope);
        return (string)(await command.ExecuteScalarAsync(ct).ConfigureAwait(false))!;
    }

    /// <summary>Invalidates in-flight work inside the same transaction that revokes or forgets evidence.</summary>
    internal static async Task AdvanceRevisionAsync(SqliteConnection connection, SqliteTransaction transaction,
        string scope, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO learning_revisions(scope_key, revision) VALUES($scope, $revision)
            ON CONFLICT(scope_key) DO UPDATE SET revision = excluded.revision;
            """;
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$revision", Guid.NewGuid().ToString("N"));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Opens the already initialized database without creating files in the current repository.</summary>
    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWrite,
            DefaultTimeout = 5,
            ForeignKeys = true,
            Pooling = false
        }.ToString());
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Clears global captured evidence and pending proposals together.</summary>
    public async Task ClearObservationsAsync(CancellationToken ct)
    {
        var scope = PersistentMemoryService.GlobalScope;
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        await ClearLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        await AdvanceRevisionAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Removes retained evidence and all derived proposals as one privacy operation.</summary>
    internal static async Task ClearLearningAsync(SqliteConnection connection, SqliteTransaction transaction,
        string scope, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM learning_observations WHERE scope_key = $scope;
            DELETE FROM memory_proposals WHERE scope_key = $scope;
            """;
        command.Parameters.AddWithValue("$scope", scope);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}

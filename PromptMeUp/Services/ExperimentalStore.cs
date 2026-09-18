// SPDX-License-Identifier: MIT

using System.Text.Json;
using Microsoft.Data.Sqlite;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Stores project-scoped experiment preferences without enabling any feature by default.</summary>
public sealed partial class ExperimentalStore(AppPaths paths, ISensitiveDataRedactor redactor, ILocalizationService text)
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Loads explicit preferences for the current project, preserving disabled defaults.</summary>
    public async Task<ExperimentalSettings> SettingsAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        return await ReadSettingsAsync(connection, null, PersistentMemoryService.ResolveProjectScope(), ct).ConfigureAwait(false);
    }

    /// <summary>Persists reviewed preferences and removes retained observations when capture is switched off.</summary>
    public async Task SaveSettingsAsync(ExperimentalSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        var previous = await ReadSettingsAsync(connection, transaction, scope, ct).ConfigureAwait(false);
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

    /// <summary>Reads a bounded project preference by a stable internal key.</summary>
    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        ValidatePreference(key, string.Empty);
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM experimental_settings WHERE scope_key = $scope AND name = $name;";
        command.Parameters.AddWithValue("$scope", PersistentMemoryService.ResolveProjectScope());
        command.Parameters.AddWithValue("$name", key);
        return await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
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
        await WritePreferenceAsync(connection, null, PersistentMemoryService.ResolveProjectScope(), key, value, ct).ConfigureAwait(false);
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
            INSERT INTO experimental_settings(scope_key, name, value) VALUES($scope, $name, $value)
            ON CONFLICT(scope_key, name) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$name", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reads opt-in state from the same snapshot as a dependent write.</summary>
    private async Task<ExperimentalSettings> ReadSettingsAsync(SqliteConnection connection, SqliteTransaction? transaction,
        string scope, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT value FROM experimental_settings WHERE scope_key = $scope AND name = 'settings';";
        command.Parameters.AddWithValue("$scope", scope);
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
        return value is null ? new() : JsonSerializer.Deserialize<ExperimentalSettings>(value, Json) ?? throw InvalidLearning();
    }

    /// <summary>Snapshots project identity and purge epochs before capturing input or requesting reflection.</summary>
    public async Task<string> RevisionAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        return await ReadRevisionAsync(connection, null, PersistentMemoryService.ResolveProjectScope(), ct).ConfigureAwait(false);
    }

    /// <summary>Reads both epochs in one SQLite snapshot so global and project purges invalidate in-flight work.</summary>
    private static async Task<string> ReadRevisionAsync(SqliteConnection connection, SqliteTransaction? transaction,
        string scope, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT $scope || ':' || COALESCE((SELECT revision FROM learning_revisions WHERE scope_key = $scope), '')
                || ':' || COALESCE((SELECT revision FROM learning_revisions WHERE scope_key = 'global'), '');
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

    /// <summary>Clears the current project's captured evidence and pending proposals together.</summary>
    public async Task ClearObservationsAsync(CancellationToken ct)
    {
        var scope = PersistentMemoryService.ResolveProjectScope();
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
            DELETE FROM learning_observations WHERE scope_key = $scope OR $scope = 'global';
            DELETE FROM memory_proposals WHERE scope_key = $scope OR $scope = 'global';
            """;
        command.Parameters.AddWithValue("$scope", scope);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}

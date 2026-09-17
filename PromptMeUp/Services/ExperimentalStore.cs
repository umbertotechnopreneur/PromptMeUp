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
        var value = await GetAsync("settings", ct).ConfigureAwait(false);
        return value is null ? new() : JsonSerializer.Deserialize<ExperimentalSettings>(value, Json)
            ?? throw new InvalidOperationException(text.Text("Lab.Invalid"));
    }

    /// <summary>Persists reviewed preferences and removes retained observations when capture is switched off.</summary>
    public async Task SaveSettingsAsync(ExperimentalSettings settings, CancellationToken ct)
    {
        await SetAsync("settings", JsonSerializer.Serialize(settings, Json), ct).ConfigureAwait(false);
        if (!settings.Enabled || !settings.CaptureObservations)
        {
            await ClearObservationsAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>Reads a bounded project preference by a stable internal key.</summary>
    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
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
        if (key.Length > 120 || value.Length > 4096 || value != redactor.Redact(value))
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO experimental_settings(scope_key, name, value) VALUES($scope, $name, $value)
            ON CONFLICT(scope_key, name) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$scope", PersistentMemoryService.ResolveProjectScope());
        command.Parameters.AddWithValue("$name", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Opens the already initialized database without creating files in the current repository.</summary>
    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWrite,
            DefaultTimeout = 5
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
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM learning_observations WHERE scope_key = $scope;
            DELETE FROM memory_proposals WHERE scope_key = $scope;
            """;
        command.Parameters.AddWithValue("$scope", PersistentMemoryService.ResolveProjectScope());
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}

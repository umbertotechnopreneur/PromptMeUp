// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Stores explicitly confirmed reminders without background timers, provider calls, or operating-system notifications.</summary>
public sealed class ReminderService(AppPaths paths, ISensitiveDataRedactor redactor, ILocalizationService text,
    SkillCatalogService skills, TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <summary>Parses an explicit ISO offset or a local clock time, rejecting ambiguous or missing daylight-saving times.</summary>
    public DateTimeOffset ParseTime(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > 64)
        {
            throw InvalidTime();
        }
        input = input.Trim();
        var now = _clock.GetUtcNow();
        string[] offsetFormats = ["yyyy-MM-dd'T'HH:mmzzz", "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"];
        string[] utcFormats = ["yyyy-MM-dd'T'HH:mm'Z'", "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"];
        if (DateTimeOffset.TryParseExact(input, offsetFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var explicitTime)
            || DateTimeOffset.TryParseExact(input, utcFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out explicitTime))
        {
            return explicitTime > now ? explicitTime : throw InvalidTime();
        }
        string[] clockFormats = ["H:mm", "HH:mm", "htt", "h:mmtt", "h tt", "h:mm tt"];
        if (!TimeOnly.TryParseExact(input.ToUpperInvariant(), clockFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out var localTime))
        {
            throw InvalidTime();
        }
        var zone = _clock.LocalTimeZone;
        var localNow = TimeZoneInfo.ConvertTime(now, zone);
        var candidate = DateTime.SpecifyKind(localNow.Date.Add(localTime.ToTimeSpan()), DateTimeKind.Unspecified);
        if (candidate <= localNow.DateTime)
        {
            candidate = candidate.AddDays(1);
        }
        if (zone.IsInvalidTime(candidate) || zone.IsAmbiguousTime(candidate))
        {
            throw InvalidTime();
        }
        var due = new DateTimeOffset(candidate, zone.GetUtcOffset(candidate));
        return due > now ? due : throw InvalidTime();
    }

    /// <summary>Reports whether the current project opted in to this exact, currently available bundled package.</summary>
    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        return await IsActivatedAsync(connection, transaction, PersistentMemoryService.ResolveProjectScope(), ct).ConfigureAwait(false);
    }

    /// <summary>Returns this project's pending reminders only while their explicitly approved feature remains active.</summary>
    public async Task<IReadOnlyList<Reminder>> ListAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        var scope = PersistentMemoryService.ResolveProjectScope();
        return await IsActivatedAsync(connection, transaction, scope, ct).ConfigureAwait(false)
            ? await ReadAsync(connection, transaction, scope, null, ct).ConfigureAwait(false) : [];
    }

    /// <summary>Persists one reviewed, credential-free note after checking opt-in and the fifty-reminder limit atomically.</summary>
    public async Task<Reminder> CreateAsync(DateTimeOffset dueAt, string message, CancellationToken ct)
    {
        if (message is null)
        {
            throw Invalid();
        }
        message = message.Trim();
        if (message.Length is < 1 or > 500 || message.Any(char.IsControl) || message != redactor.Redact(message))
        {
            throw Invalid();
        }
        dueAt = DateTimeOffset.FromUnixTimeMilliseconds(dueAt.ToUnixTimeMilliseconds()).ToOffset(dueAt.Offset);
        if (dueAt <= _clock.GetUtcNow())
        {
            throw InvalidTime();
        }
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        await RequireActivatedAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM skill_reminders WHERE scope_key = $scope;";
        command.Parameters.AddWithValue("$scope", scope);
        if ((long)(await command.ExecuteScalarAsync(ct).ConfigureAwait(false))! >= 50)
        {
            throw new InvalidOperationException(text.Text("Reminder.Limit"));
        }
        var reminder = new Reminder(Guid.NewGuid().ToString("N"), message, dueAt);
        command.CommandText = """
            INSERT INTO skill_reminders(id, scope_key, message, due_unix_ms, offset_minutes)
            VALUES($id, $scope, $message, $due, $offset);
            """;
        command.Parameters.AddWithValue("$id", reminder.Id);
        command.Parameters.AddWithValue("$message", reminder.Message);
        command.Parameters.AddWithValue("$due", reminder.DueAt.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("$offset", (int)reminder.DueAt.Offset.TotalMinutes);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return reminder;
    }

    /// <summary>Deletes only the exact reminder that was displayed and confirmed in the current project.</summary>
    public async Task<bool> CancelAsync(Reminder reminder, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        await RequireActivatedAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM skill_reminders WHERE id = $id AND scope_key = $scope
                AND message = $message AND due_unix_ms = $due AND offset_minutes = $offset;
            """;
        command.Parameters.AddWithValue("$id", reminder.Id);
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$message", reminder.Message);
        command.Parameters.AddWithValue("$due", reminder.DueAt.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("$offset", (int)reminder.DueAt.Offset.TotalMinutes);
        var deleted = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 1;
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return deleted;
    }

    /// <summary>Displays due reminders before removing them atomically; failed display retains all reminders for a later prompt.</summary>
    public async Task DeliverDueAsync(Action<Reminder> display, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(display);
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        if (!await IsActivatedAsync(connection, transaction, scope, ct).ConfigureAwait(false))
        {
            return;
        }
        var now = _clock.GetUtcNow().ToUnixTimeMilliseconds();
        var due = await ReadAsync(connection, transaction, scope, now, ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM skill_reminders WHERE id = $id AND scope_key = $scope;";
        command.Parameters.AddWithValue("$scope", scope);
        var id = command.Parameters.Add("$id", SqliteType.Text);
        foreach (var reminder in due)
        {
            ct.ThrowIfCancellationRequested();
            display(reminder);
            id.Value = reminder.Id;
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reads a bounded snapshot without exposing recognizable credentials from externally modified database content.</summary>
    private async Task<IReadOnlyList<Reminder>> ReadAsync(SqliteConnection connection, SqliteTransaction transaction,
        string scope, long? dueBefore, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT id, message, due_unix_ms, offset_minutes FROM skill_reminders
            WHERE scope_key = $scope AND ($now IS NULL OR due_unix_ms <= $now)
            ORDER BY due_unix_ms, id LIMIT 50;
            """;
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$now", dueBefore.HasValue ? dueBefore.Value : DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var result = new List<Reminder>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            result.Add(new Reminder(reader.GetString(0), redactor.Redact(reader.GetString(1)),
                DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(2)).ToOffset(TimeSpan.FromMinutes(reader.GetInt32(3)))));
        }
        return result;
    }

    /// <summary>Rejects stale consent or a local package that attempts to borrow the bundled reminder implementation.</summary>
    private async Task RequireActivatedAsync(SqliteConnection connection, SqliteTransaction transaction, string scope, CancellationToken ct)
    {
        if (!await IsActivatedAsync(connection, transaction, scope, ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException(text.Text("Lab.Activate"));
        }
    }

    /// <summary>Checks opt-in and the approved fingerprint in the same database transaction as each dependent operation.</summary>
    private async Task<bool> IsActivatedAsync(SqliteConnection connection, SqliteTransaction transaction, string scope, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT value FROM experimental_settings WHERE scope_key = $scope AND name = 'settings';";
        command.Parameters.AddWithValue("$scope", scope);
        var settingsJson = await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
        if (settingsJson is null || JsonSerializer.Deserialize<ExperimentalSettings>(settingsJson, ExperimentalStore.Json)?.Enabled != true)
        {
            return false;
        }
        command.CommandText = "SELECT value FROM experimental_settings WHERE scope_key = $scope AND name = 'skill:set_reminder';";
        var fingerprint = await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
        if (string.IsNullOrEmpty(fingerprint))
        {
            return false;
        }
        var package = skills.List().SingleOrDefault(skill => skill.Name == "set_reminder");
        return package is { Origin: "bundled", UnavailableReason: null } && package.Fingerprint == fingerprint;
    }

    /// <summary>Opens only the initialized application database with deterministic transaction locking and bounded wait time.</summary>
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

    /// <summary>Returns a localized validation error without including untrusted note contents.</summary>
    private InvalidOperationException Invalid() => new(text.Text("Reminder.InvalidNote"));

    /// <summary>Explains supported time formats and daylight-saving ambiguity without echoing input.</summary>
    private InvalidOperationException InvalidTime() => new(text.Text("Reminder.InvalidTime"));
}

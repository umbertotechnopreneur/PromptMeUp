// SPDX-License-Identifier: MIT

using System.Text.Json;
using Microsoft.Data.Sqlite;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public sealed partial class ExperimentalStore
{
    /// <summary>Captures only a completed directly typed user turn after checking current opt-in preferences.</summary>
    public async Task CaptureAsync(string sessionId, string userMessage, CancellationToken ct, string? expectedRevision = null)
    {
        if (!Guid.TryParseExact(sessionId, "N", out _))
        {
            throw InvalidLearning();
        }
        if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Length > 4000)
        {
            return;
        }
        var safe = redactor.Redact(userMessage).Trim();
        if (safe.Length is 0 or > 4000)
        {
            return;
        }
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        var settings = await ReadSettingsAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        if (!settings.Enabled || !settings.CaptureObservations
            || !await RevisionMatchesAsync(connection, transaction, scope, expectedRevision, ct).ConfigureAwait(false))
        {
            return;
        }
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO learning_observations(id, scope_key, session_id, body, created_unix)
            VALUES($id, $scope, $session, $body, $now);
            DELETE FROM learning_observations WHERE scope_key = $scope AND id NOT IN (
                SELECT id FROM learning_observations WHERE scope_key = $scope ORDER BY created_unix DESC, rowid DESC LIMIT 200);
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$session", sessionId);
        command.Parameters.AddWithValue("$body", safe);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        await PruneLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reads recent current-project evidence while expiring retained content older than thirty days.</summary>
    public async Task<IReadOnlyList<LearningObservation>> ObservationsAsync(CancellationToken ct)
    {
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        await PruneLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id, session_id, body, created_unix FROM learning_observations WHERE scope_key = $scope ORDER BY created_unix DESC, rowid DESC LIMIT 200;";
        command.Parameters.AddWithValue("$scope", scope);
        var result = new List<LearningObservation>();
        await using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var body = redactor.Redact(reader.GetString(2));
                if (!Guid.TryParseExact(reader.GetString(0), "N", out _) || !Guid.TryParseExact(reader.GetString(1), "N", out _)
                    || string.IsNullOrWhiteSpace(body) || body.Length > 4000 || reader.GetInt64(3) > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    throw InvalidLearning();
                }
                result.Add(new(reader.GetString(0), reader.GetString(1), body, DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3))));
            }
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return result;
    }

    /// <summary>Lists bounded pending proposals without exposing other projects' learning data.</summary>
    public async Task<IReadOnlyList<MemoryProposal>> ProposalsAsync(CancellationToken ct)
    {
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        await PruneLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT payload_json FROM memory_proposals WHERE scope_key = $scope AND status = 'pending' AND created_unix >= $expiry ORDER BY created_unix DESC, id LIMIT 100;";
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$expiry", DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds());
        var result = new List<MemoryProposal>();
        await using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var proposal = JsonSerializer.Deserialize<MemoryProposal>(reader.GetString(0), Json) ?? throw InvalidLearning();
                ValidateProposal(proposal);
                result.Add(proposal);
            }
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return result;
    }

    /// <summary>Atomically saves validated proposals, retaining rejection history to suppress duplicate suggestions.</summary>
    public async Task<int> SaveProposalsAsync(IReadOnlyList<MemoryProposal> proposals, CancellationToken ct, string? expectedRevision = null)
    {
        if (proposals is null || proposals.Count > 8)
        {
            throw InvalidLearning();
        }
        foreach (var proposal in proposals)
        {
            ValidateProposal(proposal);
        }
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        var settings = await ReadSettingsAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        if (!settings.Enabled || !await RevisionMatchesAsync(connection, transaction, scope, expectedRevision, ct).ConfigureAwait(false)
            || (proposals.Any(proposal => proposal.SourceIds.Count > 0) && !settings.CaptureObservations))
        {
            throw InvalidLearning();
        }
        await PruneLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        var existing = new List<MemoryProposal>();
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "SELECT payload_json FROM memory_proposals WHERE scope_key = $scope AND status <> 'expired' LIMIT 301;";
            read.Parameters.AddWithValue("$scope", scope);
            await using var reader = await read.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                existing.Add(JsonSerializer.Deserialize<MemoryProposal>(reader.GetString(0), Json) ?? throw InvalidLearning());
            }
        }
        if (existing.Count > 300)
        {
            throw InvalidLearning();
        }
        var added = 0;
        foreach (var proposal in proposals)
        {
            await ValidateEvidenceAsync(connection, transaction, scope, proposal, ct).ConfigureAwait(false);
            if (existing.Any(previous => EquivalentProposal(previous, proposal)))
            {
                continue;
            }
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO memory_proposals(id, scope_key, payload_json, status, created_unix)
                SELECT $id, $scope, $payload, 'pending', $now
                WHERE (SELECT COUNT(*) FROM memory_proposals WHERE scope_key = $scope AND status = 'pending') < 100;
                """;
            command.Parameters.AddWithValue("$id", proposal.Id);
            command.Parameters.AddWithValue("$scope", scope);
            command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(proposal, Json));
            command.Parameters.AddWithValue("$now", proposal.CreatedAt.ToUnixTimeSeconds());
            var inserted = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            if (inserted != 1)
            {
                throw InvalidLearning();
            }
            added += inserted;
            existing.Add(proposal);
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return added;
    }

    /// <summary>Rejects only the exact still-pending reviewed payload.</summary>
    public async Task RejectAsync(MemoryProposal proposal, CancellationToken ct)
    {
        ValidateProposal(proposal);
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE memory_proposals SET status = 'rejected' WHERE id = $id AND scope_key = $scope AND status = 'pending' AND payload_json = $payload AND created_unix >= $expiry;";
        command.Parameters.AddWithValue("$id", proposal.Id);
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(proposal, Json));
        command.Parameters.AddWithValue("$expiry", DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds());
        if (await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1)
        {
            throw InvalidLearning();
        }
        await PruneLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Applies a reviewed proposal to global memory; the legacy scope argument no longer changes its destination.</summary>
    public async Task ApproveAsync(MemoryProposal proposal, string reviewedText, bool global, CancellationToken ct)
    {
        ValidateProposal(proposal);
        if (proposal.Operation == "flag" || string.IsNullOrWhiteSpace(reviewedText) || reviewedText.Length > PersistentMemoryService.MaximumCharacters
            || reviewedText != redactor.Redact(reviewedText))
        {
            throw InvalidLearning();
        }
        var projectScope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        var currentSettings = await ReadSettingsAsync(connection, transaction, projectScope, ct).ConfigureAwait(false);
        if (!currentSettings.Enabled || (proposal.SourceIds.Count > 0 && !currentSettings.CaptureObservations))
        {
            throw InvalidLearning();
        }
        await ValidateEvidenceAsync(connection, transaction, projectScope, proposal, ct).ConfigureAwait(false);
        await using var claim = connection.CreateCommand();
        claim.Transaction = transaction;
        claim.CommandText = "UPDATE memory_proposals SET status = 'approved' WHERE id = $id AND scope_key = $scope AND status = 'pending' AND payload_json = $payload AND created_unix >= $expiry;";
        claim.Parameters.AddWithValue("$id", proposal.Id);
        claim.Parameters.AddWithValue("$scope", projectScope);
        claim.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(proposal, Json));
        claim.Parameters.AddWithValue("$expiry", DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds());
        if (await claim.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1)
        {
            throw InvalidLearning();
        }
        const string scope = "global";
        if (proposal.Operation is "merge" or "archive")
        {
            foreach (var target in proposal.Targets)
            {
                await using var remove = connection.CreateCommand();
                remove.Transaction = transaction;
                remove.CommandText = "DELETE FROM persistent_memories WHERE id = $id AND scope_key = $scope;";
                remove.Parameters.AddWithValue("$id", target.Id);
                remove.Parameters.AddWithValue("$scope", scope);
                if (await remove.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1)
                {
                    throw InvalidLearning();
                }
            }
        }
        if (proposal.Operation is "add" or "merge")
        {
            // A bounded cleanup can leave other identical legacy copies for a later explicit review.
            var mergingIdenticalCopies = proposal.Operation == "merge"
                && proposal.Targets.All(target => string.Equals(target.Text, reviewedText.Trim(), StringComparison.Ordinal));
            await using var check = connection.CreateCommand();
            check.Transaction = transaction;
            check.CommandText = "SELECT COUNT(*), COALESCE(SUM(body = $body), 0) FROM persistent_memories WHERE scope_key = $scope;";
            check.Parameters.AddWithValue("$scope", scope);
            check.Parameters.AddWithValue("$body", reviewedText.Trim());
            await using (var reader = await check.ExecuteReaderAsync(ct).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(ct).ConfigureAwait(false)
                    || (proposal.Operation == "add" && reader.GetInt64(0) >= PersistentMemoryService.MaximumMemoriesPerScope)
                    || (reader.GetInt64(1) > 0 && !mergingIdenticalCopies))
                {
                    throw InvalidLearning();
                }
            }
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO persistent_memories(id, scope_key, body, updated_unix) VALUES($id, $scope, $body, $now);
                INSERT INTO memory_provenance(memory_id, kind, sources_json, proposal_id) VALUES($id, $kind, $sources, $proposal);
                """;
            insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
            insert.Parameters.AddWithValue("$scope", scope);
            insert.Parameters.AddWithValue("$body", reviewedText.Trim());
            insert.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            insert.Parameters.AddWithValue("$kind", proposal.Kind);
            insert.Parameters.AddWithValue("$sources", JsonSerializer.Serialize(proposal.SourceIds, Json));
            insert.Parameters.AddWithValue("$proposal", proposal.Id);
            await insert.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        // Removing approved memory also revokes retained learning that could recreate its old contents.
        if (proposal.Operation is "merge" or "archive")
        {
            await ClearLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
            await AdvanceRevisionAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        }
        else
        {
            await PruneLearningAsync(connection, transaction, projectScope, ct).ConfigureAwait(false);
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Verifies all source IDs, independent sessions, scopes, and exact target snapshots before saving or applying.</summary>
    private async Task ValidateEvidenceAsync(SqliteConnection connection, SqliteTransaction transaction, string scope, MemoryProposal proposal, CancellationToken ct)
    {
        var sessions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in proposal.SourceIds)
        {
            await using var source = connection.CreateCommand();
            source.Transaction = transaction;
            source.CommandText = "SELECT session_id FROM learning_observations WHERE id = $id AND scope_key = $scope AND created_unix >= $expiry;";
            source.Parameters.AddWithValue("$id", id);
            source.Parameters.AddWithValue("$scope", scope);
            source.Parameters.AddWithValue("$expiry", DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds());
            var session = await source.ExecuteScalarAsync(ct).ConfigureAwait(false) as string ?? throw InvalidLearning();
            sessions.Add(session);
        }
        if (proposal.Operation == "add" && sessions.Count < 2)
        {
            throw InvalidLearning();
        }
        foreach (var target in proposal.Targets)
        {
            await using var check = connection.CreateCommand();
            check.Transaction = transaction;
            check.CommandText = "SELECT COUNT(*) FROM persistent_memories WHERE id = $id AND scope_key = $scope AND body = $body AND updated_unix = $updated;";
            check.Parameters.AddWithValue("$id", target.Id);
            check.Parameters.AddWithValue("$scope", target.IsGlobal ? "global" : scope);
            check.Parameters.AddWithValue("$body", target.Text);
            check.Parameters.AddWithValue("$updated", target.UpdatedAt.ToUnixTimeSeconds());
            if (Convert.ToInt64(await check.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1)
            {
                throw InvalidLearning();
            }
        }
    }

    /// <summary>Rejects malformed, duplicate, oversized, and credential-bearing proposal fields.</summary>
    private void ValidateProposal(MemoryProposal proposal)
    {
        if (proposal is null || !Guid.TryParseExact(proposal.Id, "N", out _) || proposal.Kind is not ("memory" or "lesson" or "correction" or "preference")
            || proposal.Operation is not ("add" or "merge" or "archive" or "flag")
            || string.IsNullOrWhiteSpace(proposal.Text) || proposal.Text.Length > 1000
            || string.IsNullOrWhiteSpace(proposal.Rationale) || proposal.Rationale.Length > 1000
            || proposal.CreatedAt < DateTimeOffset.UtcNow.AddDays(-30) || proposal.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(1)
            || proposal.SourceIds is null || proposal.Targets is null || proposal.SourceIds.Count > 8 || proposal.Targets.Count > 8
            || proposal.SourceIds.Any(id => !Guid.TryParseExact(id, "N", out _))
            || proposal.SourceIds.Distinct().Count() != proposal.SourceIds.Count
            || proposal.Targets.Any(target => target is null || !Guid.TryParseExact(target.Id, "N", out _)
                || string.IsNullOrWhiteSpace(target.Text) || target.Text.Length > PersistentMemoryService.MaximumCharacters
                || target.UpdatedAt.ToUnixTimeSeconds() < 0 || target.UpdatedAt > DateTimeOffset.UtcNow.AddMinutes(1))
            || proposal.Targets.Select(target => target.Id).Distinct().Count() != proposal.Targets.Count
            || proposal.Targets.Select(target => target.IsGlobal).Distinct().Count() > 1
            || (proposal.Operation == "add" && (proposal.Targets.Count != 0 || proposal.SourceIds.Count < 2))
            || (proposal.Operation == "merge" && proposal.Targets.Count < 2)
            || (proposal.Operation is "archive" or "flag" && proposal.Targets.Count == 0))
        {
            throw InvalidLearning();
        }
        var json = JsonSerializer.Serialize(proposal, Json);
        if (json.Length > 32768 || json != redactor.Redact(json)
            || proposal.Text != redactor.Redact(proposal.Text) || proposal.Rationale != redactor.Redact(proposal.Rationale)
            || proposal.Targets.Any(target => target.Text != redactor.Redact(target.Text)))
        {
            throw InvalidLearning();
        }
    }

    /// <summary>Compares suggestions independently of generated identifiers, source ordering, and explanations.</summary>
    private static bool EquivalentProposal(MemoryProposal left, MemoryProposal right) =>
        left.Operation == right.Operation && string.Equals(left.Text?.Trim(), right.Text.Trim(), StringComparison.OrdinalIgnoreCase)
        && left.Targets is not null && left.Targets.OrderBy(target => target.Id, StringComparer.Ordinal)
            .SequenceEqual(right.Targets.OrderBy(target => target.Id, StringComparer.Ordinal));

    /// <summary>Checks an optional caller snapshot under the write lock before retaining any new evidence.</summary>
    private static async Task<bool> RevisionMatchesAsync(SqliteConnection connection, SqliteTransaction transaction,
        string scope, string? expectedRevision, CancellationToken ct) => expectedRevision is null
        || expectedRevision == await ReadRevisionAsync(connection, transaction, scope, ct).ConfigureAwait(false);

    /// <summary>Deletes one observation and all retained proposals derived from it without deleting approved memories.</summary>
    public async Task<bool> ForgetObservationAsync(string id, CancellationToken ct)
    {
        if (!Guid.TryParseExact(id, "N", out _))
        {
            throw InvalidLearning();
        }
        var scope = PersistentMemoryService.ResolveProjectScope();
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM learning_observations WHERE id = $id AND scope_key = $scope;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$scope", scope);
        var removed = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 1;
        if (removed)
        {
            command.CommandText = "DELETE FROM memory_proposals WHERE scope_key = $scope AND EXISTS (SELECT 1 FROM json_each(payload_json, '$.sourceIds') WHERE value = $id);";
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            await AdvanceRevisionAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return removed;
    }

    /// <summary>Prunes expired evidence and derivatives, expires changed targets, and bounds completed review history.</summary>
    private static async Task PruneLearningAsync(SqliteConnection connection, SqliteTransaction transaction, string scope, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM learning_observations WHERE scope_key = $scope AND created_unix < $expiry;
            DELETE FROM memory_proposals WHERE scope_key = $scope AND (created_unix < $expiry OR EXISTS (
                SELECT 1 FROM json_each(payload_json, '$.sourceIds') AS source
                WHERE NOT EXISTS (SELECT 1 FROM learning_observations AS observation
                    WHERE observation.id = source.value AND observation.scope_key = $scope)));
            UPDATE memory_proposals SET status = 'expired' WHERE scope_key = $scope AND status = 'pending' AND EXISTS (
                SELECT 1 FROM json_each(payload_json, '$.targets') AS target WHERE NOT EXISTS (
                    SELECT 1 FROM persistent_memories AS memory
                    WHERE memory.id = json_extract(target.value, '$.id')
                        AND memory.body = json_extract(target.value, '$.text')
                        AND memory.updated_unix = unixepoch(json_extract(target.value, '$.updatedAt'))
                        AND memory.scope_key = CASE WHEN json_extract(target.value, '$.isGlobal') = 1 THEN 'global' ELSE $scope END));
            DELETE FROM memory_proposals WHERE scope_key = $scope AND status <> 'pending' AND id NOT IN (
                SELECT id FROM memory_proposals WHERE scope_key = $scope AND status <> 'pending'
                ORDER BY created_unix DESC, rowid DESC LIMIT 200);
            """;
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$expiry", DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds());
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reports stale or invalid learning evidence without disclosing its contents in an error.</summary>
    private InvalidOperationException InvalidLearning() => new(text.Text("Lab.Invalid"));
}

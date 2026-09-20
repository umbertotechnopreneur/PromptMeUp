// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class GlobalMemoryMigrationTests
{
    /// <summary>Migration makes every project note global while preserving identifiers, duplicates, timestamps, and provenance.</summary>
    [Fact]
    public async Task Initialize_LegacyScopes_PreservesEveryNoteAndProvenance()
    {
        using var fixture = new RegressionFixture();
        await CreateLegacyMemoriesAsync(fixture, 3);
        var beforeNotes = await ReadNoteSnapshotAsync(fixture);
        var beforeProvenance = await ReadProvenanceSnapshotAsync(fixture);

        await fixture.Database.InitializeAsync(default);
        await fixture.Database.InitializeAsync(default);

        Assert.Equal(5L, await fixture.ScalarAsync("PRAGMA user_version;"));
        Assert.Equal(3L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories WHERE scope_key = 'global';"));
        Assert.Equal(beforeNotes, await ReadNoteSnapshotAsync(fixture));
        Assert.Equal(beforeProvenance, await ReadProvenanceSnapshotAsync(fixture));
        var memories = await CreateService(fixture).ListAsync(default);
        Assert.Equal(3, memories.Count);
        Assert.All(memories, memory => Assert.True(memory.IsGlobal));
        Assert.Equal(2, memories.Count(memory => memory.Text == "Duplicate legacy note."));
    }

    /// <summary>Accumulated legacy notes remain readable, editable, refreshable, and deletable above the new-note limit.</summary>
    [Fact]
    public async Task Initialize_OverfullLegacyCollection_PreservesAccessWithoutAllowingNewNotes()
    {
        using var fixture = new RegressionFixture();
        var count = PersistentMemoryService.MaximumMemoriesPerScope * 3 + 1;
        await CreateLegacyMemoriesAsync(fixture, count);
        var before = await ReadNoteSnapshotAsync(fixture);

        await fixture.Database.InitializeAsync(default);

        var service = CreateService(fixture);
        Assert.Equal(before, await ReadNoteSnapshotAsync(fixture));
        var memories = await service.ListAsync(default);
        Assert.Equal(count, memories.Count);
        await Assert.ThrowsAsync<MemoryValidationException>(() => service.RememberAsync("New note beyond the limit.", false, default));
        var duplicate = memories.Single(memory => memory.Id == 1.ToString("x32"));
        var refreshed = await service.UpdateAsync(duplicate.Id, duplicate.Text, false, default);
        Assert.Equal(duplicate.Id, refreshed.Id);
        Assert.Equal(2L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories WHERE body = 'Duplicate legacy note.';"));
        await service.RememberAsync(duplicate.Text, false, default);
        var edited = await service.UpdateAsync(duplicate.Id, "Edited migrated note.", false, default);
        Assert.Equal(duplicate.Id, edited.Id);
        Assert.True(edited.IsGlobal);
        Assert.True(await service.ForgetAsync(edited.Id, default, edited));
        Assert.Equal(count - 1, (await service.ListAsync(default)).Count);
        Assert.Equal("Duplicate legacy note.", await fixture.ScalarAsync(
            "SELECT body FROM persistent_memories WHERE id = $id;", ("$id", 2.ToString("x32"))));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM memory_provenance;"));
    }

    /// <summary>A failed scope conversion rolls back every note, provenance row, and schema version before a retry.</summary>
    [Fact]
    public async Task Initialize_MigrationFailure_RollsBackWholeUpgrade()
    {
        using var fixture = new RegressionFixture();
        await CreateLegacyMemoriesAsync(fixture, 3);
        var beforeNotes = await ReadNoteSnapshotAsync(fixture);
        var beforeScopes = await ReadScopeSnapshotAsync(fixture);
        var beforeProvenance = await ReadProvenanceSnapshotAsync(fixture);
        await fixture.ScalarAsync("""
            CREATE TRIGGER reject_fixture_scope_update BEFORE UPDATE OF scope_key ON persistent_memories
            WHEN OLD.id = '00000000000000000000000000000002'
            BEGIN SELECT RAISE(ABORT, 'Synthetic migration failure.'); END;
            """);

        await Assert.ThrowsAsync<SqliteException>(() => fixture.Database.InitializeAsync(default));

        Assert.Equal(3L, await fixture.ScalarAsync("PRAGMA user_version;"));
        Assert.Equal(beforeNotes, await ReadNoteSnapshotAsync(fixture));
        Assert.Equal(beforeScopes, await ReadScopeSnapshotAsync(fixture));
        Assert.Equal(beforeProvenance, await ReadProvenanceSnapshotAsync(fixture));
        await fixture.ScalarAsync("DROP TRIGGER reject_fixture_scope_update;");
        await fixture.Database.InitializeAsync(default);
        Assert.Equal(5L, await fixture.ScalarAsync("PRAGMA user_version;"));
        Assert.Equal(3L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories WHERE scope_key = 'global';"));
    }

    /// <summary>Creates version-three fixture notes across global and hashed project scopes without invoking current initialization.</summary>
    private static Task<object?> CreateLegacyMemoriesAsync(RegressionFixture fixture, int count) => fixture.ScalarAsync("""
        CREATE TABLE persistent_memories (
            id TEXT NOT NULL PRIMARY KEY CHECK(length(id) = 32),
            scope_key TEXT NOT NULL CHECK(scope_key = 'global' OR length(scope_key) = 64),
            body TEXT NOT NULL CHECK(length(body) BETWEEN 1 AND 1000),
            updated_unix INTEGER NOT NULL CHECK(updated_unix >= 0)
        );
        CREATE TABLE memory_provenance (
            memory_id TEXT NOT NULL PRIMARY KEY,
            kind TEXT NOT NULL CHECK(kind IN ('memory', 'lesson', 'correction', 'preference')),
            sources_json TEXT NOT NULL CHECK(length(sources_json) <= 1024 AND json_valid(sources_json)),
            proposal_id TEXT NOT NULL CHECK(length(proposal_id) = 32),
            FOREIGN KEY(memory_id) REFERENCES persistent_memories(id) ON DELETE CASCADE
        );
        WITH RECURSIVE notes(number) AS (
            SELECT 1 UNION ALL SELECT number + 1 FROM notes WHERE number < $count
        )
        INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
        SELECT printf('%032x', number),
            CASE number % 3 WHEN 0 THEN 'global' WHEN 1 THEN $firstScope ELSE $secondScope END,
            CASE WHEN number <= 2 THEN 'Duplicate legacy note.' ELSE 'Legacy note ' || number END,
            1000 + number FROM notes;
        INSERT INTO memory_provenance (memory_id, kind, sources_json, proposal_id)
        VALUES ('00000000000000000000000000000001', 'preference', '["11111111111111111111111111111111"]', 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'),
            ('00000000000000000000000000000002', 'lesson', '["22222222222222222222222222222222"]', 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb');
        PRAGMA user_version = 3;
        """, ("$count", count), ("$firstScope", new string('A', 64)), ("$secondScope", new string('B', 64)));

    /// <summary>Reads every note field that the scope-only migration must preserve in a deterministic JSON snapshot.</summary>
    private static Task<object?> ReadNoteSnapshotAsync(RegressionFixture fixture) => fixture.ScalarAsync("""
        SELECT json_group_array(json_object('id', id, 'body', body, 'updated', updated_unix))
        FROM (SELECT id, body, updated_unix FROM persistent_memories ORDER BY id);
        """);

    /// <summary>Reads the exact legacy scope assignment for rollback assertions.</summary>
    private static Task<object?> ReadScopeSnapshotAsync(RegressionFixture fixture) => fixture.ScalarAsync("""
        SELECT json_group_array(json_object('id', id, 'scope', scope_key))
        FROM (SELECT id, scope_key FROM persistent_memories ORDER BY id);
        """);

    /// <summary>Reads complete provenance rows without interpreting or rewriting their stored JSON.</summary>
    private static Task<object?> ReadProvenanceSnapshotAsync(RegressionFixture fixture) => fixture.ScalarAsync("""
        SELECT json_group_array(json_object('id', memory_id, 'kind', kind, 'sources', sources_json, 'proposal', proposal_id))
        FROM (SELECT memory_id, kind, sources_json, proposal_id FROM memory_provenance ORDER BY memory_id);
        """);

    /// <summary>Creates the production memory service against isolated persistence and production redaction.</summary>
    private static PersistentMemoryService CreateService(RegressionFixture fixture) => new(
        fixture.Paths, new SensitiveDataRedactor(), new LocalizationService(), NullLogger<PersistentMemoryService>.Instance);
}

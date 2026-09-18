// SPDX-License-Identifier: MIT

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class MemoryReflectionTests
{
    /// <summary>Counts the serialized envelope and escapes while retaining complete evidence from separate sessions.</summary>
    [Fact]
    public void DreamBatch_RespectsSerializedLimitAndPreservesCompleteObservations()
    {
        var service = Create();
        var observations = new[]
        {
            Observation("one", new string('"', 3900)),
            Observation("two", "Use project-specific configuration."),
            Observation("three", "Keep settings specific to this project.")
        };

        var batch = service.DreamBatch(observations, 1000);

        Assert.True(batch.Request.Length <= 1000);
        Assert.Equal(2, batch.Observations.Count);
        Assert.Equal(1, batch.OmittedCount);
        Assert.Equal(2, batch.Observations.Select(item => item.SessionId).Distinct().Count());
        using var document = JsonDocument.Parse(batch.Request);
        foreach (var item in document.RootElement.GetProperty("observations").EnumerateArray())
        {
            var original = observations.Single(source => source.Id == item.GetProperty("id").GetString());
            Assert.Equal(original.Text, item.GetProperty("text").GetString());
        }
    }

    /// <summary>Refuses an input ceiling too small for two full independent observations instead of truncating evidence.</summary>
    [Fact]
    public void DreamBatch_TooSmallForTwoSessions_Rejects()
    {
        var observations = new[] { Observation("one", "A fact."), Observation("two", "Another fact.") };

        Assert.Throws<InvalidOperationException>(() => Create().DreamBatch(observations, 80));
    }

    /// <summary>Accepts an evidence-backed addition while keeping source IDs and independent-session requirements intact.</summary>
    [Fact]
    public void Parse_ValidDreamProposal_PreservesSourceIdentity()
    {
        var service = Create();
        var sources = new[] { Observation("one", "Prefer concise output."), Observation("two", "Keep answers concise.") };
        var batch = service.DreamBatch(sources, 4000);

        var result = service.Parse(Response("add", sources.Select(item => item.Id).ToArray(), []), batch);

        var proposal = Assert.Single(result);
        Assert.Equal("add", proposal.Operation);
        Assert.Equal(sources.Select(item => item.Id), proposal.SourceIds);
        Assert.Empty(proposal.Targets);
    }

    /// <summary>Prevents fabricated source IDs and same-session messages from becoming cross-session evidence.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_UnsupportedDreamEvidence_Rejects(bool unknownSource)
    {
        var service = Create();
        var sources = new[] { Observation("one", "Prefer concise output."), Observation("two", "Keep answers concise.") };
        var batch = service.DreamBatch(sources, 4000);
        var ids = sources.Select(item => item.Id).ToArray();
        if (unknownSource)
        {
            ids[1] = Guid.NewGuid().ToString("N");
        }
        else
        {
            batch = batch with { Observations = sources.Select(item => item with { SessionId = "one" }).ToArray() };
        }

        Assert.Throws<InvalidOperationException>(() => service.Parse(Response("add", ids, []), batch));
    }

    /// <summary>Rejects malformed contracts and duplicate JSON properties instead of taking the last occurrence.</summary>
    [Theory]
    [InlineData("{\"proposals\":[],\"proposals\":[]}")]
    [InlineData("{\"proposals\":null}")]
    [InlineData("{\"proposals\":[],\"execute\":true}")]
    [InlineData("{\"proposals\":[null]}")]
    [InlineData("[]")]
    [InlineData("{")]
    public void Parse_MalformedContract_Rejects(string response)
    {
        var service = Create();
        var batch = service.DreamBatch([Observation("one", "A fact."), Observation("two", "Another fact.")], 4000);

        Assert.Throws<InvalidOperationException>(() => service.Parse(response, batch));
    }

    /// <summary>Binds maintenance targets to complete snapshots regardless of obsolete destination flags.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Parse_HeartbeatMerge_PreservesExactSnapshotsWithoutScopePartition(bool legacyFlag)
    {
        var service = Create();
        var saved = new[] { Memory(false, "A note."), Memory(legacyFlag, "A related note.") };
        var batch = service.HeartbeatBatch(saved, 4000);

        var result = service.Parse(Response("merge", [], saved.Select(item => item.Id).ToArray()), batch);

        Assert.Equal(saved, Assert.Single(result).Targets);
    }

    /// <summary>Stops maintenance from citing a note outside the exact approved batch.</summary>
    [Fact]
    public void Parse_UnknownMaintenanceTarget_Rejects()
    {
        var service = Create();
        var saved = new[] { Memory(true, "A note."), Memory(true, "A related note.") };
        var batch = service.HeartbeatBatch(saved, 4000);
        var ids = saved.Select(item => item.Id).ToArray();
        ids[1] = Guid.NewGuid().ToString("N");

        Assert.Throws<InvalidOperationException>(() => service.Parse(Response("merge", [], ids), batch));
    }

    /// <summary>Finds exact duplicates in one saved-note collection without preserving obsolete scope partitions.</summary>
    [Fact]
    public void FindDuplicates_RequiresExactTextRegardlessOfLegacyFlag()
    {
        var saved = new[]
        {
            Memory(false, "A note."), Memory(false, "A note."), Memory(true, "A note."), Memory(false, "a note.")
        };

        var proposal = Assert.Single(Create().FindDuplicates(saved));

        Assert.Equal("merge", proposal.Operation);
        Assert.Equal(saved.Take(3).Select(item => item.Id).Order(), proposal.Targets.Select(item => item.Id).Order());
        Assert.Empty(proposal.SourceIds);
    }

    /// <summary>Preserved legacy collections above two hundred notes can still produce complete, bounded maintenance batches.</summary>
    [Fact]
    public async Task HeartbeatBatch_ListsOverTwoHundredLegacyNotesAndKeepsProviderInputBounded()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.ScalarAsync("""
            WITH RECURSIVE legacy(n) AS (VALUES(1) UNION ALL SELECT n+1 FROM legacy WHERE n < 205)
            INSERT INTO persistent_memories(id, scope_key, body, updated_unix)
            SELECT printf('%032x', n), CASE WHEN n % 3 = 0 THEN 'global' ELSE printf('%064x', n % 3) END,
                   'Preserved preference ' || n || '.', 1700000000 + n
            FROM legacy;
            """);
        var store = new PersistentMemoryService(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService(),
            NullLogger<PersistentMemoryService>.Instance);
        var listed = await store.ListAsync(default);

        var batch = Create().HeartbeatBatch(listed, 4000);

        Assert.Equal(205, listed.Count);
        Assert.All(listed, memory => Assert.True(memory.IsGlobal));
        Assert.InRange(batch.Memories.Count, 1, 24);
        Assert.Equal(listed.Count - batch.Memories.Count, batch.OmittedCount);
        Assert.InRange(batch.Request.Length, 1, 4000);
        Assert.Equal(205L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories;"));
        using var request = JsonDocument.Parse(batch.Request);
        var supplied = request.RootElement.GetProperty("memories");
        Assert.Equal(batch.Memories.Count, supplied.GetArrayLength());
        foreach (var item in supplied.EnumerateArray())
        {
            Assert.Equal(new[] { "id", "text", "updated_at" }, item.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.False(item.TryGetProperty("scope", out _));
            var original = Assert.Single(listed, memory => memory.Id == item.GetProperty("id").GetString());
            Assert.Equal(original.Text, item.GetProperty("text").GetString());
        }
    }

    /// <summary>Creates a deterministic local reflection service with no network or filesystem dependencies.</summary>
    private static MemoryReflectionService Create() => new(new SensitiveDataRedactor(), new LocalizationService());

    /// <summary>Creates synthetic source evidence without captured user data.</summary>
    private static LearningObservation Observation(string session, string text) => new(Guid.NewGuid().ToString("N"), session, text, DateTimeOffset.UtcNow);

    /// <summary>Creates an exact synthetic memory snapshot, including flags retained by legacy callers.</summary>
    private static PersistentMemory Memory(bool global, string text) => new(Guid.NewGuid().ToString("N"), text, global, DateTimeOffset.UtcNow);

    /// <summary>Serializes a complete provider contract for structural and evidence-validation cases.</summary>
    private static string Response(string operation, string[] sourceIds, string[] targetIds) => JsonSerializer.Serialize(new
    {
        proposals = new[] { new { kind = "preference", operation, text = "Prefer concise output.", rationale = "Supported by supplied evidence.", source_ids = sourceIds, target_ids = targetIds } }
    });
}

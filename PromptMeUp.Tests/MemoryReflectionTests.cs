// SPDX-License-Identifier: MIT

using System.Text.Json;
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

    /// <summary>Binds maintenance targets to their complete local snapshots rather than model-authored memory contents.</summary>
    [Fact]
    public void Parse_HeartbeatMerge_PreservesExactSnapshots()
    {
        var service = Create();
        var saved = new[] { Memory(false, "A note."), Memory(false, "A related note.") };
        var batch = service.HeartbeatBatch(saved, 4000);

        var result = service.Parse(Response("merge", [], saved.Select(item => item.Id).ToArray()), batch);

        Assert.Equal(saved, Assert.Single(result).Targets);
    }

    /// <summary>Stops maintenance from merging global and project memories or citing memories outside the approved batch.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_UnsupportedMaintenanceTargets_Rejects(bool mixedScope)
    {
        var service = Create();
        var saved = new[] { Memory(false, "A note."), Memory(mixedScope, "A related note.") };
        var batch = service.HeartbeatBatch(saved, 4000);
        var ids = saved.Select(item => item.Id).ToArray();
        if (!mixedScope)
        {
            ids[1] = Guid.NewGuid().ToString("N");
        }

        Assert.Throws<InvalidOperationException>(() => service.Parse(Response("merge", [], ids), batch));
    }

    /// <summary>Limits local duplicate proposals to exact same-scope text without deleting or modifying any input.</summary>
    [Fact]
    public void FindDuplicates_RequiresExactSameScopeText()
    {
        var saved = new[]
        {
            Memory(false, "A note."), Memory(false, "A note."), Memory(true, "A note."), Memory(false, "a note.")
        };

        var proposal = Assert.Single(Create().FindDuplicates(saved));

        Assert.Equal("merge", proposal.Operation);
        Assert.Equal(saved.Take(2).Select(item => item.Id).Order(), proposal.Targets.Select(item => item.Id).Order());
        Assert.Empty(proposal.SourceIds);
    }

    /// <summary>Creates a deterministic local reflection service with no network or filesystem dependencies.</summary>
    private static MemoryReflectionService Create() => new(new SensitiveDataRedactor(), new LocalizationService());

    /// <summary>Creates synthetic source evidence without captured user data.</summary>
    private static LearningObservation Observation(string session, string text) => new(Guid.NewGuid().ToString("N"), session, text, DateTimeOffset.UtcNow);

    /// <summary>Creates a synthetic exact memory snapshot for scope and identity checks.</summary>
    private static PersistentMemory Memory(bool global, string text) => new(Guid.NewGuid().ToString("N"), text, global, DateTimeOffset.UtcNow);

    /// <summary>Serializes a complete provider contract for structural and evidence-validation cases.</summary>
    private static string Response(string operation, string[] sourceIds, string[] targetIds) => JsonSerializer.Serialize(new
    {
        proposals = new[] { new { kind = "preference", operation, text = "Prefer concise output.", rationale = "Supported by supplied evidence.", source_ids = sourceIds, target_ids = targetIds } }
    });
}

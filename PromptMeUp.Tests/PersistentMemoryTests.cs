// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class PersistentMemoryTests
{
    /// <summary>Explicit notes survive reopening as global regardless of the legacy flag and avoid duplicate records.</summary>
    [Fact]
    public async Task Remember_ReopenedStore_UsesGlobalStorageAndDeduplicates()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var project = await service.RememberAsync("Use the repository build script.", false, default);
        var global = await service.RememberAsync("Prefer concise explanations.", true, default);
        var repeated = await service.RememberAsync("  Use the repository build script.  ", false, default);

        var memories = await CreateService(fixture).ListAsync(default);

        Assert.Equal(project.Id, repeated.Id);
        Assert.Equal(2, memories.Count);
        Assert.Contains(memories, memory => memory.Id == project.Id && memory.IsGlobal);
        Assert.Contains(memories, memory => memory.Id == global.Id && memory.IsGlobal);
        var scope = Assert.IsType<string>(await fixture.ScalarAsync(
            "SELECT scope_key FROM persistent_memories WHERE id = $id;", ("$id", project.Id)));
        Assert.Equal("global", scope);
    }

    /// <summary>Both legacy scope flags address the same global note, which can be selected and deleted by identifier.</summary>
    [Fact]
    public async Task LegacyScopeFlags_AddressOneGlobalCollection()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var saved = await service.RememberAsync("Release instructions for every project.", false, default);
        var repeated = await service.RememberAsync(saved.Text, true, default);

        Assert.Equal(saved.Id, repeated.Id);
        Assert.True(Assert.Single(await service.ListAsync(default)).IsGlobal);
        Assert.Equal(saved.Id, Assert.Single((await service.SelectAsync("Release instructions", default)).Memories).Id);
        Assert.True(await service.ForgetAsync(saved.Id, default));
        Assert.False(await service.ForgetAsync(saved.Id, default));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories;"));
    }

    /// <summary>Rejected note inputs never reach SQLite and surface recoverable validation errors.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("api_key=synthetic-memory-fixture-value")]
    public async Task Remember_InvalidInput_DoesNotPersist(string note)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);

        await Assert.ThrowsAsync<MemoryValidationException>(() => service.RememberAsync(note, false, default));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories;"));
    }

    /// <summary>The maximum note length is accepted exactly while oversize notes and partial identifiers are rejected.</summary>
    [Fact]
    public async Task NoteAndIdentifierLimits_RejectBeforeMutation()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var memory = await service.RememberAsync(new string('x', PersistentMemoryService.MaximumCharacters), true, default);

        await Assert.ThrowsAsync<MemoryValidationException>(() => service.RememberAsync(memory.Text + "x", true, default));
        await Assert.ThrowsAsync<MemoryValidationException>(() => service.ForgetAsync(memory.Id[..8], default));
        Assert.Single(await service.ListAsync(default));
    }

    /// <summary>A full collection refuses new notes under either legacy flag while allowing duplicate refreshes.</summary>
    [Fact]
    public async Task CollectionLimit_IsAtomicAndIgnoresLegacyScope()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await SeedGlobalNotesAsync(fixture, PersistentMemoryService.MaximumMemoriesPerScope);
        var service = CreateService(fixture);

        await Assert.ThrowsAsync<MemoryValidationException>(() => service.RememberAsync("One note too many.", true, default));
        await service.RememberAsync("Synthetic note 1", true, default);
        await Assert.ThrowsAsync<MemoryValidationException>(() => service.RememberAsync("Another new note.", false, default));
        await service.RememberAsync("Synthetic note 2", false, default);

        var memories = await service.ListAsync(default);
        Assert.Equal(PersistentMemoryService.MaximumMemoriesPerScope, memories.Count(memory => memory.IsGlobal));
        Assert.All(memories, memory => Assert.True(memory.IsGlobal));
    }

    /// <summary>All notes remain eligible, lexical relevance ranks first, and repeated content loads once.</summary>
    [Fact]
    public async Task Select_RelevanceAndDeduplication_RankGlobalNotes()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var project = await service.RememberAsync("Release builds use the repository script.", false, default);
        await service.RememberAsync(project.Text, true, default);
        var preference = await service.RememberAsync("Prefer concise explanations.", true, default);
        var unrelated = await service.RememberAsync("Orchard recipes contain apples.", false, default);

        var selection = await service.SelectAsync("Prepare release builds", default);

        Assert.Equal(3, selection.Memories.Count);
        Assert.Equal(project.Id, selection.Memories[0].Id);
        Assert.Contains(selection.Memories, memory => memory.Id == project.Id);
        Assert.Contains(selection.Memories, memory => memory.Id == preference.Id);
        Assert.Contains(selection.Memories, memory => memory.Id == unrelated.Id);
    }

    /// <summary>Multibyte notes are charged by UTF-8 size and never exceed the total memory content allowance.</summary>
    [Fact]
    public async Task Select_MultilingualNotes_StayWithinTokenBudget()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        for (var index = 0; index < 6; index++)
        {
            await service.RememberAsync($"Release {index}: " + new string('界', 500), true, default);
        }

        var selection = await service.SelectAsync("release", default);

        Assert.Single(selection.Memories);
        Assert.InRange(selection.EstimatedTokens, 1, PersistentMemoryService.MaximumContentTokens);
        Assert.Equal(selection.Memories.Sum(memory => ContextTokenEstimator.Text(memory.Text) + 4), selection.EstimatedTokens);
    }

    /// <summary>A large collection of short relevant notes is independently capped by the maximum selected count.</summary>
    [Fact]
    public async Task Select_ShortNotes_StayWithinCountLimit()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await SeedGlobalNotesAsync(fixture, 12);

        var selection = await CreateService(fixture).SelectAsync("Synthetic note", default);

        Assert.Equal(PersistentMemoryService.MaximumMemories, selection.Memories.Count);
        Assert.True(selection.EstimatedTokens <= PersistentMemoryService.MaximumContentTokens);
    }

    /// <summary>Current redaction repairs legacy stored content before returning it to the local list or provider selection.</summary>
    [Fact]
    public async Task Read_LegacyRecognizableCredential_RepairsBeforeSelection()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var note = "api_key=synthetic-legacy-memory-value";
        var safe = new SensitiveDataRedactor().Redact(note);
        Assert.NotEqual(note, safe);
        await fixture.ScalarAsync("""
            INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
            VALUES ($id, 'global', $body, 1);
            """, ("$id", Guid.NewGuid().ToString("N")), ("$body", note));
        var service = CreateService(fixture);

        Assert.Equal(safe, Assert.Single(await service.ListAsync(default)).Text);
        Assert.Equal(safe, Assert.Single((await service.SelectAsync("preferences", default)).Memories).Text);
        Assert.Equal(safe, await fixture.ScalarAsync("SELECT body FROM persistent_memories;"));
    }

    /// <summary>Legacy collections above the creation limit stay fully listable and allow existing notes to refresh.</summary>
    [Fact]
    public async Task Read_OverfullCollection_PreservesEveryNoteAndAllowsRefresh()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var count = PersistentMemoryService.MaximumMemoriesPerScope * 3 + 1;
        await SeedGlobalNotesAsync(fixture, count);
        var service = CreateService(fixture);

        Assert.Equal(count, (await service.ListAsync(default)).Count);
        await Assert.ThrowsAsync<MemoryValidationException>(() => service.RememberAsync("Another new note.", false, default));
        await service.RememberAsync("Synthetic note 1", false, default);
        Assert.Equal(count, (await service.ListAsync(default)).Count);
    }

    /// <summary>Creates the actual memory service using isolated persistence and the production redaction policy.</summary>
    private static PersistentMemoryService CreateService(RegressionFixture fixture) => new(
        fixture.Paths, new SensitiveDataRedactor(), new LocalizationService(), NullLogger<PersistentMemoryService>.Instance);

    /// <summary>Seeds a bounded fixture collection without changing the process working directory or real user data.</summary>
    private static Task<object?> SeedGlobalNotesAsync(RegressionFixture fixture, int count) => fixture.ScalarAsync("""
        WITH RECURSIVE notes(number) AS (
            SELECT 1 UNION ALL SELECT number + 1 FROM notes WHERE number < $count
        )
        INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
        SELECT printf('%032x', number), 'global', 'Synthetic note ' || number, number FROM notes;
        """, ("$count", count));
}

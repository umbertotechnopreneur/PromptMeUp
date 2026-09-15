// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class PersistentMemoryUpdateTests
{
    /// <summary>Editing and moving a note keeps its exact identifier and persists the reviewed text and destination scope.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_ChangesTextAndScope_PreservesIdentifier(bool global)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var original = await service.RememberAsync("Original saved note.", !global, default);

        var updated = await service.UpdateAsync(original.Id, "  Updated saved note.  ", global, default);
        var persisted = Assert.Single(await CreateService(fixture).ListAsync(default));

        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.Id, persisted.Id);
        Assert.Equal("Updated saved note.", persisted.Text);
        Assert.Equal(global, persisted.IsGlobal);
        Assert.Equal(updated, persisted);
    }

    /// <summary>Invalid, missing, and foreign-project identifiers cannot replace any local or foreign saved content.</summary>
    [Fact]
    public async Task Update_InaccessibleIdentifier_LeavesAllNotesUnchanged()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var local = await service.RememberAsync("Local saved note.", false, default);
        var foreignId = Guid.NewGuid().ToString("N");
        await fixture.ScalarAsync("""
            INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
            VALUES ($id, $scope, 'Foreign saved note.', 1);
            """, ("$id", foreignId), ("$scope", new string('F', 64)));

        foreach (var id in new[] { local.Id[..8], local.Id + "' OR 1=1; --", Guid.NewGuid().ToString("N"), foreignId })
        {
            await Assert.ThrowsAsync<MemoryValidationException>(() => service.UpdateAsync(id, "Replacement note.", true, default));
        }

        Assert.Equal(local, Assert.Single(await service.ListAsync(default)));
        Assert.Equal("Foreign saved note.", await fixture.ScalarAsync(
            "SELECT body FROM persistent_memories WHERE id = $id;", ("$id", foreignId)));
        Assert.Equal(2L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories;"));
    }

    /// <summary>Rejected empty and recognizable credential-bearing edits leave the original note and its scope intact.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("api_key=synthetic-memory-update-fixture")]
    public async Task Update_InvalidText_DoesNotMutate(string note)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var original = await service.RememberAsync("Original safe note.", false, default);

        await Assert.ThrowsAsync<MemoryValidationException>(() => service.UpdateAsync(original.Id, note, true, default));

        Assert.Equal(original, Assert.Single(await service.ListAsync(default)));
    }

    /// <summary>Overlength edits fail before mutation while an exact-limit update remains supported.</summary>
    [Fact]
    public async Task Update_TextLengthBoundary_PreservesNoteUntilValid()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var original = await service.RememberAsync("Original safe note.", true, default);
        var maximumNote = new string('x', PersistentMemoryService.MaximumCharacters);

        await Assert.ThrowsAsync<MemoryValidationException>(() => service.UpdateAsync(original.Id, maximumNote + "x", false, default));
        Assert.Equal(original, Assert.Single(await service.ListAsync(default)));

        await service.UpdateAsync(original.Id, maximumNote, true, default);
        Assert.Equal(maximumNote, Assert.Single(await service.ListAsync(default)).Text);
    }

    /// <summary>A full destination rejects moving a note atomically while still allowing an edit within that full scope.</summary>
    [Fact]
    public async Task Update_FullDestination_RejectsMoveWithoutLosingSource()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await SeedGlobalNotesAsync(fixture, PersistentMemoryService.MaximumMemoriesPerScope);
        var service = CreateService(fixture);
        var source = await service.RememberAsync("Project note to move.", false, default);
        var global = (await service.ListAsync(default)).First(memory => memory.IsGlobal);

        await Assert.ThrowsAsync<MemoryValidationException>(() => service.UpdateAsync(source.Id, "Moved project note.", true, default));

        var afterFailure = await service.ListAsync(default);
        Assert.Equal(source, Assert.Single(afterFailure, memory => !memory.IsGlobal));
        Assert.Equal(PersistentMemoryService.MaximumMemoriesPerScope, afterFailure.Count(memory => memory.IsGlobal));
        var edited = await service.UpdateAsync(global.Id, "Edited within the full scope.", true, default);
        Assert.Equal(global.Id, edited.Id);
        Assert.Equal(PersistentMemoryService.MaximumMemoriesPerScope + 1, (await service.ListAsync(default)).Count);
    }

    /// <summary>Colliding text in the same or another destination scope never overwrites or merges either saved identifier.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_DuplicateText_LeavesBothNotesUnchanged(bool targetGlobal)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var source = await service.RememberAsync("Source saved note.", false, default);
        var other = await service.RememberAsync("Existing destination note.", targetGlobal, default);

        await Assert.ThrowsAsync<MemoryValidationException>(() => service.UpdateAsync(source.Id, other.Text, targetGlobal, default));

        var memories = await service.ListAsync(default);
        Assert.Equal(2, memories.Count);
        Assert.Contains(source, memories);
        Assert.Contains(other, memories);
    }

    /// <summary>A cancelled update leaves persistence untouched and does not prevent a later valid edit.</summary>
    [Fact]
    public async Task Update_Cancelled_DoesNotMutateOrBlockLaterSave()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = CreateService(fixture);
        var original = await service.RememberAsync("Original saved note.", false, default);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.UpdateAsync(
            original.Id, "Cancelled edit.", true, new CancellationToken(canceled: true)));

        Assert.Equal(original, Assert.Single(await service.ListAsync(default)));
        var saved = await service.UpdateAsync(original.Id, "Later valid edit.", false, default);
        Assert.Equal(saved, Assert.Single(await service.ListAsync(default)));
    }

    /// <summary>Creates the production memory service against the fixture's isolated database and credential policy.</summary>
    private static PersistentMemoryService CreateService(RegressionFixture fixture) => new(
        fixture.Paths, new SensitiveDataRedactor(), new LocalizationService(), NullLogger<PersistentMemoryService>.Instance);

    /// <summary>Fills only the isolated global scope with bounded synthetic notes.</summary>
    private static Task<object?> SeedGlobalNotesAsync(RegressionFixture fixture, int count) => fixture.ScalarAsync("""
        WITH RECURSIVE notes(number) AS (
            SELECT 1 UNION ALL SELECT number + 1 FROM notes WHERE number < $count
        )
        INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
        SELECT printf('%032x', number), 'global', 'Synthetic update note ' || number, number FROM notes;
        """, ("$count", count));
}

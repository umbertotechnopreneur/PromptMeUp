// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ExperimentalStoreTests
{
    /// <summary>Another instance's revoked consent cannot be restored by either unrelated toggle from an older settings screen.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SaveSettings_StaleUnrelatedToggle_DoesNotRestoreRevokedConsent(bool disableExperiment, bool automaticSkills)
    {
        using var fixture = new RegressionFixture();
        var first = await PrepareAsync(fixture);
        var second = CreateStore(fixture);
        var stale = await first.SettingsAsync(default);
        var proposal = await AddProposalAsync(first);
        await first.SaveProposalsAsync([proposal], default);
        var revoked = disableExperiment ? new ExperimentalSettings() : stale with { CaptureObservations = false };
        await second.SaveSettingsAsync(revoked, await second.SettingsAsync(default), default);
        var revision = await second.RevisionAsync(default);
        var requested = automaticSkills ? stale with { AutomaticSkills = true } : stale with { MaintenanceReminder = true };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => first.SaveSettingsAsync(requested, stale, default));

        Assert.Equal(new LocalizationService().Text("Lab.Invalid"), error.Message);
        Assert.Equal(revoked, await first.SettingsAsync(default));
        Assert.Equal(revision, await first.RevisionAsync(default));
        Assert.Empty(await first.ObservationsAsync(default));
        Assert.Empty(await first.ProposalsAsync(default));
        await first.CaptureAsync(Guid.NewGuid().ToString("N"), "Must remain uncaptured after consent was revoked.", default, revision);
        Assert.Empty(await first.ObservationsAsync(default));
    }

    /// <summary>A rejected stale disable cannot erase observations or proposals retained by a newer settings snapshot.</summary>
    [Fact]
    public async Task SaveSettings_StaleDisable_PreservesSettingsRevisionAndEvidence()
    {
        using var fixture = new RegressionFixture();
        var first = await PrepareAsync(fixture);
        var second = CreateStore(fixture);
        var stale = await first.SettingsAsync(default);
        var proposal = await AddProposalAsync(first);
        await first.SaveProposalsAsync([proposal], default);
        var observations = await first.ObservationsAsync(default);
        var current = stale with { AutomaticSkills = true };
        await second.SaveSettingsAsync(current, await second.SettingsAsync(default), default);
        var revision = await second.RevisionAsync(default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => first.SaveSettingsAsync(new(), stale, default));

        Assert.Equal(current, await first.SettingsAsync(default));
        Assert.Equal(revision, await first.RevisionAsync(default));
        Assert.Equal(observations, await first.ObservationsAsync(default));
        Assert.Equal(proposal.Id, Assert.Single(await first.ProposalsAsync(default)).Id);
    }

    /// <summary>Enabling capture requires the fresh reviewed state, while a newly confirmed snapshot can still enable it normally.</summary>
    [Fact]
    public async Task SaveSettings_StaleCaptureApproval_RequiresFreshReviewedSnapshot()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var first = CreateStore(fixture);
        var second = CreateStore(fixture);
        await first.SaveSettingsAsync(new(Enabled: true), await first.SettingsAsync(default), default);
        var stale = await first.SettingsAsync(default);
        var current = stale with { MaintenanceReminder = true };
        await second.SaveSettingsAsync(current, await second.SettingsAsync(default), default);
        var revision = await second.RevisionAsync(default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => first.SaveSettingsAsync(stale with { CaptureObservations = true }, stale, default));

        Assert.Equal(current, await first.SettingsAsync(default));
        Assert.Equal(revision, await first.RevisionAsync(default));
        var reviewed = await first.SettingsAsync(default);
        await first.SaveSettingsAsync(reviewed with { CaptureObservations = true }, reviewed, default);
        var enabled = await second.SettingsAsync(default);
        Assert.True(enabled.Enabled);
        Assert.True(enabled.CaptureObservations);
        Assert.True(enabled.MaintenanceReminder);
        Assert.NotEqual(revision, await second.RevisionAsync(default));
        await first.CaptureAsync(Guid.NewGuid().ToString("N"), "Explicitly enabled with a fresh reviewed snapshot.", default);
        Assert.Single(await second.ObservationsAsync(default));
    }

    /// <summary>Capture stays off by default and a revoked opt-in cannot be restored by an older in-flight turn.</summary>
    [Fact]
    public async Task Capture_RevokedRevision_DoesNotResurrectAfterReenable()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var store = CreateStore(fixture);
        var session = Guid.NewGuid().ToString("N");
        await store.CaptureAsync(session, "A disabled observation.", default);
        Assert.Empty(await store.ObservationsAsync(default));
        await store.SaveSettingsAsync(new(Enabled: true, CaptureObservations: true), await store.SettingsAsync(default), default);
        var revision = await store.RevisionAsync(default);
        await store.CaptureAsync(session, "A retained observation.", default, revision);
        Assert.Single(await store.ObservationsAsync(default));

        await store.SaveSettingsAsync(new(Enabled: true, CaptureObservations: false), await store.SettingsAsync(default), default);
        await store.SaveSettingsAsync(new(Enabled: true, CaptureObservations: true), await store.SettingsAsync(default), default);
        await store.CaptureAsync(session, "An old turn that finished late.", default, revision);

        Assert.Empty(await store.ObservationsAsync(default));
        Assert.NotEqual(revision, await store.RevisionAsync(default));
    }

    /// <summary>Valid independent-session evidence promotes exactly once into the explicitly reviewed scope.</summary>
    [Fact]
    public async Task Approve_Add_RequiresExactPayloadAndPersistsProvenance()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var proposal = await AddProposalAsync(store);
        Assert.Equal(1, await store.SaveProposalsAsync([proposal], default));

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApproveAsync(
            proposal with { Rationale = "A different unseen explanation." }, proposal.Text, true, default));
        Assert.Empty(await CreateMemories(fixture).ListAsync(default));
        await store.ApproveAsync(proposal, "Reviewed preference.", true, default);

        var memory = Assert.Single(await CreateMemories(fixture).ListAsync(default));
        Assert.True(memory.IsGlobal);
        Assert.Equal("Reviewed preference.", memory.Text);
        Assert.Equal(proposal.Id, await fixture.ScalarAsync("SELECT proposal_id FROM memory_provenance WHERE memory_id = $id;", ("$id", memory.Id)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApproveAsync(proposal, proposal.Text, false, default));
        Assert.Single(await CreateMemories(fixture).ListAsync(default));
    }

    /// <summary>Two turns from one session and evidence copied from another project cannot justify a learned memory.</summary>
    [Fact]
    public async Task SaveProposals_SameSessionOrForeignEvidence_RejectsAtomically()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var session = Guid.NewGuid().ToString("N");
        await store.CaptureAsync(session, "First preference.", default);
        await store.CaptureAsync(session, "Repeated preference.", default);
        var sources = await store.ObservationsAsync(default);
        var proposal = Proposal("add", sources.Select(source => source.Id).ToArray(), []);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveProposalsAsync([proposal], default));
        await fixture.ScalarAsync("UPDATE learning_observations SET session_id = $session, scope_key = $scope WHERE id = $id;",
            ("$session", Guid.NewGuid().ToString("N")), ("$scope", new string('F', 64)), ("$id", sources[0].Id));

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveProposalsAsync([proposal], default));

        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM memory_proposals;"));
    }

    /// <summary>Removing evidence also removes derived proposals and invalidates already-running reflection work.</summary>
    [Fact]
    public async Task ForgetObservation_RemovesDerivativesAndInvalidatesRevision()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var proposal = await AddProposalAsync(store);
        await store.SaveProposalsAsync([proposal], default);
        var revision = await store.RevisionAsync(default);

        Assert.True(await store.ForgetObservationAsync(proposal.SourceIds[0], default));
        Assert.False(await store.ForgetObservationAsync(proposal.SourceIds[0], default));
        Assert.Empty(await store.ProposalsAsync(default));
        Assert.Single(await store.ObservationsAsync(default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveProposalsAsync([], default, revision));
    }

    /// <summary>Deleting a global note purges every project's learning but retains unrelated approved memories.</summary>
    [Fact]
    public async Task ForgetGlobal_PurgesAllLearningAndBlocksOldCapture()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var proposal = await AddProposalAsync(store);
        await store.SaveProposalsAsync([proposal], default);
        var memories = CreateMemories(fixture);
        var forgotten = await memories.RememberAsync("Forget this global preference.", true, default);
        var retained = await memories.RememberAsync("Keep this approved project fact.", false, default);
        await fixture.ScalarAsync("""
            INSERT INTO learning_observations(id, scope_key, session_id, body, created_unix)
            VALUES($id, $scope, $session, 'Another project observation.', $now);
            """, ("$id", Guid.NewGuid().ToString("N")), ("$scope", new string('F', 64)),
            ("$session", Guid.NewGuid().ToString("N")), ("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        var revision = await store.RevisionAsync(default);

        Assert.True(await memories.ForgetAsync(forgotten.Id, default, forgotten));
        await store.CaptureAsync(Guid.NewGuid().ToString("N"), "Late old observation.", default, revision);

        Assert.Equal(retained, Assert.Single(await memories.ListAsync(default)));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_observations;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM memory_proposals;"));
    }

    /// <summary>One failed merge rolls back source deletion and proposal claim, allowing a corrected reviewed merge.</summary>
    [Fact]
    public async Task Approve_MergeDuplicate_RollsBackEveryWrite()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var memories = CreateMemories(fixture);
        var first = await memories.RememberAsync("First merge source.", false, default);
        var second = await memories.RememberAsync("Second merge source.", false, default);
        var duplicate = await memories.RememberAsync("Existing separate memory.", false, default);
        var proposal = Proposal("merge", [], [first, second]);
        await store.SaveProposalsAsync([proposal], default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApproveAsync(proposal, duplicate.Text, false, default));

        Assert.Equal(3, (await memories.ListAsync(default)).Count);
        Assert.Equal(proposal.Id, Assert.Single(await store.ProposalsAsync(default)).Id);
        await store.ApproveAsync(proposal, "Combined reviewed memory.", false, default);
        Assert.Equal(2, (await memories.ListAsync(default)).Count);
        Assert.Empty(await store.ProposalsAsync(default));
    }

    /// <summary>Modified target snapshots cannot be approved and automatically leave the pending review queue.</summary>
    [Fact]
    public async Task Approve_StaleTarget_FailsAndExpiresPendingProposal()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var memories = CreateMemories(fixture);
        var original = await memories.RememberAsync("Original target.", false, default);
        var proposal = Proposal("archive", [], [original]);
        await store.SaveProposalsAsync([proposal], default);
        var updated = await memories.UpdateAsync(original.Id, "User changed this target.", false, default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApproveAsync(proposal, proposal.Text, false, default));

        Assert.Equal(updated, Assert.Single(await memories.ListAsync(default)));
        Assert.Empty(await store.ProposalsAsync(default));
        Assert.Equal("expired", await fixture.ScalarAsync("SELECT status FROM memory_proposals WHERE id = $id;", ("$id", proposal.Id)));
    }

    /// <summary>Rejected suggestions stay suppressed when the model changes only explanation, casing, or target order.</summary>
    [Fact]
    public async Task SaveProposals_ReorderedRejectedDuplicate_DoesNotResuggest()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var memories = CreateMemories(fixture);
        var first = await memories.RememberAsync("First target.", false, default);
        var second = await memories.RememberAsync("Second target.", false, default);
        var proposal = Proposal("merge", [], [first, second]);
        await store.SaveProposalsAsync([proposal], default);
        await store.RejectAsync(proposal, default);

        var added = await store.SaveProposalsAsync([proposal with
        {
            Id = Guid.NewGuid().ToString("N"), Text = "  " + proposal.Text.ToUpperInvariant() + "  ",
            Targets = [second, first], Rationale = "Changed explanation."
        }], default);

        Assert.Equal(0, added);
        Assert.Empty(await store.ProposalsAsync(default));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM memory_proposals;"));
    }

    /// <summary>Expiring one source deletes its derived proposal rather than leaving an unreviewable pending item.</summary>
    [Fact]
    public async Task Retention_ExpiredSource_DeletesDependentProposal()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var proposal = await AddProposalAsync(store);
        await store.SaveProposalsAsync([proposal], default);
        await fixture.ScalarAsync("UPDATE learning_observations SET created_unix = $expired WHERE id = $id;",
            ("$expired", DateTimeOffset.UtcNow.AddDays(-31).ToUnixTimeSeconds()), ("$id", proposal.SourceIds[0]));

        Assert.Empty(await store.ProposalsAsync(default));
        Assert.Single(await store.ObservationsAsync(default));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM memory_proposals;"));
    }

    /// <summary>Malformed model fields fail with a bounded application error before saving any row in a batch.</summary>
    [Fact]
    public async Task SaveProposals_NullTargetOrExpiredTimestamp_RejectsWholeBatch()
    {
        using var fixture = new RegressionFixture();
        var store = await PrepareAsync(fixture);
        var proposal = await AddProposalAsync(store);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveProposalsAsync(
            [proposal, proposal with { Id = Guid.NewGuid().ToString("N"), Operation = "archive", Targets = [null!] }], default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveProposalsAsync(
            [proposal with { CreatedAt = DateTimeOffset.UtcNow.AddDays(-31) }], default));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM memory_proposals;"));
    }

    /// <summary>Initializes a disposable database with explicitly enabled capture and no provider or process calls.</summary>
    private static async Task<ExperimentalStore> PrepareAsync(RegressionFixture fixture)
    {
        await fixture.Database.InitializeAsync(default);
        var store = CreateStore(fixture);
        await store.SaveSettingsAsync(new(Enabled: true, CaptureObservations: true), await store.SettingsAsync(default), default);
        return store;
    }

    /// <summary>Creates an isolated experimental store using production redaction and localization.</summary>
    private static ExperimentalStore CreateStore(RegressionFixture fixture) =>
        new(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService());

    /// <summary>Creates the real memory persistence service without real user data or logging sinks.</summary>
    private static PersistentMemoryService CreateMemories(RegressionFixture fixture) =>
        new(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService(), NullLogger<PersistentMemoryService>.Instance);

    /// <summary>Captures independent-session synthetic evidence for one valid memory proposal.</summary>
    private static async Task<MemoryProposal> AddProposalAsync(ExperimentalStore store)
    {
        await store.CaptureAsync(Guid.NewGuid().ToString("N"), "Prefer short explanations.", default);
        await store.CaptureAsync(Guid.NewGuid().ToString("N"), "Keep explanations concise.", default);
        return Proposal("add", (await store.ObservationsAsync(default)).Select(source => source.Id).ToArray(), []);
    }

    /// <summary>Builds a bounded valid proposal with the supplied operation and immutable evidence snapshots.</summary>
    private static MemoryProposal Proposal(string operation, IReadOnlyList<string> sources, IReadOnlyList<PersistentMemory> targets) =>
        new(Guid.NewGuid().ToString("N"), "preference", operation, "Prefer concise explanations.", "Consistent reviewed evidence.",
            sources, targets, DateTimeOffset.UtcNow);
}

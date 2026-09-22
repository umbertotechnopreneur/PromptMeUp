// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SettingsFeatureSaveTests
{
    /// <summary>Reading and editing a discarded settings draft leaves project consent and package approvals untouched.</summary>
    [Fact]
    public async Task DraftWithoutSave_DoesNotMutatePreferencesOrApprovals()
    {
        using var fixture = new RegressionFixture();
        var (_, catalog, service) = await PrepareAsync(fixture);
        var skill = Package(fixture, catalog, "save-cancel");
        var overview = await service.ReadAsync(default);
        var state = Assert.Single(overview.Skills, item => item.Skill.Name == skill.Name);
        var discarded = new SettingsFeatureChanges(overview.Settings, new(Enabled: true, AutomaticSkills: true), [Change(state, true)]);

        Assert.True(discarded.Settings.Enabled);
        Assert.Equal(new SkillsAndMemorySettings(), (await service.ReadAsync(default)).Settings);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skills_and_memory_settings;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_revisions;"));
    }

    /// <summary>One save persists reviewed feature preferences and multiple exact package approvals without running any action.</summary>
    [Fact]
    public async Task Save_MultipleEdits_PersistsOneReviewedDraft()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var first = Package(fixture, catalog, "save-first");
        var second = Package(fixture, catalog, "save-second");
        var overview = await service.ReadAsync(default);
        var next = new SkillsAndMemorySettings(Enabled: true, AutomaticSkills: true, MaintenanceReminder: true);
        var changes = overview.Skills.Where(state => state.Skill.Name == first.Name || state.Skill.Name == second.Name)
            .Select(state => Change(state, true)).ToArray();

        await service.SaveAsync(new(overview.Settings, next, changes), default);

        Assert.Equal(next, await store.SettingsAsync(default));
        Assert.Equal(first.Fingerprint, await store.GetAsync("skill:" + first.Name, default));
        Assert.Equal(second.Fingerprint, await store.GetAsync("skill:" + second.Name, default));
        Assert.Equal(2, (await service.ReadAsync(default)).EnabledSkillCount);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM activity_audit;"));
    }

    /// <summary>A concurrent package change rejects the whole draft, including a privacy switch and an earlier valid skill edit.</summary>
    [Fact]
    public async Task Save_StaleApproval_RollsBackAllPreferenceAndSkillEdits()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var first = Package(fixture, catalog, "save-atomic-first");
        var second = Package(fixture, catalog, "save-atomic-second");
        var overview = await service.ReadAsync(default);
        var changes = overview.Skills.Where(state => state.Skill.Name == first.Name || state.Skill.Name == second.Name)
            .Select(state => Change(state, true)).ToArray();
        await store.SetAsync("skill:" + second.Name, string.Empty, default);
        var revision = await store.RevisionAsync(default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(
            new(overview.Settings, new(Enabled: true, CaptureObservations: true), changes, CaptureConsent: true), default));

        Assert.Equal(overview.Settings, await store.SettingsAsync(default));
        Assert.Null(await store.GetAsync("skill:" + first.Name, default));
        Assert.Equal(string.Empty, await store.GetAsync("skill:" + second.Name, default));
        Assert.Equal(revision, await store.RevisionAsync(default));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skills_and_memory_settings;"));
    }

    /// <summary>A stale settings screen cannot restore capture consent that another instance has revoked.</summary>
    [Fact]
    public async Task Save_StalePreferences_DoesNotEnableAnyDraftSkill()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        await store.SaveSettingsAsync(new(Enabled: true, CaptureObservations: true), new(), default);
        var skill = Package(fixture, catalog, "save-stale-settings");
        var overview = await service.ReadAsync(default);
        var state = Assert.Single(overview.Skills, item => item.Skill.Name == skill.Name);
        var current = overview.Settings with { CaptureObservations = false };
        await store.SaveSettingsAsync(current, overview.Settings, default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(new(overview.Settings,
            overview.Settings with { MaintenanceReminder = true }, [Change(state, true)]), default));

        Assert.Equal(current, await store.SettingsAsync(default));
        Assert.Null(await store.GetAsync("skill:" + skill.Name, default));
    }

    /// <summary>Capture requires fresh acknowledgement whenever effective capture starts, including latent flags behind a disabled master.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_NewEffectiveCapture_RequiresConsent(bool latentCapture)
    {
        using var fixture = new RegressionFixture();
        var (store, _, service) = await PrepareAsync(fixture);
        var previous = new SkillsAndMemorySettings(CaptureObservations: latentCapture, AutomaticSkills: true);
        await store.SaveSettingsAsync(previous, new(), default);
        var next = previous with { Enabled = true, CaptureObservations = true };
        var revision = await store.RevisionAsync(default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(new(previous, next, []), default));

        Assert.Equal(previous, await store.SettingsAsync(default));
        Assert.Equal(revision, await store.RevisionAsync(default));
        await service.SaveAsync(new(previous, next, [], CaptureConsent: true), default);
        Assert.Equal(next, await store.SettingsAsync(default));
    }

    /// <summary>Disabling the master or capture requires acknowledgement before discarding any retained learning evidence.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_StopsLearning_RequiresConsentAndPurgesAtomically(bool disableMaster)
    {
        using var fixture = new RegressionFixture();
        var (store, _, service) = await PrepareAsync(fixture);
        var previous = new SkillsAndMemorySettings(Enabled: true, CaptureObservations: true, MaintenanceReminder: true);
        await store.SaveSettingsAsync(previous, new(), default);
        await store.CaptureAsync(Guid.NewGuid().ToString("N"), "A retained user preference.", default);
        var next = disableMaster ? previous with { Enabled = false } : previous with { CaptureObservations = false };
        var revision = await store.RevisionAsync(default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(new(previous, next, []), default));

        Assert.Equal(previous, await store.SettingsAsync(default));
        Assert.Single(await store.ObservationsAsync(default));
        Assert.Equal(revision, await store.RevisionAsync(default));
        await service.SaveAsync(new(previous, next, [], ClearLearningConsent: true), default);
        Assert.Equal(next, await store.SettingsAsync(default));
        Assert.Empty(await store.ObservationsAsync(default));
        Assert.NotEqual(revision, await store.RevisionAsync(default));
    }

    /// <summary>An unchanged disabled draft neither normalizes latent flags nor purges evidence or creates a new revision.</summary>
    [Fact]
    public async Task Save_UnchangedDisabledPreferences_PreservesFlagsAndRetainedRows()
    {
        using var fixture = new RegressionFixture();
        var (store, _, service) = await PrepareAsync(fixture);
        var settings = new SkillsAndMemorySettings(AutomaticSkills: true, CaptureObservations: true, MaintenanceReminder: true);
        await store.SaveSettingsAsync(settings, new(), default);
        await fixture.ScalarAsync("""
            INSERT INTO learning_observations(id, scope_key, session_id, body, created_unix)
            VALUES($id, $scope, $session, 'Synthetic retained row.', $now);
            """, ("$id", Guid.NewGuid().ToString("N")), ("$scope", PersistentMemoryService.ResolveProjectScope()),
            ("$session", Guid.NewGuid().ToString("N")), ("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        var serialized = await store.GetAsync("settings", default);
        var revision = await store.RevisionAsync(default);

        await service.SaveAsync(new(settings, settings, [], ClearLearningConsent: true), default);

        Assert.Equal(settings, await store.SettingsAsync(default));
        Assert.Equal(serialized, await store.GetAsync("settings", default));
        Assert.Equal(revision, await store.RevisionAsync(default));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_observations;"));
    }

    /// <summary>A package changed since inspection blocks every draft write before its old fingerprint can be approved.</summary>
    [Fact]
    public async Task Save_PackageDrift_RejectsWithoutPartialChanges()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var stable = Package(fixture, catalog, "save-drift-stable");
        var changed = Package(fixture, catalog, "save-drift-changed");
        var overview = await service.ReadAsync(default);
        var changes = overview.Skills.Where(state => state.Skill.Name == stable.Name || state.Skill.Name == changed.Name)
            .Select(state => Change(state, true)).ToArray();
        File.AppendAllText(Path.Combine(changed.Directory, "SKILL.md"), "\nNew instructions after inspection.");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(new(overview.Settings,
            new(Enabled: true, AutomaticSkills: true), changes), default));

        Assert.Equal(overview.Settings, await store.SettingsAsync(default));
        Assert.Null(await store.GetAsync("skill:" + stable.Name, default));
        Assert.Null(await store.GetAsync("skill:" + changed.Name, default));
    }

    /// <summary>Unsupported and over-budget instruction packages cannot acquire an approval through the settings save path.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_UnavailableOrOversizedPackage_RejectsBeforeWriting(bool oversized)
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var skill = Package(fixture, catalog, "save-unusable", oversized ? new string('a', 8000) : "Short instructions.",
            oversized ? string.Empty : "os: [synthetic-unsupported]\n");
        var overview = await service.ReadAsync(default);
        var state = Assert.Single(overview.Skills, item => item.Skill.Name == skill.Name);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(new(overview.Settings,
            new(Enabled: true), [Change(state, true)]), default));

        Assert.Null(await store.GetAsync("skill:" + skill.Name, default));
        Assert.Equal(overview.Settings, await store.SettingsAsync(default));
    }

    /// <summary>Revoking an exact stored approval remains possible even when the previously inspected package becomes malformed.</summary>
    [Fact]
    public async Task Save_DeactivateChangedPackage_DoesNotRequireReadableFiles()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var skill = Package(fixture, catalog, "save-revoke");
        await catalog.EnableAsync(skill, true, default);
        var overview = await service.ReadAsync(default);
        var state = Assert.Single(overview.Skills, item => item.Skill.Name == skill.Name);
        File.WriteAllText(Path.Combine(skill.Directory, "SKILL.md"), "The package is no longer valid YAML.");

        await service.SaveAsync(new(overview.Settings, overview.Settings, [Change(state, false)]), default);

        Assert.Equal(string.Empty, await store.GetAsync("skill:" + skill.Name, default));
        Assert.Equal(overview.Settings, await store.SettingsAsync(default));
    }

    /// <summary>Reapproving changed local content compares the outdated stored fingerprint, not merely a false enabled flag.</summary>
    [Fact]
    public async Task Save_ReapproveInspectedRevision_ReplacesExactOldFingerprint()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var old = Package(fixture, catalog, "save-reapprove");
        await catalog.EnableAsync(old, true, default);
        File.AppendAllText(Path.Combine(old.Directory, "SKILL.md"), "\nA short reviewed clarification.");
        var overview = await service.ReadAsync(default);
        var state = Assert.Single(overview.Skills, item => item.Skill.Name == old.Name);
        Assert.False(state.Enabled);
        Assert.Equal(old.Fingerprint, state.ApprovalFingerprint);

        await service.SaveAsync(new(overview.Settings, overview.Settings, [Change(state, true)]), default);

        Assert.NotEqual(old.Fingerprint, state.Skill.Fingerprint);
        Assert.Equal(state.Skill.Fingerprint, await store.GetAsync("skill:" + old.Name, default));
        Assert.False((await store.SettingsAsync(default)).Enabled);
    }

    /// <summary>Reenabling the master rechecks existing approvals rather than bypassing the package budget validation.</summary>
    [Fact]
    public async Task Save_MasterReactivation_RevalidatesExistingApprovals()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var skill = Package(fixture, catalog, "save-reactivate", new string('a', 8000));
        await store.SetAsync("skill:" + skill.Name, skill.Fingerprint, default);
        var overview = await service.ReadAsync(default);
        var state = Assert.Single(overview.Skills, item => item.Skill.Name == skill.Name);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(new(overview.Settings,
            new(Enabled: true), [Change(state, true)]), default));

        Assert.False((await store.SettingsAsync(default)).Enabled);
        Assert.Equal(skill.Fingerprint, await store.GetAsync("skill:" + skill.Name, default));
    }

    /// <summary>A catalog that became unreadable cannot be bypassed by submitting an empty skill snapshot during activation.</summary>
    [Fact]
    public async Task Save_MasterActivationMalformedCatalog_RejectsWithoutAnyWrites()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var malformed = Package(fixture, catalog, "save-malformed-activation");
        File.WriteAllText(Path.Combine(malformed.Directory, "SKILL.md"), "Malformed synthetic package.");
        var overview = await service.ReadAsync(default);
        Assert.True(overview.CatalogUnavailable);
        Assert.Empty(overview.Skills);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(
            new(overview.Settings, new(Enabled: true), []), default));

        Assert.Equal(overview.Settings, await store.SettingsAsync(default));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skills_and_memory_settings;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_revisions;"));
    }

    /// <summary>A damaged package never prevents disabling the project gate and clearing learning with explicit consent.</summary>
    [Fact]
    public async Task Save_MasterDisableMalformedCatalog_RemainsAvailable()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var previous = new SkillsAndMemorySettings(Enabled: true, CaptureObservations: true);
        await store.SaveSettingsAsync(previous, new(), default);
        await store.CaptureAsync(Guid.NewGuid().ToString("N"), "Synthetic captured preference.", default);
        var malformed = Package(fixture, catalog, "save-malformed-disable");
        File.WriteAllText(Path.Combine(malformed.Directory, "SKILL.md"), "Malformed synthetic package.");
        var overview = await service.ReadAsync(default);
        Assert.True(overview.CatalogUnavailable);
        var next = previous with { Enabled = false };

        await service.SaveAsync(new(overview.Settings, next, [], ClearLearningConsent: true), default);

        Assert.Equal(next, await store.SettingsAsync(default));
        Assert.Empty(await store.ObservationsAsync(default));
    }

    /// <summary>An existing exact package approval must be represented in the reviewed rows before a disabled master can reactivate it.</summary>
    [Fact]
    public async Task Save_MasterActivationOmittedApprovedPackage_RejectsWholeDraft()
    {
        using var fixture = new RegressionFixture();
        var (store, catalog, service) = await PrepareAsync(fixture);
        var approved = Package(fixture, catalog, "save-omitted-approved");
        var requested = Package(fixture, catalog, "save-requested");
        await catalog.EnableAsync(approved, true, default);
        var overview = await service.ReadAsync(default);
        var requestedState = Assert.Single(overview.Skills, state => state.Skill.Name == requested.Name);
        var revision = await store.RevisionAsync(default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(new(overview.Settings,
            new(Enabled: true), [Change(requestedState, true)]), default));

        Assert.Equal(overview.Settings, await store.SettingsAsync(default));
        Assert.Equal(revision, await store.RevisionAsync(default));
        Assert.Equal(approved.Fingerprint, await store.GetAsync("skill:" + approved.Name, default));
        Assert.Null(await store.GetAsync("skill:" + requested.Name, default));
    }

    /// <summary>A cancelled save fails before opening the local database or validating package files.</summary>
    [Fact]
    public async Task Save_Cancelled_DoesNotAccessUninitializedStorage()
    {
        using var fixture = new RegressionFixture();
        var store = new SkillsAndMemoryStore(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService());
        var catalog = Catalog(fixture, store);
        var service = new SettingsFeatureOverviewService(store, catalog);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SaveAsync(
            new(new(), new(), []), new CancellationToken(canceled: true)));

        Assert.False(File.Exists(fixture.Paths.DatabasePath));
    }

    /// <summary>Creates real settings persistence and catalog services against a fresh disposable database.</summary>
    private static async Task<(SkillsAndMemoryStore Store, SkillCatalogService Catalog, SettingsFeatureOverviewService Service)> PrepareAsync(RegressionFixture fixture)
    {
        await fixture.Database.InitializeAsync(default);
        var store = new SkillsAndMemoryStore(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService());
        var catalog = Catalog(fixture, store);
        return (store, catalog, new SettingsFeatureOverviewService(store, catalog));
    }

    /// <summary>Creates a catalog using only packaged local prompts and synthetic fixture files.</summary>
    private static SkillCatalogService Catalog(RegressionFixture fixture, SkillsAndMemoryStore store) =>
        new(fixture.Paths, store, new LocalizationService(), new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));

    /// <summary>Copies the exact original approval into a requested row edit.</summary>
    private static SettingsSkillChange Change(SettingsSkillState state, bool enabled) =>
        new(state.Skill, state.Enabled, enabled) { ExpectedApprovalFingerprint = state.ApprovalFingerprint };

    /// <summary>Writes a small inert package in the isolated local catalog without executables or network access.</summary>
    private static SkillDefinition Package(RegressionFixture fixture, SkillCatalogService catalog, string name,
        string instructions = "Use short explanations.", string metadata = "")
    {
        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"),
            $"---\nname: {name}\ndescription: Synthetic settings save package\nversion: 1.0.0\n{metadata}---\n{instructions}");
        return catalog.Inspect(directory);
    }
}

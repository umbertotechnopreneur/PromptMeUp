// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SettingsFeatureOverviewTests
{
    /// <summary>Reading a fresh installation preserves disabled defaults without creating preferences or skill directories.</summary>
    [Fact]
    public async Task Read_Defaults_DoesNotPersistConsent()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var store = Store(fixture);
        var catalog = Catalog(fixture, store);
        var service = new SettingsFeatureOverviewService(store, catalog);

        var overview = await service.ReadAsync(default);

        Assert.Equal(new ExperimentalSettings(), overview.Settings);
        Assert.False(overview.CatalogUnavailable);
        Assert.Equal(0, overview.EnabledSkillCount);
        Assert.Equal(catalog.List().Count, overview.SkillCount);
        Assert.True(overview.SkillCount > 0);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_revisions;"));
        Assert.False(Directory.Exists(Path.Combine(fixture.Paths.DataDirectory, "skills")));
    }

    /// <summary>One service instance refreshes settings, package approvals, and the catalog while respecting the master switch.</summary>
    [Fact]
    public async Task Read_ChangedSettingsAndCatalog_RefreshesEffectiveCounts()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var store = Store(fixture);
        var catalog = Catalog(fixture, store);
        var service = new SettingsFeatureOverviewService(store, catalog);
        var baseline = await service.ReadAsync(default);
        var first = catalog.Inspect(Package(fixture, "overview-first"));
        await catalog.EnableAsync(first, true, default);

        var masterOff = await service.ReadAsync(default);
        Assert.Equal(baseline.SkillCount + 1, masterOff.SkillCount);
        Assert.Equal(0, masterOff.EnabledSkillCount);
        Assert.True(await catalog.IsEnabledAsync(first, default));
        var retainedApproval = Assert.Single(masterOff.Skills, state => state.Skill.Name == first.Name);
        Assert.True(retainedApproval.Enabled);
        Assert.Equal(first.Fingerprint, retainedApproval.ApprovalFingerprint);

        var settings = new ExperimentalSettings(Enabled: true, MaintenanceReminder: true);
        await store.SaveSettingsAsync(settings, masterOff.Settings, default);
        var masterOn = await service.ReadAsync(default);
        Assert.Equal(settings, masterOn.Settings);
        Assert.Equal(1, masterOn.EnabledSkillCount);

        var second = catalog.Inspect(Package(fixture, "overview-second"));
        await catalog.EnableAsync(second, true, default);
        var expanded = await service.ReadAsync(default);
        Assert.Equal(baseline.SkillCount + 2, expanded.SkillCount);
        Assert.Equal(2, expanded.EnabledSkillCount);

        await catalog.EnableAsync(first, false, default);
        Assert.Equal(1, (await service.ReadAsync(default)).EnabledSkillCount);
        await store.SaveSettingsAsync(settings with { Enabled = false }, settings, default);
        var disabled = await service.ReadAsync(default);
        Assert.Equal(settings with { Enabled = false }, disabled.Settings);
        Assert.Equal(0, disabled.EnabledSkillCount);
        Assert.Equal(expanded.SkillCount, disabled.SkillCount);
        Assert.True(await catalog.IsEnabledAsync(second, default));
    }

    /// <summary>Changed fingerprints and unavailable platforms remain in the total but never count as effective approvals.</summary>
    [Fact]
    public async Task Read_ChangedOrUnsupportedPackage_ExcludesIneffectiveApprovals()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var store = Store(fixture);
        var catalog = Catalog(fixture, store);
        var service = new SettingsFeatureOverviewService(store, catalog);
        var baseline = await service.ReadAsync(default);
        await store.SaveSettingsAsync(new(Enabled: true), baseline.Settings, default);
        var stable = catalog.Inspect(Package(fixture, "overview-stable"));
        var changedDirectory = Package(fixture, "overview-changed");
        var changed = catalog.Inspect(changedDirectory);
        var unsupported = catalog.Inspect(Package(fixture, "overview-unavailable", "os: [synthetic-unsupported]\n"));
        await catalog.EnableAsync(stable, true, default);
        await catalog.EnableAsync(changed, true, default);
        await store.SetAsync("skill:" + unsupported.Name, unsupported.Fingerprint, default);
        File.AppendAllText(Path.Combine(changedDirectory, "SKILL.md"), "\nChanged after approval.");

        var overview = await service.ReadAsync(default);

        Assert.NotEqual(changed.Fingerprint, catalog.Inspect(changedDirectory).Fingerprint);
        Assert.NotNull(unsupported.UnavailableReason);
        Assert.Equal(baseline.SkillCount + 3, overview.SkillCount);
        Assert.Equal(1, overview.EnabledSkillCount);
        var unsupportedState = Assert.Single(overview.Skills, state => state.Skill.Name == unsupported.Name);
        Assert.True(unsupportedState.Enabled);
        Assert.Equal(unsupported.Fingerprint, unsupportedState.ApprovalFingerprint);
        Assert.Equal(changed.Fingerprint, await store.GetAsync("skill:" + changed.Name, default));
        Assert.Equal(unsupported.Fingerprint, await store.GetAsync("skill:" + unsupported.Name, default));
    }

    /// <summary>The overview leaves expired observations, preferences, revisions, and reflection output untouched.</summary>
    [Fact]
    public async Task Read_ExpiredObservation_DoesNotPruneOrRunLearning()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var store = Store(fixture);
        var settings = new ExperimentalSettings(Enabled: true, AutomaticSkills: true, CaptureObservations: true,
            MaintenanceReminder: true);
        await store.SaveSettingsAsync(settings, await store.SettingsAsync(default), default);
        await store.CaptureAsync(Guid.NewGuid().ToString("N"), "Synthetic retained preference.", default);
        var expired = DateTimeOffset.UtcNow.AddDays(-31).ToUnixTimeSeconds();
        await fixture.ScalarAsync("UPDATE learning_observations SET created_unix = $expired;", ("$expired", expired));
        var storedSettings = await store.GetAsync("settings", default);
        var revision = await store.RevisionAsync(default);
        var service = new SettingsFeatureOverviewService(store, Catalog(fixture, store));

        var first = await service.ReadAsync(default);
        var second = await service.ReadAsync(default);

        Assert.Equal(settings, first.Settings);
        Assert.Equal(first.Settings, second.Settings);
        Assert.Equal(first.EnabledSkillCount, second.EnabledSkillCount);
        Assert.Equal(first.SkillCount, second.SkillCount);
        Assert.Equal(first.CatalogUnavailable, second.CatalogUnavailable);
        Assert.Equal(first.Skills.Select(state => (state.Skill.Name, state.Skill.Fingerprint, state.Enabled, state.ApprovalFingerprint)),
            second.Skills.Select(state => (state.Skill.Name, state.Skill.Fingerprint, state.Enabled, state.ApprovalFingerprint)));
        Assert.Equal(0, first.EnabledSkillCount);
        Assert.Equal(storedSettings, await store.GetAsync("settings", default));
        Assert.Equal(revision, await store.RevisionAsync(default));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM learning_observations;"));
        Assert.Equal(expired, await fixture.ScalarAsync("SELECT created_unix FROM learning_observations;"));
        Assert.Equal("Synthetic retained preference.", await fixture.ScalarAsync("SELECT body FROM learning_observations;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM memory_proposals;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skill_reminders;"));
    }

    /// <summary>A cancelled request stops before accessing even an uninitialized local database.</summary>
    [Fact]
    public async Task Read_Cancelled_DoesNotAccessStorage()
    {
        using var fixture = new RegressionFixture();
        var store = Store(fixture);
        var service = new SettingsFeatureOverviewService(store, Catalog(fixture, store));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReadAsync(cancellation.Token));

        Assert.False(File.Exists(fixture.Paths.DatabasePath));
    }

    /// <summary>A malformed package cannot block settings, alter consent, or expose private error details with either master state.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Read_MalformedCatalog_PreservesPreferencesAndReportsUnavailable(bool enabled)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var store = Store(fixture);
        var settings = new ExperimentalSettings(Enabled: enabled, MaintenanceReminder: true);
        await store.SaveSettingsAsync(settings, await store.SettingsAsync(default), default);
        var storedSettings = await store.GetAsync("settings", default);
        var revision = await store.RevisionAsync(default);
        var directory = Package(fixture, "overview-private-package");
        File.WriteAllText(Path.Combine(directory, "SKILL.md"), "---\nname: [invalid]\n---\nSynthetic private package contents.");
        var logger = new RecordingLogger();
        var service = new SettingsFeatureOverviewService(store, Catalog(fixture, store), logger);

        var overview = await service.ReadAsync(default);

        Assert.Equal(settings, overview.Settings);
        Assert.True(overview.CatalogUnavailable);
        Assert.Equal(0, overview.EnabledSkillCount);
        Assert.Equal(0, overview.SkillCount);
        Assert.Equal(storedSettings, await store.GetAsync("settings", default));
        Assert.Equal(revision, await store.RevisionAsync(default));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM experimental_settings;"));
        var log = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal("Skill catalog unavailable (InvalidOperationException).", log.Message);
        Assert.Null(log.Exception);

        Package(fixture, "overview-private-package");
        var repaired = await service.ReadAsync(default);
        Assert.False(repaired.CatalogUnavailable);
        Assert.Equal(settings, repaired.Settings);
        Assert.True(repaired.SkillCount > 0);
    }

    /// <summary>Core settings storage failures remain errors instead of being mislabeled as unavailable catalog counts.</summary>
    [Fact]
    public async Task Read_UninitializedDatabase_PropagatesStorageFailure()
    {
        using var fixture = new RegressionFixture();
        var store = Store(fixture);
        var logger = new RecordingLogger();
        var service = new SettingsFeatureOverviewService(store, Catalog(fixture, store), logger);

        await Assert.ThrowsAsync<SqliteException>(() => service.ReadAsync(default));

        Assert.Empty(logger.Entries);
        Assert.False(File.Exists(fixture.Paths.DatabasePath));
    }

    /// <summary>Creates a project store backed only by the fixture's disposable database.</summary>
    private static ExperimentalStore Store(RegressionFixture fixture) =>
        new(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService());

    /// <summary>Creates the actual package catalog with local prompt files and no provider collaborator.</summary>
    private static SkillCatalogService Catalog(RegressionFixture fixture, ExperimentalStore store) =>
        new(fixture.Paths, store, new LocalizationService(),
            new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));

    /// <summary>Writes a small synthetic local package without executable actions or runtime dependencies.</summary>
    private static string Package(RegressionFixture fixture, string name, string metadata = "")
    {
        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"),
            $"---\nname: {name}\ndescription: Synthetic overview package\nversion: 1.0.0\n{metadata}---\nUse short explanations.");
        return directory;
    }

    private sealed class RecordingLogger : ILogger<SettingsFeatureOverviewService>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        /// <summary>Declines scopes because the test captures only emitted overview diagnostics.</summary>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>Enables every level so accidental sensitive diagnostics remain observable.</summary>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <summary>Captures messages and exception objects without forwarding private test data to an external sink.</summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }
}

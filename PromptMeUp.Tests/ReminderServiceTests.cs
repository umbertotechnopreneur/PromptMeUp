// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ReminderServiceTests
{
    /// <summary>Clock-only input schedules the next local occurrence while explicit dates retain their reviewed offset.</summary>
    [Theory]
    [InlineData("14:30", "2030-05-20T14:30:00+00:00")]
    [InlineData("09:00", "2030-05-21T09:00:00+00:00")]
    [InlineData("3pm", "2030-05-20T15:00:00+00:00")]
    [InlineData("3:30pm", "2030-05-20T15:30:00+00:00")]
    [InlineData("2030-05-20T14:30:00+02:00", "2030-05-20T14:30:00+02:00")]
    [InlineData("2030-05-20T14:30Z", "2030-05-20T14:30:00+00:00")]
    public void ParseTime_ValidInput_PreservesNextOccurrenceAndOffset(string input, string expected)
    {
        using var fixture = new RegressionFixture();
        var service = Create(fixture, new FixedClock());

        var due = service.ParseTime(input);

        Assert.Equal(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), due);
        Assert.Equal(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture).Offset, due.Offset);
    }

    /// <summary>Dates without offsets, past dates, culture-dependent dates, and relative input fail closed.</summary>
    [Theory]
    [InlineData("2030-05-20T14:30:00")]
    [InlineData("2030-05-19T14:30:00Z")]
    [InlineData("05/20/2030 14:30")]
    [InlineData("in 30 minutes")]
    [InlineData("25:00")]
    [InlineData("")]
    public void ParseTime_InvalidInput_IsRejected(string input)
    {
        using var fixture = new RegressionFixture();

        Assert.Throws<InvalidOperationException>(() => Create(fixture, new FixedClock()).ParseTime(input));
    }

    /// <summary>Daylight-saving gaps and overlaps require an explicit offset rather than silently selecting an instant.</summary>
    [Theory]
    [InlineData(3, 10, "02:30")]
    [InlineData(11, 3, "01:30")]
    public void ParseTime_DaylightSavingGapOrOverlap_IsRejected(int month, int day, string input)
    {
        using var fixture = new RegressionFixture();
        var daylight = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2020, 1, 1), new DateTime(2040, 12, 31),
            TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday));
        var zone = TimeZoneInfo.CreateCustomTimeZone("ReminderTestZone", TimeSpan.FromHours(-5), "Test zone", "Standard", "Daylight", [daylight]);
        var clock = new FixedClock { Now = new DateTimeOffset(2030, month, day, 0, 0, 0, TimeSpan.FromHours(-5)), Zone = zone };

        Assert.Throws<InvalidOperationException>(() => Create(fixture, clock).ParseTime(input));
    }

    /// <summary>Defaults cannot create reminders, and revoking activation pauses rather than consuming pending reminders.</summary>
    [Fact]
    public async Task Activation_RevokedOrDisabled_PausesDeliveryAndRejectsWrites()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var clock = new FixedClock();
        var service = Create(fixture, clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(clock.Now.AddMinutes(1), "Check the build.", default));
        var (store, catalog, skill) = await EnableAsync(fixture);
        var reminder = await service.CreateAsync(clock.Now.AddMinutes(1), "Check the build.", default);
        clock.Now = clock.Now.AddMinutes(2);

        await catalog.EnableAsync(skill, false, default);
        Assert.Empty(await DeliverAsync(service));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skill_reminders;"));
        await catalog.EnableAsync(skill, true, default);
        await store.SaveSettingsAsync(new(), await store.SettingsAsync(default), default);
        Assert.Empty(await DeliverAsync(service));
        await store.SaveSettingsAsync(new(Enabled: true), await store.SettingsAsync(default), default);

        Assert.Equal(reminder, Assert.Single(await DeliverAsync(service)));
        Assert.Empty(await DeliverAsync(service));
    }

    /// <summary>Pending reminders survive service restarts and are consumed exactly once without touching another project.</summary>
    [Fact]
    public async Task DeliverDue_RestartedService_ConsumesOnlyCurrentProjectOnce()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await EnableAsync(fixture);
        var clock = new FixedClock();
        var service = Create(fixture, clock);
        var due = await service.CreateAsync(clock.Now.AddMinutes(1), "Read the local result.", default);
        var future = await service.CreateAsync(clock.Now.AddHours(1), "Review later.", default);
        await fixture.ScalarAsync("""
            INSERT INTO skill_reminders(id, scope_key, message, due_unix_ms, offset_minutes)
            VALUES($id, $scope, 'Another project.', $due, 0);
            """, ("$id", Guid.NewGuid().ToString("N")), ("$scope", new string('F', 64)), ("$due", due.DueAt.ToUnixTimeMilliseconds()));
        clock.Now = clock.Now.AddMinutes(2);

        var reopened = Create(fixture, clock);
        Assert.Equal(due, Assert.Single(await DeliverAsync(reopened)));
        Assert.Empty(await DeliverAsync(reopened));
        Assert.Equal(future, Assert.Single(await reopened.ListAsync(default)));
        Assert.Equal(2L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skill_reminders;"));
    }

    /// <summary>Stored reminders are bounded, credential-free, and deleted only with the exact reviewed snapshot.</summary>
    [Fact]
    public async Task CreateAndCancel_ValidateNoteLimitAndExactSnapshot()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await EnableAsync(fixture);
        var clock = new FixedClock();
        var service = Create(fixture, clock);
        foreach (var note in new[] { "", new string('x', 501), "line\nbreak", "OPENAI_API_KEY=" + "sk-" + new string('x', 32) })
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(clock.Now.AddHours(1), note, default));
        }
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skill_reminders;"));
        var reminder = await service.CreateAsync(clock.Now.AddHours(1), "Reviewed note.", default);
        Assert.False(await service.CancelAsync(reminder with { Message = "Unreviewed note." }, default));
        Assert.True(await service.CancelAsync(reminder, default));
        Assert.False(await service.CancelAsync(reminder, default));
        for (var index = 0; index < 50; index++)
        {
            await service.CreateAsync(clock.Now.AddHours(1), "Pending " + index, default);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(clock.Now.AddHours(2), "One too many.", default));
        Assert.Equal(50L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skill_reminders;"));
    }

    /// <summary>An enabled local package cannot impersonate the bundled reminder service even with the same package name.</summary>
    [Fact]
    public async Task LocalOverride_CannotBorrowBuiltInReminderActions()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var (_, catalog, _) = await EnableAsync(fixture);
        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", "set_reminder");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "SKILL.md"),
            "---\nname: set_reminder\ndescription: Test-only local override.\n---\nLocal package contents.");
        var local = Assert.Single(catalog.List(), skill => skill.Name == "set_reminder");
        await catalog.EnableAsync(local, true, default);
        var clock = new FixedClock();
        var service = Create(fixture, clock);

        Assert.False(await service.IsAvailableAsync(default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(clock.Now.AddHours(1), "Must not persist.", default));
        Assert.Empty(await DeliverAsync(service));
    }

    /// <summary>A failed console display rolls back deletion so unshown notes survive until a later active chat prompt.</summary>
    [Fact]
    public async Task DeliverDue_FailedDisplay_DoesNotLosePendingReminders()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await EnableAsync(fixture);
        var clock = new FixedClock();
        var service = Create(fixture, clock);
        var first = await service.CreateAsync(clock.Now.AddMinutes(1), "First reminder.", default);
        var second = await service.CreateAsync(clock.Now.AddMinutes(2), "Second reminder.", default);
        clock.Now = clock.Now.AddMinutes(3);
        var shown = new List<Reminder>();

        await Assert.ThrowsAsync<IOException>(() => service.DeliverDueAsync(reminder =>
        {
            if (reminder.Id == second.Id)
            {
                throw new IOException("Synthetic display failure.");
            }
            shown.Add(reminder);
        }, default));

        Assert.Equal(first, Assert.Single(shown));
        Assert.Equal(2, (await service.ListAsync(default)).Count);
        Assert.Equal(new[] { first, second }, await DeliverAsync(service));
        Assert.Empty(await service.ListAsync(default));
    }

    /// <summary>Captures displayed reminders through the same delivery callback used by the interactive workflow.</summary>
    private static async Task<IReadOnlyList<Reminder>> DeliverAsync(ReminderService service)
    {
        var result = new List<Reminder>();
        await service.DeliverDueAsync(result.Add, default);
        return result;
    }

    /// <summary>Creates isolated application collaborators with a deterministic clock and no external calls.</summary>
    private static ReminderService Create(RegressionFixture fixture, FixedClock clock)
    {
        var text = new LocalizationService();
        var redactor = new SensitiveDataRedactor();
        var store = new SkillsAndMemoryStore(fixture.Paths, redactor, text);
        var catalog = new SkillCatalogService(fixture.Paths, store, text,
            new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));
        return new ReminderService(fixture.Paths, redactor, text, catalog, clock);
    }

    /// <summary>Explicitly enables the project and exact bundled package for one test database.</summary>
    private static async Task<(SkillsAndMemoryStore Store, SkillCatalogService Catalog, SkillDefinition Skill)> EnableAsync(RegressionFixture fixture)
    {
        var text = new LocalizationService();
        var store = new SkillsAndMemoryStore(fixture.Paths, new SensitiveDataRedactor(), text);
        var catalog = new SkillCatalogService(fixture.Paths, store, text,
            new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));
        var skill = Assert.Single(catalog.List(), item => item.Name == "set_reminder" && item.Origin == "bundled");
        await store.SaveSettingsAsync(new(Enabled: true), await store.SettingsAsync(default), default);
        await catalog.EnableAsync(skill, true, default);
        return (store, catalog, skill);
    }

    /// <summary>Exposes a mutable deterministic instant and timezone without scheduling callbacks.</summary>
    private sealed class FixedClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2030, 5, 20, 12, 0, 0, TimeSpan.Zero);
        internal TimeZoneInfo Zone { get; init; } = TimeZoneInfo.Utc;
        public override TimeZoneInfo LocalTimeZone => Zone;

        /// <summary>Returns the test-controlled instant without consulting the machine clock.</summary>
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    }
}

// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class PreferredNameSettingsTests
{
    /// <summary>Accepts optional Unicode names and ordinary nicknames while trimming and applying canonical normalization.</summary>
    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("  Jose\u0301  ", "José")]
    [InlineData("  Nguyễn \"Bin\"  ", "Nguyễn \"Bin\"")]
    [InlineData("Zoë Anne-Marie O’Neil", "Zoë Anne-Marie O’Neil")]
    [InlineData("小明", "小明")]
    [InlineData("ليلى", "ليلى")]
    [InlineData("Alex 🦊", "Alex 🦊")]
    [InlineData("<name>Captain</name>", "<name>Captain</name>")]
    public void Normalize_OptionalUnicodeName_PreservesPlainData(string? value, string expected)
    {
        Assert.Equal(expected, PreferredNamePolicy.Normalize(value, new SensitiveDataRedactor()));
    }

    /// <summary>Applies the eighty-unit limit after canonical normalization rather than rejecting valid decomposed input.</summary>
    [Fact]
    public void Normalize_LengthLimit_AppliesAfterTrimAndNormalization()
    {
        var redactor = new SensitiveDataRedactor();
        var decomposed = string.Concat(Enumerable.Repeat("e\u0301", PreferredNamePolicy.MaximumLength));

        Assert.Equal(80, PreferredNamePolicy.MaximumLength);
        Assert.Equal(new string('é', 80), PreferredNamePolicy.Normalize("  " + decomposed + "  ", redactor));
        Assert.Equal(string.Concat(Enumerable.Repeat("🦊", 40)), PreferredNamePolicy.Normalize(string.Concat(Enumerable.Repeat("🦊", 40)), redactor));
        Assert.Throws<ArgumentException>(() => PreferredNamePolicy.Normalize(new string('a', 81), redactor));
        Assert.Throws<ArgumentException>(() => PreferredNamePolicy.Normalize(string.Concat(Enumerable.Repeat("🦊", 41)), redactor));
    }

    /// <summary>Rejects controls even at trimmed edges, Unicode formatting and separators, and malformed surrogate sequences without echoing input.</summary>
    [Fact]
    public void Normalize_UnsafeUnicode_RejectsWithGenericError()
    {
        string[] invalid =
        [
            "\nNick", "Nick\n", "\tNick", "Nick\t", "Nick\r\n", "Nick\u001b[2J", "Nick\0", "Nick\u007f",
            "Nick\u0085", "Nick\u2028", "Nick\u2029", "Nick\u200b", "Nick\u200d", "Nick\u202e",
            "Nick\u2066", "Nick\uFEFF", "\ud800", "Nick\udc00", "\ud800x", "x\ud800\ud800"
        ];
        var redactor = new SensitiveDataRedactor();
        var generic = Assert.Throws<ArgumentException>(() => PreferredNamePolicy.Normalize(new string('a', 81), redactor));

        foreach (var value in invalid)
        {
            var error = Assert.Throws<ArgumentException>(() => PreferredNamePolicy.Normalize(value, redactor));

            Assert.Equal(generic.Message, error.Message);
            Assert.Null(error.InnerException);
            Assert.DoesNotContain(value, error.ToString(), StringComparison.Ordinal);
        }
    }

    /// <summary>Rejects recognizable synthetic credential forms rather than converting them into saved display names.</summary>
    [Fact]
    public void Normalize_RecognizableCredentials_RejectsWithoutDisclosingValue()
    {
        string[] values = ["password=synthetic-value", "api_key=synthetic-value", "sk-" + new string('x', 32)];
        foreach (var value in values)
        {
            var error = Assert.Throws<ArgumentException>(() => PreferredNamePolicy.Normalize(value, new SensitiveDataRedactor()));

            Assert.DoesNotContain(value, error.ToString(), StringComparison.Ordinal);
            Assert.Null(error.InnerException);
        }
    }

    /// <summary>Adds an empty name and upgrades a legacy database while preserving its preferences, preamble, and notes.</summary>
    [Fact]
    public async Task Database_LegacySettings_AddEmptyNameIdempotently()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        Assert.Empty(AppSettings.Default.PreferredName);
        Assert.Empty((await fixture.Database.LoadSettingsAsync(default)).PreferredName);
        await fixture.Database.SaveSettingsAsync(AppSettings.Default with
        {
            SetupCompleted = true,
            Language = "fr",
            Theme = "green",
            ContextTokenBudget = 24_000,
            CustomInstruction = "Keep this existing preamble."
        }, default);
        var original = await fixture.Database.LoadSettingsAsync(default);
        await fixture.ScalarAsync("""
            ALTER TABLE app_settings DROP COLUMN preferred_name;
            INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
            VALUES ($id, 'global', 'Keep this saved note.', 1);
            PRAGMA user_version = 3;
            """, ("$id", Guid.NewGuid().ToString("N")));
        Assert.Equal(3L, await fixture.ScalarAsync("PRAGMA user_version;"));

        await fixture.Database.InitializeAsync(default);
        await fixture.Database.InitializeAsync(default);

        Assert.Equal(original, await fixture.Database.LoadSettingsAsync(default));
        Assert.Equal(string.Empty, await fixture.ScalarAsync("SELECT preferred_name FROM app_settings;"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM pragma_table_info('app_settings') WHERE name = 'preferred_name';"));
        Assert.Equal("Keep this saved note.", await fixture.ScalarAsync("SELECT body FROM persistent_memories;"));
        Assert.Equal(5L, await fixture.ScalarAsync("PRAGMA user_version;"));
    }

    /// <summary>Persists only the normalized name, survives reinitialization, and clears it without altering other settings.</summary>
    [Fact]
    public async Task Database_Name_RoundTripsAndClearsIndependently()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = await fixture.Database.LoadSettingsAsync(default);

        await fixture.Database.SaveSettingsAsync(original with { PreferredName = "  Jose\u0301  " }, default);
        await fixture.Database.InitializeAsync(default);

        Assert.Equal(original with { PreferredName = "José" }, await fixture.Database.LoadSettingsAsync(default));
        Assert.Equal("José", await fixture.ScalarAsync("SELECT preferred_name FROM app_settings;"));
        await fixture.Database.SaveSettingsAsync(original with { PreferredName = "   " }, default);
        Assert.Equal(original, await fixture.Database.LoadSettingsAsync(default));
        Assert.Equal(string.Empty, await fixture.ScalarAsync("SELECT preferred_name FROM app_settings;"));
    }

    /// <summary>Rejects invalid or secret-bearing names before any stored setting can be overwritten.</summary>
    [Fact]
    public async Task Database_InvalidName_PreservesSavedSettings()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = (await fixture.Database.LoadSettingsAsync(default)) with { PreferredName = "Captain" };
        await fixture.Database.SaveSettingsAsync(original, default);
        string[] invalid = ["Nick\n", "Nick\u202e", "Nick\ud800", new string('a', 81), "password=synthetic-value"];

        foreach (var value in invalid)
        {
            var error = await Assert.ThrowsAsync<ArgumentException>(() => fixture.Database.SaveSettingsAsync(
                original with { PreferredName = value, CustomInstruction = "Must not replace the old preamble." }, default));

            Assert.DoesNotContain(value, error.ToString(), StringComparison.Ordinal);
            Assert.Equal(original, await fixture.Database.LoadSettingsAsync(default));
        }
    }

    /// <summary>Fails closed on unsafe names inserted outside the application instead of returning or silently repairing them.</summary>
    [Theory]
    [InlineData("Nick\n")]
    [InlineData("Nick\u200b")]
    [InlineData("password=synthetic-value")]
    public async Task Database_LoadUnsafeName_RejectsWithoutEchoOrWrite(string value)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.ScalarAsync("UPDATE app_settings SET preferred_name = $name;", ("$name", value));

        var error = await Assert.ThrowsAsync<ArgumentException>(() => fixture.Database.LoadSettingsAsync(default));

        Assert.DoesNotContain(value, error.ToString(), StringComparison.Ordinal);
        Assert.Equal(value, await fixture.ScalarAsync("SELECT preferred_name FROM app_settings;"));
    }

    /// <summary>Normalizes safe externally supplied names before use without interpreting name text as a preamble.</summary>
    [Fact]
    public async Task Database_LoadCanonicalName_PreservesExistingPreamble()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.ScalarAsync("UPDATE app_settings SET preferred_name = $name, custom_instruction = $preamble;",
            ("$name", "  Jose\u0301  "), ("$preamble", "Keep answers concise."));

        var loaded = await fixture.Database.LoadSettingsAsync(default);

        Assert.Equal("José", loaded.PreferredName);
        Assert.Equal("Keep answers concise.", loaded.CustomInstruction);
    }

    /// <summary>Keeps a saved name out of initialization logs and ordinary session audit metadata.</summary>
    [Fact]
    public async Task Database_Name_DoesNotEnterLogsOrSessionAudit()
    {
        using var fixture = new RegressionFixture();
        var logger = new RecordingLogger();
        var database = new SqliteDatabaseService(fixture.Paths, logger, new PromptInjectionProtectionService(), new SensitiveDataRedactor());
        await database.InitializeAsync(default);
        const string name = "SyntheticPrivateNickname42";
        await database.SaveSettingsAsync((await database.LoadSettingsAsync(default)) with { PreferredName = name }, default);
        var settings = await database.LoadSettingsAsync(default);
        var audit = new ActivityAuditService(database, new SensitiveDataRedactor());

        await audit.StartSessionAsync(Guid.NewGuid().ToString("N"), "chat", settings, null, default);

        Assert.All(logger.Messages, message => Assert.DoesNotContain(name, message, StringComparison.Ordinal));
        Assert.DoesNotContain(name, Assert.IsType<string>(await fixture.ScalarAsync("SELECT metadata_json FROM ai_sessions;")), StringComparison.Ordinal);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT (SELECT COUNT(*) FROM activity_audit) + (SELECT COUNT(*) FROM ai_session_events);"));
    }

    private sealed class RecordingLogger : ILogger<SqliteDatabaseService>
    {
        public List<string> Messages { get; } = [];

        /// <summary>Declines logger scopes because the test records only emitted database messages.</summary>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>Enables all log levels so accidental name disclosure remains observable.</summary>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <summary>Records formatted log content without forwarding it to any external sink.</summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}

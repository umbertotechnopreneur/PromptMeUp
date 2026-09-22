// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ThemeSettingsTests
{
    /// <summary>Upgrades settings that predate themes while preserving all preferences and explicit memories.</summary>
    [Fact]
    public async Task Database_VersionTwoMigration_AddsCyanAndPreservesExistingData()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.SaveSettingsAsync(AppSettings.Default with
        {
            SetupCompleted = true,
            Language = "fr",
            MaxConversationTurns = 9,
            ContextTokenBudget = 24_000,
            ReviewCommandsWithAi = false,
            CustomInstruction = "Prefer concise answers."
        }, default);
        var original = await fixture.Database.LoadSettingsAsync(default);
        await fixture.ScalarAsync("""
            ALTER TABLE app_settings DROP COLUMN theme;
            PRAGMA user_version = 2;
            INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
            VALUES ($id, 'global', 'Keep this note after upgrading.', 1);
            """, ("$id", Guid.NewGuid().ToString("N")));

        await fixture.Database.InitializeAsync(default);
        await fixture.Database.InitializeAsync(default);
        var migrated = await fixture.Database.LoadSettingsAsync(default);

        Assert.Equal(original with { Theme = "cyan" }, migrated);
        Assert.Equal("Keep this note after upgrading.", await fixture.ScalarAsync("SELECT body FROM persistent_memories;"));
        Assert.Equal(8L, await fixture.ScalarAsync("PRAGMA user_version;"));
    }

    /// <summary>Persists a selected theme through reinitialization without changing any unrelated setting.</summary>
    [Fact]
    public async Task Database_ThemeChoice_RoundTripsWithoutChangingOtherPreferences()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = await fixture.Database.LoadSettingsAsync(default);
        var selected = original with { Theme = "green" };

        await fixture.Database.SaveSettingsAsync(selected, default);
        await fixture.Database.InitializeAsync(default);
        var reloaded = await fixture.Database.LoadSettingsAsync(default);

        Assert.Equal(selected, reloaded);
        Assert.Equal("green", await fixture.ScalarAsync("SELECT theme FROM app_settings;"));
    }

    /// <summary>Invalid theme identifiers fail before overwriting the persisted preference.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("Green")]
    [InlineData("../green")]
    [InlineData("green blue")]
    public async Task Database_InvalidThemeIdentifier_PreservesSavedSettings(string id)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = await fixture.Database.LoadSettingsAsync(default);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Database.SaveSettingsAsync(original with { Theme = id }, default));

        Assert.Equal(original, await fixture.Database.LoadSettingsAsync(default));
    }

    /// <summary>Routes the standalone theme chooser without requiring an additional value.</summary>
    [Fact]
    public void Parse_ThemeWithoutValue_SelectsTheThemeChooser()
    {
        var result = new CommandLineParser(new LocalizationService()).Parse(["--theme"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Theme, result.Options!.Command);
        Assert.Null(result.Options.Query);
    }

    /// <summary>Keeps the theme chooser separate from conflicting commands and positional text.</summary>
    [Theory]
    [InlineData("--setup")]
    [InlineData("--ai-settings")]
    [InlineData("--status")]
    [InlineData("green")]
    public void Parse_ThemeWithConflictingArgument_RejectsAmbiguousInvocation(string argument)
    {
        var result = new CommandLineParser(new LocalizationService()).Parse(["--theme", argument]);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }
}

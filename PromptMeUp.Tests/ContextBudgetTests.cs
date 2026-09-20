// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Services.OpenAi;

namespace PromptMeUp.Tests;

public sealed class ContextBudgetTests
{
    /// <summary>A smaller request budget removes complete old turns while preserving newer messages in their original order.</summary>
    [Fact]
    public void SetTokenBudget_SmallerRemainingContext_PrunesWholeTurns()
    {
        var memory = new ConversationMemoryService().Create(AppSettings.Default);
        memory.Add("user", "First question");
        memory.Add("assistant", new string('x', 200));
        memory.Add("user", "Second question");
        memory.Add("assistant", "Second answer");
        memory.Add("user", "Third question");
        ChatMessage[] expected = [new("user", "Second question"), new("assistant", "Second answer"), new("user", "Third question")];
        var remaining = ContextTokenEstimator.Messages(expected);

        var update = memory.SetTokenBudget(remaining);

        Assert.Equal(2, update.PrunedMessages);
        Assert.Equal(expected, update.Snapshot.Messages);
        Assert.Equal(remaining, update.Snapshot.EstimatedTokens);
        Assert.Equal(remaining, update.Snapshot.TokenBudget);
    }

    /// <summary>A fully consumed instruction budget clears active turns and rejects another message without exceeding the ceiling.</summary>
    [Fact]
    public void SetTokenBudget_ZeroRemainingSpace_ClearsAndRejectsNewInput()
    {
        var memory = new ConversationMemoryService().Create(AppSettings.Default);
        memory.Add("user", "Question");
        memory.Add("assistant", "Answer");

        var update = memory.SetTokenBudget(0);

        Assert.Equal(2, update.PrunedMessages);
        Assert.Empty(update.Snapshot.Messages);
        Assert.Throws<ConversationLimitException>(() => memory.Add("user", "Another question"));
        Assert.Empty(memory.Snapshot().Messages);
    }

    /// <summary>Invalid negative context budgets fail before changing retained conversation messages.</summary>
    [Fact]
    public void SetTokenBudget_NegativeValue_LeavesContextUntouched()
    {
        var memory = new ConversationMemoryService().Create(AppSettings.Default);
        memory.Add("user", "Question");

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.SetTokenBudget(-1));
        Assert.Equal("Question", Assert.Single(memory.Snapshot().Messages).Content);
    }

    /// <summary>Chat and query requests honor the persisted operating budget and an explicit environment override.</summary>
    [Theory]
    [InlineData("chat-system")]
    [InlineData("query-system")]
    public void ResolveInputBudget_OrdinaryConversation_UsesConfiguredAbsoluteCeiling(string promptId)
    {
        var settings = AppSettings.Default with { ContextTokenBudget = 8_000 };
        var prompt = CreatePrompt(promptId);

        Assert.Equal(8_000, OpenAiRequestBuilder.ResolveInputBudget(prompt, settings, 1_800, ConversationContextLimits.Default));
        Assert.Equal(12_000, OpenAiRequestBuilder.ResolveInputBudget(prompt, settings, 1_800, new ConversationContextLimits(12_000)));
    }

    /// <summary>The model window always reserves the requested output even when the operating budget would allow more input.</summary>
    [Fact]
    public void ResolveInputBudget_LimitedModelSpace_ReservesOutput()
    {
        var settings = AppSettings.Default with { MaxContextPercent = 95, ContextTokenBudget = 16_000 };
        var window = AiModelCatalog.Resolve(settings.Model).ContextWindowTokens;
        var output = checked((int)(window - 4_000));

        var budget = OpenAiRequestBuilder.ResolveInputBudget(CreatePrompt("chat-system"), settings, output, ConversationContextLimits.Default);

        Assert.Equal(4_000, budget);
        Assert.Equal(window, budget + output);
    }

    /// <summary>The ordinary conversation cap does not replace independently sized artifact output and context limits.</summary>
    [Fact]
    public void ResolveInputBudget_ArtifactPrompt_PreservesSeparateLimit()
    {
        var settings = AppSettings.Default with { Model = "gpt-5.4-mini", MaxContextPercent = 95, ContextTokenBudget = 4_000 };
        var window = AiModelCatalog.Resolve(settings.Model).ContextWindowTokens;
        const int output = 65_536;

        var budget = OpenAiRequestBuilder.ResolveInputBudget(CreatePrompt("script-system"), settings, output, new ConversationContextLimits(4_000));

        Assert.Equal(Math.Min(window * 95 / 100, window - output), budget);
        Assert.True(budget > settings.ContextTokenBudget);
    }

    /// <summary>Missing environment configuration leaves the persisted user setting in charge.</summary>
    [Fact]
    public void Configuration_UnsetEnvironment_PreservesSettingsDefault()
    {
        var limits = ContextBudgetConfiguration.Load(_ => null, new LocalizationService());

        Assert.Null(limits.MaxInputTokens);
        Assert.Equal(16_000, AppSettings.Default.ContextTokenBudget);
    }

    /// <summary>Out-of-range, malformed, blank, and padded environment values fail explicitly without fallback.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("3999")]
    [InlineData("200001")]
    [InlineData("-1")]
    [InlineData("4,000")]
    [InlineData("4000 ")]
    [InlineData("")]
    [InlineData("invalid")]
    public void Configuration_InvalidOverride_FailsExplicitly(string value) =>
        Assert.Throws<InvalidOperationException>(() => ContextBudgetConfiguration.Load(_ => value, new LocalizationService()));

    /// <summary>Both configured boundaries and the ordinary default are accepted without rewriting the chosen value.</summary>
    [Theory]
    [InlineData("4000", 4_000)]
    [InlineData("16000", 16_000)]
    [InlineData("200000", 200_000)]
    public void Configuration_ValidOverride_UsesExactValue(string value, int expected)
    {
        var limits = ContextBudgetConfiguration.Load(_ => value, new LocalizationService());

        Assert.Equal(expected, limits.MaxInputTokens);
    }

    /// <summary>A version-one database gains the default context budget without losing existing preferences or explicit memories.</summary>
    [Fact]
    public async Task Database_VersionOneMigration_PreservesSettingsAndNotes()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.SaveSettingsAsync(AppSettings.Default with
        {
            Language = "fr",
            MaxConversationTurns = 9,
            CustomInstruction = "Prefer concise answers."
        }, default);
        await fixture.ScalarAsync("""
            ALTER TABLE app_settings DROP COLUMN context_token_budget;
            PRAGMA user_version = 1;
            INSERT INTO persistent_memories (id, scope_key, body, updated_unix)
            VALUES ($id, 'global', 'Preserve this explicit note.', 1);
            """, ("$id", Guid.NewGuid().ToString("N")));

        await fixture.Database.InitializeAsync(default);
        await fixture.Database.InitializeAsync(default);
        var migrated = await fixture.Database.LoadSettingsAsync(default);

        Assert.Equal(16_000, migrated.ContextTokenBudget);
        Assert.Equal("fr", migrated.Language);
        Assert.Equal(9, migrated.MaxConversationTurns);
        Assert.Equal("Prefer concise answers.", migrated.CustomInstruction);
        Assert.Equal("Preserve this explicit note.", await fixture.ScalarAsync("SELECT body FROM persistent_memories;"));
        Assert.Equal(5L, await fixture.ScalarAsync("PRAGMA user_version;"));
    }

    /// <summary>Valid budgets round-trip through settings while invalid values leave the saved budget unchanged.</summary>
    [Fact]
    public async Task Database_ContextBudget_RoundTripsAndRejectsInvalidValues()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var settings = AppSettings.Default with { ContextTokenBudget = 24_000 };
        await fixture.Database.SaveSettingsAsync(settings, default);

        Assert.Equal(24_000, (await fixture.Database.LoadSettingsAsync(default)).ContextTokenBudget);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Database.SaveSettingsAsync(settings with { ContextTokenBudget = 200_001 }, default));
        Assert.Equal(24_000, (await fixture.Database.LoadSettingsAsync(default)).ContextTokenBudget);
    }

    /// <summary>Creates a minimal prompt definition without transport or external prompt dependencies.</summary>
    private static PromptDefinition CreatePrompt(string id) => new(id, 1, "Synthetic context regression", [],
        new Dictionary<string, string> { ["en"] = "Answer the question." }, new Dictionary<string, string>());
}

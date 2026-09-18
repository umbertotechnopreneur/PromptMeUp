// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SkillContextBudgetTests
{
    private const string Markup = "<tag attr=\"value\">content</tag>";

    /// <summary>Rejects oversized plain and escaped instructions in every language before saving their activation fingerprints.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public async Task Enable_OversizedInstructions_ReportsLocalizedErrorWithoutPersisting(string language)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        text.SetLanguage(language);
        var store = Store(fixture, text);
        var catalog = Catalog(fixture, store, text);

        foreach (var escaped in new[] { false, true })
        {
            var name = escaped ? "budget-markup" : "budget-ascii";
            var skill = Package(fixture, catalog, name, OversizedInstructions(escaped));

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.EnableAsync(skill, true, default));

            Assert.Equal(text.Text("Lab.SkillTooLarge", name, SkillCatalogService.MaximumContextTokens), error.Message);
            Assert.NotEqual("Lab.SkillTooLarge", error.Message);
            Assert.Null(await store.GetAsync("skill:" + name, default));
            Assert.False(await catalog.IsEnabledAsync(skill, default));
        }
    }

    /// <summary>Revalidates legacy activations before manual selection without replacing a previously usable choice.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectForQuestions_LegacyOversizedActivation_PreservesPreviousSelection(bool escaped)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var store = Store(fixture, text);
        var catalog = Catalog(fixture, store, text);
        await store.SaveSettingsAsync(new(Enabled: true), new(), default);
        var previous = Package(fixture, catalog, "budget-previous", "Use this short reference.");
        var oversized = Package(fixture, catalog, "budget-legacy", OversizedInstructions(escaped));
        await catalog.EnableAsync(previous, true, default);
        await catalog.SelectForQuestionsAsync(previous, default);
        await store.SetAsync("skill:" + oversized.Name, oversized.Fingerprint, default);
        Assert.True(await catalog.IsEnabledAsync(oversized, default));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.SelectForQuestionsAsync(oversized, default));

        Assert.Equal(text.Text("Lab.SkillTooLarge", oversized.Name, SkillCatalogService.MaximumContextTokens), error.Message);
        Assert.Equal(previous.Name, await store.GetAsync("selected-skill", default));
        Assert.Equal(oversized.Fingerprint, await store.GetAsync("skill:" + oversized.Name, default));
        Assert.Equal(previous.Name, Assert.Single(await catalog.SelectAsync("unrelated question", default)).Name);
    }

    /// <summary>Includes JSON escaping, localized wrapper text, and message overhead in the shared multi-skill selection ceiling.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public async Task Select_MultipleEscapedPackages_FitsCompleteSerializedContext(string language)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        text.SetLanguage(language);
        var store = Store(fixture, text);
        var catalog = Catalog(fixture, store, text);
        await store.SaveSettingsAsync(new(Enabled: true, AutomaticSkills: true), new(), default);
        var instructions = string.Concat(Enumerable.Repeat(Markup, 50));
        var all = new List<SkillDefinition>();
        foreach (var name in new[] { "budget-a", "budget-b", "budget-c" })
        {
            var skill = Package(fixture, catalog, name, instructions);
            all.Add(skill);
            await catalog.EnableAsync(skill, true, default);
        }
        Assert.True(ContextTokenEstimator.Text(string.Concat(all.Select(skill => skill.Instructions))) < SkillCatalogService.MaximumContextTokens);
        var oversizedContext = await catalog.ContextAsync(all, default);
        Assert.True(ContextTokenEstimator.Messages([new("user", oversizedContext)]) > SkillCatalogService.MaximumContextTokens);

        var selected = await catalog.SelectAsync("budget", default);
        var context = await catalog.ContextAsync(selected, default);

        Assert.Equal(["budget-a", "budget-b"], selected.Select(skill => skill.Name));
        Assert.All(selected, skill => Assert.Equal(instructions, skill.Instructions));
        Assert.Contains("\\u003Ctag", context);
        Assert.DoesNotContain("budget-c", context);
        Assert.InRange(ContextTokenEstimator.Messages([new("user", context)]), 1L, SkillCatalogService.MaximumContextTokens);
    }

    /// <summary>Budgets bundled translations before activation and selection, including a language change after approval.</summary>
    [Fact]
    public async Task BundledInstructions_ResolveBeforeBudgetingAndRemainLocalizedInSelection()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var store = Store(fixture, text);
        var realPrompts = Prompts(fixture);
        var translations = SupportedLanguages.Codes.ToDictionary(language => language, language => "Short synthetic guidance in " + language);
        translations["it"] = OversizedInstructions(true);
        var translated = new PromptDefinition("skill-set_reminder", 1, "Synthetic budget regression", [], translations, new Dictionary<string, string>());
        var prompts = TestProxy.Create<IPromptCatalogService>((method, args) => method.Name == nameof(IPromptCatalogService.GetAsync)
            ? (string)args[0]! == translated.Id
                ? Task.FromResult(translated)
                : realPrompts.GetAsync((string)args[0]!, (CancellationToken)args[1]!)
            : throw new NotSupportedException(method.Name));
        var catalog = new SkillCatalogService(fixture.Paths, store, text, prompts);
        var bundled = catalog.List().Single(skill => skill.Name == "set_reminder");
        Assert.Equal("bundled", bundled.Origin);
        Assert.True(ContextTokenEstimator.Text(bundled.Instructions) < SkillCatalogService.MaximumContextTokens);
        text.SetLanguage("it");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.EnableAsync(bundled, true, default));

        Assert.Equal(text.Text("Lab.SkillTooLarge", bundled.Name, SkillCatalogService.MaximumContextTokens), error.Message);
        Assert.Null(await store.GetAsync("skill:" + bundled.Name, default));
        text.SetLanguage("en");
        await store.SaveSettingsAsync(new(Enabled: true), new(), default);
        await catalog.EnableAsync(bundled, true, default);
        await catalog.SelectForQuestionsAsync(bundled, default);
        var selected = Assert.Single(await catalog.SelectAsync("unrelated question", default));
        Assert.Equal(translations["en"], selected.Instructions);
        var context = await catalog.ContextAsync([selected], default);
        Assert.Contains(translations["en"], context);
        Assert.InRange(ContextTokenEstimator.Messages([new("user", context)]), 1L, SkillCatalogService.MaximumContextTokens);

        text.SetLanguage("it");

        var warnings = new List<string>();
        Assert.Empty(await catalog.SelectAsync("unrelated question", default, warnings));
        Assert.Equal(text.Text("Lab.SkillTooLarge", bundled.Name, SkillCatalogService.MaximumContextTokens), Assert.Single(warnings));
        Assert.Equal(bundled.Name, await store.GetAsync("selected-skill", default));
        Assert.Equal(bundled.Fingerprint, await store.GetAsync("skill:" + bundled.Name, default));
    }

    /// <summary>Builds either a plain over-budget body or a smaller body that exceeds the limit only after JSON escaping.</summary>
    private static string OversizedInstructions(bool escaped) => escaped
        ? string.Concat(Enumerable.Repeat(Markup, 210))
        : new string('a', 8_000);

    /// <summary>Writes an inert local package inside the fixture without scripts, commands, or external resources.</summary>
    private static SkillDefinition Package(RegressionFixture fixture, SkillCatalogService catalog, string name, string instructions)
    {
        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"),
            $"---\nname: {name}\ndescription: Budget regression example\nversion: 1.0.0\n---\n{instructions}");
        return catalog.Inspect(directory);
    }

    /// <summary>Creates preferences over the initialized fixture database with normal redaction and localization.</summary>
    private static ExperimentalStore Store(RegressionFixture fixture, ILocalizationService text) =>
        new(fixture.Paths, new SensitiveDataRedactor(), text);

    /// <summary>Uses production packaged prompts for every regression except the explicit localization seam.</summary>
    private static SkillCatalogService Catalog(RegressionFixture fixture, ExperimentalStore store, ILocalizationService text) =>
        new(fixture.Paths, store, text, Prompts(fixture));

    /// <summary>Creates the real read-only YAML catalog over the fixture's copied prompt resources.</summary>
    private static YamlPromptCatalogService Prompts(RegressionFixture fixture) =>
        new(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
}

// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class BundledSkillTests
{
    private static readonly string[] ExpectedNames =
    [
        "git", "filesystem", "concat-files", "clipboard", "screenshot", "system_info",
        "http_request", "web_search", "timezone_convert", "set_reminder"
    ];

    /// <summary>Loads the actual packaged catalog and verifies its exact scope without enabling any skill.</summary>
    [Fact]
    public async Task PackagedCatalog_HasExactlyTenDisabledBundledSkills()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var store = new SkillsAndMemoryStore(fixture.Paths, new SensitiveDataRedactor(), text);
        var catalog = new SkillCatalogService(fixture.Paths, store, text,
            new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));

        var packages = catalog.List();

        Assert.Equal(ExpectedNames.Order(StringComparer.Ordinal), packages.Select(package => package.Name).Order(StringComparer.Ordinal));
        Assert.Equal(new SkillsAndMemorySettings(), await store.SettingsAsync(default));
        Assert.False(Directory.Exists(Path.Combine(fixture.Paths.DataDirectory, "skills")));
        foreach (var package in packages)
        {
            Assert.Equal("bundled", package.Origin);
            Assert.Equal(Path.Combine(AppContext.BaseDirectory, "skills", package.Name), package.Directory);
            Assert.False(await catalog.IsEnabledAsync(package, default));
        }
        Assert.Empty(await catalog.SelectAsync("git filesystem search reminders", default));
    }

    /// <summary>Composes every actual script with its editable defaults and each localized error message without credential false positives.</summary>
    [Theory]
    [InlineData("concat-files")]
    [InlineData("clipboard")]
    [InlineData("screenshot")]
    [InlineData("system_info")]
    [InlineData("http_request")]
    [InlineData("web_search")]
    [InlineData("timezone_convert")]
    public void PackagedScript_DefaultParametersPreserveFullSnapshot(string name)
    {
        using var fixture = new RegressionFixture();
        var text = new LocalizationService();
        var package = Catalog(fixture, text).List().Single(skill => skill.Name == name);
        var source = Assert.Single(package.Scripts);
        var redactor = new SensitiveDataRedactor();
        var actions = new SkillActionService(redactor, text);

        Assert.Equal("run", source.Key);
        foreach (var language in SupportedLanguages.Codes)
        {
            text.SetLanguage(language);
            var command = actions.ScriptCommand(package, source.Key, actions.DefaultParameters(package));

            Assert.Contains(source.Value, command);
            Assert.Equal(command, redactor.Redact(command));
            Assert.Contains("'_ValidationError' = " + ScriptArtifactService.Quote(
                text.Text(name == "concat-files" ? "Lab.ConcatInvalid" : "Lab.SkillActionInvalid")), command);
            Assert.DoesNotContain("$PSScriptRoot", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("$PSCommandPath", command, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Prevents user replacement of the trusted validation message for all seven packaged script contracts.</summary>
    [Theory]
    [InlineData("concat-files")]
    [InlineData("clipboard")]
    [InlineData("screenshot")]
    [InlineData("system_info")]
    [InlineData("http_request")]
    [InlineData("web_search")]
    [InlineData("timezone_convert")]
    public void PackagedScript_RejectsValidationMessageOverride(string name)
    {
        using var fixture = new RegressionFixture();
        var text = new LocalizationService();
        var package = Catalog(fixture, text).List().Single(skill => skill.Name == name);
        var actions = new SkillActionService(new SensitiveDataRedactor(), text);

        foreach (var parameters in new[] { "{\"_ValidationError\":\"replacement\"}", "{\"_validationerror\":\"replacement\"}" })
        {
            var error = Assert.Throws<InvalidOperationException>(() => actions.ScriptCommand(package, "run", parameters));

            Assert.Equal(text.Text("Lab.Invalid"), error.Message);
        }
    }

    /// <summary>Restricts the managed reminder action to the bundled package even when a valid local package replaces its name.</summary>
    [Fact]
    public void Reminder_LocalOverrideCannotBorrowManagedAction()
    {
        using var fixture = new RegressionFixture();
        var text = new LocalizationService();
        var catalog = Catalog(fixture, text);
        var bundled = catalog.List().Single(skill => skill.Name == "set_reminder");
        var actions = new SkillActionService(new SensitiveDataRedactor(), text);
        Assert.Equal(["manage"], actions.Actions(bundled));
        Assert.Empty(bundled.Scripts);

        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", "set_reminder");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(bundled.Directory, "SKILL.md"), Path.Combine(directory, "SKILL.md"));
        var local = catalog.List().Single(skill => skill.Name == "set_reminder");

        Assert.Equal("local", local.Origin);
        Assert.Empty(actions.Actions(local));
        Assert.Equal("{}", actions.DefaultParameters(local));
        Assert.Throws<InvalidOperationException>(() => actions.NativeCommand(local, "manage", "."));
        Assert.Throws<InvalidOperationException>(() => actions.ScriptCommand(local, "manage", "{}"));
    }

    /// <summary>Loads each shipped skill prompt through the production YAML parser and requires explicit content in all six languages.</summary>
    [Fact]
    public async Task PackagedPrompts_ProvideAllSixLanguagesForEverySkill()
    {
        using var fixture = new RegressionFixture();
        var prompts = new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
        var packages = Catalog(fixture, new LocalizationService()).List();

        foreach (var name in ExpectedNames)
        {
            var prompt = await prompts.GetAsync("skill-" + name, default);

            Assert.Equal("skill-" + name, prompt.Id);
            Assert.True(prompt.Version > 0);
            var package = Assert.Single(packages, skill => skill.Name == name);
            Assert.Equal(package.Icon, prompt.Metadata["icon"]);
            Assert.Equal(package.Color, prompt.Metadata["color"]);
            Assert.Equal(SupportedLanguages.Codes.Order(StringComparer.Ordinal), prompt.Texts.Keys.Order(StringComparer.Ordinal));
            foreach (var language in SupportedLanguages.Codes)
            {
                Assert.False(string.IsNullOrWhiteSpace(prompt.Texts[language]));
                Assert.Equal(prompt.Texts[language], prompt.ResolveText(language));
            }
            Assert.Equal(SupportedLanguages.Codes.Count, prompt.Texts.Values.Distinct(StringComparer.Ordinal).Count());
        }
    }

    /// <summary>Creates a read-only catalog over packaged files and the fixture's isolated local package directory.</summary>
    private static SkillCatalogService Catalog(RegressionFixture fixture, LocalizationService text) =>
        new(fixture.Paths, new SkillsAndMemoryStore(fixture.Paths, new SensitiveDataRedactor(), text), text,
            new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));
}

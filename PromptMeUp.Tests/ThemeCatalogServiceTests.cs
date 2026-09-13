// SPDX-License-Identifier: MIT

using System.Text.Json.Nodes;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ThemeCatalogServiceTests
{
    private const string ValidTheme = """
        {
          "version": 1,
          "id": "cyan",
          "name": "Readable test palette",
          "colors": {
            "background": "#101010",
            "primary": "#FFFFFF",
            "muted": "#CCCCCC",
            "accent": "#99EEFF",
            "info": "#99CCFF",
            "divider": "#AAAAAA",
            "success": "#99FFBB",
            "warning": "#FFEE99",
            "error": "#FFAAAA",
            "selectionBackground": "#99EEFF",
            "selectionForeground": "#101010"
          }
        }
        """;

    /// <summary>Ensures packaged theme resources load and the initial palette agrees with the bundled default.</summary>
    [Fact]
    public void Catalog_BundledThemes_IncludeAllChoicesAndMatchTheDefault()
    {
        var catalog = new ThemeCatalogService(Path.Combine(AppContext.BaseDirectory, "themes"));

        Assert.Equal(TerminalThemeDefinition.Default, catalog.Resolve("cyan") with { SourcePath = null });
        Assert.Contains(catalog.Themes, theme => theme.Id == "green");
        Assert.Contains(catalog.Themes, theme => theme.Id == "amber");
        Assert.Equal(13, catalog.Themes.Count);
        Assert.All(catalog.Themes, theme =>
        {
            Assert.Equal(3, theme.Version);
            Assert.False(string.IsNullOrWhiteSpace(theme.Author));
            Assert.False(string.IsNullOrWhiteSpace(theme.Description));
            Assert.Equal("https://github.com/umbertotechnopreneur/PromptMeUp", theme.Website);
            Assert.Equal(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "themes", theme.Id + ".json")), theme.SourcePath);
        });
    }

    /// <summary>Rejects malformed JSON and incomplete documents instead of replacing them with default colors.</summary>
    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"version\":1,\"id\":\"cyan\",\"name\":\"Missing colors\"}")]
    public void Catalog_MalformedDefinition_FailsClearly(string json)
    {
        using var fixture = new RegressionFixture();
        WriteTheme(fixture, json);

        var error = Assert.Throws<InvalidOperationException>(() => new ThemeCatalogService(fixture.Paths.DataDirectory));

        Assert.Contains("cyan.json", error.Message, StringComparison.Ordinal);
    }

    /// <summary>Rejects ambiguous duplicate keys at both the definition and palette object levels.</summary>
    [Theory]
    [InlineData("\"version\": 1,", "\"version\": 1, \"version\": 1,")]
    [InlineData("\"primary\": \"#FFFFFF\",", "\"primary\": \"#FFFFFF\", \"primary\": \"#000000\",")]
    public void Catalog_DuplicateProperty_IsRejected(string original, string replacement)
    {
        using var fixture = new RegressionFixture();
        WriteTheme(fixture, ValidTheme.Replace(original, replacement, StringComparison.Ordinal));

        Assert.Throws<InvalidOperationException>(() => new ThemeCatalogService(fixture.Paths.DataDirectory));
    }

    /// <summary>Rejects unsupported versions and unexpected schema fields rather than silently ignoring them.</summary>
    [Theory]
    [InlineData("version")]
    [InlineData("unknown")]
    [InlineData("missing-color")]
    public void Catalog_UnsupportedSchema_IsRejected(string scenario)
    {
        using var fixture = new RegressionFixture();
        var definition = JsonNode.Parse(ValidTheme)!;
        switch (scenario)
        {
            case "version":
                definition["version"] = 4;
                break;
            case "unknown":
                definition["unexpected"] = true;
                break;
            case "missing-color":
                definition["colors"]!.AsObject().Remove("muted");
                break;
        }
        WriteTheme(fixture, definition.ToJsonString());

        Assert.Throws<InvalidOperationException>(() => new ThemeCatalogService(fixture.Paths.DataDirectory));
    }

    /// <summary>Requires readable contrast for every text role, the selection, and structural dividers.</summary>
    [Theory]
    [InlineData("primary")]
    [InlineData("muted")]
    [InlineData("accent")]
    [InlineData("info")]
    [InlineData("success")]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("divider")]
    [InlineData("selectionForeground")]
    public void Catalog_IndistinguishableForeground_IsRejected(string role)
    {
        using var fixture = new RegressionFixture();
        var definition = JsonNode.Parse(ValidTheme)!;
        var colors = definition["colors"]!;
        colors[role] = colors[role == "selectionForeground" ? "selectionBackground" : "background"]!.GetValue<string>();
        WriteTheme(fixture, definition.ToJsonString());

        var error = Assert.Throws<InvalidOperationException>(() => new ThemeCatalogService(fixture.Paths.DataDirectory));

        Assert.Contains("contrast", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Rejects color names, terminal markup, and unsupported RGB spellings in editable palette files.</summary>
    [Theory]
    [InlineData("white")]
    [InlineData("#FFF")]
    [InlineData("#00112233")]
    [InlineData("#GG0000")]
    [InlineData("[red]")]
    public void Catalog_NonRgbColor_IsRejected(string color)
    {
        using var fixture = new RegressionFixture();
        var definition = JsonNode.Parse(ValidTheme)!;
        definition["colors"]!["accent"] = color;
        WriteTheme(fixture, definition.ToJsonString());

        Assert.Throws<InvalidOperationException>(() => new ThemeCatalogService(fixture.Paths.DataDirectory));
    }

    /// <summary>Does not silently substitute cyan for an unavailable saved selection.</summary>
    [Fact]
    public void Resolve_MissingSelection_RejectsInsteadOfFallingBack()
    {
        using var fixture = new RegressionFixture();
        WriteTheme(fixture, ValidTheme);
        var catalog = new ThemeCatalogService(fixture.Paths.DataDirectory);

        Assert.Equal("Readable test palette", catalog.Resolve("cyan").Name);
        Assert.Null(catalog.Resolve("cyan").Author);
        Assert.Null(catalog.Resolve("cyan").Description);
        Assert.Throws<InvalidOperationException>(() => catalog.Resolve("missing"));
    }

    /// <summary>Rejects missing, empty, padded, or multiline metadata in the enriched theme schema.</summary>
    [Theory]
    [InlineData("author", null)]
    [InlineData("author", "")]
    [InlineData("author", " Author")]
    [InlineData("description", null)]
    [InlineData("description", "")]
    [InlineData("description", "First\nSecond")]
    public void Catalog_InvalidMetadata_IsRejected(string property, string? value)
    {
        using var fixture = new RegressionFixture();
        var definition = JsonNode.Parse(ValidTheme)!;
        definition["version"] = 2;
        definition["author"] = "Theme author";
        definition["description"] = "A readable palette.";
        if (value is null)
        {
            definition.AsObject().Remove(property);
        }
        else
        {
            definition[property] = value;
        }
        WriteTheme(fixture, definition.ToJsonString());

        Assert.Throws<InvalidOperationException>(() => new ThemeCatalogService(fixture.Paths.DataDirectory));
    }

    /// <summary>Writes only a disposable fixture palette without changing packaged application resources.</summary>
    private static void WriteTheme(RegressionFixture fixture, string json) =>
        File.WriteAllText(Path.Combine(fixture.Paths.DataDirectory, "cyan.json"), json);
}

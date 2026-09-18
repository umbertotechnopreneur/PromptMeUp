// SPDX-License-Identifier: MIT

using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SkillCatalogTests
{
    /// <summary>Rejects malformed field types through the localized validation contract.</summary>
    [Theory]
    [InlineData("name: [demo]\ndescription: Example")]
    [InlineData("name: demo\ndescription: Example\nmetadata: [invalid]")]
    [InlineData("name: demo\ndescription: Example\nos: [{bad: shape}]")]
    [InlineData("name: demo\ndescription: Example\nrequires:\n  env: ['BAD=NAME']")]
    [InlineData("name: demo\ndescription: Example\nversion: [invalid]")]
    [InlineData("name: demo\ndescription: Example\nunused: [[[[[[[[[nested]]]]]]]]]")]
    [InlineData("name: demo\ndescription: [")]
    public void Inspect_InvalidYaml_ReportsLocalizedError(string metadata)
    {
        using var fixture = new RegressionFixture();
        var directory = Package(fixture, "demo", "---\n" + metadata + "\n---\nUse this procedure.");

        var error = Assert.Throws<InvalidOperationException>(() => Catalog(fixture).Inspect(directory));

        Assert.Equal(new LocalizationService().Text("Lab.Invalid"), error.Message);
    }

    /// <summary>Excludes the unwanted package independent of metadata casing and rejects device aliases.</summary>
    [Theory]
    [InlineData("metals-dev-monitor")]
    [InlineData("Metals-Dev-Monitor")]
    [InlineData("CON")]
    public void Inspect_ExcludedOrReservedName_IsRejected(string name)
    {
        using var fixture = new RegressionFixture();
        var directory = Package(fixture, "demo", Definition(name));

        Assert.Throws<InvalidOperationException>(() => Catalog(fixture).Inspect(directory));
    }

    /// <summary>Rejects traversal, device names, and implicit case aliases before publishing any package.</summary>
    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("/escape.txt")]
    [InlineData("demo/CON.txt")]
    [InlineData("demo/extra.txt.")]
    [InlineData("Demo/extra.txt")]
    [InlineData("demo//extra.txt")]
    public void StageZip_UnsafeEntry_RemovesOnlyItsStagingArea(string entry)
    {
        using var fixture = new RegressionFixture();
        var zip = Archive(fixture, [("demo/SKILL.md", Definition("demo"), 0), (entry, "data", 0)]);

        Assert.Throws<InvalidOperationException>(() => Catalog(fixture).StageZip(zip));

        var staging = Path.Combine(fixture.Paths.DataDirectory, "skill-staging");
        Assert.True(!Directory.Exists(staging) || !Directory.EnumerateFileSystemEntries(staging).Any());
        Assert.False(Directory.Exists(Path.Combine(fixture.Paths.DataDirectory, "skills")));
        Assert.True(File.Exists(zip));
    }

    /// <summary>Rejects entries that represent links even when their names are ordinary filenames.</summary>
    [Fact]
    public void StageZip_LinkAttributes_AreRejected()
    {
        using var fixture = new RegressionFixture();
        var zip = Archive(fixture,
            [("demo/SKILL.md", Definition("demo"), 0), ("demo/linked.txt", "../outside", unchecked((int)0xA1FF0000))]);

        Assert.Throws<InvalidOperationException>(() => Catalog(fixture).StageZip(zip));
    }

    /// <summary>Accepts verified macOS system aliases for staging, import, inspection, and generated staging cleanup.</summary>
    [Theory]
    [InlineData("/tmp")]
    [InlineData("/var/tmp")]
    public void Import_MacOsSystemAlias_PreservesPackageContainment(string temporaryRoot)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }
        using var fixture = new RegressionFixture();
        var directory = Path.Combine(temporaryRoot, "PromptMeUp.SkillAlias." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var paths = fixture.Paths with { DataDirectory = directory };
            var text = new LocalizationService();
            var catalog = new SkillCatalogService(paths, new ExperimentalStore(paths, new SensitiveDataRedactor(), text), text,
                new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));
            var zip = Archive(fixture, [("demo/SKILL.md", Definition("demo"), 0)]);
            var staged = catalog.StageZip(zip);

            catalog.Import(catalog.Inspect(staged));
            catalog.DiscardStaging(staged);

            var imported = Assert.Single(catalog.List(), skill => skill.Name == "demo");
            Assert.Equal(Path.Combine(directory, "skills", "demo"), imported.Directory);
            Assert.False(Directory.Exists(Path.GetDirectoryName(staged)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Keeps rejecting arbitrary package ancestors even when the operating system's temporary root is an allowed alias.</summary>
    [Fact]
    public void Inspect_UserCreatedAncestorLink_IsRejected()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        using var fixture = new RegressionFixture();
        var directory = Package(fixture, "demo", Definition("demo"));
        var link = Path.Combine(fixture.Paths.DataDirectory, "linked-skills");
        Directory.CreateSymbolicLink(link, Path.GetDirectoryName(directory)!);
        try
        {
            Assert.Throws<InvalidOperationException>(() => Catalog(fixture).Inspect(Path.Combine(link, "demo")));
            Assert.True(File.Exists(Path.Combine(directory, "SKILL.md")));
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    /// <summary>Rejects a file used as an implicit directory on all supported operating systems.</summary>
    [Fact]
    public void StageZip_FileDirectoryCollision_IsRejected()
    {
        using var fixture = new RegressionFixture();
        var zip = Archive(fixture,
            [("demo/SKILL.md", Definition("demo"), 0), ("demo/assets", "file", 0), ("demo/assets/note.txt", "child", 0)]);

        Assert.Throws<InvalidOperationException>(() => Catalog(fixture).StageZip(zip));
    }

    /// <summary>Rejects expanded files beyond the package limit despite high compression.</summary>
    [Fact]
    public void StageZip_OversizedExpandedFile_IsRejected()
    {
        using var fixture = new RegressionFixture();
        var zip = Archive(fixture,
            [("demo/SKILL.md", Definition("demo"), 0), ("demo/large.txt", new string('x', 65_537), 0)]);

        Assert.Throws<InvalidOperationException>(() => Catalog(fixture).StageZip(zip));
    }

    /// <summary>Never executes a staged script and does not publish edits made after the review snapshot.</summary>
    [Fact]
    public void Import_ChangedSnapshotOrName_IsRejected()
    {
        using var fixture = new RegressionFixture();
        var catalog = Catalog(fixture);
        var zip = Archive(fixture,
            [("demo/SKILL.md", Definition("demo"), 0), ("demo/scripts/run.ps1", "throw 'must never execute during import'", 0)]);
        var staged = catalog.StageZip(zip);
        var reviewed = catalog.Inspect(staged);

        Assert.Throws<InvalidOperationException>(() => catalog.Import(reviewed with { Name = "../outside" }));
        File.AppendAllText(Path.Combine(staged, "SKILL.md"), "\nModified instructions.");
        Assert.Throws<InvalidOperationException>(() => catalog.Import(reviewed));
        catalog.DiscardStaging(staged + Path.DirectorySeparatorChar);

        Assert.False(Directory.Exists(Path.GetDirectoryName(staged)));
        Assert.False(Directory.Exists(Path.Combine(fixture.Paths.DataDirectory, "skills")));
    }

    /// <summary>Rejects case-insensitive script action collisions instead of selecting an ambiguous snapshot.</summary>
    [Fact]
    public void Inspect_DuplicateScriptBasenames_IsRejected()
    {
        using var fixture = new RegressionFixture();
        var directory = Package(fixture, "demo", Definition("demo"));
        Directory.CreateDirectory(Path.Combine(directory, "scripts", "nested"));
        File.WriteAllText(Path.Combine(directory, "scripts", "run.ps1"), "Write-Output 'first'");
        File.WriteAllText(Path.Combine(directory, "scripts", "nested", "RUN.ps1"), "Write-Output 'second'");

        Assert.Throws<InvalidOperationException>(() => Catalog(fixture).Inspect(directory));
    }

    /// <summary>Requires new approval after any content change while retaining an immutable script snapshot.</summary>
    [Fact]
    public async Task IsEnabled_ChangedScript_InvalidatesApproval()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var directory = Package(fixture, "demo", Definition("Demo"));
        Directory.CreateDirectory(Path.Combine(directory, "scripts"));
        var script = Path.Combine(directory, "scripts", "run.ps1");
        File.WriteAllText(script, "Write-Output 'reviewed'");
        var catalog = Catalog(fixture);
        var reviewed = catalog.Inspect(directory);

        Assert.Equal("demo", reviewed.Name);
        Assert.False(await catalog.IsEnabledAsync(reviewed, default));
        await catalog.EnableAsync(reviewed, true, default);
        Assert.True(await catalog.IsEnabledAsync(reviewed, default));
        File.WriteAllText(script, "Write-Output 'changed'");

        Assert.False(await catalog.IsEnabledAsync(catalog.Inspect(directory), default));
        Assert.Equal("Write-Output 'reviewed'", reviewed.Scripts["run"]);
    }

    /// <summary>Gives local packages precedence without lending them the bundled native command adapter.</summary>
    [Fact]
    public void List_LocalOverride_RetainsLocalOrigin()
    {
        using var fixture = new RegressionFixture();
        Package(fixture, "git", Definition("git"));

        var skill = Assert.Single(Catalog(fixture).List(), skill => skill.Name == "git");

        Assert.Equal("local", skill.Origin);
        Assert.Empty(new SkillActionService(new SensitiveDataRedactor(), new LocalizationService()).Actions(skill));
    }

    /// <summary>Keeps all selection disabled by default and enforces a shared context budget when enabled.</summary>
    [Fact]
    public async Task Select_DefaultDisabled_ThenHonorsBudget()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var catalog = Catalog(fixture);
        var first = catalog.Inspect(Package(fixture, "first", Definition("first", new string('a', 5_500))));
        var second = catalog.Inspect(Package(fixture, "second", Definition("second", new string('b', 5_500))));
        await catalog.EnableAsync(first, true, default);
        await catalog.EnableAsync(second, true, default);

        Assert.Empty(await catalog.SelectAsync("example", default));
        var store = Store(fixture);
        await store.SaveSettingsAsync(new(Enabled: true, AutomaticSkills: true), await store.SettingsAsync(default), default);
        var selected = await catalog.SelectAsync("example", default);

        Assert.Single(selected);
        Assert.Equal("first", selected[0].Name);
    }

    /// <summary>Creates a package under this fixture's own local skills directory.</summary>
    private static string Package(RegressionFixture fixture, string name, string definition)
    {
        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"), definition);
        return directory;
    }

    /// <summary>Builds a valid inert package definition with predictable selection keywords.</summary>
    private static string Definition(string name, string instructions = "Use this procedure.") =>
        $"---\nname: {name}\ndescription: Example package\nversion: 1.0.0\n---\n{instructions}";

    /// <summary>Builds only a synthetic archive and never extracts or runs its contents directly.</summary>
    private static string Archive(RegressionFixture fixture, IEnumerable<(string Path, string Content, int Attributes)> entries)
    {
        var path = Path.Combine(fixture.Paths.DataDirectory, "package.ZIP");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var item in entries)
        {
            var entry = zip.CreateEntry(item.Path);
            entry.ExternalAttributes = item.Attributes;
            using var writer = new StreamWriter(entry.Open());
            writer.Write(item.Content);
        }
        return path;
    }

    /// <summary>Creates a catalog with the shared redaction and localization implementations.</summary>
    private static SkillCatalogService Catalog(RegressionFixture fixture) => new(fixture.Paths, Store(fixture), new LocalizationService(),
        new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));

    /// <summary>Creates preferences in the same isolated database as the fixture.</summary>
    private static ExperimentalStore Store(RegressionFixture fixture) => new(fixture.Paths, new SensitiveDataRedactor(), new LocalizationService());
}

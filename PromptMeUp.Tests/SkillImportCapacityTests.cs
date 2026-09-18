// SPDX-License-Identifier: MIT

using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SkillImportCapacityTests
{
    /// <summary>Allows the last supported package and keeps the complete catalog readable.</summary>
    [Fact]
    public void Import_SixtyFourthPackage_Succeeds()
    {
        using var fixture = new RegressionFixture();
        CreatePackages(fixture, 63);
        var catalog = Catalog(fixture);
        var reviewed = StagePackage(fixture, catalog, "last-package");

        catalog.Import(reviewed);

        var local = catalog.List().Where(skill => skill.Origin == "local").ToArray();
        Assert.Equal(64, local.Length);
        Assert.Equal(reviewed.Fingerprint, Assert.Single(local, skill => skill.Name == reviewed.Name).Fingerprint);
        Assert.False(Directory.Exists(reviewed.Directory));
        catalog.DiscardStaging(reviewed.Directory);
    }

    /// <summary>Rejects an overflowing import before moving it or changing any existing package.</summary>
    [Fact]
    public void Import_SixtyFifthPackage_PreservesCatalogAndStaging()
    {
        using var fixture = new RegressionFixture();
        CreatePackages(fixture, 64);
        var catalog = Catalog(fixture);
        var before = catalog.List().Select(skill => (skill.Name, skill.Fingerprint)).ToArray();
        var reviewed = StagePackage(fixture, catalog, "overflow-package");

        var error = Assert.Throws<InvalidOperationException>(() => catalog.Import(reviewed));

        Assert.Equal(new LocalizationService().Text("Lab.Invalid"), error.Message);
        Assert.Equal(before, catalog.List().Select(skill => (skill.Name, skill.Fingerprint)).ToArray());
        Assert.Equal(reviewed.Fingerprint, catalog.Inspect(reviewed.Directory).Fingerprint);
        Assert.False(Directory.Exists(Path.Combine(fixture.Paths.DataDirectory, "skills", reviewed.Name)));
        catalog.DiscardStaging(reviewed.Directory);
    }

    /// <summary>Fails fast on import contention without damaging staging, then allows retry after release.</summary>
    [Fact]
    public void Import_AnotherCallerHoldsLock_PreservesPackageForRetry()
    {
        using var fixture = new RegressionFixture();
        CreatePackages(fixture, 63);
        var catalog = Catalog(fixture);
        var reviewed = StagePackage(fixture, catalog, "retry-package");
        var lockPath = Path.Combine(fixture.Paths.DataDirectory, "skills", ".import.lock");
        using (var competingImport = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Throws<IOException>(() => catalog.Import(reviewed));
            Assert.Equal(63, catalog.List().Count(skill => skill.Origin == "local"));
            Assert.Equal(reviewed.Fingerprint, catalog.Inspect(reviewed.Directory).Fingerprint);
        }

        catalog.Import(reviewed);

        Assert.Equal(64, catalog.List().Count(skill => skill.Origin == "local"));
        Assert.True(File.Exists(lockPath));
        catalog.DiscardStaging(reviewed.Directory);
    }

    /// <summary>Serializes competing imports so two callers cannot both consume the final package slot.</summary>
    [Fact]
    public async Task Import_ConcurrentFinalSlot_NeverExceedsCapacity()
    {
        using var fixture = new RegressionFixture();
        CreatePackages(fixture, 63);
        var catalog = Catalog(fixture);
        var first = StagePackage(fixture, catalog, "first-candidate");
        var second = StagePackage(fixture, catalog, "second-candidate");
        using var start = new ManualResetEventSlim(false);
        var attempts = new[] { first, second }.Select(reviewed => Task.Run(() =>
        {
            start.Wait();
            try
            {
                Catalog(fixture).Import(reviewed);
                return true;
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            {
                return false;
            }
        })).ToArray();

        start.Set();
        var results = await Task.WhenAll(attempts);

        Assert.Single(results, succeeded => succeeded);
        Assert.Equal(64, catalog.List().Count(skill => skill.Origin == "local"));
        var rejected = results[0] ? second : first;
        Assert.Equal(rejected.Fingerprint, catalog.Inspect(rejected.Directory).Fingerprint);
        catalog.DiscardStaging(first.Directory);
        catalog.DiscardStaging(second.Directory);
    }

    /// <summary>Creates bounded inert packages only inside the disposable fixture directory.</summary>
    private static void CreatePackages(RegressionFixture fixture, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var name = $"existing-{index:D2}";
            var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", name);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "SKILL.md"), Definition(name));
        }
    }

    /// <summary>Exercises the normal ZIP inspection path without running any package content.</summary>
    private static SkillDefinition StagePackage(RegressionFixture fixture, SkillCatalogService catalog, string name)
    {
        var path = Path.Combine(fixture.Paths.DataDirectory, name + ".zip");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry(name + "/SKILL.md");
            using var writer = new StreamWriter(entry.Open());
            writer.Write(Definition(name));
        }
        return catalog.Inspect(catalog.StageZip(path));
    }

    /// <summary>Provides a valid small package with no executable actions.</summary>
    private static string Definition(string name) =>
        $"---\nname: {name}\ndescription: Synthetic import-capacity fixture\nversion: 1.0.0\n---\nUse this procedure.";

    /// <summary>Creates an independent catalog instance backed by the same fixture storage.</summary>
    private static SkillCatalogService Catalog(RegressionFixture fixture)
    {
        var text = new LocalizationService();
        return new SkillCatalogService(fixture.Paths, new ExperimentalStore(fixture.Paths, new SensitiveDataRedactor(), text), text,
            new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance));
    }
}

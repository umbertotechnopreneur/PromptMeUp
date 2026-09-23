// SPDX-License-Identifier: MIT

using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class DiagnosticBundleServiceTests
{
    /// <summary>Creates an inspectable archive while excluding persistence and redacting credentials and user paths.</summary>
    [Fact]
    public async Task PrepareAsync_CreatesBoundedRedactedArchive()
    {
        using var fixture = new RegressionFixture();
        var key = "sk-" + new string('s', 32);
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        await File.WriteAllTextAsync(Path.Combine(fixture.Paths.LogsDirectory, "promptmeup-20260924.log"),
            $"startup path={profile} authorization=Bearer {key}\n");
        await File.WriteAllTextAsync(fixture.Paths.DatabasePath, "database must stay outside the archive");
        var service = new DiagnosticBundleService(fixture.Paths, new SensitiveDataRedactor(),
            NullLogger<DiagnosticBundleService>.Instance);

        var result = await service.PrepareAsync(fixture.Paths.DataDirectory, default);

        Assert.True(File.Exists(result.Path));
        Assert.Equal(1, result.LogFileCount);
        using var archive = ZipFile.OpenRead(result.Path);
        Assert.Equal(["README.txt", "diagnostics.json", "logs/promptmeup-20260924.log"],
            archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal));
        var log = await ReadEntryAsync(archive, "logs/promptmeup-20260924.log");
        Assert.DoesNotContain(key, log, StringComparison.Ordinal);
        Assert.DoesNotContain(profile, log, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%USERPROFILE%", log, StringComparison.Ordinal);
        var diagnostics = await ReadEntryAsync(archive, "diagnostics.json");
        Assert.DoesNotContain(Environment.MachineName, diagnostics, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.UserName, diagnostics, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database must stay outside", string.Join('\n', archive.Entries.Select(entry => entry.FullName)), StringComparison.Ordinal);
    }

    /// <summary>Reads a named UTF-8 archive entry for focused content assertions.</summary>
    private static async Task<string> ReadEntryAsync(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name) ?? throw new InvalidOperationException("Missing archive entry: " + name);
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

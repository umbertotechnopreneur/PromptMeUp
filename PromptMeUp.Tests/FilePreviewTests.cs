// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class FilePreviewTests : IDisposable
{
    private readonly string _directory = CreateDirectory();
    private readonly LocalizationService _text = new();

    /// <summary>Shows exact prefix-renaming effects without changing any source file.</summary>
    [Fact]
    public void Build_Rename_IsAnInspectionOnly()
    {
        var path = Path.Combine(_directory, "a.log");
        File.WriteAllText(path, "log");
        var preview = Service().Build(Options("rename", _directory, prefix: "old-", pattern: "*.log"));
        var effect = Assert.Single(preview.Effects);
        Assert.Equal(path, effect.Source);
        Assert.Equal(Path.Combine(_directory, "old-a.log"), effect.Destination);
        Assert.True(File.Exists(path));
        Assert.False(File.Exists(effect.Destination));
    }

    /// <summary>Flags existing destinations and refuses to construct an executable collision.</summary>
    [Fact]
    public void Build_Collision_BlocksCommandCreation()
    {
        var path = Path.Combine(_directory, "a.log");
        File.WriteAllText(path, "original");
        var service = Service();
        var preview = service.Build(Options("copy", path, _directory));
        Assert.True(preview.Effects[0].Collision);
        Assert.Throws<InvalidOperationException>(() => service.BuildCommand(preview, preview.Effects[0]));
    }

    /// <summary>Rejects changed source metadata after preview.</summary>
    [Fact]
    public void BuildCommand_ChangedSource_RequiresNewPreview()
    {
        var path = Path.Combine(_directory, "a.log");
        File.WriteAllText(path, "a");
        var service = Service();
        var preview = service.Build(Options("delete", path));
        File.AppendAllText(path, "changed");
        Assert.Throws<InvalidOperationException>(() => service.BuildCommand(preview, preview.Effects[0]));
        Assert.True(File.Exists(path));
    }

    /// <summary>Protects a destination created between command preview and authorization.</summary>
    [Fact]
    public async Task Copy_DestinationAppearsBeforeRun_PreservesItsContents()
    {
        var source = Path.Combine(_directory, "a.log");
        var target = Directory.CreateDirectory(Path.Combine(_directory, "target")).FullName;
        File.WriteAllText(source, "source");
        var service = Service();
        var preview = service.Build(Options("copy", source, target));
        var command = service.BuildCommand(preview, preview.Effects[0]);
        var destination = Path.Combine(target, "a.log");
        File.WriteAllText(destination, "keep");

        var result = await ExecuteAsync(command);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("keep", File.ReadAllText(destination));
        Assert.Equal("source", File.ReadAllText(source));
    }

    /// <summary>Preserves literal quoting and copies a file containing an apostrophe in its name.</summary>
    [Fact]
    public async Task Copy_QuotedFilename_UsesLiteralPath()
    {
        var source = Path.Combine(_directory, "a'b.log");
        var target = Directory.CreateDirectory(Path.Combine(_directory, "target")).FullName;
        File.WriteAllText(source, "content");
        var service = Service();
        var preview = service.Build(Options("copy", source, target));
        var result = await ExecuteAsync(service.BuildCommand(preview, preview.Effects[0]));
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("content", File.ReadAllText(Path.Combine(target, "a'b.log")));
    }

    /// <summary>Rejects link traversal on platforms where creating a link requires no additional privileges.</summary>
    [Fact]
    public void Build_LinkedSource_RejectsTraversal()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        var actual = Directory.CreateDirectory(Path.Combine(_directory, "actual")).FullName;
        File.WriteAllText(Path.Combine(actual, "a.log"), "a");
        var link = Path.Combine(_directory, "link");
        Directory.CreateSymbolicLink(link, actual);
        Assert.Throws<InvalidOperationException>(() => Service().Build(Options("delete", link)));
    }

    /// <summary>Validates the narrow operation vocabulary and operation-specific argument requirements.</summary>
    [Fact]
    public void Parse_OperationOptions_AreStrict()
    {
        var parser = new CommandLineParser(_text);
        Assert.True(parser.Parse(["--preview", "rename", "--file", "logs", "--prefix", "old-"]).Succeeded);
        Assert.False(parser.Parse(["--preview", "copy", "--file", "logs"]).Succeeded);
        Assert.False(parser.Parse(["--preview", "delete", "--file", "logs", "--output", "target"]).Succeeded);
        Assert.False(parser.Parse(["--preview", "execute", "--file", "logs"]).Succeeded);
        Assert.False(parser.Parse(["--status", "--prefix", "old-"]).Succeeded);
    }

    /// <summary>Removes only the randomly named directory created by this fixture.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>Creates a real, non-linked temporary root on supported test platforms.</summary>
    private static string CreateDirectory()
    {
        var temporary = Path.GetTempPath();
        if (OperatingSystem.IsMacOS() && temporary.StartsWith("/var/", StringComparison.Ordinal))
        {
            temporary = "/private" + temporary;
        }
        return Directory.CreateDirectory(Path.Combine(temporary, "hm-preview-" + Guid.NewGuid().ToString("N"))).FullName;
    }

    /// <summary>Builds one isolated preview service without AI or process collaborators.</summary>
    private FilePreviewService Service() => new(_text, new BoundedTextInput(new SensitiveDataRedactor(), _text));

    /// <summary>Creates explicit file-effect options for one test case.</summary>
    private static CommandLineOptions Options(string operation, string source, string? output = null, string? prefix = null, string? pattern = null) =>
        new(AppCommand.Preview, null, "en", true, true, false, false, null, source, output, null, operation, prefix, pattern);

    /// <summary>Runs only the concrete temporary-file command explicitly prepared by the test.</summary>
    private static Task<CommandExecutionResult> ExecuteAsync(string command) =>
        new CommandExecutionService(NullLogger<CommandExecutionService>.Instance).ExecuteAsync(
            ApprovedCommand.Create(command, new CommandRiskAssessment(60, CommandRiskLevel.High, "Disposable test files.", false, null)),
            TimeSpan.FromSeconds(30), CancellationToken.None);
}

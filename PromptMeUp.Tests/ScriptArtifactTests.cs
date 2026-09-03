// SPDX-License-Identifier: MIT

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ScriptArtifactTests
{
    /// <summary>Rejects invalid artifact shapes and accepts a complete source without extracting Markdown heuristically.</summary>
    [Fact]
    public void Parse_RequiresExactArtifactFields()
    {
        var service = Create();
        Assert.Equal("Get-Location", service.Parse(JsonSerializer.Serialize(new { explanation = "Inspect location.", source = "Get-Location" })).Source);
        Assert.Throws<InvalidOperationException>(() => service.Parse("{}"));
        Assert.Throws<InvalidOperationException>(() => service.Parse(JsonSerializer.Serialize(new { explanation = "Empty.", source = "" })));
    }

    /// <summary>Preserves existing destination contents even when a save is explicitly requested.</summary>
    [Fact]
    public async Task SaveAsync_ExistingDestination_IsNeverOverwritten()
    {
        var path = Path.Combine(Path.GetTempPath(), "hm-script-" + Guid.NewGuid().ToString("N") + ".ps1");
        try
        {
            await File.WriteAllTextAsync(path, "original");
            await Assert.ThrowsAsync<InvalidOperationException>(() => Create().SaveAsync(path, "Get-Location", CancellationToken.None));
            Assert.Equal("original", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Parses hostile-looking quoted source as data and never evaluates its file-writing expression.</summary>
    [Fact]
    public async Task ValidationCommand_ParsesSourceWithoutExecutingIt()
    {
        var marker = Path.Combine(Path.GetTempPath(), "hm-not-executed-" + Guid.NewGuid().ToString("N"));
        var source = "[IO.File]::WriteAllText(" + ScriptArtifactService.Quote(marker) + ", 'unexpected')";
        var command = ScriptArtifactService.BuildValidationCommand(source);
        var approved = ApprovedCommand.Create(command, new CommandRiskAssessment(35, CommandRiskLevel.Medium, "Parser only.", false, null));
        var result = await new CommandExecutionService(NullLogger<CommandExecutionService>.Instance)
            .ExecuteAsync(approved, TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.False(File.Exists(marker));
        Assert.Equal(0, result.ExitCode);
        using var document = JsonDocument.Parse(result.StandardOutput);
        Assert.True(document.RootElement.GetProperty("SyntaxValid").GetBoolean());
    }

    /// <summary>Reports a syntax error through the typed command outcome.</summary>
    [Fact]
    public async Task ValidationCommand_InvalidSyntax_ReportsFailure()
    {
        var approved = ApprovedCommand.Create(ScriptArtifactService.BuildValidationCommand("if ("),
            new CommandRiskAssessment(35, CommandRiskLevel.Medium, "Parser only.", false, null));
        var result = await new CommandExecutionService(NullLogger<CommandExecutionService>.Instance)
            .ExecuteAsync(approved, TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.Equal(1, result.ExitCode);
        using var document = JsonDocument.Parse(result.StandardOutput);
        Assert.False(document.RootElement.GetProperty("SyntaxValid").GetBoolean());
    }

    /// <summary>Requires an explicit request and scopes file options to the relevant feature.</summary>
    [Fact]
    public void Parse_ScriptOptions_AreScoped()
    {
        var parser = new CommandLineParser(new LocalizationService());
        Assert.True(parser.Parse(["--script", "Archive logs", "--file", "old.ps1", "--output", "new.ps1"]).Succeeded);
        Assert.False(parser.Parse(["--script"]).Succeeded);
        Assert.False(parser.Parse(["--status", "--output", "new.ps1"]).Succeeded);
    }

    /// <summary>Creates a real artifact validator without provider or user data dependencies.</summary>
    private static ScriptArtifactService Create() => new(new SensitiveDataRedactor(), new LocalizationService());
}

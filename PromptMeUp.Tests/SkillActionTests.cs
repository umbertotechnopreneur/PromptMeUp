// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SkillActionTests
{
    /// <summary>Rejects malformed, nested, duplicate, null, or invalidly named script parameters.</summary>
    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"Path\":\"one\",\"path\":\"two\"}")]
    [InlineData("{\"Path\":{\"nested\":true}}")]
    [InlineData("{\"Path\":null}")]
    [InlineData("{\"0Invalid\":\"value\"}")]
    [InlineData("{\"Path\":\"\\u0000\"}")]
    public void ScriptCommand_InvalidParameters_AreLocalized(string parameters)
    {
        var text = new LocalizationService();
        var service = new SkillActionService(new SensitiveDataRedactor(), text);

        var error = Assert.Throws<InvalidOperationException>(() => service.ScriptCommand(Script(), "run", parameters));

        Assert.Equal(text.Text("Lab.Invalid"), error.Message);
    }

    /// <summary>Rejects credential-shaped argument names even when their synthetic values contain no recognizable token.</summary>
    [Theory]
    [InlineData("ApiKey")]
    [InlineData("Password")]
    [InlineData("access_token")]
    [InlineData("Authorization")]
    public void ScriptCommand_SecretParameterName_IsRejected(string name)
    {
        var text = new LocalizationService();
        var parameters = JsonSerializer.Serialize(new Dictionary<string, string> { [name] = "synthetic-test-value" });

        var error = Assert.Throws<InvalidOperationException>(() =>
            new SkillActionService(new SensitiveDataRedactor(), text).ScriptCommand(Script(), "run", parameters));

        Assert.Equal(text.Text("Memory.Secret"), error.Message);
    }

    /// <summary>Quotes scalar data while keeping the complete script source visible in the authorized command.</summary>
    [Fact]
    public void ScriptCommand_QuotesScalarParametersAndPreservesSnapshot()
    {
        var source = "param([string]$Path, [int]$Limit, [bool]$Flag)\nWrite-Output $Path";
        var payload = "example'; Write-Output 'untrusted'; $(Get-Location)";
        var parameters = JsonSerializer.Serialize(new { Path = payload, Limit = 3, Flag = true });

        var command = new SkillActionService(new SensitiveDataRedactor(), new LocalizationService())
            .ScriptCommand(Script(source), "run", parameters);

        Assert.Contains("'Path' = " + ScriptArtifactService.Quote(payload), command);
        Assert.Contains("'Limit' = '3'", command);
        Assert.Contains("'Flag' = $true", command);
        Assert.Contains("& {\n" + source + "\n} @skillParameters", command);
        Assert.DoesNotContain("-File", command);
    }

    /// <summary>Rejects credentials inside escaped JSON bodies before converting them to PowerShell literals.</summary>
    [Theory]
    [InlineData("password")]
    [InlineData("api_key")]
    [InlineData("access_token")]
    public void ScriptCommand_SerializedBodyCredentials_AreRejected(string field)
    {
        var body = JsonSerializer.Serialize(new Dictionary<string, string> { [field] = "synthetic-value" });
        var parameters = JsonSerializer.Serialize(new { Url = "https://example.com", Method = "POST", Body = body });
        var text = new LocalizationService();

        var error = Assert.Throws<InvalidOperationException>(() =>
            new SkillActionService(new SensitiveDataRedactor(), text).ScriptCommand(Script(), "run", parameters));

        Assert.Equal(text.Text("Memory.Secret"), error.Message);
    }

    /// <summary>Rejects scripts whose relative dependencies cannot be represented by the reviewed inline snapshot.</summary>
    [Theory]
    [InlineData("Get-Content (Join-Path $PSScriptRoot 'unreviewed.txt')")]
    [InlineData("Write-Output $PSCommandPath")]
    public void ScriptCommand_RelativePackageDependency_IsRejected(string source)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SkillActionService(new SensitiveDataRedactor(), new LocalizationService()).ScriptCommand(Script(source), "run", "{}"));
    }

    /// <summary>Local overrides cannot acquire native actions by reusing a bundled skill's name.</summary>
    [Fact]
    public void NativeCommand_LocalOverride_CannotUseBundledAdapter()
    {
        var service = new SkillActionService(new SensitiveDataRedactor(), new LocalizationService());
        var local = Script() with { Name = "git" };

        Assert.Equal(["run"], service.Actions(local));
        Assert.Throws<InvalidOperationException>(() => service.NativeCommand(local, "status", "."));
    }

    /// <summary>Native Git paths remain literal data and cannot append command options or shell code.</summary>
    [Fact]
    public void NativeCommand_LiteralPath_IsQuoted()
    {
        var service = new SkillActionService(new SensitiveDataRedactor(), new LocalizationService());
        var path = Path.Combine(Path.GetTempPath(), "skill's repository");

        var command = service.NativeCommand(Script() with { Name = "git", Origin = "bundled" }, "status", path);

        Assert.Contains("-C " + ScriptArtifactService.Quote(Path.GetFullPath(path)), command);
        Assert.EndsWith("status --short --branch", command);
    }

    /// <summary>Protects the bundled script's localized error parameter from user replacement.</summary>
    [Fact]
    public void ScriptCommand_BundledConcat_InjectsOnlyTrustedValidationMessage()
    {
        var text = new LocalizationService();
        var service = new SkillActionService(new SensitiveDataRedactor(), text);
        var skill = Script() with { Name = "concat-files", Origin = "bundled" };

        var command = service.ScriptCommand(skill, "run", "{\"InputFolder\":\".\"}");

        Assert.Contains("'_ValidationError' = " + ScriptArtifactService.Quote(text.Text("Lab.ConcatInvalid")), command);
        Assert.Throws<InvalidOperationException>(() => service.ScriptCommand(skill, "run", "{\"_ValidationError\":\"override\"}"));
    }

    /// <summary>Creates only inert script data without requiring package files or process execution.</summary>
    private static SkillDefinition Script(string source = "Write-Output 'reviewed'") =>
        new("example", "Example skill", "1.0.0", "Instructions", "", "local", "fingerprint", null,
            new Dictionary<string, string> { ["run"] = source });
}

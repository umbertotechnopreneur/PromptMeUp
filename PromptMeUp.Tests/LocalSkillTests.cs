// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class LocalSkillTests
{
    /// <summary>Checks that every local package can produce a fully reviewed command without false credential matches.</summary>
    [Theory]
    [InlineData("clipboard")]
    [InlineData("screenshot")]
    [InlineData("system_info")]
    [InlineData("timezone_convert")]
    public async Task ScriptCommand_PreservesLocalPackageSnapshot(string package)
    {
        var source = await SourceAsync(package);
        var skill = new SkillDefinition(package, "Local package", "1.0.0", "Instructions", "", "bundled", "fingerprint", null,
            new Dictionary<string, string> { ["run"] = source });
        var command = new SkillActionService(new SensitiveDataRedactor(), new LocalizationService()).ScriptCommand(skill, "run", "{}");

        Assert.Contains(source, command);
        Assert.DoesNotContain("$PSScriptRoot", command, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Compiles native declarations without reading a clipboard, obtaining screen geometry, or capturing an image.</summary>
    [Theory]
    [InlineData("clipboard")]
    [InlineData("screenshot")]
    public async Task DesktopInterop_CompilesWithoutAccessingDesktop(string package)
    {
        var script = await SourceAsync(package);
        var source = Regex.Match(script, "(?s)Add-Type -TypeDefinition @'\\r?\\n(.*?)\\r?\\n'@").Groups[1].Value;
        Assert.NotEmpty(source);

        var result = await ExecuteAsync("Add-Type -TypeDefinition " + ScriptArtifactService.Quote(source) + "; 'compiled-offline'");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("compiled-offline", result.Output);
    }

    /// <summary>Uses an explicit ISO source date and real OS time zone rules without network or AI calls.</summary>
    [Fact]
    public async Task TimeZone_ConvertsExplicitDate()
    {
        var result = await RunAsync("timezone_convert", "@{ FromTimeZone = 'UTC'; ToTimeZone = 'Asia/Tokyo'; Time = '2026-09-18T09:00' }");

        Assert.True(result.ExitCode == 0, result.Errors);
        using var document = JsonDocument.Parse(result.Output);
        Assert.Equal("2026-09-18T18:00:00+09:00", document.RootElement.GetProperty("TargetTime").GetString());
        Assert.Equal("2026-09-18T09:00:00+00:00", document.RootElement.GetProperty("SourceTime").GetString());
    }

    /// <summary>Rejects daylight saving gaps, repeated hours, and non-ISO or impossible dates instead of guessing.</summary>
    [Theory]
    [InlineData("2026-03-29T02:30")]
    [InlineData("2026-10-25T02:30")]
    [InlineData("2026-02-30T12:00")]
    [InlineData("14:30")]
    [InlineData("2026-09-18T09:00+02:00")]
    public async Task TimeZone_RejectsAmbiguousOrInvalidWallTimes(string time)
    {
        var result = await RunAsync("timezone_convert", "@{ FromTimeZone = 'Europe/Rome'; ToTimeZone = 'UTC'; Time = " + ScriptArtifactService.Quote(time) + " }");

        Assert.Equal(23, result.ExitCode);
        Assert.Contains("rejected", result.Output);
    }

    /// <summary>Finds exact available identifiers with a bounded literal filter rather than guessing a city.</summary>
    [Fact]
    public async Task TimeZone_ListIsBoundedAndConversionRejectsFragments()
    {
        var listed = await RunAsync("timezone_convert", "@{ Action = 'list'; Filter = '' }");
        Assert.True(listed.ExitCode == 0, listed.Errors);
        using var document = JsonDocument.Parse(listed.Output);
        Assert.InRange(document.RootElement.GetProperty("Zones").GetArrayLength(), 1, 50);

        var invalid = await RunAsync("timezone_convert", "@{ ToTimeZone = 'synthetic-unknown-zone' }");
        Assert.Equal(23, invalid.ExitCode);
    }

    /// <summary>Reads environment names but never exposes even an ordinary synthetic environment value.</summary>
    [Fact]
    public async Task SystemInfo_EnvironmentOmitsValues()
    {
        var result = await RunAsync("system_info", "@{ Action = 'env' }");

        Assert.True(result.ExitCode == 0, result.Errors);
        using var document = JsonDocument.Parse(result.Output);
        Assert.InRange(document.RootElement.GetProperty("Names").GetArrayLength(), 1, 100);
        Assert.DoesNotContain("synthetic-value-must-never-be-output", result.Output);
    }

    /// <summary>Rejects invalid actions before touching desktop APIs, clipboard contents, or output paths on every platform.</summary>
    [Theory]
    [InlineData("clipboard", "@{ Action = 'invalid' }")]
    [InlineData("clipboard", "@{ Action = 'write' }")]
    [InlineData("screenshot", "@{ Mode = 'invalid'; OutputPath = 'unused.png' }")]
    [InlineData("screenshot", "@{ Mode = 'full_screen' }")]
    public async Task DesktopActions_RejectInvalidInputWithoutAccess(string package, string parameters)
    {
        var result = await RunAsync(package, parameters);

        Assert.Equal(23, result.ExitCode);
        Assert.Contains("rejected", result.Output);
    }

    /// <summary>Loads only the production script copied alongside the test assembly.</summary>
    private static Task<string> SourceAsync(string package) =>
        File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "skills", package, "scripts", "run.ps1"));

    /// <summary>Invokes a bounded local script and reports validation failures without exposing underlying OS error details.</summary>
    private static async Task<(int ExitCode, string Output, string Errors)> RunAsync(string package, string parameters)
    {
        var source = await SourceAsync(package);
        return await ExecuteAsync("$localSkillParameters = " + parameters + "; try { & {\n" + source + "\n} @localSkillParameters; } catch { 'rejected'; exit 23 }");
    }

    /// <summary>Runs a non-interactive test process with a deadline and guaranteed child cleanup.</summary>
    private static async Task<(int ExitCode, string Output, string Errors)> ExecuteAsync(string command)
    {
        var start = new ProcessStartInfo("pwsh")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add("$ErrorActionPreference = 'Stop'; & ([scriptblock]::Create([Console]::In.ReadToEnd()))");
        start.Environment["PROMPTMEUP_SYNTHETIC_SKILL_VALUE"] = "synthetic-value-must-never-be-output";
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            await process.StandardInput.WriteAsync(command.AsMemory(), timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        return (process.ExitCode, await output, await errors);
    }
}

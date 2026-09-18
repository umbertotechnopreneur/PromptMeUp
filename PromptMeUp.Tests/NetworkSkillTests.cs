// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Text.RegularExpressions;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class NetworkSkillTests
{
    /// <summary>Compiles each production transport and checks its address and URL guardrails without making any network request.</summary>
    [Theory]
    [InlineData("http_request", "PromptMeUpHttpSkill")]
    [InlineData("web_search", "PromptMeUpWebSearchSkill")]
    public async Task Transport_RejectsNonPublicDestinationsWithoutNetwork(string package, string typeName)
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "skills", package, "scripts", "run.ps1"));
        var source = Regex.Match(script, @"(?s)\$networkSource = @'\r?\n(.*?)\r?\n'@").Groups[1].Value;
        Assert.NotEmpty(source);
        var command = $$"""
            $ErrorActionPreference = 'Stop'
            Add-Type -TypeDefinition {{ScriptArtifactService.Quote(source)}}
            $blocked = @('0.0.0.0', '10.1.2.3', '100.64.0.1', '127.0.0.1', '169.254.169.254', '172.16.0.1', '192.168.0.1', '192.0.0.1', '192.0.2.1', '192.88.99.1', '198.18.0.1', '198.51.100.1', '203.0.113.1', '224.0.0.1', '255.255.255.255', '::', '::1', '::ffff:127.0.0.1', '64:ff9b::7f00:1', 'fc00::1', 'fe80::1', 'ff02::1', '2001:db8::1', '2002:7f00:1::', '3fff::1')
            foreach ($address in $blocked) {
                if ([{{typeName}}]::IsPublic([Net.IPAddress]::Parse($address))) { throw "Accepted blocked address: $address" }
            }
            foreach ($address in @('8.8.8.8', '1.1.1.1', '::ffff:8.8.8.8', '2606:4700:4700::1111', '2001:4860:4860::8888')) {
                if (-not [{{typeName}}]::IsPublic([Net.IPAddress]::Parse($address))) { throw "Rejected public address: $address" }
            }
            $invalidUrls = @('http://example.com', 'file:///etc/passwd', 'https://example.com:444', 'https://name@example.com', 'https://localhost', 'https://service.local', 'https://service.internal', 'https://router.home.arpa', 'https://127.0.0.1', 'https://[::1]', 'https://example.com/#fragment', 'https://example.com/?api%5Fkey=sample', 'https://example.com/?sig=sample')
            foreach ($url in $invalidUrls) {
                $accepted = $false
                try { $null = [{{typeName}}]::ValidateUri($url); $accepted = $true } catch { }
                if ($accepted) { throw "Accepted invalid URL: $url" }
            }
            $null = [{{typeName}}]::ValidateUri('https://example.com/?page=2')
            Write-Output 'validated-offline'
            """;
        var start = new ProcessStartInfo("pwsh")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(command);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
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
        Assert.True(process.ExitCode == 0, await errors);
        Assert.Contains("validated-offline", await output);
    }
}

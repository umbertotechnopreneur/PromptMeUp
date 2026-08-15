// SPDX-License-Identifier: MIT

using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class RuntimeContextServiceTests
{
    /// <summary>Verifies that a home-directory identity and credential-shaped folder name never reach provider context.</summary>
    [Fact]
    public void Build_HomeDirectoryAndCredential_RedactsBothBeforePromptUse()
    {
        var secret = "sk-proj-abcdefghijklmnopqrstuvwxyz0123456789";
        var snapshot = new RuntimeContextSnapshot(
            $@"C:\Users\Ada\workspace\{secret}",
            @"C:\Users\Ada",
            "Windows 11 Pro",
            RuntimePlatform.Windows,
            "X64",
            16,
            "AMD Ryzen 9",
            34_359_738_368UL,
            ["NVIDIA RTX"]);

        var context = RuntimeContextService.Build(snapshot, new SensitiveDataRedactor());
        var promptBlock = context.ToPromptBlock();

        Assert.Equal("~/workspace/[redacted-openai-key]", context.WorkingDirectory);
        Assert.Contains("Windows console; prefer PowerShell 7 syntax", context.CommandEnvironment, StringComparison.Ordinal);
        Assert.Contains("AMD Ryzen 9; 16 logical processor(s), X64", context.Cpu, StringComparison.Ordinal);
        Assert.Equal("32.0 GiB physical memory", context.Memory);
        Assert.Equal("NVIDIA RTX", context.Gpu);
        Assert.DoesNotContain("Ada", promptBlock, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, promptBlock, StringComparison.Ordinal);
        Assert.Contains("Privacy boundary", promptBlock, StringComparison.Ordinal);
    }

    /// <summary>Verifies that unavailable probe data is explicit and does not cause fabricated platform details.</summary>
    [Fact]
    public void Build_UnavailableHardware_UsesExplicitFallbacks()
    {
        var snapshot = new RuntimeContextSnapshot(
            "/srv/work",
            null,
            null,
            RuntimePlatform.Other,
            "Arm64",
            4,
            null,
            null,
            []);

        var context = RuntimeContextService.Build(snapshot, new SensitiveDataRedactor());

        Assert.Equal("/srv/work", context.WorkingDirectory);
        Assert.Equal("unavailable", context.OperatingSystem);
        Assert.Equal("terminal family unavailable; ask before assuming shell syntax", context.CommandEnvironment);
        Assert.Equal("4 logical processor(s), Arm64", context.Cpu);
        Assert.Equal("physical total unavailable to the portable runtime", context.Memory);
        Assert.Equal("not exposed by the portable runtime", context.Gpu);
    }

    /// <summary>Verifies that a network working directory with a server identity is withheld from the provider-bound context.</summary>
    [Fact]
    public void Build_NetworkWorkingDirectory_WithholdsServerIdentity()
    {
        var snapshot = new RuntimeContextSnapshot(
            @"\\build-server\private-share\workspace",
            @"C:\Users\Ada",
            "Windows 11 Pro",
            RuntimePlatform.Windows,
            "X64",
            8,
            null,
            null,
            []);

        var context = RuntimeContextService.Build(snapshot, new SensitiveDataRedactor());

        Assert.Equal("network working directory (path withheld)", context.WorkingDirectory);
        Assert.DoesNotContain("build-server", context.ToPromptBlock(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-share", context.ToPromptBlock(), StringComparison.OrdinalIgnoreCase);
    }
}

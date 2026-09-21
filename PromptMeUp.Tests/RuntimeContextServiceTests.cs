// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class RuntimeContextServiceTests
{
    /// <summary>Verifies every supported platform produces only a family, numeric version, and the fixed approved-command shell.</summary>
    [Theory]
    [InlineData("Windows", "Windows 11.0.22631")]
    [InlineData("Linux", "Linux 6.8.0")]
    [InlineData("MacOS", "macOS 15.1.0")]
    public void Build_SupportedPlatform_UsesFamilyVersionAndEffectiveShell(string platformName, string expectedOperatingSystem)
    {
        var platform = Enum.Parse<RuntimePlatform>(platformName);
        var snapshot = new RuntimeContextSnapshot(platform, new Version(platform == RuntimePlatform.Windows ? 11 : platform == RuntimePlatform.Linux ? 6 : 15, platform == RuntimePlatform.Windows ? 0 : platform == RuntimePlatform.Linux ? 8 : 1, platform == RuntimePlatform.Windows ? 22631 : 0), @"C:\Users\Ada\workspace", @"C:\Users\Ada");

        var context = RuntimeContextService.Build(snapshot, "Bash", true, new SensitiveDataRedactor());

        Assert.Equal(expectedOperatingSystem, context.OperatingSystem);
        Assert.Equal("PowerShell 7 (pwsh -NoLogo -NoProfile -NonInteractive)", context.CommandShell);
        Assert.Equal("Bash", context.PreferredScriptInterpreter);
        Assert.True(context.IsPreferredScriptInterpreterAvailable);
        Assert.Equal("~/workspace", context.WorkingDirectory);
    }

    /// <summary>Verifies the explicit fallback does not fabricate an operating-system version or a local interpreter.</summary>
    [Fact]
    public void Build_UnknownPlatform_UsesExplicitNonIdentifyingFallbacks()
    {
        var context = RuntimeContextService.Build(
            new RuntimeContextSnapshot(RuntimePlatform.Other, null, null, null),
            "Batch / CMD",
            false,
            new SensitiveDataRedactor());

        Assert.Equal("Unknown operating system", context.OperatingSystem);
        Assert.Equal("Batch / CMD", context.PreferredScriptInterpreter);
        Assert.False(context.IsPreferredScriptInterpreterAvailable);
    }

    /// <summary>Verifies each supported provider language receives the same safe technical facts without path or hardware disclosure.</summary>
    [Theory]
    [InlineData("it")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public void ToPromptBlock_SupportedLanguage_ContainsOnlySafeTechnicalFacts(string language)
    {
        var context = RuntimeContextService.Build(
            new RuntimeContextSnapshot(RuntimePlatform.Windows, new Version(11, 0, 22631), @"C:\Users\Ada\workspace", @"C:\Users\Ada"),
            "Python 3",
            true,
            new SensitiveDataRedactor());

        var promptBlock = context.ToPromptBlock(language, includeWorkingDirectory: false);

        Assert.Contains("Windows 11.0.22631", promptBlock, StringComparison.Ordinal);
        Assert.Contains("PowerShell 7 (pwsh -NoLogo -NoProfile -NonInteractive)", promptBlock, StringComparison.Ordinal);
        Assert.Contains("Python 3", promptBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("CPU", promptBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("GPU", promptBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("Current working directory", promptBlock, StringComparison.Ordinal);
        if (!string.Equals(language, "en", StringComparison.Ordinal))
        {
            Assert.NotEqual(context.ToPromptBlock("en", includeWorkingDirectory: false), promptBlock);
        }
    }

    /// <summary>Verifies the provider context derives its selected interpreter and availability from the catalog without exposing its executable path.</summary>
    [Fact]
    public void GetCurrent_SelectedInterpreter_DoesNotExposeExecutablePath()
    {
        var context = new RuntimeContextService(new ScriptLanguageCatalog(), new SensitiveDataRedactor()).GetCurrent(ScriptLanguage.PowerShell);

        var promptBlock = context.ToPromptBlock("en", includeWorkingDirectory: true);

        Assert.Equal("PowerShell 7", context.PreferredScriptInterpreter);
        Assert.DoesNotContain("\\Users\\", promptBlock, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preferred script interpreter: PowerShell 7", promptBlock, StringComparison.Ordinal);
    }
}

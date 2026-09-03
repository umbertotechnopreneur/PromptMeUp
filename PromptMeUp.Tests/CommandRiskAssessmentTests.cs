// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class CommandRiskAssessmentTests
{
    /// <summary>Verifies mutations, compound commands, redirects, and unknown Git arguments never inherit a diagnostic prefix's low score.</summary>
    [Theory]
    [InlineData("git branch -D review-placeholder", 75)]
    [InlineData("git branch --delete review-placeholder", 75)]
    [InlineData("git branch new-branch", 35)]
    [InlineData("Get-Date | Set-Content -LiteralPath review-placeholder.txt", 75)]
    [InlineData("Get-Date > review-placeholder.txt", 35)]
    [InlineData("Get-Date; Write-Output next", 35)]
    [InlineData("Get-Date\nWrite-Output next", 35)]
    [InlineData("Get-Item $(Write-Output item)", 35)]
    [InlineData("git log --output=review-placeholder.txt", 35)]
    [InlineData("git diff --ext-diff", 35)]
    public void AssessLocally_MutatingOrUnrecognizedShape_NeverLow(string command, int minimumScore)
    {
        var result = CommandRiskAssessmentService.AssessLocally(command, "en");

        Assert.True(result.Score >= minimumScore);
        Assert.NotEqual(CommandRiskLevel.Low, result.Level);
    }

    /// <summary>Verifies complete known diagnostic forms retain useful low-risk guidance.</summary>
    [Theory]
    [InlineData("git status --short")]
    [InlineData("git branch --show-current")]
    [InlineData("Get-ChildItem -LiteralPath ./source")]
    [InlineData("dotnet --info")]
    public void AssessLocally_CompleteInspectionShape_RemainsLow(string command) =>
        Assert.Equal(CommandRiskLevel.Low, CommandRiskAssessmentService.AssessLocally(command, "en").Level);

    /// <summary>Verifies that a familiar inspection command remains in the low local-risk band.</summary>
    [Fact]
    public void AssessLocally_ReadOnlyCommand_ReturnsLowRisk()
    {
        var result = CommandRiskAssessmentService.AssessLocally("Get-Location", "en");

        Assert.Equal(15, result.Score);
        Assert.Equal(CommandRiskLevel.Low, result.Level);
    }

    /// <summary>Verifies that a broad recursive deletion remains critical without an AI review.</summary>
    [Fact]
    public void AssessLocally_RecursiveForcedDelete_ReturnsCriticalRisk()
    {
        var result = CommandRiskAssessmentService.AssessLocally("Remove-Item C:\\data -Recurse -Force", "en");

        Assert.Equal(95, result.Score);
        Assert.Equal(CommandRiskLevel.Critical, result.Level);
    }

    /// <summary>Verifies that an explicit elevation request receives at least a high local score.</summary>
    [Fact]
    public void AssessLocally_RunAsElevation_ReturnsHighRisk()
    {
        var result = CommandRiskAssessmentService.AssessLocally("Start-Process pwsh -Verb RunAs", "en");

        Assert.True(result.Score >= 75);
        Assert.Equal(CommandRiskLevel.High, result.Level);
    }
}

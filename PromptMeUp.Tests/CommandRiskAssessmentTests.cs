// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class CommandRiskAssessmentTests
{
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

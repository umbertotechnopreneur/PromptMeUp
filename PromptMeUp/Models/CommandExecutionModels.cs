// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public enum CommandExecutionMode
{
    Confirm,
    Direct
}

public enum CommandRiskLevel
{
    Unknown,
    Low,
    Medium,
    High,
    Critical
}

public sealed record CommandRiskAssessment(
    int Score,
    CommandRiskLevel Level,
    string DescriptionMarkdown,
    bool UsedAi,
    string? Advisory)
{
    public bool CanRunDirect => UsedAi && Score is >= 0 and < 60
        && Level is CommandRiskLevel.Low or CommandRiskLevel.Medium;
}

public sealed class ApprovedCommand
{
    /// <summary>Creates the execution capability after the workflow completes the selected authorization gate.</summary>
    private ApprovedCommand(string text, CommandRiskAssessment assessment)
    {
        Text = text;
        Assessment = assessment;
        ApprovedAt = DateTimeOffset.UtcNow;
        AuthorizationId = Guid.NewGuid().ToString("N");
    }

    public string Text { get; }

    public CommandRiskAssessment Assessment { get; }

    public DateTimeOffset ApprovedAt { get; }

    internal string AuthorizationId { get; }

    /// <summary>Creates the short-lived capability after confirmation or an explicitly requested direct countdown.</summary>
    internal static ApprovedCommand Create(string text, CommandRiskAssessment assessment) => new(text, assessment);
}

public sealed record CommandExecutionResult(
    string Command,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut,
    bool OutputTruncated,
    long ElapsedMilliseconds);

// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

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
    string? Advisory);

public sealed class ApprovedCommand
{
    /// <summary>Creates the execution capability after the view has obtained explicit user authorization.</summary>
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

    /// <summary>Creates the short-lived command capability after the user confirms the rendered preview.</summary>
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

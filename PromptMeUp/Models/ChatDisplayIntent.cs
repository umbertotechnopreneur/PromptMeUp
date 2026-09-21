// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Describes explicit chat preferences; summary and execution confirmation are global while command suggestions stay in the current chat.</summary>
public sealed record ChatDisplayIntent(
    bool? ShowSessionSummary,
    bool? ShowCommandSuggestions,
    bool? RequireExecutionConfirmation,
    bool ContinueChat)
{
    public bool HasChanges => ShowSessionSummary.HasValue
        || ShowCommandSuggestions.HasValue
        || RequireExecutionConfirmation.HasValue;
}

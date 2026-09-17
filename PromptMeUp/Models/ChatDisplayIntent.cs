// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Describes explicit display changes for the current chat without controlling command execution.</summary>
public sealed record ChatDisplayIntent(bool? ShowSessionSummary, bool? ShowCommandSuggestions, bool ContinueChat)
{
    public bool HasChanges => ShowSessionSummary.HasValue || ShowCommandSuggestions.HasValue;
}

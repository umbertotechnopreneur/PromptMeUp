// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record ScriptArtifact(string Explanation, string Source);

/// <summary>Identifies the user-selected follow-up for a reviewed script draft.</summary>
public enum ScriptAction
{
    Save,
    Execute,
    DoNothing,
    Validate,
    Revise
}

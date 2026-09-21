// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Identifies a script language supported by the reviewed artifact workflow.</summary>
public enum ScriptLanguage
{
    PowerShell,
    Batch,
    Bash,
    Python,
    JavaScript
}

/// <summary>Describes one supported script language without coupling the UI to process execution.</summary>
public sealed record ScriptLanguageDefinition(
    ScriptLanguage Language,
    string StorageValue,
    string DisplayName,
    string FileExtension,
    string PromptGuidance,
    bool SupportsValidation,
    IReadOnlyList<string> ExecutableCandidates);

/// <summary>Reports whether the selected language can run on this computer at the moment.</summary>
public sealed record ScriptRuntimeAvailability(bool IsAvailable, string? ExecutablePath);

/// <summary>Combines the rendered artifact and its selected language-specific actions for a passive view.</summary>
public sealed record ScriptPresentation(
    ScriptArtifact Artifact,
    string? Original,
    ScriptLanguageDefinition Language,
    ScriptRuntimeAvailability Runtime,
    string OutputPath,
    bool OutputWasSpecified);

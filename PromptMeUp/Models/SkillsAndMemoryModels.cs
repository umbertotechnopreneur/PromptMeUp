// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Describes one inspected skill package; approval is bound to its content fingerprint.</summary>
public sealed record SkillDefinition(string Name, string Description, string Version, string Instructions,
    string Directory, string Origin, string Fingerprint, string? UnavailableReason, IReadOnlyDictionary<string, string> Scripts)
{
    public string Icon { get; init; } = "🧩";

    public string Color { get; init; } = "#89DCEB";
}

/// <summary>Identifies a workflow-owned skill menu action without coupling a view to menu positions.</summary>
internal enum SkillMenuAction
{
    EnableSkillsAndMemory,
    DisableSkillsAndMemory,
    Import,
    ToggleAutomatic,
    ClearSelection,
    ToggleSkill,
    SelectSkill,
    RunSkill,
    ToggleCapture,
    ToggleReminder,
    ReviewObservations,
    ClearObservations,
    Dream,
    Heartbeat,
    Confirm,
    Cancel
}

/// <summary>Groups related skill commands in the compact sidebar without coupling the view to workflow behavior.</summary>
internal sealed record SkillMenuGroup(
    string Label,
    string Description,
    IReadOnlyList<SkillMenuItem> Items,
    string Icon = "🧩");

/// <summary>Supplies one inline skill command and its workflow payload to the interactive view.</summary>
internal sealed record SkillMenuItem(
    SkillMenuAction Action,
    string Label,
    string Description,
    SkillDefinition? Skill = null,
    string Icon = "🧩",
    string? ActionName = null,
    bool CanExecute = true,
    string? InputLabel = null,
    string InitialInput = "",
    bool MultilineInput = false);

/// <summary>Returns one central skill action together with its reviewed inline field value.</summary>
internal sealed record SkillMenuSelection(SkillMenuItem Item, string? Input);

/// <summary>Contains only explicitly enabled skills and memory behavior for the user.</summary>
public sealed record SkillsAndMemorySettings(bool Enabled = false, bool AutomaticSkills = false,
    bool CaptureObservations = false, bool MaintenanceReminder = false);

/// <summary>Identifies one bounded, redacted conversation observation and its original session.</summary>
public sealed record LearningObservation(string Id, string SessionId, string Text, DateTimeOffset CreatedAt);

/// <summary>Represents an untrusted suggestion that requires local validation and explicit review.</summary>
public sealed record MemoryProposal(string Id, string Kind, string Operation, string Text, string Rationale,
    IReadOnlyList<string> SourceIds, IReadOnlyList<PersistentMemory> Targets, DateTimeOffset CreatedAt);

/// <summary>Reports maintenance success separately from an attempt that failed or produced no candidates.</summary>
public sealed record MaintenanceState(string Kind, DateTimeOffset? LastSuccess, int PendingCount);

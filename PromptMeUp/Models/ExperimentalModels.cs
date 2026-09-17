// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Describes one inspected skill package; approval is bound to its content fingerprint.</summary>
public sealed record SkillDefinition(string Name, string Description, string Version, string Instructions,
    string Directory, string Origin, string Fingerprint, string? UnavailableReason, IReadOnlyDictionary<string, string> Scripts);

/// <summary>Contains only explicitly enabled experimental behavior in the current project.</summary>
public sealed record ExperimentalSettings(bool Enabled = false, bool AutomaticSkills = false,
    bool CaptureObservations = false, bool MaintenanceReminder = false);

/// <summary>Identifies one bounded, redacted conversation observation and its original session.</summary>
public sealed record LearningObservation(string Id, string SessionId, string Text, DateTimeOffset CreatedAt);

/// <summary>Represents an untrusted suggestion that requires local validation and explicit review.</summary>
public sealed record MemoryProposal(string Id, string Kind, string Operation, string Text, string Rationale,
    IReadOnlyList<string> SourceIds, IReadOnlyList<PersistentMemory> Targets, DateTimeOffset CreatedAt);

/// <summary>Reports maintenance success separately from an attempt that failed or produced no candidates.</summary>
public sealed record MaintenanceState(string Kind, DateTimeOffset? LastSuccess, int PendingCount);

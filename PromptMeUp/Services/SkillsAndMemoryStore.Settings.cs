// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public sealed partial class SkillsAndMemoryStore
{
    /// <summary>Rejects malformed drafts and missing capture or clearing acknowledgements before opening a write transaction.</summary>
    internal void ValidateFeatureChanges(SettingsFeatureChanges changes)
    {
        if (changes is null || changes.Expected is null || changes.Settings is null || changes.Skills is null
            || changes.Skills.Count > 128)
        {
            throw InvalidLearning();
        }
        var previousCapture = changes.Expected.Enabled && changes.Expected.CaptureObservations;
        var nextCapture = changes.Settings.Enabled && changes.Settings.CaptureObservations;
        if ((nextCapture && !previousCapture && !changes.CaptureConsent)
            || (ClearsLearning(changes) && !changes.ClearLearningConsent))
        {
            throw InvalidLearning();
        }
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var change in changes.Skills)
        {
            if (change is null || change.Skill is null || string.IsNullOrEmpty(change.Skill.Name)
                || change.Skill.Name.Length > 64 || !char.IsAsciiLetter(change.Skill.Name[0])
                || !change.Skill.Name.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
                || change.Skill.Name != change.Skill.Name.ToLowerInvariant() || change.Skill.Name == "metals-dev-monitor"
                || !names.Add(change.Skill.Name) || !ValidApproval(change.Skill.Fingerprint, required: true)
                || !ValidApproval(change.ExpectedApprovalFingerprint, required: false)
                || change.ExpectedEnabled != (change.ExpectedApprovalFingerprint == change.Skill.Fingerprint))
            {
                throw InvalidLearning();
            }
            ValidatePreference("skill:" + change.Skill.Name, change.Enabled ? change.Skill.Fingerprint : string.Empty);
        }
    }

    /// <summary>Saves a fully validated draft only when every preference and approval still matches its reviewed snapshot.</summary>
    internal async Task SaveFeatureChangesAsync(SettingsFeatureChanges changes, CancellationToken ct)
    {
        ValidateFeatureChanges(changes);
        var scope = PersistentMemoryService.GlobalScope;
        await using var connection = await OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();
        var previous = await ReadSettingsAsync(connection, transaction, scope, ct).ConfigureAwait(false);
        if (previous != changes.Expected)
        {
            throw InvalidLearning();
        }
        foreach (var change in changes.Skills)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT value FROM skills_and_memory_settings WHERE scope_key = $scope AND name = $name;";
            command.Parameters.AddWithValue("$scope", scope);
            command.Parameters.AddWithValue("$name", "skill:" + change.Skill.Name);
            var current = await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
            if (current != change.ExpectedApprovalFingerprint)
            {
                throw InvalidLearning();
            }
        }

        // Complete every compare before writing so a stale skill never partly saves the privacy switches.
        if (previous != changes.Settings)
        {
            await WritePreferenceAsync(connection, transaction, scope, "settings", JsonSerializer.Serialize(changes.Settings, Json), ct).ConfigureAwait(false);
            if (previous.Enabled != changes.Settings.Enabled || previous.CaptureObservations != changes.Settings.CaptureObservations)
            {
                await AdvanceRevisionAsync(connection, transaction, scope, ct).ConfigureAwait(false);
            }
            if (ClearsLearning(changes))
            {
                await ClearLearningAsync(connection, transaction, scope, ct).ConfigureAwait(false);
            }
        }
        foreach (var change in changes.Skills.Where(change => change.ExpectedEnabled != change.Enabled))
        {
            await WritePreferenceAsync(connection, transaction, scope, "skill:" + change.Skill.Name,
                change.Enabled ? change.Skill.Fingerprint : string.Empty, ct).ConfigureAwait(false);
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Identifies a reviewed transition that discards global learning evidence without normalizing unrelated flags.</summary>
    private static bool ClearsLearning(SettingsFeatureChanges changes) =>
        (changes.Expected.Enabled && !changes.Settings.Enabled)
        || (changes.Expected.CaptureObservations && !changes.Settings.CaptureObservations);

    /// <summary>Accepts only exact content fingerprints or the distinct missing and explicitly disabled approval states.</summary>
    private static bool ValidApproval(string? value, bool required) =>
        (!required && string.IsNullOrEmpty(value)) || (value is { Length: 64 } && value.All(Uri.IsHexDigit));
}

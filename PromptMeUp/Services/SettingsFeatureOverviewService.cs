// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Reads project feature drafts and saves explicitly reviewed preferences without executing skill actions.</summary>
public sealed class SettingsFeatureOverviewService(SkillsAndMemoryStore store, SkillCatalogService skills,
    ILogger<SettingsFeatureOverviewService>? logger = null, ILocalizationService? localization = null)
{
    private readonly ILogger<SettingsFeatureOverviewService> _logger = logger ?? NullLogger<SettingsFeatureOverviewService>.Instance;
    private readonly ILocalizationService _text = localization ?? new LocalizationService();

    /// <summary>Refreshes preferences and catalog counts; effective activation also requires the project master switch.</summary>
    public async Task<SettingsFeatureOverview> ReadAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var settings = await store.SettingsAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        try
        {
            var catalog = skills.List();
            var states = new List<SettingsSkillState>();
            foreach (var skill in catalog)
            {
                ct.ThrowIfCancellationRequested();
                var approval = await store.GetAsync("skill:" + skill.Name, ct).ConfigureAwait(false);
                states.Add(new(skill, approval == skill.Fingerprint) { ApprovalFingerprint = approval });
            }
            ct.ThrowIfCancellationRequested();
            var enabledCount = settings.Enabled ? states.Count(state => state.Enabled && state.Skill.UnavailableReason is null) : 0;
            return new(settings, enabledCount, catalog.Count) { Skills = states.ToArray() };
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogWarning("Skill catalog unavailable ({ErrorType}).", exception.GetType().Name);
            return new(settings, 0, 0) { CatalogUnavailable = true };
        }
    }

    /// <summary>Validates every newly active package before atomically saving the reviewed project feature draft.</summary>
    public async Task SaveAsync(SettingsFeatureChanges changes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        store.ValidateFeatureChanges(changes);
        var snapshot = changes with { Skills = changes.Skills.ToArray() };
        if (!snapshot.Expected.Enabled && snapshot.Settings.Enabled)
        {
            snapshot = await ValidateReactivationSnapshotAsync(snapshot, ct).ConfigureAwait(false);
        }
        foreach (var change in snapshot.Skills)
        {
            ct.ThrowIfCancellationRequested();
            if (change.Enabled && (!change.ExpectedEnabled || (!snapshot.Expected.Enabled && snapshot.Settings.Enabled)))
            {
                await skills.ValidateActivationAsync(change.Skill, ct).ConfigureAwait(false);
            }
        }
        await store.SaveFeatureChangesAsync(snapshot, ct).ConfigureAwait(false);
    }

    /// <summary>Requires a readable catalog and reviewed snapshots for every currently approved package before reactivation.</summary>
    private async Task<SettingsFeatureChanges> ValidateReactivationSnapshotAsync(SettingsFeatureChanges changes, CancellationToken ct)
    {
        var catalog = skills.List();
        var reviewed = changes.Skills.ToDictionary(change => change.Skill.Name, StringComparer.Ordinal);
        var complete = changes.Skills.ToList();
        foreach (var skill in catalog)
        {
            ct.ThrowIfCancellationRequested();
            var approval = await store.GetAsync("skill:" + skill.Name, ct).ConfigureAwait(false);
            if (reviewed.TryGetValue(skill.Name, out var change))
            {
                if (approval == skill.Fingerprint && change.Skill.Fingerprint != skill.Fingerprint)
                {
                    throw new InvalidOperationException(_text.Text("Lab.Invalid"));
                }
                continue;
            }
            if (approval == skill.Fingerprint)
            {
                throw new InvalidOperationException(_text.Text("Lab.Invalid"));
            }
            // Also compare omitted inactive rows so another process cannot approve one during this save.
            complete.Add(new(skill, false, false) { ExpectedApprovalFingerprint = approval });
        }
        return changes with { Skills = complete.ToArray() };
    }
}

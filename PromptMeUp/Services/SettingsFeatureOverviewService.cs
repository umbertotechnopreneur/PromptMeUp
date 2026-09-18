// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Reads the local settings overview without activating features or accessing retained learning data.</summary>
public sealed class SettingsFeatureOverviewService(ExperimentalStore store, SkillCatalogService skills,
    ILogger<SettingsFeatureOverviewService>? logger = null)
{
    private readonly ILogger<SettingsFeatureOverviewService> _logger = logger ?? NullLogger<SettingsFeatureOverviewService>.Instance;

    /// <summary>Refreshes preferences and catalog counts; effective activation also requires the experiment master switch.</summary>
    public async Task<SettingsFeatureOverview> ReadAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var settings = await store.SettingsAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        try
        {
            var catalog = skills.List();
            var enabledCount = 0;
            if (settings.Enabled)
            {
                foreach (var skill in catalog)
                {
                    ct.ThrowIfCancellationRequested();
                    if (await skills.IsEnabledAsync(skill, ct).ConfigureAwait(false))
                    {
                        enabledCount++;
                    }
                }
            }
            ct.ThrowIfCancellationRequested();
            return new(settings, enabledCount, catalog.Count);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogWarning("Skill catalog unavailable ({ErrorType}).", exception.GetType().Name);
            return new(settings, 0, 0) { CatalogUnavailable = true };
        }
    }
}

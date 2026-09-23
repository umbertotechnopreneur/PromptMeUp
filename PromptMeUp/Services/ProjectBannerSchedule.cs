// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PromptMeUp.Services;

/// <summary>Persists the local day on which the project footer was last shown.</summary>
public interface IProjectBannerSchedule
{
    /// <summary>Returns whether the project footer may be shown and records the current local day when it may.</summary>
    bool TryMarkRenderedToday();
}

/// <summary>Coordinates the once-per-local-day project footer across application invocations.</summary>
public sealed class ProjectBannerSchedule(string statePath, TimeProvider clock, ILogger<ProjectBannerSchedule> logger) : IProjectBannerSchedule
{
    private readonly string _statePath = string.IsNullOrWhiteSpace(statePath)
        ? throw new ArgumentException("A project-banner state path is required.", nameof(statePath))
        : statePath;

    /// <summary>Records the current local date unless the same date is already stored.</summary>
    public bool TryMarkRenderedToday()
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var marker = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            if (File.Exists(_statePath)
                && string.Equals(File.ReadAllText(_statePath).Trim(), marker, StringComparison.Ordinal))
            {
                return false;
            }

            File.WriteAllText(_statePath, marker, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Unable to persist the project-banner display date.");
            return true;
        }
    }
}

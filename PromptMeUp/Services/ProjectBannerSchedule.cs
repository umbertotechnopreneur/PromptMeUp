// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PromptMeUp.Services;

/// <summary>Persists the local day on which the project footer was last shown.</summary>
public interface IProjectBannerSchedule
{
    /// <summary>Returns whether the project footer may be shown and records the current local day when it may.</summary>
    bool TryMarkRenderedToday();

    /// <summary>Returns whether the opening tip may be shown and records the current local day when it may.</summary>
    bool TryMarkOpeningTipRenderedToday();
}

/// <summary>Coordinates independent once-per-local-day opening and footer banners across invocations.</summary>
public sealed class ProjectBannerSchedule(string statePath, string openingTipStatePath, TimeProvider clock, ILogger<ProjectBannerSchedule> logger) : IProjectBannerSchedule
{
    private readonly string _statePath = string.IsNullOrWhiteSpace(statePath)
        ? throw new ArgumentException("A project-banner state path is required.", nameof(statePath))
        : statePath;
    private readonly string _openingTipStatePath = string.IsNullOrWhiteSpace(openingTipStatePath)
        ? throw new ArgumentException("An opening-tip state path is required.", nameof(openingTipStatePath))
        : openingTipStatePath;

    /// <summary>Records the current local date unless the same date is already stored.</summary>
    public bool TryMarkRenderedToday() => TryMarkToday(_statePath, "project banner");

    /// <summary>Records the opening tip separately from the project footer for the current local date.</summary>
    public bool TryMarkOpeningTipRenderedToday() => TryMarkToday(_openingTipStatePath, "opening tip");

    /// <summary>Persists one independent daily marker, allowing rendering when persistence is unavailable.</summary>
    private bool TryMarkToday(string path, string label)
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var marker = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            if (File.Exists(path)
                && string.Equals(File.ReadAllText(path).Trim(), marker, StringComparison.Ordinal))
            {
                return false;
            }

            File.WriteAllText(path, marker, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Unable to persist the {BannerLabel} display date.", label);
            return true;
        }
    }
}

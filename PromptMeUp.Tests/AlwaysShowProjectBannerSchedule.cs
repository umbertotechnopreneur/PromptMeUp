// SPDX-License-Identifier: MIT

using PromptMeUp.Services;

namespace PromptMeUp.Tests;

/// <summary>Keeps view tests independent from the persisted daily footer schedule.</summary>
internal sealed class AlwaysShowProjectBannerSchedule : IProjectBannerSchedule
{
    /// <summary>Allows each test fixture to render its expected banner.</summary>
    public bool TryMarkRenderedToday() => true;

    /// <summary>Allows opening-tip rendering in test fixtures.</summary>
    public bool TryMarkOpeningTipRenderedToday() => true;
}

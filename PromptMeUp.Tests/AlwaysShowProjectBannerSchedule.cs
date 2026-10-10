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

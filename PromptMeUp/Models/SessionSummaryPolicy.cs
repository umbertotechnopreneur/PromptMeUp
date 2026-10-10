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


namespace PromptMeUp.Models;

/// <summary>Identifies when a session summary could be displayed within an application workflow.</summary>
internal enum SessionSummaryTiming
{
    AfterVisibleTurn,
    WorkflowEnd
}

/// <summary>Applies the persisted visibility preference consistently without suppressing an end-of-workflow summary.</summary>
internal static class SessionSummaryPolicy
{
    /// <summary>Determines whether the configured policy permits a summary at the requested workflow point.</summary>
    internal static bool ShouldRender(AppSettings settings, SessionSummaryTiming timing)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return timing is SessionSummaryTiming.WorkflowEnd || settings.ShowSessionSummaryDuringWork;
    }
}

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

/// <summary>Describes explicit chat preferences; summary and execution confirmation are global while command suggestions stay in the current chat.</summary>
public sealed record ChatDisplayIntent(
    bool? ShowSessionSummary,
    bool? ShowCommandSuggestions,
    bool? RequireExecutionConfirmation,
    bool ContinueChat)
{
    public bool HasChanges => ShowSessionSummary.HasValue
        || ShowCommandSuggestions.HasValue
        || RequireExecutionConfirmation.HasValue;
}

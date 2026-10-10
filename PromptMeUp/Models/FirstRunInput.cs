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

/// <summary>Names explicit navigation without interpreting a blank field as consent.</summary>
public enum FirstRunAction { Next, Back, Exit }

/// <summary>Returns one reviewed value and the chosen navigation action.</summary>
public sealed record FirstRunInput<T>(FirstRunAction Action, T Value);

/// <summary>Keeps the reviewed personal, learning, command, and skill choices together for onboarding.</summary>
public sealed record FirstRunPreferences(string Name, bool Enabled, bool Capture,
    bool ConfirmCommands, IReadOnlyList<string> EnabledSkills);

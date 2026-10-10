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


namespace PromptMeUp.Views;

/// <summary>Provides Unicode-safe text-element boundaries for terminal editors.</summary>
internal static class TerminalTextElements
{
    /// <summary>Finds the preceding Unicode text element for cursor movement and deletion.</summary>
    public static int Previous(string value, int index) =>
        System.Globalization.StringInfo.ParseCombiningCharacters(value).LastOrDefault(start => start < index);

    /// <summary>Finds the following Unicode text element without splitting a composed character.</summary>
    public static int Next(string value, int index) =>
        System.Globalization.StringInfo.ParseCombiningCharacters(value).FirstOrDefault(start => start > index, value.Length);
}

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


using System.Text;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

internal static class ContextTokenEstimator
{
    /// <summary>Estimates text tokens locally from UTF-8 bytes without claiming provider tokenization accuracy.</summary>
    internal static long Text(string text) => string.IsNullOrEmpty(text)
        ? 0
        : Math.Max(1, (long)Math.Ceiling(Encoding.UTF8.GetByteCount(text) / 4d));

    /// <summary>Includes the same per-message overhead in memory budgeting and provider request estimates.</summary>
    internal static long Messages(IEnumerable<ChatMessage> messages) =>
        messages.Sum(message => Text(message.Content) + 4);
}

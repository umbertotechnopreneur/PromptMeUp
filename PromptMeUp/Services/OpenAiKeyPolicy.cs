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


namespace PromptMeUp.Services;

public static class OpenAiKeyPolicy
{
    // Windows Credential Manager accepts at most 2,560 bytes; the vault uses UTF-16.
    public const int MaximumLength = 1_280;

    /// <summary>Checks whether a value has the local shape expected for an OpenAI secret without authenticating it.</summary>
    public static bool IsPlausible(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret)
            || secret.Length < 20
            || secret.Length > MaximumLength
            || !secret.StartsWith("sk-", StringComparison.Ordinal)
            || !string.Equals(secret, secret.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        return secret.All(character => !char.IsWhiteSpace(character) && !char.IsControl(character));
    }
}

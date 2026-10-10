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

namespace PromptMeUp.Services;

internal static class CredentialFieldPolicy
{
    private static readonly string[] SecretSuffixes =
    [
        "passwd", "secret",
        "accesstoken", "authtoken", "bearertoken", "refreshtoken", "sessiontoken",
        "idtoken", "securitytoken", "accesskey", "secretkey", "privatekey",
        "sharedaccesssignature"
    ];

    /// <summary>Recognizes credential field names across common casing and separator conventions without matching token metrics.</summary>
    internal static bool IsSecret(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var normalized = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            if (character is '_' or '-' or '.' || char.IsWhiteSpace(character))
            {
                continue;
            }

            normalized.Append(char.ToLowerInvariant(character));
        }

        var field = normalized.ToString();
        if (field is "token"
            || field.Contains("apikey", StringComparison.Ordinal)
            || field.Contains("adminkey", StringComparison.Ordinal)
            || field.Contains("password", StringComparison.Ordinal)
            || field.Contains("authorization", StringComparison.Ordinal))
        {
            return true;
        }

        // Credential suffixes allow provider prefixes while leaving counters, types, and expiry metadata intact.
        return SecretSuffixes.Any(suffix => field.EndsWith(suffix, StringComparison.Ordinal));
    }
}

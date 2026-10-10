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

public static class OpenAiEndpointPolicy
{
    /// <summary>Allows only the official HTTPS Responses endpoint so an API key cannot be redirected to another host.</summary>
    public static bool IsAllowed(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint))
        {
            return false;
        }

        return endpoint.Scheme == Uri.UriSchemeHttps
               && endpoint.Host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase)
               && endpoint.Port == 443
               && endpoint.AbsolutePath.TrimEnd('/').Equals("/v1/responses", StringComparison.Ordinal)
               && string.IsNullOrEmpty(endpoint.Query)
               && string.IsNullOrEmpty(endpoint.Fragment)
               && string.IsNullOrEmpty(endpoint.UserInfo);
    }
}

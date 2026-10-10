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

public sealed class OpenAiEndpointPolicyTests
{
    /// <summary>Verifies that the official Responses endpoint is accepted.</summary>
    [Fact]
    public void IsAllowed_OfficialResponsesEndpoint_ReturnsTrue() =>
        Assert.True(OpenAiEndpointPolicy.IsAllowed("https://api.openai.com/v1/responses"));

    /// <summary>Verifies that a different HTTPS host cannot receive the configured OpenAI key.</summary>
    [Fact]
    public void IsAllowed_AlternateHttpsHost_ReturnsFalse() =>
        Assert.False(OpenAiEndpointPolicy.IsAllowed("https://example.test/v1/responses"));

    /// <summary>Verifies that query parameters cannot alter the official endpoint contract.</summary>
    [Fact]
    public void IsAllowed_EndpointWithQuery_ReturnsFalse() =>
        Assert.False(OpenAiEndpointPolicy.IsAllowed("https://api.openai.com/v1/responses?forward=true"));
}

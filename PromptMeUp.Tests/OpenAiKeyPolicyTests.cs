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

public sealed class OpenAiKeyPolicyTests
{
    /// <summary>Verifies that a sufficiently long synthetic key with the expected prefix is accepted locally.</summary>
    [Fact]
    public void IsPlausible_WellFormedSyntheticValue_ReturnsTrue()
    {
        var value = "sk-" + new string('x', 24);

        Assert.True(OpenAiKeyPolicy.IsPlausible(value));
    }

    /// <summary>Verifies that missing, short, wrongly prefixed, padded, and whitespace-containing values are rejected.</summary>
    [Fact]
    public void IsPlausible_MalformedValues_ReturnsFalse()
    {
        var embeddedWhitespace = "sk-" + new string('x', 12) + " " + new string('x', 12);
        var padded = " " + "sk-" + new string('x', 24);

        Assert.False(OpenAiKeyPolicy.IsPlausible(null));
        Assert.False(OpenAiKeyPolicy.IsPlausible("sk-short"));
        Assert.False(OpenAiKeyPolicy.IsPlausible(new string('x', 30)));
        Assert.False(OpenAiKeyPolicy.IsPlausible(padded));
        Assert.False(OpenAiKeyPolicy.IsPlausible(embeddedWhitespace));
    }
}

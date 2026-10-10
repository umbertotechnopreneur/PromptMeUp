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

public sealed class SensitiveDataRedactorTests
{
    /// <summary>Verifies that recognizable OpenAI, bearer, and environment credentials never survive persistence redaction.</summary>
    [Fact]
    public void Redact_CredentialShapes_RemovesSecretValues()
    {
        var secret = string.Concat("sk-", "proj-", "abcdefghijklmnopqrstuvwxyz0123456789");
        var input = $"OPENAI_API_KEY={secret}\nAuthorization: Bearer abcdefghijklmnop\nplain text";

        var result = new SensitiveDataRedactor().Redact(input);

        Assert.DoesNotContain(secret, result, StringComparison.Ordinal);
        Assert.DoesNotContain("abcdefghijklmnop", result, StringComparison.Ordinal);
        Assert.Contains("plain text", result, StringComparison.Ordinal);
    }
}

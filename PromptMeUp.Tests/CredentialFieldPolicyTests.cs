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

public sealed class CredentialFieldPolicyTests
{
    /// <summary>Recognizes common credential fields in JSON, configuration, and environment output.</summary>
    [Theory]
    [InlineData("accessToken")]
    [InlineData("ACCESS_TOKEN")]
    [InlineData("access-token")]
    [InlineData("access.token")]
    [InlineData("access token")]
    [InlineData("providerAccessToken")]
    [InlineData("SecretAccessKey")]
    [InlineData("AWS_SECRET_ACCESS_KEY")]
    [InlineData("SessionToken")]
    [InlineData("AWS_SESSION_TOKEN")]
    [InlineData("refreshToken")]
    [InlineData("id_token")]
    [InlineData("AuthToken")]
    [InlineData("bearerToken")]
    [InlineData("SecurityToken")]
    [InlineData("token")]
    [InlineData("OPENAI_API_KEY")]
    [InlineData("OPENAI_ADMIN_KEY")]
    [InlineData("apiKey")]
    [InlineData("adminKey")]
    [InlineData("DbPassword")]
    [InlineData("PASSWD")]
    [InlineData("Authorization")]
    [InlineData("clientSecret")]
    [InlineData("PrivateKey")]
    [InlineData("SharedAccessSignature")]
    [InlineData("apiKeyValue")]
    [InlineData("adminKeyValue")]
    [InlineData("passwordValue")]
    [InlineData("authorizationHeader")]
    public void IsSecret_CredentialField_ReturnsTrue(string field)
    {
        Assert.True(CredentialFieldPolicy.IsSecret(field));
    }

    /// <summary>Keeps token usage, type, expiry, and unrelated metadata available for audit analysis.</summary>
    [Theory]
    [InlineData("inputTokens")]
    [InlineData("output_tokens")]
    [InlineData("totalTokens")]
    [InlineData("cachedInputTokens")]
    [InlineData("tokenCount")]
    [InlineData("tokenType")]
    [InlineData("token_type")]
    [InlineData("maxTokens")]
    [InlineData("contextTokenLimit")]
    [InlineData("accessTokenExpiresAt")]
    [InlineData("sessionTokenCount")]
    [InlineData("refreshTokenLifetime")]
    [InlineData("Credentials")]
    [InlineData("model")]
    [InlineData("")]
    public void IsSecret_MetadataField_ReturnsFalse(string field)
    {
        Assert.False(CredentialFieldPolicy.IsSecret(field));
    }
}

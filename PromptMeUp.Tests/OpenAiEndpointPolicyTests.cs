// SPDX-License-Identifier: MIT

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

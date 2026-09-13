// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class CredentialValueRegressionTests
{
    /// <summary>Verifies complete PowerShell and JSON values are masked while adjacent non-secret data is retained.</summary>
    [Theory]
    [InlineData("PASSWORD='synthetic''tail'; safe=keep")]
    [InlineData("PASSWORD=\"synthetic`\"tail\"; safe=keep")]
    [InlineData("PASSWORD=\"synthetic\"\"tail\"; safe=keep")]
    [InlineData("PASSWORD='synthetic'unsafetail; safe=keep")]
    [InlineData("PASSWORD=synthetic` unsafetail; safe=keep")]
    [InlineData("PASSWORD='synthetic\\'; safe=keep")]
    [InlineData("PASSWORD=\"synthetic\\\"; safe=keep")]
    [InlineData("PASSWORD='synthetic\nunsafetail'; safe=keep")]
    [InlineData("PASSWORD=[redacted-credential]synthetic-unsafetail; safe=keep")]
    [InlineData("PASSWORD=[redacted-not-a-marker]synthetic-unsafetail; safe=keep")]
    [InlineData("123PASSWORD=synthetic-unsafetail; safe=keep")]
    [InlineData("Authorization: Basic synthetic-unsafetail; safe=keep")]
    [InlineData("{\"accessToken\":\"synthetic value\",\"safe\":\"keep\"}")]
    [InlineData("{\"SecretAccessKey\":\"synthetic\\\"unsafetail\",\"safe\":\"keep\"}")]
    [InlineData("{\"SessionToken\":\"synthetic`\",\"safe\":\"keep\"}")]
    [InlineData("{\"parent\":{\"accessToken\":\"synthetic value\"},\"safe\":\"keep\"}")]
    [InlineData("{\\\"accessToken\\\":\\\"synthetic value\\\",\\\"safe\\\":\\\"keep\\\"}")]
    [InlineData("log: {\"accessToken\":[\"synthetic-first\",\"synthetic-tail\"],\"safe\":\"keep\"}")]
    [InlineData("log: {\"accessToken\":{\"value\":\"synthetic-tail\"},\"safe\":\"keep\"}")]
    [InlineData("log: {\"Secret Access Key\":\"synthetic-tail\",\"safe\":\"keep\"}")]
    [InlineData("log: {\"access\\u0054oken\":\"synthetic-tail\",\"safe\":\"keep\"}")]
    public void Redact_CompleteCredential_RemovesWholeValueAndRetainsSafeNeighbor(string input)
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.Redact(input);

        Assert.DoesNotContain("synthetic", result, StringComparison.Ordinal);
        Assert.DoesNotContain("tail", result, StringComparison.Ordinal);
        Assert.Contains("keep", result, StringComparison.Ordinal);
        Assert.Equal(result, redactor.Redact(result));
        if (input.StartsWith("{\"", StringComparison.Ordinal))
        {
            using var json = JsonDocument.Parse(result);
            Assert.Equal("keep", json.RootElement.GetProperty("safe").GetString());
        }
    }

    /// <summary>Verifies truncated quoted values cannot leave a suffix behind when the output limit removes the closing quote.</summary>
    [Theory]
    [InlineData("PASSWORD='synthetic unsafetail")]
    [InlineData("PASSWORD=\"synthetic unsafetail")]
    [InlineData("PASSWORD=\"synthetic`\" unsafetail")]
    [InlineData("{\"accessToken\":\"synthetic unsafetail")]
    [InlineData("{\\\"accessToken\\\":\\\"synthetic unsafetail")]
    public void Redact_TruncatedCredential_RemovesRemainder(string input)
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.Redact(input);

        Assert.DoesNotContain("synthetic", result, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafetail", result, StringComparison.Ordinal);
        Assert.Contains("[redacted-credential]", result, StringComparison.Ordinal);
        Assert.Equal(result, redactor.Redact(result));
    }

    /// <summary>Verifies token usage, token types, and expiration metadata do not become credential assignments.</summary>
    [Fact]
    public void Redact_TokenMetadata_RemainsUnchanged()
    {
        const string input = "{\"inputTokens\":32,\"output_tokens\":7,\"tokenType\":\"Bearer\",\"accessTokenExpiresAt\":\"tomorrow\",\"safe\":\"keep\"}";

        Assert.Equal(input, new SensitiveDataRedactor().Redact(input));
    }

    /// <summary>Verifies authentication headers retain their scheme and redact short tokens as well as known bearer shapes.</summary>
    [Theory]
    [InlineData("Bearer")]
    [InlineData("Basic")]
    public void Redact_AuthorizationHeader_PreservesScheme(string scheme)
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.Redact($"Authorization: {scheme} xyz");

        Assert.Equal($"Authorization: {scheme} [redacted-credential]", result);
        Assert.Equal(result, redactor.Redact(result));
    }
}

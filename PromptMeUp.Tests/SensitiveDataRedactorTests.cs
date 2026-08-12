// SPDX-License-Identifier: MIT

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

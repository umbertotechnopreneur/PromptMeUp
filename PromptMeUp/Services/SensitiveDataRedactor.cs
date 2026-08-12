// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;

namespace PromptMeUp.Services;

public interface ISensitiveDataRedactor
{
    string Redact(string value);
}

public sealed partial class SensitiveDataRedactor : ISensitiveDataRedactor
{
    /// <summary>Redacts recognizable credentials while preserving ordinary prompt and command text.</summary>
    public string Redact(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var redacted = OpenAiKeyPattern().Replace(value, "[redacted-openai-key]");
        redacted = BearerPattern().Replace(redacted, "$1[redacted-bearer-token]");
        return CredentialAssignmentPattern().Replace(redacted, "$1$2[redacted-credential]");
    }

    /// <summary>Matches current OpenAI secret-key prefixes without requiring a specific key length.</summary>
    [GeneratedRegex(@"(?<![A-Za-z0-9_-])sk-(?:proj-|admin-)?[A-Za-z0-9_-]{16,}", RegexOptions.CultureInvariant)]
    private static partial Regex OpenAiKeyPattern();

    /// <summary>Matches an HTTP bearer credential while retaining the authentication scheme.</summary>
    [GeneratedRegex(@"(?i)\b(Bearer\s+)[A-Za-z0-9._~+/=-]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex BearerPattern();

    /// <summary>Matches common credential assignments in shell output, configuration snippets, and JSON-like text.</summary>
    [GeneratedRegex(@"(?im)\b(OPENAI_(?:API|ADMIN)_KEY|[A-Z0-9_]*(?:PASSWORD|PASSWD|SECRET|API_?KEY|ACCESS_TOKEN|AUTH_TOKEN|BEARER_TOKEN))\b(\s*[:=]\s*)(?:""[^""\r\n]*""|'[^'\r\n]*'|[^\s,;]+)", RegexOptions.CultureInvariant)]
    private static partial Regex CredentialAssignmentPattern();
}

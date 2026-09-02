// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.RegularExpressions;

namespace PromptMeUp.Services;

public interface ISensitiveDataRedactor
{
    string Redact(string value);
}

public sealed partial class SensitiveDataRedactor : ISensitiveDataRedactor
{
    /// <summary>Redacts recognizable credentials while preserving ordinary prompt and command text.</summary>
    public string Redact(string value) => RedactText(value, 0);

    /// <summary>Decodes serialized JSON strings before inspecting credentials, with a bounded nesting depth.</summary>
    private static string RedactText(string value, int depth)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var trimmed = value.AsSpan().Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            try
            {
                var decoded = JsonSerializer.Deserialize<string>(value)!;
                var safe = depth < 8 ? RedactText(decoded, depth + 1) : "[redacted]";
                return string.Equals(decoded, safe, StringComparison.Ordinal) ? value : JsonSerializer.Serialize(safe);
            }
            catch (JsonException)
            {
                // Ordinary quoted command text need not be a complete JSON string.
            }
        }

        var redacted = OpenAiKeyPattern().Replace(value, "[redacted-openai-key]");
        redacted = BearerPattern().Replace(redacted, "$1[redacted-bearer-token]");
        return CredentialAssignmentPattern().Replace(redacted, RedactAssignment);
    }

    /// <summary>Preserves JSON quoting and makes repeated redaction stable for stored placeholders.</summary>
    private static string RedactAssignment(Match match)
    {
        var value = match.Groups["value"].Value;
        var quote = value.StartsWith("\\\"", StringComparison.Ordinal) ? "\\\""
            : value.StartsWith('"') ? "\""
            : value.StartsWith('\'') ? "'" : string.Empty;
        var content = quote.Length > 0 && value.EndsWith(quote, StringComparison.Ordinal)
            ? value[quote.Length..^quote.Length]
            : value;
        var replacement = content is "[redacted]" or "[redacted-credential]" or "[redacted-openai-key]" or "[redacted-bearer-token]"
            ? value
            : quote + "[redacted-credential]" + quote;
        return match.Groups["prefix"].Value + replacement;
    }

    /// <summary>Matches current OpenAI secret-key prefixes without requiring a specific key length.</summary>
    [GeneratedRegex(@"(?<![A-Za-z0-9_-])sk-(?:proj-|admin-)?[A-Za-z0-9_-]{16,}", RegexOptions.CultureInvariant)]
    private static partial Regex OpenAiKeyPattern();

    /// <summary>Matches an HTTP bearer credential while retaining the authentication scheme.</summary>
    [GeneratedRegex(@"(?i)\b(Bearer\s+)[A-Za-z0-9._~+/=-]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex BearerPattern();

    /// <summary>Matches common credential assignments in shell output, configuration snippets, and JSON-like text.</summary>
    [GeneratedRegex("""(?im)\b(?<prefix>(?:OPENAI_(?:API|ADMIN)_KEY|[A-Z0-9_]*(?:PASSWORD|PASSWD|SECRET|API_?KEY|ACCESS_TOKEN|AUTH_TOKEN|BEARER_TOKEN))\b(?:\\?["'])?\s*[:=]\s*)(?<value>\\"(?:(?:\\){3}"|\\[^"]|[^\\\r\n])*\\"|"(?:\\.|[^"\\\r\n])*"|'(?:\\.|[^'\\\r\n])*'|\[redacted(?:-(?:credential|openai-key|bearer-token))?\]|[^\s,;}\]]+)""", RegexOptions.CultureInvariant)]
    private static partial Regex CredentialAssignmentPattern();
}

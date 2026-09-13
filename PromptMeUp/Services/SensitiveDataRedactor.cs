// SPDX-License-Identifier: MIT

using System.Text;
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
        if (trimmed.Length >= 2 && trimmed[0] is '{' or '[')
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                using var buffer = new MemoryStream();
                using var writer = new Utf8JsonWriter(buffer);
                var changed = RedactJson(document.RootElement, writer, depth);
                writer.Flush();
                return changed ? Encoding.UTF8.GetString(buffer.ToArray()) : value;
            }
            catch (JsonException)
            {
                // Truncated JSON and shell text still need the conservative assignment scanner below.
            }
        }
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
        return RedactAssignments(redacted);
    }

    /// <summary>Redacts decoded JSON field names and nested strings while preserving non-secret types and metadata.</summary>
    private static bool RedactJson(JsonElement element, Utf8JsonWriter writer, int depth)
    {
        var changed = false;
        if (element.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in element.EnumerateObject())
            {
                var safeName = depth < 8 ? RedactText(property.Name, depth + 1) : "[redacted]";
                writer.WritePropertyName(safeName);
                changed |= !string.Equals(property.Name, safeName, StringComparison.Ordinal);
                if (CredentialFieldPolicy.IsSecret(property.Name))
                {
                    if (property.Value.ValueKind == JsonValueKind.String && IsRedactedMarker(property.Value.GetString()!))
                    {
                        property.Value.WriteTo(writer);
                        continue;
                    }
                    writer.WriteStringValue("[redacted-credential]");
                    changed = true;
                }
                else
                {
                    changed |= RedactJson(property.Value, writer, depth);
                }
            }
            writer.WriteEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in element.EnumerateArray())
            {
                changed |= RedactJson(item, writer, depth);
            }
            writer.WriteEndArray();
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            var content = element.GetString()!;
            var safe = depth < 8 ? RedactText(content, depth + 1) : "[redacted]";
            writer.WriteStringValue(safe);
            changed = !string.Equals(content, safe, StringComparison.Ordinal);
        }
        else
        {
            element.WriteTo(writer);
        }
        return changed;
    }

    /// <summary>Recognizes complete placeholders so repeated redaction remains stable.</summary>
    private static bool IsRedactedMarker(string value) =>
        value is "[redacted]" or "[redacted-credential]" or "[redacted-openai-key]" or "[redacted-bearer-token]";

    /// <summary>Finds credential fields and consumes their complete values without swallowing unrelated assignments.</summary>
    private static string RedactAssignments(string text)
    {
        var output = new StringBuilder(text.Length);
        var copied = 0;
        var searchFrom = 0;
        while (CredentialAssignmentPattern().Match(text, searchFrom) is { Success: true } match)
        {
            var start = match.Index + match.Length;
            searchFrom = start;
            var name = match.Groups["name"].Value;
            if (name.Contains("\\u", StringComparison.Ordinal))
            {
                name = JsonSerializer.Deserialize<string>($"\"{name}\"")!;
            }
            if (!CredentialFieldPolicy.IsSecret(name) || start == text.Length)
            {
                continue;
            }
            if (name.Contains("authorization", StringComparison.OrdinalIgnoreCase))
            {
                start = SkipAuthorizationScheme(text, start);
            }

            var quote = text.AsSpan(start).StartsWith("\\\"", StringComparison.Ordinal) ? "\\\""
                : text[start] is '"' or '\'' ? text[start].ToString() : string.Empty;
            var shellValue = match.Groups["separator"].Value == "=";
            var end = ReadValueEnd(text, start, shellValue);
            if (end == start)
            {
                continue;
            }
            var value = text[start..end];
            var content = quote.Length > 0 && value.Length >= quote.Length * 2 && value.EndsWith(quote, StringComparison.Ordinal)
                ? value[quote.Length..^quote.Length]
                : value;
            var replacement = IsRedactedMarker(content)
                ? value
                : quote + "[redacted-credential]" + quote;
            output.Append(text.AsSpan(copied, start - copied));
            output.Append(replacement);
            copied = end;
            searchFrom = end;
        }
        return copied == 0 ? text : output.Append(text.AsSpan(copied)).ToString();
    }

    /// <summary>Preserves an unquoted HTTP authentication scheme while redacting its credential token.</summary>
    private static int SkipAuthorizationScheme(string text, int start)
    {
        foreach (var scheme in new[] { "Bearer", "Basic" })
        {
            var end = start + scheme.Length;
            if (end < text.Length && text.AsSpan(start, scheme.Length).Equals(scheme, StringComparison.OrdinalIgnoreCase)
                && text[end] is ' ' or '\t')
            {
                while (end < text.Length && text[end] is ' ' or '\t')
                {
                    end++;
                }
                return end < text.Length ? end : start;
            }
        }
        return start;
    }

    /// <summary>Reads a scalar assignment, including quoted PowerShell segments and escaped serialized JSON strings.</summary>
    private static int ReadValueEnd(string text, int start, bool shellValue)
    {
        if (text.AsSpan(start).StartsWith("\\\"", StringComparison.Ordinal))
        {
            return ReadSerializedQuoteEnd(text, start);
        }
        var index = start;
        if (text.AsSpan(start).StartsWith("[redacted", StringComparison.Ordinal))
        {
            var close = text.IndexOf(']', start);
            if (close >= 0)
            {
                index = close + 1;
            }
        }
        while (index < text.Length && !IsValueBoundary(text[index]))
        {
            if (text[index] is '{' or '[')
            {
                index = ReadContainerEnd(text, index, shellValue);
                if (!shellValue)
                {
                    return index;
                }
            }
            else if (shellValue && text[index] == '@' && index + 2 < text.Length
                && text[index + 1] is '"' or '\'' && text[index + 2] is '\r' or '\n')
            {
                index = ReadHereStringEnd(text, index);
            }
            else if (text[index] is '"' or '\'')
            {
                index = ReadQuoteEnd(text, index, shellValue);
                if (!shellValue)
                {
                    return index;
                }
            }
            else if (shellValue && text[index] == '`')
            {
                index = Math.Min(text.Length, index + 2);
            }
            else
            {
                index++;
            }
        }
        return index;
    }

    /// <summary>Consumes a credential-valued JSON container inside log text, including an incomplete captured container.</summary>
    private static int ReadContainerEnd(string text, int start, bool shellValue)
    {
        var depth = 0;
        for (var index = start; index < text.Length; index++)
        {
            if (text[index] is '"' or '\'')
            {
                index = ReadQuoteEnd(text, index, shellValue) - 1;
            }
            else if (text[index] is '{' or '[')
            {
                depth++;
            }
            else if (text[index] is '}' or ']' && --depth == 0)
            {
                return index + 1;
            }
        }
        return text.Length;
    }

    /// <summary>Consumes PowerShell here-strings until a quote-at terminator at the start of a line.</summary>
    private static int ReadHereStringEnd(string text, int start)
    {
        var quote = text[start + 1];
        for (var index = start + 3; index + 1 < text.Length; index++)
        {
            if (text[index] == quote && text[index + 1] == '@' && text[index - 1] is '\n' or '\r')
            {
                return index + 2;
            }
        }
        return text.Length;
    }

    /// <summary>Consumes JSON escapes or PowerShell doubled quotes and backticks, redacting to EOF if truncated.</summary>
    private static int ReadQuoteEnd(string text, int start, bool shellValue)
    {
        var quote = text[start];
        var index = start + 1;
        while (index < text.Length)
        {
            if (quote == '"' && text[index] == (shellValue ? '`' : '\\'))
            {
                index = Math.Min(text.Length, index + 2);
            }
            else if (text[index] == quote)
            {
                // PowerShell allows doubled single and double quotes inside a quoted value.
                if ((shellValue || quote == '\'') && index + 1 < text.Length && text[index + 1] == quote)
                {
                    index += 2;
                }
                else
                {
                    return index + 1;
                }
            }
            else
            {
                index++;
            }
        }
        return text.Length;
    }

    /// <summary>Reads a quote escaped by one JSON serialization layer without stopping on embedded escaped quotes.</summary>
    private static int ReadSerializedQuoteEnd(string text, int start)
    {
        var index = start + 2;
        while (index < text.Length)
        {
            if (text[index] != '\\')
            {
                index++;
                continue;
            }
            var slashStart = index;
            while (index < text.Length && text[index] == '\\')
            {
                index++;
            }
            // One slash closes the string; three encode an inner quote, repeating every four slashes.
            if (index < text.Length && text[index] == '"' && (index - slashStart) % 4 == 1)
            {
                return index + 1;
            }
        }
        return text.Length;
    }

    /// <summary>Identifies delimiters outside a quoted scalar value.</summary>
    private static bool IsValueBoundary(char character) => char.IsWhiteSpace(character) || character is ',' or ';' or '}' or ']';

    /// <summary>Matches current OpenAI secret-key prefixes without requiring a specific key length.</summary>
    [GeneratedRegex(@"(?<![A-Za-z0-9_-])sk-(?:proj-|admin-)?[A-Za-z0-9_-]{16,}", RegexOptions.CultureInvariant)]
    private static partial Regex OpenAiKeyPattern();

    /// <summary>Matches an HTTP bearer credential while retaining the authentication scheme.</summary>
    [GeneratedRegex(@"(?i)\b(Bearer\s+)[A-Za-z0-9._~+/=-]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex BearerPattern();

    /// <summary>Finds candidate field names; the shared policy decides which values require redaction.</summary>
    [GeneratedRegex("""(?i)(?:\\?["'](?<name>(?:\\u[0-9a-f]{4}|[A-Z0-9_ .-])+)\\?["']|\b(?<name>[A-Z0-9_][A-Z0-9_.-]*)(?:\\?["'])?)\s*(?<separator>[:=])\s*""", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex CredentialAssignmentPattern();
}

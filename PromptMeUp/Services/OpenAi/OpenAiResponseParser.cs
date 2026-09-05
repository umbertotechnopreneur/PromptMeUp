// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.RegularExpressions;
using PromptMeUp.Models;

namespace PromptMeUp.Services.OpenAi;

internal static partial class OpenAiResponseParser
{
    private const int MaximumSuggestedCommands = 8;
    private const int MaximumSuggestionLabelLength = 160;
    private const int MaximumSuggestionCommandLength = 4_096;

    /// <summary>Parses stable provider fields and, for chat, the strict user-facing response envelope.</summary>
    internal static AiResponse ParseResponse(
        string json,
        int statusCode,
        long elapsedMilliseconds,
        string? providerRequestId,
        bool parseStructuredChatResponse = false)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (string.Equals(ReadOptionalString(root, "status"), "incomplete", StringComparison.OrdinalIgnoreCase))
        {
            throw CreateMissingTextException(root, statusCode);
        }
        var text = ReadOutputText(root);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw CreateMissingTextException(root, statusCode);
        }

        var responseText = text.Trim();
        var suggestedCommands = Array.Empty<SuggestedCommand>();
        if (parseStructuredChatResponse)
        {
            var chat = ParseChatResponse(responseText, statusCode);
            responseText = chat.Markdown;
            suggestedCommands = chat.SuggestedCommands.ToArray();
        }

        var usage = root.TryGetProperty("usage", out var usageElement)
            ? ParseUsage(usageElement)
            : EmptyUsage;
        return new AiResponse(
            ReadOptionalString(root, "id") ?? Guid.NewGuid().ToString("N"),
            ReadOptionalString(root, "model") ?? "unknown",
            responseText,
            usage,
            new AiContextUsage(usage.InputTokens, usage.OutputTokens, 0, usage.InputTokens, 0, 0, false),
            null,
            null,
            statusCode,
            elapsedMilliseconds,
            providerRequestId)
        {
            SuggestedCommands = suggestedCommands
        };
    }

    /// <summary>Reads provider accounting independently of whether its answer satisfies the local content contract.</summary>
    internal static AiResponseAccounting ParseAccounting(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new AiResponseAccounting(ReadOptionalString(root, "id"), ReadOptionalString(root, "model"),
            root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object ? ParseUsage(usage) : EmptyUsage);
    }

    /// <summary>Parses command suggestions only from the structured chat envelope and fails closed on invalid data.</summary>
    internal static ChatResponseContent ParseChatResponse(string text, int statusCode)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Chat response must be an object.");
            }

            var markdown = ReadRequiredString(root, "answer_markdown");
            if (!root.TryGetProperty("commands", out var commands) || commands.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("Chat command collection is missing.");
            }
            if (commands.GetArrayLength() > MaximumSuggestedCommands)
            {
                throw new JsonException("Chat command collection exceeds its limit.");
            }

            var parsed = new List<SuggestedCommand>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in commands.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException("Chat command must be an object.");
                }

                var label = ReadRequiredString(item, "label");
                var command = ReadRequiredString(item, "command");
                if (label.Length > MaximumSuggestionLabelLength
                    || command.Length > MaximumSuggestionCommandLength
                    || ContainsControlCharacter(label)
                    || ContainsControlCharacter(command)
                    || !seen.Add(command))
                {
                    throw new JsonException("Chat command is not safely shaped.");
                }

                // A candidate is actionable only when the user can already inspect the exact same text in the answer.
                if (!markdown.Contains(command, StringComparison.Ordinal) || SensitiveCommandPattern().IsMatch(command))
                {
                    continue;
                }

                parsed.Add(new SuggestedCommand(label, command));
            }

            return new ChatResponseContent(markdown, parsed);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            throw new OpenAiRequestException(
                "The AI chat response did not match the required safe structure.",
                "invalid_chat_response",
                statusCode,
                exception);
        }
    }

    /// <summary>Parses the deliberately small JSON contract returned by the risk-review prompt.</summary>
    internal static CommandRiskAssessment ParseRiskAssessment(string text)
    {
        var json = StripCodeFence(text);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var score = Math.Clamp(root.GetProperty("score").GetInt32(), 0, 100);
            var levelText = root.GetProperty("level").GetString() ?? string.Empty;
            var description = root.GetProperty("description_markdown").GetString();
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new JsonException("Risk description is empty.");
            }

            var level = levelText.ToLowerInvariant() switch
            {
                "low" => CommandRiskLevel.Low,
                "medium" => CommandRiskLevel.Medium,
                "high" => CommandRiskLevel.High,
                "critical" => CommandRiskLevel.Critical,
                _ => ScoreToLevel(score)
            };
            return new CommandRiskAssessment(score, level, description.Trim(), true, null);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new OpenAiRequestException("The AI command review returned an invalid structure.", "invalid_risk_review", null, exception);
        }
    }

    /// <summary>Extracts the standard provider error message while ignoring malformed envelopes.</summary>
    internal static string? ReadApiError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.GetProperty("error").GetProperty("message").GetString();
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Concatenates output_text entries from every assistant output message.</summary>
    private static string ReadOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (string.Equals(ReadOptionalString(part, "type"), "output_text", StringComparison.Ordinal)
                    && ReadOptionalString(part, "text") is { Length: > 0 } value)
                {
                    parts.Add(value);
                }
            }
        }

        return string.Join(Environment.NewLine, parts);
    }

    /// <summary>Creates a stable error for incomplete, refused, or absent text output.</summary>
    private static OpenAiRequestException CreateMissingTextException(JsonElement root, int statusCode)
    {
        if (string.Equals(ReadOptionalString(root, "status"), "incomplete", StringComparison.OrdinalIgnoreCase))
        {
            return new OpenAiRequestException("OpenAI returned an incomplete response.", "incomplete_response", statusCode);
        }

        if (ReadRefusal(root) is not null)
        {
            return new OpenAiRequestException("OpenAI refused this request.", "model_refusal", statusCode);
        }

        return new OpenAiRequestException("OpenAI returned no text output.", "empty_response", statusCode);
    }

    /// <summary>Finds a provider refusal without treating it as an executable or displayable answer.</summary>
    private static string? ReadRefusal(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (string.Equals(ReadOptionalString(part, "type"), "refusal", StringComparison.Ordinal)
                    && ReadOptionalString(part, "refusal") is { Length: > 0 } refusal)
                {
                    return refusal;
                }
            }
        }

        return null;
    }

    /// <summary>Extracts token counters from the Responses API usage object.</summary>
    private static AiUsageMetrics ParseUsage(JsonElement usage)
    {
        var input = ReadInt64(usage, "input_tokens");
        var output = ReadInt64(usage, "output_tokens");
        var total = ReadInt64(usage, "total_tokens");
        var cached = usage.TryGetProperty("input_tokens_details", out var inputDetails)
            ? ReadInt64(inputDetails, "cached_tokens")
            : 0;
        var cacheWrite = usage.TryGetProperty("input_tokens_details", out inputDetails)
            ? ReadInt64(inputDetails, "cache_write_tokens")
            : 0;
        var reasoning = usage.TryGetProperty("output_tokens_details", out var outputDetails)
            ? ReadInt64(outputDetails, "reasoning_tokens")
            : 0;
        return new AiUsageMetrics(input, cached, cacheWrite, output, reasoning, total == 0 ? input + output : total);
    }

    /// <summary>Maps a numeric advisory score to its display level.</summary>
    private static CommandRiskLevel ScoreToLevel(int score) => score switch
    {
        >= 85 => CommandRiskLevel.Critical,
        >= 60 => CommandRiskLevel.High,
        >= 30 => CommandRiskLevel.Medium,
        _ => CommandRiskLevel.Low
    };

    /// <summary>Removes an optional JSON code fence without interpreting other Markdown.</summary>
    private static string StripCodeFence(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLine = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstLine >= 0 && lastFence > firstLine
            ? trimmed[(firstLine + 1)..lastFence].Trim()
            : trimmed;
    }

    /// <summary>Reads a scalar token counter and treats absent fields as zero.</summary>
    private static long ReadInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? Math.Max(0, value)
            : 0;

    /// <summary>Reads a nullable JSON string.</summary>
    private static string? ReadOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    /// <summary>Reads a non-empty string that the structured chat contract requires.</summary>
    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        var value = ReadOptionalString(element, propertyName)?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Required string '{propertyName}' is missing.");
        }

        return value;
    }

    /// <summary>Rejects control characters because one candidate must always remain an exact single command line.</summary>
    private static bool ContainsControlCharacter(string value) => value.Any(char.IsControl);

    /// <summary>Identifies recognizable credentials that must not become selectable command arguments.</summary>
    [GeneratedRegex(@"(?ix)(?:\bsk-(?:proj-|admin-)?[A-Za-z0-9_-]{16,}|\bbearer\s+[A-Za-z0-9._~+/=-]{8,}|\b[A-Z0-9_]*(?:PASSWORD|PASSWD|SECRET|API_?KEY|ACCESS_TOKEN|AUTH_TOKEN|BEARER_TOKEN)\b\s*[:=]\s*[^\s,;]+)")]
    private static partial Regex SensitiveCommandPattern();

    internal static AiUsageMetrics EmptyUsage { get; } = new(0, 0, 0, 0, 0, 0);
}

internal sealed record ChatResponseContent(string Markdown, IReadOnlyList<SuggestedCommand> SuggestedCommands);

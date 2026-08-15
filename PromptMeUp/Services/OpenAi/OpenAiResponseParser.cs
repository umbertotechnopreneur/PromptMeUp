// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Services.OpenAi;

internal static class OpenAiResponseParser
{
    /// <summary>Parses the stable response fields and tolerates non-text output items.</summary>
    internal static AiResponse ParseResponse(
        string json,
        int statusCode,
        long elapsedMilliseconds,
        string? providerRequestId)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var text = ReadOutputText(root);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new OpenAiRequestException("OpenAI returned no text output.", "empty_response", statusCode);
        }

        var usage = root.TryGetProperty("usage", out var usageElement)
            ? ParseUsage(usageElement)
            : EmptyUsage;
        return new AiResponse(
            ReadOptionalString(root, "id") ?? Guid.NewGuid().ToString("N"),
            ReadOptionalString(root, "model") ?? "unknown",
            text.Trim(),
            usage,
            new AiContextUsage(usage.InputTokens, usage.OutputTokens, 0, usage.InputTokens, 0, 0, false),
            null,
            null,
            statusCode,
            elapsedMilliseconds,
            providerRequestId);
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

    internal static AiUsageMetrics EmptyUsage { get; } = new(0, 0, 0, 0, 0, 0);
}

// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using PromptMeUp.Models;

namespace PromptMeUp.Services.OpenAi;

internal static class OpenAiRequestBuilder
{
    private const long MinimumExplicitCachePrefixTokens = 1_024;

    /// <summary>Builds a provider payload without placing secrets in the serializable object.</summary>
    internal static IReadOnlyDictionary<string, object> BuildBody(
        PromptDefinition prompt,
        AppSettings settings,
        IReadOnlyList<ChatMessage> messages,
        string instructions,
        int maxOutputTokens)
    {
        var text = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["verbosity"] = ResolveVerbosity(settings.OutputDetail)
        };
        if (IsStructuredAssistantPrompt(prompt))
        {
            text["format"] = BuildChatResponseFormat();
        }
        else if (prompt.Id == "script-system")
        {
            text["format"] = FeatureResponseFormats.Script();
        }
        else if (prompt.Id == "plan-system")
        {
            text["format"] = FeatureResponseFormats.Plan();
        }

        var body = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["model"] = settings.Model,
            ["reasoning"] = new { effort = settings.ReasoningEffort },
            ["text"] = text,
            ["max_output_tokens"] = maxOutputTokens,
            ["store"] = false
        };

        if (settings.PromptCachingEnabled
            && IsGpt56(settings.Model)
            && EstimateTokens(instructions) >= MinimumExplicitCachePrefixTokens)
        {
            // A chat retains implicit checkpoints for its append-only history; a one-shot query avoids caching its unique suffix.
            body["input"] = BuildExplicitCacheInput(instructions, messages);
            body["prompt_cache_key"] = BuildPromptCacheKey(prompt, settings.Model, instructions);
            body["prompt_cache_options"] = new
            {
                mode = UsesGrowingConversation(prompt) ? "implicit" : "explicit",
                ttl = "30m"
            };
        }
        else
        {
            body["instructions"] = instructions;
            body["input"] = messages.Select(message => new
            {
                role = NormalizeRole(message.Role),
                content = message.Content
            }).ToArray();
            if (settings.PromptCachingEnabled)
            {
                body["prompt_cache_key"] = BuildPromptCacheKey(prompt, settings.Model, instructions);
                if (settings.Model == "gpt-5.5")
                {
                    body["prompt_cache_retention"] = "24h";
                }
            }
        }

        return body;
    }

    /// <summary>Combines immutable YAML, approved preferences, optional locale, and sanitized runtime context for assistant prompts.</summary>
    internal static string BuildInstructions(
        PromptDefinition prompt,
        AppSettings settings,
        string language,
        RuntimeContext? runtimeContext = null,
        ArtifactLimits? limits = null)
    {
        var artifactLimits = limits ?? ArtifactLimits.Default;
        var builder = new StringBuilder(prompt.ResolveText(language)
            .Replace("{max_script_bytes}", artifactLimits.MaxScriptBytes.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{max_plan_bytes}", artifactLimits.MaxPlanBytes.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        if (IsStructuredAssistantPrompt(prompt) || prompt.Metadata.GetValueOrDefault("runtime-context") == "sanitized")
        {
            if (!string.IsNullOrWhiteSpace(settings.CustomInstruction))
            {
                builder.AppendLine()
                    .AppendLine()
                    .AppendLine("<user-configured-preamble>")
                    .AppendLine(settings.CustomInstruction.Trim())
                    .Append("</user-configured-preamble>");
            }

            if (settings.IncludeWindowsLocation)
            {
                // Only coarse machine locale is included; precise location is neither requested nor inferred.
                builder.AppendLine()
                    .AppendLine()
                    .Append("Windows locale context: culture=")
                    .Append(System.Globalization.CultureInfo.CurrentCulture.Name)
                    .Append(", timezone=")
                    .Append(TimeZoneInfo.Local.Id)
                    .Append('.');
            }

            if (runtimeContext is not null)
            {
                builder.AppendLine()
                    .AppendLine()
                    .Append(runtimeContext.ToPromptBlock());
            }
        }

        return builder.ToString();
    }

    /// <summary>Builds a lightweight preflight estimate from the populated instruction and bounded message list.</summary>
    internal static AiContextUsage EstimateContext(
        string instructions,
        IReadOnlyList<ChatMessage> messages,
        string model)
    {
        var instructionTokens = EstimateTokens(instructions);
        var conversationTokens = messages.Sum(message => EstimateTokens(message.Content) + 4L);
        var latestPromptTokens = messages.LastOrDefault(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)) is { } latest
            ? EstimateTokens(latest.Content)
            : 0;
        return new AiContextUsage(
            instructionTokens + conversationTokens + 8,
            0,
            instructionTokens,
            conversationTokens,
            latestPromptTokens,
            AiModelCatalog.Resolve(model).ContextWindowTokens,
            true);
    }

    /// <summary>Chooses a bounded response budget and honors a smaller YAML diagnostic limit.</summary>
    internal static int ResolveMaxOutputTokens(PromptDefinition prompt, string detail, ArtifactLimits? limits = null)
    {
        if (prompt.Id is "script-system" or "plan-system")
        {
            return (limits ?? ArtifactLimits.Default).MaxOutputTokens;
        }
        var configured = detail switch
        {
            "compact" => 900,
            "detailed" => 3000,
            _ => 1800
        };
        return prompt.Metadata.TryGetValue("max-output-tokens", out var text)
               && int.TryParse(text, out var promptLimit)
               && promptLimit is > 0 and <= 16_384
            ? Math.Min(configured, promptLimit)
            : configured;
    }

    /// <summary>Places an explicit cache breakpoint after the stable instruction for GPT-5.6 requests.</summary>
    private static IReadOnlyList<object> BuildExplicitCacheInput(
        string instructions,
        IReadOnlyList<ChatMessage> messages)
    {
        var input = new List<object>
        {
            new
            {
                role = "developer",
                content = new object[]
                {
                    new
                    {
                        type = "input_text",
                        text = instructions,
                        prompt_cache_breakpoint = new { mode = "explicit" }
                    }
                }
            }
        };
        input.AddRange(messages.Select(message => (object)new
        {
            role = NormalizeRole(message.Role),
            content = message.Content
        }));
        return input;
    }

    /// <summary>Identifies the assistant prompt whose bounded history grows by appending reusable turns.</summary>
    private static bool UsesGrowingConversation(PromptDefinition prompt) =>
        string.Equals(prompt.Id, "chat-system", StringComparison.Ordinal);

    /// <summary>Creates a stable routing key without embedding instruction text or user data.</summary>
    private static string BuildPromptCacheKey(PromptDefinition prompt, string model, string instructions)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(instructions)))
            .ToLowerInvariant()[..16];
        return $"promptmeup:{model}:{prompt.Id}:v{prompt.Version}:{hash}";
    }

    /// <summary>Builds the strict user-facing chat envelope without granting the model a command-execution capability.</summary>
    private static object BuildChatResponseFormat() => new
    {
        type = "json_schema",
        name = "promptmeup_chat_response_v1",
        strict = true,
        schema = new
        {
            type = "object",
            properties = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["answer_markdown"] = new { type = "string" },
                ["commands"] = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            label = new { type = "string" },
                            command = new { type = "string" }
                        },
                        required = new[] { "label", "command" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "answer_markdown", "commands" },
            additionalProperties = false
        }
    };

    /// <summary>Approximates text tokens from UTF-8 payload size until provider usage supplies the exact count.</summary>
    private static long EstimateTokens(string text) => string.IsNullOrEmpty(text)
        ? 0
        : Math.Max(1, (long)Math.Ceiling(Encoding.UTF8.GetByteCount(text) / 4d));

    /// <summary>Maps output-detail preference to the Responses API verbosity vocabulary.</summary>
    private static string ResolveVerbosity(string detail) => detail switch
    {
        "compact" => "low",
        "detailed" => "high",
        _ => "medium"
    };

    /// <summary>Restricts conversation roles to those accepted by the provider.</summary>
    private static string NormalizeRole(string role) => role.ToLowerInvariant() switch
    {
        "assistant" => "assistant",
        "developer" => "developer",
        _ => "user"
    };

    /// <summary>Identifies assistant prompts that return the typed answer-and-command envelope.</summary>
    private static bool IsStructuredAssistantPrompt(PromptDefinition prompt) =>
        string.Equals(prompt.Id, "chat-system", StringComparison.OrdinalIgnoreCase)
        || string.Equals(prompt.Id, "query-system", StringComparison.OrdinalIgnoreCase)
        || prompt.Metadata.GetValueOrDefault("response-format") == "promptmeup-console-response-v1";

    /// <summary>Identifies the model family that supports explicit prompt-cache breakpoints.</summary>
    private static bool IsGpt56(string model) => model.StartsWith("gpt-5.6", StringComparison.OrdinalIgnoreCase);
}

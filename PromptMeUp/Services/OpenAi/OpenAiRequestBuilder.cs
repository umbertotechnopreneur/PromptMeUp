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
            text["format"] = BuildChatResponseFormat(SupportsAppGuide(prompt));
        }
        else if (prompt.Id == "script-system")
        {
            text["format"] = FeatureResponseFormats.Script();
        }
        else if (prompt.Id == "plan-system")
        {
            text["format"] = FeatureResponseFormats.Plan();
        }
        else if (prompt.Id == "memory-forget")
        {
            text["format"] = FeatureResponseFormats.MemoryForget();
        }
        else if (prompt.Id is "memory-dream" or "memory-heartbeat")
        {
            text["format"] = FeatureResponseFormats.MemoryReflection(prompt.Id == "memory-dream");
        }
        else if (prompt.Id == "chat-display-intent")
        {
            text["format"] = FeatureResponseFormats.ChatDisplayIntent();
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
        ArtifactLimits? limits = null,
        AppGuideContext? guide = null)
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

        ValidateGuideContext(guide);
        if (guide is { Topics.Count: > 0 })
        {
            if (!SupportsAppGuide(prompt))
            {
                throw new InvalidOperationException("This prompt does not support the application guide.");
            }
            builder.AppendLine().AppendLine().Append(guide.Text);
        }

        return builder.ToString();
    }

    /// <summary>Builds a lightweight preflight estimate from the populated instruction and bounded message list.</summary>
    internal static AiContextUsage EstimateContext(
        string instructions,
        IReadOnlyList<ChatMessage> messages,
        string model,
        AppGuideContext? guide = null)
    {
        ValidateGuideContext(guide);
        var instructionTokens = EstimateTokens(instructions);
        var conversationTokens = ContextTokenEstimator.Messages(messages);
        var userTokens = messages.Where(message => NormalizeRole(message.Role) == "user")
            .Sum(message => EstimateTokens(message.Content));
        var assistantTokens = messages.Where(message => NormalizeRole(message.Role) == "assistant")
            .Sum(message => EstimateTokens(message.Content));
        // Protocol envelopes and developer messages belong to the system share, leaving message text attributable by role.
        var systemTokens = instructionTokens + conversationTokens + 8 - userTokens - assistantTokens;
        var latestPromptTokens = messages.LastOrDefault(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)) is { } latest
            ? EstimateTokens(latest.Content)
            : 0;
        return new AiContextUsage(
            instructionTokens + conversationTokens + 8,
            0,
            systemTokens,
            conversationTokens,
            latestPromptTokens,
            AiModelCatalog.Resolve(model).ContextWindowTokens,
            true)
        {
            UserMessageTokens = userTokens,
            AssistantMessageTokens = assistantTokens,
            GuideTokens = guide?.Tokens ?? 0
        };
    }

    /// <summary>Shares the complete input ceiling between pre-send pruning, display, and the final provider guard.</summary>
    internal static long ResolveInputBudget(
        PromptDefinition prompt,
        AppSettings settings,
        int maxOutputTokens,
        ConversationContextLimits limits)
    {
        var window = AiModelCatalog.Resolve(settings.Model).ContextWindowTokens;
        var budget = Math.Min(checked(window * settings.MaxContextPercent / 100), window - maxOutputTokens);
        return prompt.Id is "chat-system" or "query-system"
            ? Math.Min(budget, limits.MaxInputTokens ?? settings.ContextTokenBudget)
            : budget;
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
    private static object BuildChatResponseFormat(bool supportsGuide)
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal)
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
        };
        if (supportsGuide)
        {
            properties["guide_topics"] = new
            {
                type = "array",
                items = new { type = "string", @enum = AppGuideService.Topics },
                maxItems = AppGuideService.MaxTopics
            };
        }
        return new
        {
            type = "json_schema",
            name = supportsGuide ? "promptmeup_chat_response_v2" : "promptmeup_chat_response_v1",
            strict = true,
            schema = new
            {
                type = "object",
                properties,
                required = supportsGuide
                    ? new[] { "answer_markdown", "commands", "guide_topics" }
                    : ["answer_markdown", "commands"],
                additionalProperties = false
            }
        };
    }

    /// <summary>Rejects inconsistent guide content before it becomes trusted provider instructions.</summary>
    private static void ValidateGuideContext(AppGuideContext? guide)
    {
        if (guide is null)
        {
            return;
        }
        if (guide.Topics.Count > AppGuideService.MaxTopics
            || guide.Topics.Distinct(StringComparer.Ordinal).Count() != guide.Topics.Count
            || guide.Topics.Any(topic => !AppGuideService.Topics.Contains(topic, StringComparer.Ordinal))
            || guide.Tokens != EstimateTokens(guide.Text)
            || guide.Tokens > AppGuideService.MaximumTokens
            || (guide.Topics.Count == 0) != string.IsNullOrWhiteSpace(guide.Text))
        {
            throw new InvalidOperationException("The application guide context is invalid or exceeds its limit.");
        }
    }

    /// <summary>Approximates text tokens from UTF-8 payload size until provider usage supplies the exact count.</summary>
    private static long EstimateTokens(string text) => ContextTokenEstimator.Text(text);

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
        || prompt.Metadata.GetValueOrDefault("response-format") == "promptmeup-console-response-v1"
        || SupportsAppGuide(prompt);

    /// <summary>Enables guide retrieval only for prompts declaring the versioned guide-aware response contract.</summary>
    internal static bool SupportsAppGuide(PromptDefinition prompt) =>
        prompt.Metadata.GetValueOrDefault("response-format") == "promptmeup-console-response-v2";

    /// <summary>Identifies the model family that supports explicit prompt-cache breakpoints.</summary>
    private static bool IsGpt56(string model) => model.StartsWith("gpt-5.6", StringComparison.OrdinalIgnoreCase);
}

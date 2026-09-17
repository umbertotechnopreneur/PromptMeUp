// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services.OpenAi;

internal static class FeatureResponseFormats
{
    /// <summary>Restricts memory lookup to identifiers supplied in one bounded batch.</summary>
    internal static object MemoryForget() => new
    {
        type = "json_schema",
        name = "promptmeup_memory_forget_v1",
        strict = true,
        schema = new
        {
            type = "object",
            properties = new
            {
                matched_ids = new { type = "array", items = new { type = "string" }, maxItems = 8 }
            },
            required = new[] { "matched_ids" },
            additionalProperties = false
        }
    };

    /// <summary>Limits display classification to two session preferences and whether the message also needs a chat answer.</summary>
    internal static object ChatDisplayIntent() => new
    {
        type = "json_schema",
        name = "promptmeup_chat_display_intent_v1",
        strict = true,
        schema = new
        {
            type = "object",
            properties = new
            {
                session_summary = new { type = "string", @enum = new[] { "unchanged", "show", "hide" } },
                command_suggestions = new { type = "string", @enum = new[] { "unchanged", "show", "hide" } },
                continue_chat = new { type = "boolean" }
            },
            required = new[] { "session_summary", "command_suggestions", "continue_chat" },
            additionalProperties = false
        }
    };

    /// <summary>Defines explicit steps and separate verification commands for a guided plan.</summary>
    internal static object Plan() => new
    {
        type = "json_schema",
        name = "promptmeup_plan_v1",
        strict = true,
        schema = new
        {
            type = "object",
            properties = new
            {
                steps = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            label = new { type = "string" },
                            command = new { type = "string" },
                            verification = new { type = "string" },
                            expected = new { type = "string" }
                        },
                        required = new[] { "label", "command", "verification", "expected" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "steps" },
            additionalProperties = false
        }
    };

    /// <summary>Defines the strict artifact contract independently from executable chat suggestions.</summary>
    internal static object Script() => new
    {
        type = "json_schema",
        name = "promptmeup_script_v1",
        strict = true,
        schema = new
        {
            type = "object",
            properties = new
            {
                explanation = new { type = "string" },
                source = new { type = "string" }
            },
            required = new[] { "explanation", "source" },
            additionalProperties = false
        }
    };
}

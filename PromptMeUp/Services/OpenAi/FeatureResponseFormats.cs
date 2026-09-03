// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services.OpenAi;

internal static class FeatureResponseFormats
{
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

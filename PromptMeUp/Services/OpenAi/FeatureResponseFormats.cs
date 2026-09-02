// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services.OpenAi;

internal static class FeatureResponseFormats
{
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

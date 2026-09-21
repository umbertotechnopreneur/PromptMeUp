// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public static class ChatDisplayIntentParser
{
    private const int MaximumResponseLength = 1_024;

    /// <summary>Accepts only the bounded chat-preference contract and rejects malformed or contradictory routing decisions.</summary>
    public static ChatDisplayIntent Parse(string text, int statusCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > MaximumResponseLength)
            {
                throw new JsonException("The chat preference intent is empty or exceeds its limit.");
            }

            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 2 });
            var root = document.RootElement;
            var expected = new HashSet<string>([
                "session_summary",
                "command_suggestions",
                "execution_confirmation",
                "continue_chat"
            ], StringComparer.Ordinal);
            if (root.ValueKind != JsonValueKind.Object
                || root.EnumerateObject().Any(property => !expected.Remove(property.Name))
                || expected.Count != 0)
            {
                throw new JsonException("The chat display intent properties do not match the response contract.");
            }

            var intent = new ChatDisplayIntent(
                ReadVisibility(root.GetProperty("session_summary")),
                ReadVisibility(root.GetProperty("command_suggestions")),
                ReadExecutionConfirmation(root.GetProperty("execution_confirmation")),
                root.GetProperty("continue_chat").GetBoolean());
            if (!intent.HasChanges && !intent.ContinueChat)
            {
                throw new JsonException("A message without display changes must continue to chat.");
            }

            return intent;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new OpenAiRequestException(
                "The AI preference response did not match the required structure.",
                "invalid_chat_display_intent",
                statusCode,
                exception);
        }
    }

    /// <summary>Maps only the three allowed visibility operations to a nullable session preference.</summary>
    private static bool? ReadVisibility(JsonElement value) => value.GetString() switch
    {
        "unchanged" => null,
        "show" => true,
        "hide" => false,
        _ => throw new JsonException("The chat visibility operation is invalid.")
    };

    /// <summary>Maps only the allowed global execution-preference operations to manual-confirmation state.</summary>
    private static bool? ReadExecutionConfirmation(JsonElement value) => value.GetString() switch
    {
        "unchanged" => null,
        "require" => true,
        "direct" => false,
        _ => throw new JsonException("The chat execution-confirmation operation is invalid.")
    };
}

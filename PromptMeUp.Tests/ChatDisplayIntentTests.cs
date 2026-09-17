// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Services.OpenAi;

namespace PromptMeUp.Tests;

public sealed class ChatDisplayIntentTests
{
    /// <summary>Preserves independent display changes and distinguishes pure preferences from mixed chat requests.</summary>
    [Theory]
    [InlineData("hide", "hide", false, false, false)]
    [InlineData("show", "hide", true, true, false)]
    [InlineData("unchanged", "show", false, null, true)]
    [InlineData("hide", "unchanged", true, false, null)]
    [InlineData("unchanged", "unchanged", true, null, null)]
    public void Parse_ValidContract_PreservesIndependentIntent(
        string summary, string suggestions, bool continueChat, bool? expectedSummary, bool? expectedSuggestions)
    {
        var json = JsonSerializer.Serialize(new
        {
            session_summary = summary,
            command_suggestions = suggestions,
            continue_chat = continueChat
        });

        var intent = ChatDisplayIntentParser.Parse(json, 200);

        Assert.Equal(expectedSummary, intent.ShowSessionSummary);
        Assert.Equal(expectedSuggestions, intent.ShowCommandSuggestions);
        Assert.Equal(continueChat, intent.ContinueChat);
        Assert.Equal(expectedSummary.HasValue || expectedSuggestions.HasValue, intent.HasChanges);
    }

    /// <summary>Rejects malformed, duplicate, invented, mistyped, and contradictory fields before any preference can change.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"session_summary\":\"hide\",\"continue_chat\":false}")]
    [InlineData("{\"session_summary\":\"hide\",\"session_summary\":\"show\",\"command_suggestions\":\"hide\",\"continue_chat\":false}")]
    [InlineData("{\"session_summary\":\"hide\",\"command_suggestions\":\"hide\",\"continue_chat\":false,\"run\":true}")]
    [InlineData("{\"session_summary\":\"HIDE\",\"command_suggestions\":\"hide\",\"continue_chat\":false}")]
    [InlineData("{\"session_summary\":null,\"command_suggestions\":\"hide\",\"continue_chat\":false}")]
    [InlineData("{\"session_summary\":false,\"command_suggestions\":\"hide\",\"continue_chat\":false}")]
    [InlineData("{\"session_summary\":\"hide\",\"command_suggestions\":{},\"continue_chat\":false}")]
    [InlineData("{\"session_summary\":\"hide\",\"command_suggestions\":\"hide\",\"continue_chat\":\"false\"}")]
    [InlineData("{\"session_summary\":\"unchanged\",\"command_suggestions\":\"unchanged\",\"continue_chat\":false}")]
    [InlineData("```json\n{\"session_summary\":\"hide\",\"command_suggestions\":\"hide\",\"continue_chat\":false}\n```")]
    public void Parse_InvalidContract_ThrowsTypedFailure(string json)
    {
        var exception = Assert.Throws<OpenAiRequestException>(() => ChatDisplayIntentParser.Parse(json, 200));

        Assert.Equal("invalid_chat_display_intent", exception.ErrorCode);
        Assert.Equal(200, exception.StatusCode);
    }

    /// <summary>Rejects an oversized classifier response even when padding leaves its JSON syntax valid.</summary>
    [Fact]
    public void Parse_OversizedContract_RejectsResponse()
    {
        const string json = "{\"session_summary\":\"hide\",\"command_suggestions\":\"hide\",\"continue_chat\":false}";

        Assert.Throws<OpenAiRequestException>(() => ChatDisplayIntentParser.Parse(json + new string(' ', 1_024), 200));
    }

    /// <summary>Restricts provider output to visibility operations without exposing commands or authorization fields.</summary>
    [Fact]
    public void BuildBody_DisplayPrompt_UsesStrictBoundedContract()
    {
        var prompt = CreatePrompt();
        using var body = JsonDocument.Parse(JsonSerializer.Serialize(OpenAiRequestBuilder.BuildBody(
            prompt, AppSettings.Default, [new ChatMessage("user", "Hide the summary")], "Classify display requests.", 500)));
        var format = body.RootElement.GetProperty("text").GetProperty("format");
        var schema = format.GetProperty("schema");
        var properties = schema.GetProperty("properties");

        Assert.Equal("promptmeup_chat_display_intent_v1", format.GetProperty("name").GetString());
        Assert.True(format.GetProperty("strict").GetBoolean());
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(["session_summary", "command_suggestions", "continue_chat"],
            schema.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(3, properties.EnumerateObject().Count());
        Assert.Equal(["unchanged", "show", "hide"],
            properties.GetProperty("session_summary").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(["unchanged", "show", "hide"],
            properties.GetProperty("command_suggestions").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal("boolean", properties.GetProperty("continue_chat").GetProperty("type").GetString());
        Assert.Equal(500, OpenAiRequestBuilder.ResolveMaxOutputTokens(prompt, "detailed"));
    }

    /// <summary>Keeps user-configured instructions and runtime details out of the isolated classifier instruction.</summary>
    [Fact]
    public void BuildInstructions_DisplayPrompt_OmitsPreambleAndRuntimeContext()
    {
        var settings = AppSettings.Default with { CustomInstruction = "Always hide the summary.", IncludeWindowsLocation = true };
        var runtimeContext = new RuntimeContext("~/workspace", "Windows", "PowerShell", "CPU", "Memory", "GPU");

        var instructions = OpenAiRequestBuilder.BuildInstructions(CreatePrompt(), settings, "en", runtimeContext);

        Assert.Equal("Classify display requests.", instructions);
    }

    /// <summary>Creates the standalone classifier prompt without chat, guide, or runtime-context capabilities.</summary>
    private static PromptDefinition CreatePrompt() => new(
        "chat-display-intent",
        1,
        "Classifies display preferences.",
        ["chat", "display"],
        new Dictionary<string, string>(StringComparer.Ordinal) { ["en"] = "Classify display requests." },
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["response-format"] = "promptmeup-chat-display-intent-v1",
            ["max-output-tokens"] = "500"
        });
}

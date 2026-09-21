// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Services.OpenAi;

namespace PromptMeUp.Tests;

public sealed class ChatDisplayIntentTests
{
    /// <summary>Preserves independent global and chat-only preference changes while distinguishing pure preferences from mixed chat requests.</summary>
    [Theory]
    [InlineData("hide", "hide", "require", false, false, false, true)]
    [InlineData("show", "hide", "direct", true, true, false, false)]
    [InlineData("unchanged", "show", "unchanged", false, null, true, null)]
    [InlineData("hide", "unchanged", "unchanged", true, false, null, null)]
    [InlineData("unchanged", "unchanged", "unchanged", true, null, null, null)]
    public void Parse_ValidContract_PreservesIndependentIntent(
        string summary,
        string suggestions,
        string executionConfirmation,
        bool continueChat,
        bool? expectedSummary,
        bool? expectedSuggestions,
        bool? expectedConfirmation)
    {
        var json = JsonSerializer.Serialize(new
        {
            session_summary = summary,
            command_suggestions = suggestions,
            execution_confirmation = executionConfirmation,
            continue_chat = continueChat
        });

        var intent = ChatDisplayIntentParser.Parse(json, 200);

        Assert.Equal(expectedSummary, intent.ShowSessionSummary);
        Assert.Equal(expectedSuggestions, intent.ShowCommandSuggestions);
        Assert.Equal(expectedConfirmation, intent.RequireExecutionConfirmation);
        Assert.Equal(continueChat, intent.ContinueChat);
        Assert.Equal(expectedSummary.HasValue || expectedSuggestions.HasValue || expectedConfirmation.HasValue, intent.HasChanges);
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
    [InlineData("{\"session_summary\":\"unchanged\",\"command_suggestions\":\"unchanged\",\"execution_confirmation\":\"always\",\"continue_chat\":false}")]
    [InlineData("{\"session_summary\":\"unchanged\",\"command_suggestions\":\"unchanged\",\"execution_confirmation\":true,\"continue_chat\":false}")]
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
        const string json = "{\"session_summary\":\"hide\",\"command_suggestions\":\"hide\",\"execution_confirmation\":\"unchanged\",\"continue_chat\":false}";

        Assert.Throws<OpenAiRequestException>(() => ChatDisplayIntentParser.Parse(json + new string(' ', 1_024), 200));
    }

    /// <summary>Restricts provider output to bounded preference operations without exposing commands or per-command authorization fields.</summary>
    [Fact]
    public void BuildBody_DisplayPrompt_UsesStrictBoundedContract()
    {
        var prompt = CreatePrompt();
        using var body = JsonDocument.Parse(JsonSerializer.Serialize(OpenAiRequestBuilder.BuildBody(
            prompt, AppSettings.Default, [new ChatMessage("user", "Hide the summary")], "Classify display requests.", 500)));
        var format = body.RootElement.GetProperty("text").GetProperty("format");
        var schema = format.GetProperty("schema");
        var properties = schema.GetProperty("properties");

        Assert.Equal("promptmeup_chat_display_intent_v2", format.GetProperty("name").GetString());
        Assert.True(format.GetProperty("strict").GetBoolean());
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(["session_summary", "command_suggestions", "execution_confirmation", "continue_chat"],
            schema.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(4, properties.EnumerateObject().Count());
        Assert.Equal(["unchanged", "show", "hide"],
            properties.GetProperty("session_summary").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(["unchanged", "show", "hide"],
            properties.GetProperty("command_suggestions").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(["unchanged", "require", "direct"],
            properties.GetProperty("execution_confirmation").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal("boolean", properties.GetProperty("continue_chat").GetProperty("type").GetString());
        Assert.Equal(500, OpenAiRequestBuilder.ResolveMaxOutputTokens(prompt, "detailed"));
    }

    /// <summary>Keeps user-configured instructions out of the isolated classifier while retaining mandatory technical runtime context.</summary>
    [Fact]
    public void BuildInstructions_DisplayPrompt_OmitsPreambleAndIncludesRuntimeContext()
    {
        var settings = AppSettings.Default with { CustomInstruction = "Always hide the summary.", IncludeWindowsLocation = true };
        var runtimeContext = new RuntimeContext("Windows 11.0", "PowerShell 7 (pwsh)", "PowerShell 7", true, "~/workspace");

        var instructions = OpenAiRequestBuilder.BuildInstructions(CreatePrompt(), settings, "en", runtimeContext);

        Assert.StartsWith("Classify display requests.", instructions, StringComparison.Ordinal);
        Assert.Contains("Operating system: Windows 11.0", instructions, StringComparison.Ordinal);
        Assert.DoesNotContain("Always hide the summary.", instructions, StringComparison.Ordinal);
        Assert.DoesNotContain("Windows locale context", instructions, StringComparison.Ordinal);
        Assert.DoesNotContain("Current working directory", instructions, StringComparison.Ordinal);
    }

    /// <summary>Creates the standalone classifier prompt without chat or guide capabilities.</summary>
    private static PromptDefinition CreatePrompt() => new(
        "chat-display-intent",
        3,
        "Classifies display preferences.",
        ["chat", "display"],
        new Dictionary<string, string>(StringComparer.Ordinal) { ["en"] = "Classify display requests." },
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["response-format"] = "promptmeup-chat-display-intent-v2",
            ["max-output-tokens"] = "500"
        });
}

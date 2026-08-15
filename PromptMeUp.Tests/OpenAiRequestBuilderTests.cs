// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services.OpenAi;

namespace PromptMeUp.Tests;

public sealed class OpenAiRequestBuilderTests
{
    /// <summary>Verifies that a long GPT-5.6 chat prefix combines a stable breakpoint with growing-history checkpoints.</summary>
    [Fact]
    public void BuildBody_LongGpt56ChatInstruction_UsesImplicitCachingAfterExplicitPrefix()
    {
        var prompt = CreatePrompt();
        var instructions = new string('x', 4_096);
        using var firstBody = BuildJson(
            prompt,
            AppSettings.Default,
            [new ChatMessage("user", "first question")],
            instructions);
        using var secondBody = BuildJson(
            prompt,
            AppSettings.Default,
            [new ChatMessage("user", "different question")],
            instructions);
        var root = firstBody.RootElement;
        var input = root.GetProperty("input");
        var prefix = input[0];
        var breakpoint = prefix.GetProperty("content")[0];

        Assert.False(root.TryGetProperty("instructions", out _));
        Assert.Equal(2, input.GetArrayLength());
        Assert.Equal("developer", prefix.GetProperty("role").GetString());
        Assert.Equal(instructions, breakpoint.GetProperty("text").GetString());
        Assert.Equal("explicit", breakpoint.GetProperty("prompt_cache_breakpoint").GetProperty("mode").GetString());
        Assert.Equal("implicit", root.GetProperty("prompt_cache_options").GetProperty("mode").GetString());
        Assert.Equal("30m", root.GetProperty("prompt_cache_options").GetProperty("ttl").GetString());
        Assert.Equal(
            root.GetProperty("prompt_cache_key").GetString(),
            secondBody.RootElement.GetProperty("prompt_cache_key").GetString());
    }

    /// <summary>Verifies that a long one-shot query caches only its stable instruction and not the unique user suffix.</summary>
    [Fact]
    public void BuildBody_LongGpt56QueryInstruction_UsesExplicitOnlyCaching()
    {
        using var body = BuildJson(
            CreatePrompt("query-system"),
            AppSettings.Default,
            [new ChatMessage("user", "unique question")],
            new string('x', 4_096));

        Assert.Equal(
            "explicit",
            body.RootElement.GetProperty("prompt_cache_options").GetProperty("mode").GetString());
    }

    /// <summary>Verifies that a short GPT-5.6 prefix keeps automatic caching without an explicit breakpoint.</summary>
    [Fact]
    public void BuildBody_ShortGpt56Instruction_UsesAutomaticCacheKey()
    {
        using var body = BuildJson(
            CreatePrompt(),
            AppSettings.Default,
            [new ChatMessage("unexpected", "question")],
            "short instruction");
        var root = body.RootElement;

        Assert.Equal("short instruction", root.GetProperty("instructions").GetString());
        Assert.Equal("user", root.GetProperty("input")[0].GetProperty("role").GetString());
        Assert.StartsWith(
            "promptmeup:gpt-5.6-terra:chat-system:v2:",
            root.GetProperty("prompt_cache_key").GetString() ?? string.Empty);
        Assert.False(root.TryGetProperty("prompt_cache_options", out _));
        Assert.False(root.TryGetProperty("prompt_cache_retention", out _));
    }

    /// <summary>Verifies that GPT-5.5 automatic caching retains the provider's 24-hour setting.</summary>
    [Fact]
    public void BuildBody_Gpt55_UsesLongCacheRetention()
    {
        using var body = BuildJson(
            CreatePrompt(),
            AppSettings.Default with { Model = "gpt-5.5" },
            [new ChatMessage("assistant", "answer")],
            "instruction");
        var root = body.RootElement;

        Assert.Equal("24h", root.GetProperty("prompt_cache_retention").GetString());
        Assert.Equal("assistant", root.GetProperty("input")[0].GetProperty("role").GetString());
        Assert.True(root.TryGetProperty("prompt_cache_key", out _));
        Assert.False(root.TryGetProperty("prompt_cache_options", out _));
    }

    /// <summary>Verifies that disabled caching omits every cache-specific request field.</summary>
    [Fact]
    public void BuildBody_CachingDisabled_OmitsCacheFields()
    {
        using var body = BuildJson(
            CreatePrompt(),
            AppSettings.Default with { PromptCachingEnabled = false },
            [new ChatMessage("user", "question")],
            new string('x', 4_096));
        var root = body.RootElement;

        Assert.True(root.TryGetProperty("instructions", out _));
        Assert.False(root.TryGetProperty("prompt_cache_key", out _));
        Assert.False(root.TryGetProperty("prompt_cache_options", out _));
        Assert.False(root.TryGetProperty("prompt_cache_retention", out _));
    }

    /// <summary>Verifies that only the chat prompt receives the trimmed custom instruction.</summary>
    [Fact]
    public void BuildInstructions_ChatPrompt_AppendsCustomInstruction()
    {
        var settings = AppSettings.Default with { CustomInstruction = "  Prefer tables.  " };

        var result = OpenAiRequestBuilder.BuildInstructions(CreatePrompt(), settings, "en");

        Assert.Equal($"Base instruction.{Environment.NewLine}{Environment.NewLine}Prefer tables.", result);
    }

    /// <summary>Verifies that the single-query prompt receives the sanitized runtime facts and the approved custom instruction.</summary>
    [Fact]
    public void BuildInstructions_QueryPrompt_AppendsRuntimeContext()
    {
        var settings = AppSettings.Default with { CustomInstruction = "Prefer concise diagnostics." };
        var runtimeContext = new RuntimeContext(
            "~/workspace",
            "Windows 11",
            "Windows console; prefer PowerShell 7 syntax and paths",
            "16 logical processor(s), X64",
            "32.0 GiB physical memory",
            "NVIDIA RTX");

        var result = OpenAiRequestBuilder.BuildInstructions(
            CreatePrompt("query-system"),
            settings,
            "en",
            runtimeContext);

        Assert.Contains("Prefer concise diagnostics.", result, StringComparison.Ordinal);
        Assert.Contains("Runtime context supplied by PromptMeUp", result, StringComparison.Ordinal);
        Assert.Contains("Current working directory (sanitized): ~/workspace", result, StringComparison.Ordinal);
        Assert.Contains("GPU: NVIDIA RTX", result, StringComparison.Ordinal);
    }

    /// <summary>Verifies that both user-facing assistant surfaces request the strict answer-and-command envelope.</summary>
    [Theory]
    [InlineData("chat-system")]
    [InlineData("query-system")]
    public void BuildBody_AssistantPrompt_UsesStructuredResponseFormat(string promptId)
    {
        using var body = BuildJson(
            CreatePrompt(promptId),
            AppSettings.Default,
            [new ChatMessage("user", "question")],
            "instruction");

        var format = body.RootElement.GetProperty("text").GetProperty("format");

        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.True(format.GetProperty("strict").GetBoolean());
        Assert.Equal("promptmeup_chat_response_v1", format.GetProperty("name").GetString());
        Assert.Contains(
            "commands",
            format.GetProperty("schema").GetProperty("required").EnumerateArray().Select(value => value.GetString()));
    }

    /// <summary>Verifies that context estimation keeps instruction, conversation, and latest-user counters distinct.</summary>
    [Fact]
    public void EstimateContext_MultipleMessages_SeparatesCounters()
    {
        var messages = new[]
        {
            new ChatMessage("user", "abcd"),
            new ChatMessage("assistant", "12345678"),
            new ChatMessage("user", "abcdefghijkl")
        };

        var result = OpenAiRequestBuilder.EstimateContext("12345678", messages, "gpt-5.5");

        Assert.Equal(28L, result.InputTokens);
        Assert.Equal(2L, result.SystemInstructionTokens);
        Assert.Equal(18L, result.ConversationTokens);
        Assert.Equal(3L, result.LatestUserPromptTokens);
        Assert.Equal(AiModelCatalog.Resolve("gpt-5.5").ContextWindowTokens, result.ContextWindowTokens);
        Assert.True(result.IsInputEstimate);
    }

    /// <summary>Serializes one generated payload so tests assert the provider-visible JSON contract.</summary>
    private static JsonDocument BuildJson(
        PromptDefinition prompt,
        AppSettings settings,
        IReadOnlyList<ChatMessage> messages,
        string instructions) =>
        JsonDocument.Parse(JsonSerializer.Serialize(OpenAiRequestBuilder.BuildBody(prompt, settings, messages, instructions, 900)));

    /// <summary>Creates a minimal prompt definition for request-building tests.</summary>
    private static PromptDefinition CreatePrompt(string id = "chat-system") => new(
        id,
        2,
        "Test prompt",
        [],
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "Base instruction."
        },
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
}

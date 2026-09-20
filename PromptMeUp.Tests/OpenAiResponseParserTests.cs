// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Services.OpenAi;

namespace PromptMeUp.Tests;

public sealed class OpenAiResponseParserTests
{
    /// <summary>Verifies response text concatenation and every normalized usage counter.</summary>
    [Fact]
    public void ParseResponse_TextAndUsage_ReturnsNormalizedResponse()
    {
        const string json = """
            {
              "id": "resp_123",
              "model": "gpt-5.6-terra-2026-08-01",
              "output": [
                { "type": "reasoning", "content": [] },
                {
                  "type": "message",
                  "content": [
                    { "type": "output_text", "text": " First " },
                    { "type": "refusal", "refusal": "ignored" },
                    { "type": "output_text", "text": "Second " }
                  ]
                }
              ],
              "usage": {
                "input_tokens": 120,
                "input_tokens_details": {
                  "cached_tokens": 40,
                  "cache_write_tokens": 8
                },
                "output_tokens": 30,
                "output_tokens_details": {
                  "reasoning_tokens": 12
                },
                "total_tokens": 0
              }
            }
            """;

        var result = OpenAiResponseParser.ParseResponse(json, 200, 37, "request_456");

        Assert.Equal("resp_123", result.Id);
        Assert.Equal("gpt-5.6-terra-2026-08-01", result.Model);
        Assert.Equal($"First {Environment.NewLine}Second", result.Text);
        Assert.Equal(new AiUsageMetrics(120, 40, 8, 30, 12, 150), result.Usage);
        Assert.Equal(120L, result.ContextUsage.InputTokens);
        Assert.Equal(30L, result.ContextUsage.OutputTokens);
        Assert.False(result.ContextUsage.IsInputEstimate);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.Equal(37, result.ElapsedMilliseconds);
        Assert.Equal("request_456", result.ProviderRequestId);
    }

    /// <summary>Verifies that missing usage remains a valid zero-cost response shape.</summary>
    [Fact]
    public void ParseResponse_MissingUsage_ReturnsEmptyUsage()
    {
        const string json = """
            { "id": "resp_1", "model": "gpt-5.5", "output": [{ "content": [{ "type": "output_text", "text": "ok" }] }] }
            """;

        var result = OpenAiResponseParser.ParseResponse(json, 200, 1, null);

        Assert.Equal(new AiUsageMetrics(0, 0, 0, 0, 0, 0), result.Usage);
    }

    /// <summary>Verifies that a valid structured chat envelope renders its Markdown and exposes only its cited command.</summary>
    [Fact]
    public void ParseResponse_StructuredChatEnvelope_MapsMarkdownAndCitedCommand()
    {
        const string markdown = "## Branch disponibili\n\n- Usa `git branch -a`.";
        const string structured = """
            {
              "answer_markdown": "## Branch disponibili\n\n- Usa `git branch -a`.",
              "commands": [
                { "label": "Elenca i branch", "command": "git branch -a" }
              ]
            }
            """;

        var result = OpenAiResponseParser.ParseResponse(
            CreateTextResponse(structured),
            200,
            1,
            null,
            parseStructuredChatResponse: true);

        Assert.Equal(markdown, result.Text);
        Assert.Equal(
            [new SuggestedCommand("Elenca i branch", "git branch -a")],
            result.SuggestedCommands);
    }

    /// <summary>Verifies that malformed structured chat text fails closed instead of becoming a displayable command response.</summary>
    [Fact]
    public void ParseResponse_MalformedStructuredChatEnvelope_FailsClosed()
    {
        var exception = Assert.Throws<OpenAiRequestException>(() => OpenAiResponseParser.ParseResponse(
            CreateTextResponse("{\"answer_markdown\":\"Usa git branch -a\",\"commands\":[}"),
            200,
            1,
            null,
            parseStructuredChatResponse: true));

        Assert.Equal("invalid_chat_response", exception.ErrorCode);
        Assert.Equal(200, exception.StatusCode);
    }

    /// <summary>Verifies a guide-only v2 response remains a successful provider call with usage and no user-facing answer.</summary>
    [Fact]
    public void ParseResponse_GuideRequest_RetainsUsageAndKnownTopics()
    {
        const string structured = """{"answer_markdown":"","commands":[],"guide_topics":["memories","privacy"]}""";
        var json = JsonSerializer.Serialize(new
        {
            id = "guide-request",
            model = "gpt-5.6-terra",
            output = new[] { new { content = new[] { new { type = "output_text", text = structured } } } },
            usage = new { input_tokens = 100, output_tokens = 12, total_tokens = 112 }
        });

        var result = OpenAiResponseParser.ParseResponse(json, 200, 1, null, true, true);

        Assert.Empty(result.Text);
        Assert.Empty(result.SuggestedCommands);
        Assert.Equal(["memories", "privacy"], result.GuideTopics);
        Assert.Equal(112, result.Usage.TotalTokens);
    }

    /// <summary>Verifies a guide-aware final answer carries no retrieval request and retains cited command safety.</summary>
    [Fact]
    public void ParseResponse_GuideAwareAnswer_RetainsCitedCommand()
    {
        const string structured = """{"answer_markdown":"Run `hm --help`.","commands":[{"label":"Show help","command":"hm --help"}],"guide_topics":[]}""";

        var result = OpenAiResponseParser.ParseResponse(CreateTextResponse(structured), 200, 1, null, true, true);

        Assert.Equal("Run `hm --help`.", result.Text);
        Assert.Empty(result.GuideTopics);
        Assert.Equal([new SuggestedCommand("Show help", "hm --help")], result.SuggestedCommands);
    }

    /// <summary>Verifies malformed, unbounded, unknown, duplicate, or mixed guide requests fail before command filtering.</summary>
    [Theory]
    [InlineData("""{"answer_markdown":"Answer","commands":[]}""")]
    [InlineData("""{"answer_markdown":"","commands":[],"guide_topics":[]}""")]
    [InlineData("""{"answer_markdown":"","commands":[],"guide_topics":"memories"}""")]
    [InlineData("""{"answer_markdown":"","commands":[],"guide_topics":[null]}""")]
    [InlineData("""{"answer_markdown":"","commands":[],"guide_topics":["unknown"]}""")]
    [InlineData("""{"answer_markdown":"","commands":[],"guide_topics":["memories","memories"]}""")]
    [InlineData("""{"answer_markdown":"","commands":[],"guide_topics":["overview","memories","privacy"]}""")]
    [InlineData("""{"answer_markdown":"Answer","commands":[],"guide_topics":["memories"]}""")]
    [InlineData("""{"answer_markdown":"","commands":[{"label":"Uncited","command":"Get-Date"}],"guide_topics":["memories"]}""")]
    [InlineData("""{"answer_markdown":"Answer","commands":[],"guide_topics":[],"unexpected":true}""")]
    [InlineData("""{"answer_markdown":"Answer","commands":[],"guide_topics":[],"guide_topics":[]}""")]
    [InlineData("""{"answer_markdown":"Run Get-Date","commands":[{"label":"Date","command":"Get-Date","extra":true}],"guide_topics":[]}""")]
    public void ParseResponse_InvalidGuideEnvelope_RejectsResponse(string structured)
    {
        var exception = Assert.Throws<OpenAiRequestException>(() =>
            OpenAiResponseParser.ParseResponse(CreateTextResponse(structured), 200, 1, null, true, true));

        Assert.Equal("invalid_chat_response", exception.ErrorCode);
    }

    /// <summary>Verifies the legacy response contract cannot silently accept a guide-only intermediate answer.</summary>
    [Fact]
    public void ParseResponse_LegacyContract_RejectsGuideOnlyAnswer()
    {
        const string structured = """{"answer_markdown":"","commands":[],"guide_topics":["memories"]}""";

        Assert.Throws<OpenAiRequestException>(() =>
            OpenAiResponseParser.ParseResponse(CreateTextResponse(structured), 200, 1, null, parseStructuredChatResponse: true));
    }

    /// <summary>Verifies that a suggested command absent from the Markdown answer is not exposed for selection.</summary>
    [Fact]
    public void ParseResponse_UncitedStructuredCommand_DoesNotSurfaceSuggestion()
    {
        const string markdown = "Controlla prima lo stato del repository.";
        const string structured = """
            {
              "answer_markdown": "Controlla prima lo stato del repository.",
              "commands": [
                { "label": "Elenca i branch", "command": "git branch -a" }
              ]
            }
            """;

        var result = OpenAiResponseParser.ParseResponse(
            CreateTextResponse(structured),
            200,
            1,
            null,
            parseStructuredChatResponse: true);

        Assert.Equal(markdown, result.Text);
        Assert.Empty(result.SuggestedCommands);
    }

    /// <summary>Verifies that a command containing a synthetic recognizable credential is never exposed for selection.</summary>
    [Fact]
    public void ParseResponse_SecretBearingStructuredCommand_DoesNotSurfaceSuggestion()
    {
        var syntheticKey = "sk-" + new string('a', 24);
        var command = $"Write-Output {syntheticKey}";
        var markdown = $"Do not run `{command}`.";
        var structured = JsonSerializer.Serialize(new
        {
            answer_markdown = markdown,
            commands = new[]
            {
                new { label = "Print credential", command }
            }
        });

        var result = OpenAiResponseParser.ParseResponse(
            CreateTextResponse(structured),
            200,
            1,
            null,
            parseStructuredChatResponse: true);

        Assert.Equal(markdown, result.Text);
        Assert.Empty(result.SuggestedCommands);
    }

    /// <summary>Verifies that a response without text keeps the stable local error contract.</summary>
    [Fact]
    public void ParseResponse_MissingText_ThrowsStableError()
    {
        var exception = Assert.Throws<OpenAiRequestException>(() =>
            OpenAiResponseParser.ParseResponse("{\"output\":[]}", 200, 1, null));

        Assert.Equal("empty_response", exception.ErrorCode);
        Assert.Equal(200, exception.StatusCode);
    }

    /// <summary>Preserves the explicit severity from fenced risk JSON without lowering it to match the score.</summary>
    [Fact]
    public void ParseRiskAssessment_FencedHighLevel_PreservesVeto()
    {
        const string response = "```json\n{\"score\":15,\"level\":\"high\",\"description_markdown\":\"  destructive  \"}\n```";

        var result = OpenAiResponseParser.ParseRiskAssessment(response);

        Assert.Equal(15, result.Score);
        Assert.Equal(CommandRiskLevel.High, result.Level);
        Assert.Equal("destructive", result.DescriptionMarkdown);
        Assert.True(result.UsedAi);
    }

    /// <summary>Verifies that malformed risk output maps to the stable provider-facing error code.</summary>
    [Fact]
    public void ParseRiskAssessment_InvalidStructure_ThrowsStableError()
    {
        var exception = Assert.Throws<OpenAiRequestException>(() =>
            OpenAiResponseParser.ParseRiskAssessment("{\"score\":25}"));

        Assert.Equal("invalid_risk_review", exception.ErrorCode);
        Assert.Null(exception.StatusCode);
    }

    /// <summary>Verifies extraction of the standard Responses API error envelope.</summary>
    [Fact]
    public void ReadApiError_StandardEnvelope_ReturnsMessage()
    {
        var result = OpenAiResponseParser.ReadApiError("{\"error\":{\"message\":\"quota exceeded\"}}");

        Assert.Equal("quota exceeded", result);
    }

    /// <summary>Verifies malformed or unexpected error envelopes remain non-fatal to fallback handling.</summary>
    [Theory]
    [InlineData("not-json")]
    [InlineData("\"forbidden\"")]
    [InlineData("{}")]
    [InlineData("{\"error\":null}")]
    public void ReadApiError_InvalidEnvelope_ReturnsNull(string json) =>
        Assert.Null(OpenAiResponseParser.ReadApiError(json));

    /// <summary>Wraps model text in the minimal Responses API envelope consumed by the response parser.</summary>
    private static string CreateTextResponse(string text) => JsonSerializer.Serialize(new
    {
        id = "resp_chat",
        model = "gpt-5.6-terra",
        output = new[]
        {
            new
            {
                content = new[]
                {
                    new { type = "output_text", text }
                }
            }
        }
    });
}

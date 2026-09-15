// SPDX-License-Identifier: MIT

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class AppGuideWorkflowTests
{
    /// <summary>Verifies natural-language guide routing, localized system context, cost accounting, reuse, and clearing.</summary>
    [Theory]
    [InlineData("en", "How do I change this app's conversation budget?")]
    [InlineData("it", "Come cambio il budget della conversazione di questa app?")]
    public async Task RunChatAsync_GuideRequest_CountsBothCallsAndReusesLocalizedChapters(string language, string question)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short")], default);
        using var handler = new GuideHttpHandler(index => Reply(index, index == 1 ? ["conversation"] : []));
        using var http = new HttpClient(handler);
        var snapshots = new List<ShellRuntimeStatus>();
        var answers = new List<string>();
        var inputs = new Queue<string>([question, "And how does that affect the next question?", "/clear", "/context", "Explain a terminal command.", "/exit"]);
        var workflow = CreateWorkflow(fixture, http, inputs, snapshots, answers, language);

        await workflow.RunChatAsync(AppSettings.Default with { Language = language, PromptCachingEnabled = false }, default);

        Assert.Empty(inputs);
        Assert.Equal(4, handler.RequestBodies.Count);
        Assert.Equal(3, answers.Count);
        Assert.All(answers, answer => Assert.False(string.IsNullOrWhiteSpace(answer)));
        var catalog = new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
        var chapter = (await catalog.GetAsync("app-guide-conversation", default)).ResolveText(language);
        using var first = JsonDocument.Parse(handler.RequestBodies[0]);
        using var guided = JsonDocument.Parse(handler.RequestBodies[1]);
        using var followUp = JsonDocument.Parse(handler.RequestBodies[2]);
        using var cleared = JsonDocument.Parse(handler.RequestBodies[3]);
        Assert.DoesNotContain("<chapter id=", first.RootElement.GetProperty("instructions").GetString());
        Assert.Contains(chapter, guided.RootElement.GetProperty("instructions").GetString());
        Assert.Contains(chapter, followUp.RootElement.GetProperty("instructions").GetString());
        Assert.DoesNotContain("<chapter id=", cleared.RootElement.GetProperty("instructions").GetString());
        Assert.Equal(question, Assert.Single(guided.RootElement.GetProperty("input").EnumerateArray())
            .GetProperty("content").GetString());
        Assert.All(guided.RootElement.GetProperty("input").EnumerateArray(), message =>
            Assert.DoesNotContain("<app-guide>", message.GetProperty("content").GetString()));
        Assert.InRange(snapshots[0].GuideTokens, 1, snapshots[0].SystemInstructionTokens);
        Assert.Equal(40, snapshots[0].InputTokens);
        Assert.Equal(60, snapshots[0].SessionInputTokens);
        Assert.Equal(2, snapshots[0].SessionOutputTokens);
        Assert.Equal(0.000060m, snapshots[0].TurnCostUsd);
        Assert.Equal(0.000060m, snapshots[0].RunningCostUsd);
        Assert.Equal(0, snapshots[2].GuideTokens);
        Assert.Equal(0, snapshots[3].GuideTokens);
        Assert.Equal(0.000200m, snapshots[^1].RunningCostUsd);
        Assert.All(snapshots, snapshot =>
        {
            Assert.True(snapshot.HasContextBreakdown);
            Assert.Equal(snapshot.ActiveContextTokens,
                snapshot.SystemInstructionTokens + snapshot.UserMessageTokens + snapshot.AssistantMessageTokens);
        });
        Assert.Equal(4L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_session_events WHERE event_type = 'app_guide_loaded';"));
    }

    /// <summary>Verifies a model cannot turn guide retrieval into an unbounded sequence of paid calls.</summary>
    [Fact]
    public async Task RunChatAsync_RepeatedGuideRequest_StopsAfterTwoAccountedCalls()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var handler = new GuideHttpHandler(index => Reply(index, index == 1 ? ["conversation"] : ["memories"]));
        using var http = new HttpClient(handler);
        var answers = new List<string>();
        var workflow = CreateWorkflow(fixture, http, new Queue<string>(["How does PromptMeUp work?"]), [], answers, "en");

        var error = await Assert.ThrowsAsync<OpenAiRequestException>(() =>
            workflow.RunChatAsync(AppSettings.Default with { PromptCachingEnabled = false }, default));

        Assert.Equal("app_guide_round_limit", error.ErrorCode);
        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Empty(answers);
        Assert.Equal(2L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
        Assert.Equal(60L, await fixture.ScalarAsync("SELECT SUM(input_tokens) FROM ai_requests;"));
    }

    /// <summary>Verifies a completed guide-selection call remains recorded when the following provider request fails.</summary>
    [Fact]
    public async Task RunChatAsync_GuideAnswerFailure_PreservesSelectionUsage()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var handler = new GuideHttpHandler(index => index == 1 ? Reply(index, ["settings"]) : "{}",
            index => index == 1 ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        using var http = new HttpClient(handler);
        var answers = new List<string>();
        var workflow = CreateWorkflow(fixture, http, new Queue<string>(["Where are PromptMeUp settings?"]), [], answers, "en");

        await Assert.ThrowsAsync<OpenAiRequestException>(() =>
            workflow.RunChatAsync(AppSettings.Default with { PromptCachingEnabled = false }, default));

        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Empty(answers);
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 0;"));
        Assert.Equal(20L, await fixture.ScalarAsync("SELECT SUM(input_tokens) FROM ai_requests;"));
    }

    /// <summary>Builds real guide and provider services around isolated storage and scripted passive views.</summary>
    private static AiConversationWorkflow CreateWorkflow(
        RegressionFixture fixture,
        HttpClient http,
        Queue<string> inputs,
        List<ShellRuntimeStatus> snapshots,
        List<string> answers,
        string language)
    {
        var text = new LocalizationService();
        text.SetLanguage(language);
        var catalog = new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
        var provider = new OpenAiService(http, fixture.Secrets, catalog,
            TestProxy.Create<IRuntimeContextService>((_, _) => new RuntimeContext("~", "test", "PowerShell 7", "test", "test", "test")),
            fixture.Database, new AiCostCalculator(), fixture.Audit, new SensitiveDataRedactor(),
            new PromptInjectionProtectionService(), NullLogger<OpenAiService>.Instance);
        return new AiConversationWorkflow(new ConversationMemoryService(), provider, catalog,
            TestProxy.Create<IPricingService>((method, _) => throw new NotSupportedException(method.Name)), fixture.Audit,
            TestProxy.Create<IAuthorizedCommandWorkflow>((method, _) => throw new NotSupportedException(method.Name)),
            TestProxy.Create<IChatView>((method, args) =>
            {
                if (method.Name == "ReadMessage") { return inputs.Dequeue(); }
                if (method.Name == "RenderAssistant") { answers.Add((string)args[0]!); }
                return null;
            }),
            TestProxy.Create<ICommandSuggestionView>((_, _) => new CommandSuggestionDecision(CommandSuggestionAction.DoNotExecute, null)),
            TestProxy.Create<ICostsView>((method, _) => throw new NotSupportedException(method.Name)),
            TestProxy.Create<IConsoleShellView>((method, args) =>
            {
                if (method.Name == "RunWithStatusAsync") { return ((Delegate)args[1]!).DynamicInvoke(); }
                if (method.Name == "RenderRuntimeStatus") { snapshots.Add((ShellRuntimeStatus)args[0]!); }
                return null;
            }), text,
            new PersistentMemoryService(fixture.Paths, new SensitiveDataRedactor(), text, NullLogger<PersistentMemoryService>.Instance),
            TestProxy.Create<IMemoryView>((method, _) => throw new NotSupportedException(method.Name)), fixture.Database,
            new AppGuideService(catalog, NullLogger<AppGuideService>.Instance));
    }

    /// <summary>Creates a v2 provider reply with separate guide routing and final-answer forms plus observable accounting.</summary>
    private static string Reply(int index, string[] topics) => JsonSerializer.Serialize(new
    {
        id = $"guide-response-{index}",
        model = "gpt-5.6-terra",
        status = "completed",
        output = new[]
        {
            new
            {
                type = "message",
                content = new[]
                {
                    new
                    {
                        type = "output_text",
                        text = JsonSerializer.Serialize(new
                        {
                            answer_markdown = topics.Length == 0 ? "Terminal guidance from the supplied app context." : string.Empty,
                            commands = Array.Empty<object>(),
                            guide_topics = topics
                        })
                    }
                }
            }
        },
        usage = new { input_tokens = index * 20, output_tokens = 1, total_tokens = index * 20 + 1 }
    });

    private sealed class GuideHttpHandler(Func<int, string> response, Func<int, HttpStatusCode>? status = null) : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        /// <summary>Captures request payloads and supplies only local synthetic replies without making network calls.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            var index = RequestBodies.Count;
            return new HttpResponseMessage(status?.Invoke(index) ?? HttpStatusCode.OK)
            {
                Content = new StringContent(response(index))
            };
        }
    }
}

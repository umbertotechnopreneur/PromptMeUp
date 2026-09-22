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
    /// <summary>Keeps localized query envelopes bounded for escaped instructions, including oversized selections retained by older versions.</summary>
    [Theory]
    [InlineData("en", false)]
    [InlineData("it", false)]
    [InlineData("fr", false)]
    [InlineData("de", false)]
    [InlineData("es", false)]
    [InlineData("vi", false)]
    [InlineData("en", true)]
    public async Task RunQueryAsync_EscapedSkillContext_DoesNotBlockAnswers(string language, bool legacyOversized)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        text.SetLanguage(language);
        var store = new SkillsAndMemoryStore(fixture.Paths, new SensitiveDataRedactor(), text);
        await store.SaveSettingsAsync(new(Enabled: true), await store.SettingsAsync(default), default);
        var prompts = new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
        var skills = new SkillCatalogService(fixture.Paths, store, text, prompts);
        var directory = Path.Combine(fixture.Paths.DataDirectory, "skills", "xml-guide");
        Directory.CreateDirectory(directory);
        var instructions = string.Concat(Enumerable.Repeat("<tag attr=\"value\">content</tag>", legacyOversized ? 210 : 90));
        File.WriteAllText(Path.Combine(directory, "SKILL.md"), "---\nname: xml-guide\ndescription: XML example guidance\n---\n" + instructions);
        var skill = skills.Inspect(directory);
        if (legacyOversized)
        {
            await store.SetAsync("skill:xml-guide", skill.Fingerprint, default);
            await store.SetAsync("selected-skill", skill.Name, default);
        }
        else
        {
            await skills.EnableAsync(skill, true, default);
            await skills.SelectForQuestionsAsync(skill, default);
        }
        var memories = new PersistentMemoryService(fixture.Paths, new SensitiveDataRedactor(), text, NullLogger<PersistentMemoryService>.Instance);
        await memories.RememberAsync("XML example project uses readable tag names.", false, default);
        using var handler = new GuideHttpHandler(index => Reply(index, []));
        using var http = new HttpClient(handler);
        var answers = new List<string>();
        var warnings = new List<string>();
        var workflow = CreateWorkflow(fixture, http, new Queue<string>(), [], answers, language, skills, warnings);

        await workflow.RunQueryAsync("Explain this XML example project.", AppSettings.Default with { Language = language }, renderQuery: true, default);

        Assert.Single(answers);
        Assert.Equal(2, handler.RequestBodies.Count);
        using var request = JsonDocument.Parse(Assert.Single(handler.ConversationRequestBodies));
        var messages = request.RootElement.GetProperty("input").EnumerateArray().ToArray();
        var envelope = messages.First(message => message.GetProperty("role").GetString() == "user").GetProperty("content").GetString()!;
        Assert.Contains("readable tag names", envelope);
        Assert.InRange(ContextTokenEstimator.Messages([new("user", envelope)]), 1, 3200);
        if (legacyOversized)
        {
            Assert.DoesNotContain("xml-guide", envelope);
            Assert.Equal(text.Text("Lab.SkillTooLarge", skill.Name, SkillCatalogService.MaximumContextTokens), Assert.Single(warnings));
        }
        else
        {
            Assert.Contains("xml-guide", envelope);
            Assert.Contains("\\u003Ctag", envelope);
            Assert.Empty(warnings);
        }
    }

    /// <summary>Supplies synthetic guide-routing replies and verifies localized two-step query and chat retrieval without enabling features.</summary>
    [Theory]
    [InlineData("en", "skills", "skill-actions", "How do I enable a skill and run one of its actions?")]
    [InlineData("it", "learning", "reflection", "Come attivo la memoria e uso Dream?")]
    [InlineData("fr", "reminders", "skills", "Comment activer les rappels dans cette application ?")]
    [InlineData("de", "reflection", "learning", "Wie verwende ich Dream und bestätige Erinnerungen?")]
    [InlineData("es", "skill-actions", "reminders", "¿Cómo uso las acciones de las skills y los recordatorios?")]
    [InlineData("vi", "skills", "learning", "Làm sao bật kỹ năng và bộ nhớ trong ứng dụng?")]
    public async Task NewGuideChapters_QueryAndChatRetrieveLocalizedContext(string language, string firstTopic, string secondTopic, string question)
    {
        foreach (var chat in new[] { false, true })
        {
            using var fixture = new RegressionFixture();
            await fixture.Database.InitializeAsync(default);
            string[] topics = [firstTopic, secondTopic];
            using var handler = new GuideHttpHandler(index => Reply(index, index == 1 ? topics : []));
            using var http = new HttpClient(handler);
            var answers = new List<string>();
            var inputs = new Queue<string>(chat ? [question, "/exit"] : []);
            var workflow = CreateWorkflow(fixture, http, inputs, [], answers, language);
            var settings = AppSettings.Default with { Language = language, PromptCachingEnabled = false };

            if (chat)
            {
                await workflow.RunChatAsync(settings, default);
            }
            else
            {
                await workflow.RunQueryAsync(question, settings, renderQuery: true, default);
            }

            Assert.Empty(inputs);
            Assert.Single(answers);
            Assert.Equal(3, handler.RequestBodies.Count);
            Assert.Equal(2, handler.ConversationRequestBodies.Count);
            using var first = JsonDocument.Parse(handler.ConversationRequestBodies[0]);
            using var guided = JsonDocument.Parse(handler.ConversationRequestBodies[1]);
            Assert.DoesNotContain("<chapter id=", first.RootElement.GetProperty("instructions").GetString());
            var guideSchema = first.RootElement.GetProperty("text").GetProperty("format").GetProperty("schema")
                .GetProperty("properties").GetProperty("guide_topics");
            Assert.Equal(AppGuideService.Topics, guideSchema.GetProperty("items").GetProperty("enum")
                .EnumerateArray().Select(item => item.GetString()));
            Assert.Equal(2, guideSchema.GetProperty("maxItems").GetInt32());
            var catalog = new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
            foreach (var topic in topics)
            {
                var chapter = await catalog.GetAsync("app-guide-" + topic, default);
                Assert.Contains(chapter.Texts[language], guided.RootElement.GetProperty("instructions").GetString());
            }
            var input = Assert.Single(guided.RootElement.GetProperty("input").EnumerateArray());
            Assert.Equal(question, input.GetProperty("content").GetString());
            Assert.DoesNotContain("<app-guide>", input.GetProperty("content").GetString());
            Assert.Equal(3L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
            Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_session_events WHERE event_type = 'app_guide_loaded';"));
            Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM skills_and_memory_settings;"));
        }
    }

    /// <summary>Verifies display classification, guide routing, localized context, cost accounting, reuse, and clearing.</summary>
    [Theory]
    [InlineData("en", "How do I change this app's conversation budget?")]
    [InlineData("it", "Come cambio il budget della conversazione di questa app?")]
    public async Task RunChatAsync_GuideRequest_CountsAllCallsAndReusesLocalizedChapters(string language, string question)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Database.ReplaceModelPricesAsync("openai", [RegressionFixture.Price("short")], default);
        using var handler = new GuideHttpHandler(index => Reply(index, index == 1 ? ["conversation"] : []));
        using var http = new HttpClient(handler);
        var snapshots = new List<ShellRuntimeStatus>();
        var answers = new List<string>();
        var inputs = new Queue<string>([question, "/status", "And how does that affect the next question?", "/status", "/clear", "/context", "Explain a terminal command.", "/exit"]);
        var workflow = CreateWorkflow(fixture, http, inputs, snapshots, answers, language);

        await workflow.RunChatAsync(AppSettings.Default with { Language = language, PromptCachingEnabled = false }, default);

        Assert.Empty(inputs);
        Assert.Equal(7, handler.RequestBodies.Count);
        Assert.Equal(4, handler.ConversationRequestBodies.Count);
        Assert.Equal(3, answers.Count);
        Assert.All(answers, answer => Assert.False(string.IsNullOrWhiteSpace(answer)));
        var catalog = new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
        var chapter = (await catalog.GetAsync("app-guide-conversation", default)).ResolveText(language);
        using var first = JsonDocument.Parse(handler.ConversationRequestBodies[0]);
        using var guided = JsonDocument.Parse(handler.ConversationRequestBodies[1]);
        using var followUp = JsonDocument.Parse(handler.ConversationRequestBodies[2]);
        using var cleared = JsonDocument.Parse(handler.ConversationRequestBodies[3]);
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
        Assert.Equal(70, snapshots[0].SessionInputTokens);
        Assert.Equal(3, snapshots[0].SessionOutputTokens);
        Assert.Equal(0.000070m, snapshots[0].TurnCostUsd);
        Assert.Equal(0.000070m, snapshots[0].RunningCostUsd);
        Assert.Equal(0, snapshots[2].GuideTokens);
        Assert.Equal(0, snapshots[3].GuideTokens);
        Assert.Equal(0.000230m, snapshots[^1].RunningCostUsd);
        Assert.All(snapshots, snapshot =>
        {
            Assert.True(snapshot.HasContextBreakdown);
            Assert.Equal(snapshot.ActiveContextTokens,
                snapshot.SystemInstructionTokens + snapshot.UserMessageTokens + snapshot.AssistantMessageTokens);
        });
        Assert.Equal(7L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
        Assert.Equal(3L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE prompt_id = 'chat-display-intent';"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_session_events WHERE event_type = 'app_guide_loaded';"));
    }

    /// <summary>Verifies a model cannot turn guide retrieval into an unbounded sequence of paid calls.</summary>
    [Fact]
    public async Task RunChatAsync_RepeatedGuideRequest_StopsAfterClassifierAndTwoGuideCalls()
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
        Assert.Equal(3, handler.RequestBodies.Count);
        Assert.Empty(answers);
        Assert.Equal(3L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
        Assert.Equal(70L, await fixture.ScalarAsync("SELECT SUM(input_tokens) FROM ai_requests;"));
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

        Assert.Equal(3, handler.RequestBodies.Count);
        Assert.Empty(answers);
        Assert.Equal(2L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 0;"));
        Assert.Equal(30L, await fixture.ScalarAsync("SELECT SUM(input_tokens) FROM ai_requests;"));
    }

    /// <summary>Builds real guide and provider services around isolated storage and scripted passive views.</summary>
    private static AiConversationWorkflow CreateWorkflow(
        RegressionFixture fixture,
        HttpClient http,
        Queue<string> inputs,
        List<ShellRuntimeStatus> snapshots,
        List<string> answers,
        string language,
        SkillCatalogService? skills = null,
        List<string>? warnings = null)
    {
        var text = new LocalizationService();
        text.SetLanguage(language);
        var catalog = new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance);
        var provider = new OpenAiService(http, fixture.Secrets, catalog,
            TestProxy.Create<IRuntimeContextService>((_, _) => new RuntimeContext("test", "PowerShell 7 (pwsh)", "PowerShell 7", true, "~")),
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
                if (method.Name == "RenderWarning") { warnings?.Add((string)args[0]!); }
                return null;
            }), text,
            new PersistentMemoryService(fixture.Paths, new SensitiveDataRedactor(), text, NullLogger<PersistentMemoryService>.Instance),
            TestProxy.Create<IMemoryView>((method, _) => throw new NotSupportedException(method.Name)), fixture.Database,
            new AppGuideService(catalog, NullLogger<AppGuideService>.Instance), NullLogger<AiConversationWorkflow>.Instance, skills);
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
        public List<string> ConversationRequestBodies { get; } = [];

        /// <summary>Captures request payloads and supplies only local synthetic replies without making network calls.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            RequestBodies.Add(body);
            if (RegressionFixture.IsDisplayIntentRequest(body))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(RegressionFixture.DisplayIntentResponseJson())
                };
            }
            ConversationRequestBodies.Add(body);
            var index = ConversationRequestBodies.Count;
            return new HttpResponseMessage(status?.Invoke(index) ?? HttpStatusCode.OK)
            {
                Content = new StringContent(response(index))
            };
        }
    }
}

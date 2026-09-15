// SPDX-License-Identifier: MIT

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class AiConversationWorkflowTests
{
    /// <summary>Verifies an answer above the configured user-input limit is rendered completely and closes the query successfully.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunQueryAsync_LongAnswer_RendersAndCompletesSession(bool renderQuery)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var answer = new string('x', 501);
        using var http = new HttpClient(new SyntheticHttpHandler(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(RegressionFixture.ResponseJson(answer))
        }));
        var rendered = new List<string>();
        var viewCalls = new List<string>();
        var workflow = new AiConversationWorkflow(new ConversationMemoryService(), fixture.CreateOpenAi(http),
            TestProxy.Create<IPromptCatalogService>((method, _) => throw new NotSupportedException(method.Name)),
            TestProxy.Create<IPricingService>((method, _) => throw new NotSupportedException(method.Name)), fixture.Audit,
            TestProxy.Create<IAuthorizedCommandWorkflow>((method, _) => throw new NotSupportedException(method.Name)),
            TestProxy.Create<IChatView>((method, args) =>
            {
                viewCalls.Add(method.Name);
                Assert.Contains(method.Name, new[] { "RenderMemoryHint", "RenderUser", "RenderAssistant" });
                if (method.Name == "RenderAssistant")
                {
                    rendered.Add((string)args[0]!);
                }
                return null;
            }),
            TestProxy.Create<ICommandSuggestionView>((_, _) => new CommandSuggestionDecision(CommandSuggestionAction.DoNotExecute, null)),
            TestProxy.Create<ICostsView>((method, _) => throw new NotSupportedException(method.Name)),
            TestProxy.Create<IConsoleShellView>((method, args) => method.Name switch
            {
                "RunWithStatusAsync" => ((Delegate)args[1]!).DynamicInvoke(),
                "RenderRuntimeStatus" => null,
                _ => throw new NotSupportedException(method.Name)
            }), new LocalizationService(),
            CreatePersistentMemory(fixture),
            TestProxy.Create<IMemoryView>((method, _) => throw new NotSupportedException(method.Name)),
            fixture.Database, CreateAppGuide(fixture));

        await workflow.RunQueryAsync("Hello", AppSettings.Default with { MaxMessageCharacters = 500 }, renderQuery, default);

        Assert.Equal(answer, Assert.Single(rendered));
        string[] expectedCalls = renderQuery ? ["RenderMemoryHint", "RenderUser", "RenderAssistant"] : ["RenderAssistant"];
        Assert.Equal(expectedCalls, viewCalls);
        Assert.Equal("completed", await fixture.ScalarAsync("SELECT status FROM ai_sessions;"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests WHERE success = 1;"));
    }

    /// <summary>Verifies exact, argument-bearing, and similarly prefixed run input without executing a command.</summary>
    [Theory]
    [InlineData("/run", true, "")]
    [InlineData("/RUN Get-Location", true, "Get-Location")]
    [InlineData("/run   Get-ChildItem  ", true, "Get-ChildItem")]
    [InlineData("/runner", false, "")]
    public void TryParseRunCommand_InputShape_ReturnsExpectedResult(
        string input,
        bool expectedMatch,
        string expectedCommand)
    {
        var matched = AiConversationWorkflow.TryParseRunCommand(input, out var command);

        Assert.Equal(expectedMatch, matched);
        Assert.Equal(expectedCommand, command);
    }

    /// <summary>Verifies that completed AI requests retain separate provider input/output counts, cache activity, and costs.</summary>
    [Fact]
    public void CreateTurnSnapshot_ProviderUsage_PreservesInputOutputCacheAndCosts()
    {
        var response = new AiResponse(
            "response-id",
            "gpt-5.6-luna",
            "answer",
            new AiUsageMetrics(1_200, 480, 32, 300, 0, 1_500),
            new AiContextUsage(1_200, 300, 700, 500, 100, 1_050_000, false),
            new AiCostBreakdown(0.001m, 0.0001m, 0.0002m, 0.002m, 0.0033m),
            0.0033m,
            200,
            42,
            "provider-request-id");

        var snapshot = AiConversationWorkflow.CreateTurnSnapshot(response, AppSettings.Default, 0.0033m);

        Assert.Equal(1_200, snapshot.InputTokens);
        Assert.Equal(300, snapshot.OutputTokens);
        Assert.Equal(480, snapshot.CachedInputTokens);
        Assert.Equal(32, snapshot.CacheWriteTokens);
        Assert.Equal(0.0013m, snapshot.PromptCostUsd);
        Assert.Equal(0.002m, snapshot.ResponseCostUsd);
        Assert.Equal(0.0033m, snapshot.RunningCostUsd);
    }

    /// <summary>Verifies status refreshes preserve last and cumulative usage while clearing only lowers retained context.</summary>
    [Fact]
    public async Task RunChatAsync_StatusAndClear_KeepUsageAndRecalculateRetainedContext()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        const string firstQuestion = "Explain the project build commands.";
        const string secondQuestion = "Describe the available output options.";
        const string answer = "These are the available terminal options.";
        using var handler = new RecordingConversationHandler(call => RegressionFixture.ResponseJson(answer, call * 1_000));
        using var http = new HttpClient(handler);
        var openAi = fixture.CreateOpenAi(http, promptId: "chat-system");
        var inputs = new Queue<string>([firstQuestion, "/status", "/context", secondQuestion, "/status", "/clear", "/status", "/exit"]);
        var snapshots = new List<ShellRuntimeStatus>();
        var settings = AppSettings.Default;
        var workflow = CreateScriptedWorkflow(fixture, openAi, inputs.Dequeue, snapshots);

        await workflow.RunChatAsync(settings, default);

        Assert.Empty(inputs);
        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Equal(7, snapshots.Count);
        Assert.All(snapshots.Take(3), snapshot =>
        {
            Assert.Equal(1_000, snapshot.InputTokens);
            Assert.Equal(1, snapshot.OutputTokens);
            Assert.Equal(1_000, snapshot.SessionInputTokens);
            Assert.Equal(1, snapshot.SessionOutputTokens);
        });
        Assert.All(snapshots.Skip(3), snapshot =>
        {
            Assert.Equal(2_000, snapshot.InputTokens);
            Assert.Equal(1, snapshot.OutputTokens);
            Assert.Equal(3_000, snapshot.SessionInputTokens);
            Assert.Equal(2, snapshot.SessionOutputTokens);
            Assert.True(snapshot.HasSessionUsage);
        });
        var firstContext = await openAi.EstimateContextAsync("chat-system",
            [new ChatMessage("user", firstQuestion), new ChatMessage("assistant", answer)], settings, "en", default);
        var secondContext = await openAi.EstimateContextAsync("chat-system",
            [new ChatMessage("user", firstQuestion), new ChatMessage("assistant", answer),
                new ChatMessage("user", secondQuestion), new ChatMessage("assistant", answer)], settings, "en", default);
        var emptyContext = await openAi.EstimateContextAsync("chat-system", [], settings, "en", default);
        Assert.All(snapshots.Take(3), snapshot => Assert.Equal(firstContext.InputTokens, snapshot.ActiveContextTokens));
        Assert.All(snapshots.Skip(3).Take(2), snapshot => Assert.Equal(secondContext.InputTokens, snapshot.ActiveContextTokens));
        Assert.All(snapshots.Skip(5), snapshot => Assert.Equal(emptyContext.InputTokens, snapshot.ActiveContextTokens));
        Assert.True(snapshots[^1].ActiveContextTokens < snapshots[4].ActiveContextTokens);
        Assert.NotEqual(snapshots[4].InputTokens + snapshots[4].OutputTokens, snapshots[4].ActiveContextTokens);
    }

    /// <summary>An oversized request leaves the chat and retained conversation available for a shorter follow-up.</summary>
    [Fact]
    public async Task RunChatAsync_ContextLimit_ReportsAndContinuesWithoutDiscardingHistory()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var handler = new RecordingConversationHandler(_ => RegressionFixture.ResponseJson("Terminal answer."));
        using var http = new HttpClient(handler);
        var inputs = new Queue<string>(["First question", new string('x', 16_000), "/status", "Short follow-up", "/exit"]);
        var snapshots = new List<ShellRuntimeStatus>();
        var errors = new List<string>();
        var workflow = CreateScriptedWorkflow(fixture, fixture.CreateOpenAi(http, promptId: "chat-system"),
            inputs.Dequeue, snapshots, displayError: errors.Add);

        await workflow.RunChatAsync(AppSettings.Default with { ContextTokenBudget = 4_000 }, default);

        Assert.Empty(inputs);
        Assert.Single(errors);
        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Equal(3, snapshots.Count);
        Assert.Equal(snapshots[0].ActiveContextTokens, snapshots[1].ActiveContextTokens);
        Assert.Contains("First question", handler.RequestBodies[1], StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 100), handler.RequestBodies[1], StringComparison.Ordinal);
        Assert.Equal("completed", await fixture.ScalarAsync("SELECT status FROM ai_sessions;"));
    }

    /// <summary>Verifies saving, listing, and forgetting an explicit note stay local and remove the persisted note.</summary>
    [Fact]
    public async Task RunChatAsync_MemoryAdministration_DoesNotCallProvider()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        using var handler = new RecordingConversationHandler(_ => throw new InvalidOperationException("Unexpected provider request."));
        using var http = new HttpClient(handler);
        var displayed = new List<IReadOnlyList<PersistentMemory>>();
        var inputs = new Queue<Func<string>>([
            () => "/remember global Use concise terminal explanations.",
            () => "/memories",
            () => "/forget " + Assert.Single(displayed[0]).Id,
            () => "/memories",
            () => "/exit"
        ]);
        var workflow = CreateScriptedWorkflow(fixture, fixture.CreateOpenAi(http, promptId: "chat-system"),
            () => inputs.Dequeue()(), [], displayed.Add);

        await workflow.RunChatAsync(AppSettings.Default, default);

        Assert.Empty(inputs);
        Assert.Empty(handler.RequestBodies);
        Assert.Equal(2, displayed.Count);
        var saved = Assert.Single(displayed[0]);
        Assert.True(saved.IsGlobal);
        Assert.Equal("Use concise terminal explanations.", saved.Text);
        Assert.Empty(displayed[1]);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories;"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests;"));
    }

    /// <summary>Verifies recalled notes occur once per payload, survive context clearing, and disappear after forgetting.</summary>
    [Fact]
    public async Task RunChatAsync_RecalledNote_IsNotRetainedAsConversationHistory()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        const string note = "Prefer command examples for the silver orchard project.";
        using var handler = new RecordingConversationHandler(_ => RegressionFixture.ResponseJson("Terminal guidance."));
        using var http = new HttpClient(handler);
        var displayed = new List<IReadOnlyList<PersistentMemory>>();
        var snapshots = new List<ShellRuntimeStatus>();
        var inputs = new Queue<Func<string>>([
            () => "/remember global " + note,
            () => "/memories",
            () => "Explain build commands.",
            () => "Explain directory commands.",
            () => "/clear",
            () => "/status",
            () => "Explain process commands.",
            () => "/forget " + Assert.Single(displayed[0]).Id,
            () => "Explain archive commands.",
            () => "/exit"
        ]);
        var workflow = CreateScriptedWorkflow(fixture, fixture.CreateOpenAi(http, promptId: "chat-system"),
            () => inputs.Dequeue()(), snapshots, displayed.Add);

        await workflow.RunChatAsync(AppSettings.Default, default);

        Assert.Empty(inputs);
        Assert.Equal(4, handler.RequestBodies.Count);
        for (var index = 0; index < handler.RequestBodies.Count; index++)
        {
            using var request = JsonDocument.Parse(handler.RequestBodies[index]);
            var messages = request.RootElement.GetProperty("input").EnumerateArray().ToArray();
            var recalled = messages.Where(message => message.GetProperty("content").GetString()!
                .Contains(note, StringComparison.Ordinal)).ToArray();
            if (index < 3)
            {
                var recalledMessage = Assert.Single(recalled);
                Assert.Equal("user", recalledMessage.GetProperty("role").GetString());
                Assert.Equal(recalledMessage.GetRawText(), messages[0].GetRawText());
            }
            else
            {
                Assert.Empty(recalled);
            }
            Assert.Equal("user", messages[^1].GetProperty("role").GetString());
            Assert.DoesNotContain(messages, message => message.GetProperty("content").GetString()!.StartsWith('/'));
            Assert.Equal(new[] { 2, 4, 2, 3 }[index], messages.Length);
        }
        Assert.Equal(9, snapshots.Count);
        Assert.All(snapshots.Skip(4).Take(2), snapshot =>
        {
            Assert.Equal(1, snapshot.MemoryCount);
            Assert.InRange(snapshot.MemoryTokens, 1, 800);
        });
        Assert.True(snapshots[4].ActiveContextTokens < snapshots[3].ActiveContextTokens);
        Assert.Equal(0, snapshots[^1].MemoryCount);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM persistent_memories;"));
        Assert.Equal(4L, await fixture.ScalarAsync("SELECT COUNT(*) FROM ai_requests;"));
    }

    /// <summary>Creates a real isolated memory store using only the fixture database and a silent logger.</summary>
    private static PersistentMemoryService CreatePersistentMemory(RegressionFixture fixture) => new(
        fixture.Paths, new SensitiveDataRedactor(), new LocalizationService(), NullLogger<PersistentMemoryService>.Instance);

    /// <summary>Creates a local guide reader from the packaged six-language prompt resources.</summary>
    private static AppGuideService CreateAppGuide(RegressionFixture fixture) => new(
        new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance),
        NullLogger<AppGuideService>.Instance);

    /// <summary>Builds a scripted chat workflow that captures status and note displays without terminal input or command execution.</summary>
    private static AiConversationWorkflow CreateScriptedWorkflow(
        RegressionFixture fixture,
        IOpenAiService openAi,
        Func<string> readMessage,
        List<ShellRuntimeStatus> snapshots,
        Action<IReadOnlyList<PersistentMemory>>? displayMemories = null,
        Action<string>? displayError = null) => new(
        new ConversationMemoryService(),
        openAi,
        new YamlPromptCatalogService(fixture.Paths, NullLogger<YamlPromptCatalogService>.Instance),
        TestProxy.Create<IPricingService>((method, _) => throw new NotSupportedException(method.Name)),
        fixture.Audit,
        TestProxy.Create<IAuthorizedCommandWorkflow>((method, _) => throw new NotSupportedException(method.Name)),
        TestProxy.Create<IChatView>((method, _) => method.Name switch
        {
            "ReadMessage" => readMessage(),
            "RenderIntro" or "RenderAssistant" or "RenderMemoryPruned" => null,
            _ => throw new NotSupportedException(method.Name)
        }),
        TestProxy.Create<ICommandSuggestionView>((_, _) => new CommandSuggestionDecision(CommandSuggestionAction.DoNotExecute, null)),
        TestProxy.Create<ICostsView>((method, _) => throw new NotSupportedException(method.Name)),
        TestProxy.Create<IConsoleShellView>((method, args) =>
        {
            if (method.Name == "RenderRuntimeStatus")
            {
                snapshots.Add((ShellRuntimeStatus)args[0]!);
                return null;
            }
            if (method.Name == "RenderError" && displayError is not null)
            {
                displayError((string)args[0]!);
                return null;
            }
            return method.Name switch
            {
                "RunWithStatusAsync" => ((Delegate)args[1]!).DynamicInvoke(),
                "RenderMuted" or "RenderSuccess" => null,
                _ => throw new NotSupportedException(method.Name)
            };
        }),
        new LocalizationService(),
        CreatePersistentMemory(fixture),
        TestProxy.Create<IMemoryView>((method, args) =>
        {
            Assert.Equal("Render", method.Name);
            displayMemories?.Invoke((IReadOnlyList<PersistentMemory>)args[0]!);
            return null;
        }),
        fixture.Database, CreateAppGuide(fixture));

    private sealed class RecordingConversationHandler : HttpMessageHandler
    {
        private readonly Func<int, string> _response;
        public List<string> RequestBodies { get; } = [];

        /// <summary>Supplies synthetic responses while preserving every outgoing request body for regression assertions.</summary>
        internal RecordingConversationHandler(Func<int, string> response)
        {
            _response = response;
        }

        /// <summary>Records local request content and returns an in-memory response without network access.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_response(RequestBodies.Count))
            };
        }
    }
}

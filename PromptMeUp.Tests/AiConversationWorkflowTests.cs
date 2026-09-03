// SPDX-License-Identifier: MIT

using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class AiConversationWorkflowTests
{
    /// <summary>Verifies an answer above the configured user-input limit is rendered completely and closes the query successfully.</summary>
    [Fact]
    public async Task RunQueryAsync_LongAnswer_RendersAndCompletesSession()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var answer = new string('x', 501);
        using var http = new HttpClient(new SyntheticHttpHandler(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(RegressionFixture.ResponseJson(answer))
        }));
        var rendered = new List<string>();
        var workflow = new AiConversationWorkflow(new ConversationMemoryService(), fixture.CreateOpenAi(http),
            TestProxy.Create<IPromptCatalogService>((method, _) => throw new NotSupportedException(method.Name)),
            TestProxy.Create<IPricingService>((method, _) => throw new NotSupportedException(method.Name)), fixture.Audit,
            TestProxy.Create<IAuthorizedCommandWorkflow>((method, _) => throw new NotSupportedException(method.Name)),
            TestProxy.Create<IChatView>((method, args) =>
            {
                Assert.Equal("RenderAssistant", method.Name);
                rendered.Add((string)args[0]!);
                return null;
            }),
            TestProxy.Create<ICommandSuggestionView>((_, _) => new CommandSuggestionDecision(CommandSuggestionAction.DoNotExecute, null)),
            TestProxy.Create<ICostsView>((method, _) => throw new NotSupportedException(method.Name)),
            TestProxy.Create<IConsoleShellView>((method, args) => method.Name switch
            {
                "RunWithStatusAsync" => ((Delegate)args[1]!).DynamicInvoke(),
                "RenderRuntimeStatus" => null,
                _ => throw new NotSupportedException(method.Name)
            }), new LocalizationService());

        await workflow.RunQueryAsync("Hello", AppSettings.Default with { MaxMessageCharacters = 500 }, false, default);

        Assert.Equal(answer, Assert.Single(rendered));
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

    /// <summary>Verifies that completed AI requests expose total context and separate provider input/output counts.</summary>
    [Fact]
    public void CreateTurnSnapshot_ProviderUsage_SeparatesContextInputAndOutput()
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

        Assert.Equal(1_500, snapshot.ContextTotalTokens);
        Assert.Equal(1_200, snapshot.InputTokens);
        Assert.Equal(300, snapshot.OutputTokens);
        Assert.Equal(480, snapshot.CachedInputTokens);
        Assert.Equal(32, snapshot.CacheWriteTokens);
        Assert.Equal(0.0013m, snapshot.PromptCostUsd);
        Assert.Equal(0.002m, snapshot.ResponseCostUsd);
        Assert.Equal(0.0033m, snapshot.RunningCostUsd);
    }
}

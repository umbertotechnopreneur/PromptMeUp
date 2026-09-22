// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Services.OpenAi;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class DirectModeTests
{
    /// <summary>Parses direct requests while preserving normal argument, source, and secret validation.</summary>
    [Fact]
    public void Parser_Direct_RequiresOneNonSecretRequest()
    {
        var parser = new CommandLineParser(new LocalizationService());
        var parsed = parser.Parse(["--direct", "show the current folder", "--language", "it"]);
        Assert.True(parsed.Succeeded);
        Assert.Equal(AppCommand.Direct, parsed.Options!.Command);
        Assert.Equal("show the current folder", parsed.Options.Query);
        Assert.False(parser.Parse(["--direct"]).Succeeded);
        Assert.False(parser.Parse(["--direct", " "]).Succeeded);
        Assert.False(parser.Parse(["--direct", "--direct", "request"]).Succeeded);
        Assert.False(parser.Parse(["--direct", "--chat", "request"]).Succeeded);
        Assert.False(parser.Parse(["--direct", "--file", "input.txt", "request"]).Succeeded);
        Assert.False(parser.Parse(["--direct", "password=synthetic-test-secret"]).Succeeded);
    }

    /// <summary>Starts default-on and persists an explicit opt-out across database reinitialization.</summary>
    [Fact]
    public async Task Settings_DirectMode_RoundTripsOptOut()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = await fixture.Database.LoadSettingsAsync(default);
        Assert.True(original.DirectModeEnabled);
        var configured = original with { DirectModeEnabled = false };
        await fixture.Database.SaveSettingsAsync(configured, default);
        await fixture.Database.InitializeAsync(default);
        Assert.Equal(configured, await fixture.Database.LoadSettingsAsync(default));
    }

    /// <summary>Keeps existing local preferences while introducing the requested default for the new setting.</summary>
    [Fact]
    public async Task Settings_VersionFour_AddsDirectModeWithoutReplacingPreferences()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = (await fixture.Database.LoadSettingsAsync(default)) with { Language = "it", Theme = "green" };
        await fixture.Database.SaveSettingsAsync(original, default);
        await fixture.ScalarAsync("ALTER TABLE app_settings DROP COLUMN direct_mode_enabled; PRAGMA user_version = 4;");
        await fixture.Database.InitializeAsync(default);
        Assert.Equal(original, await fixture.Database.LoadSettingsAsync(default));
        Assert.Equal(8L, await fixture.ScalarAsync("PRAGMA user_version;"));
    }

    /// <summary>Persists the default-off session-summary preference and adds it safely to a version-five database.</summary>
    [Fact]
    public async Task Settings_VersionFive_AddsSessionSummaryWithoutReplacingPreferences()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = (await fixture.Database.LoadSettingsAsync(default)) with { Language = "it", Theme = "green" };
        Assert.False(original.ShowSessionSummaryDuringWork);
        await fixture.Database.SaveSettingsAsync(original, default);
        await fixture.ScalarAsync("ALTER TABLE app_settings DROP COLUMN show_session_summary_during_work; PRAGMA user_version = 5;");

        await fixture.Database.InitializeAsync(default);

        Assert.Equal(original, await fixture.Database.LoadSettingsAsync(default));
        Assert.Equal(8L, await fixture.ScalarAsync("PRAGMA user_version;"));
    }

    /// <summary>Retains an explicitly enabled during-work summary preference across database reinitialization.</summary>
    [Fact]
    public async Task Settings_SessionSummaryDuringWork_RoundTrips()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var configured = (await fixture.Database.LoadSettingsAsync(default)) with { ShowSessionSummaryDuringWork = true };

        await fixture.Database.SaveSettingsAsync(configured, default);
        await fixture.Database.InitializeAsync(default);

        Assert.Equal(configured, await fixture.Database.LoadSettingsAsync(default));
    }

    /// <summary>Adds the first-run script language safely to version-six settings while preserving every pre-existing preference.</summary>
    [Fact]
    public async Task Settings_VersionSix_AddsScriptLanguageWithoutReplacingPreferences()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = (await fixture.Database.LoadSettingsAsync(default)) with
        {
            Language = "it",
            Theme = "green",
            ShowSessionSummaryDuringWork = true
        };
        await fixture.Database.SaveSettingsAsync(original, default);
        await fixture.ScalarAsync("ALTER TABLE app_settings DROP COLUMN script_language; PRAGMA user_version = 6;");

        await fixture.Database.InitializeAsync(default);

        var migrated = await fixture.Database.LoadSettingsAsync(default);
        Assert.Equal(original.Language, migrated.Language);
        Assert.Equal(original.Theme, migrated.Theme);
        Assert.Equal(original.ShowSessionSummaryDuringWork, migrated.ShowSessionSummaryDuringWork);
        Assert.True(Enum.IsDefined<ScriptLanguage>(migrated.ScriptLanguage));
        Assert.Equal(8L, await fixture.ScalarAsync("PRAGMA user_version;"));
    }

    /// <summary>Prevents countdown and execution for vetoed or incomplete reviews, including inconsistent score and severity.</summary>
    [Theory]
    [InlineData(15, CommandRiskLevel.High, true)]
    [InlineData(15, CommandRiskLevel.Critical, true)]
    [InlineData(75, CommandRiskLevel.Low, true)]
    [InlineData(15, CommandRiskLevel.Unknown, true)]
    [InlineData(15, CommandRiskLevel.Low, false)]
    public async Task Workflow_BlockedRisk_NeverReachesAuthorization(int score, CommandRiskLevel level, bool usedAi)
    {
        var calls = new List<string>();
        var workflow = CreateWorkflow(new(score, level, "Risk evidence", usedAi, null), calls);
        var result = await workflow.RunAsync("direct-test", "Get-Location", AppSettings.Default with { ReviewCommandsWithAi = false },
            default, CommandExecutionMode.Direct);
        Assert.Null(result);
        Assert.Equal(new[] { "risk:True", "command_preview", "preview", "warning", "blocked" }, calls);
    }

    /// <summary>Passes safe reviewed commands through the countdown and shared redacted output analysis exactly once.</summary>
    [Fact]
    public async Task Workflow_DirectApproval_UsesSharedResultPipeline()
    {
        var calls = new List<string>();
        var workflow = CreateWorkflow(new(15, CommandRiskLevel.Low, "Read-only", true, null), calls);
        var followUp = await workflow.RunAsync("direct-test", "Get-Location", AppSettings.Default, default, CommandExecutionMode.Direct);
        Assert.NotNull(followUp);
        Assert.Contains("Analyze this result", followUp);
        Assert.DoesNotContain("synthetic-secret", followUp);
        Assert.Equal(new[] { "risk:True", "command_preview", "preview", "authorize:Direct", "approved", "execute", "result", "command_output" }, calls);
    }

    /// <summary>Checks cancellation again after the countdown decision so an interrupted approval cannot start a process.</summary>
    [Fact]
    public async Task Workflow_CancellationAtCountdownBoundary_NeverExecutes()
    {
        using var cancellation = new CancellationTokenSource();
        var calls = new List<string>();
        var workflow = CreateWorkflow(new(15, CommandRiskLevel.Low, "Read-only", true, null), calls, cancellation.Cancel);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => workflow.RunAsync("direct-test", "Get-Location",
            AppSettings.Default, cancellation.Token, CommandExecutionMode.Direct));
        Assert.DoesNotContain("execute", calls);
        Assert.DoesNotContain("approved", calls);
    }

    /// <summary>Preserves an AI severity veto even when its numeric score is lower than the deterministic local score.</summary>
    [Fact]
    public async Task RiskService_HighLabelWithLowScore_PreservesVeto()
    {
        var service = new CommandRiskAssessmentService(
            TestProxy.Create<IOpenAiService>((_, _) => Task.FromResult(new CommandRiskAssessment(1, CommandRiskLevel.High, "Danger", true, null))),
            TestProxy.Create<IEnvironmentSecretService>((_, _) => true), new SensitiveDataRedactor(),
            NullLogger<CommandRiskAssessmentService>.Instance);
        var assessment = await service.AssessAsync("risk-test", "Get-Location", true, AppSettings.Default, "en", default);
        Assert.Equal(15, assessment.Score);
        Assert.Equal(CommandRiskLevel.High, assessment.Level);
        Assert.False(assessment.CanRunDirect);
    }

    /// <summary>Rejects malformed risk contracts instead of guessing a severity that could enable direct execution.</summary>
    [Theory]
    [InlineData(-1, "low")]
    [InlineData(101, "low")]
    [InlineData(10, "unexpected")]
    public void RiskParser_InvalidContract_Fails(int score, string level) =>
        Assert.Throws<OpenAiRequestException>(() => OpenAiResponseParser.ParseRiskAssessment(
            $$"""{"score":{{score}},"level":"{{level}}","description_markdown":"Review"}"""));

    /// <summary>Shows the context override exactly at eighty percent and never for an unavailable budget.</summary>
    [Theory]
    [InlineData(12799, 16000, false)]
    [InlineData(12800, 16000, true)]
    [InlineData(16000, 16000, true)]
    [InlineData(100, 0, false)]
    public void Summary_ContextThreshold_UsesOperatingBudget(long used, long budget, bool expected) =>
        Assert.Equal(expected, AiConversationWorkflow.IsContextWarning(ShellRuntimeStatus.FromSettings(AppSettings.Default) with
        {
            ActiveContextTokens = used,
            ContextBudgetTokens = budget
        }));

    /// <summary>Builds a command workflow with no network, process, terminal, or persistent effects.</summary>
    private static AuthorizedCommandWorkflow CreateWorkflow(CommandRiskAssessment assessment, List<string> calls, Action? onApproval = null) => new(
        TestProxy.Create<ICommandRiskAssessmentService>((_, args) =>
        {
            Assert.Equal("direct-test", args[0]);
            calls.Add($"risk:{args[2]}");
            return Task.FromResult(assessment);
        }),
        TestProxy.Create<ICommandExecutionService>((_, args) =>
        {
            calls.Add("execute");
            return Task.FromResult(new CommandExecutionResult(((ApprovedCommand)args[0]!).Text, 0, "password=synthetic-secret", "", false, false, 1));
        }),
        TestProxy.Create<IActivityAuditService>((method, args) =>
        {
            calls.Add((string)args[1]!);
            return Task.CompletedTask;
        }),
        new SensitiveDataRedactor(),
        TestProxy.Create<ICommandAuthorizationView>((method, args) =>
        {
            if (method.Name == "AuthorizeAsync")
            {
                calls.Add($"authorize:{args[0]}");
                onApproval?.Invoke();
                return Task.FromResult(true);
            }
            calls.Add(method.Name == "RenderPreview" ? "preview" : "result");
            return null;
        }),
        TestProxy.Create<IConsoleShellView>((method, args) =>
        {
            if (method.Name == "RunWithStatusAsync")
            {
                return ((Delegate)args[1]!).DynamicInvoke();
            }
            calls.Add("warning");
            return null;
        }),
        new LocalizationService(), RegressionFixture.CreatePackagedPrompts());
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class AuthorizedCommandWorkflowTests
{
    /// <summary>Verifies JSON credentials in both command streams are removed before the AI follow-up is assembled.</summary>
    [Fact]
    public async Task RunAsync_JsonCredentialsInStreams_RedactsProviderFollowUp()
    {
        var result = new CommandExecutionResult("Get-Location", 0,
            "{\"password\":\"synthetic-output\",\"safe\":\"keep\"}",
            "{\"api_key\":\"synthetic-error\"}", false, false, 5);
        var fixture = new WorkflowFixture(authorized: true, result);

        var followUp = await fixture.Workflow.RunAsync("json-output", "Get-Location", AppSettings.Default, default);

        Assert.NotNull(followUp);
        Assert.DoesNotContain("synthetic-output", followUp, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-error", followUp, StringComparison.Ordinal);
        Assert.Contains("keep", followUp, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(followUp, "[redacted-credential]"));
    }

    /// <summary>Verifies that rejecting the exact preview prevents execution and records the denial in order.</summary>
    [Fact]
    public async Task RunAsync_DeniedAuthorization_DoesNotExecuteAndAuditsDenial()
    {
        var fixture = new WorkflowFixture(authorized: false);

        var followUp = await fixture.Workflow.RunAsync(
            "session-denied",
            "Get-Location",
            AppSettings.Default,
            CancellationToken.None);

        Assert.Null(followUp);
        Assert.Equal(0, fixture.Execution.CallCount);
        Assert.Equal(0, fixture.CommandView.RenderCount);
        Assert.Equal(
            ["event:command_preview", "activity:command_authorization:denied"],
            fixture.Audit.Calls);
    }

    /// <summary>Verifies that Escape at the authorization prompt behaves as a denial and never executes the command.</summary>
    [Fact]
    public async Task RunAsync_CancelledAuthorization_DoesNotExecuteAndAuditsDenial()
    {
        var fixture = new WorkflowFixture(authorized: false, cancelAuthorization: true);

        var followUp = await fixture.Workflow.RunAsync(
            "session-cancelled",
            "Get-Location",
            AppSettings.Default,
            CancellationToken.None);

        Assert.Null(followUp);
        Assert.Equal(0, fixture.Execution.CallCount);
        Assert.Equal(0, fixture.CommandView.RenderCount);
        Assert.Equal(1, fixture.Shell.WarningCount);
        Assert.Equal(
            ["event:command_preview", "activity:command_authorization:denied"],
            fixture.Audit.Calls);
    }

    /// <summary>Verifies that one explicit approval yields exactly one execution and the expected audit sequence.</summary>
    [Fact]
    public async Task RunAsync_ApprovedAuthorization_ExecutesOnceAndAuditsInOrder()
    {
        var fixture = new WorkflowFixture(authorized: true);

        var followUp = await fixture.Workflow.RunAsync(
            "session-approved",
            "Get-Location",
            AppSettings.Default,
            CancellationToken.None);

        Assert.NotNull(followUp);
        Assert.Equal(1, fixture.Execution.CallCount);
        Assert.Equal(1, fixture.CommandView.RenderCount);
        Assert.Equal(
            [
                "event:command_preview",
                "activity:command_authorization:approved",
                "event:command_output"
            ],
            fixture.Audit.Calls);
    }

    /// <summary>Verifies that command and stream credentials are redacted before bounded provider follow-up text is returned.</summary>
    [Fact]
    public async Task RunAsync_SensitiveLongOutput_RedactsAndBoundsEachStream()
    {
        var syntheticKey = "sk-" + new string('a', 24);
        var syntheticBearer = new string('b', 24);
        const int streamLimit = 72;
        var command = $"Write-Output {syntheticKey}";
        var result = new CommandExecutionResult(
            command,
            0,
            $"stdout {syntheticKey} {new string('o', 160)}",
            $"Bearer {syntheticBearer} {new string('e', 160)}",
            TimedOut: false,
            OutputTruncated: false,
            ElapsedMilliseconds: 5);
        var fixture = new WorkflowFixture(authorized: true, result);
        var settings = AppSettings.Default with
        {
            MaxCommandOutputCharacters = streamLimit,
            MaxMessageCharacters = 4_096
        };

        var followUp = await fixture.Workflow.RunAsync(
            "session-redacted",
            command,
            settings,
            CancellationToken.None);

        Assert.NotNull(followUp);
        Assert.DoesNotContain(syntheticKey, followUp, StringComparison.Ordinal);
        Assert.DoesNotContain(syntheticBearer, followUp, StringComparison.Ordinal);
        Assert.Contains("[redacted-openai-key]", followUp, StringComparison.Ordinal);
        Assert.Contains("[redacted-bearer-token]", followUp, StringComparison.Ordinal);
        Assert.True(ExtractSection(followUp, "Standard output:", "Standard error:").Length <= streamLimit);
        Assert.True(ExtractSection(followUp, "Standard error:", "Analyze this result").Length <= streamLimit);
        Assert.Equal(2, CountOccurrences(followUp, "[truncated by PromptMeUp]"));
    }

    /// <summary>Verifies that the complete provider follow-up never exceeds the configured message boundary.</summary>
    [Fact]
    public async Task RunAsync_LongFollowUp_RespectsMessageCharacterLimit()
    {
        const int messageLimit = 96;
        var result = new CommandExecutionResult(
            "Get-Location",
            0,
            new string('o', 200),
            string.Empty,
            TimedOut: false,
            OutputTruncated: false,
            ElapsedMilliseconds: 5);
        var fixture = new WorkflowFixture(authorized: true, result);
        var settings = AppSettings.Default with
        {
            MaxCommandOutputCharacters = 500,
            MaxMessageCharacters = messageLimit
        };

        var followUp = await fixture.Workflow.RunAsync(
            "session-bounded",
            "Get-Location",
            settings,
            CancellationToken.None);

        Assert.NotNull(followUp);
        Assert.Equal(messageLimit, followUp.Length);
        Assert.EndsWith("[truncated by PromptMeUp]", followUp, StringComparison.Ordinal);
    }

    /// <summary>Extracts and trims text between two stable follow-up labels.</summary>
    private static string ExtractSection(string value, string startLabel, string endLabel)
    {
        var start = value.IndexOf(startLabel, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing start label: {startLabel}");
        start += startLabel.Length;
        var end = value.IndexOf(endLabel, start, StringComparison.Ordinal);
        Assert.True(end >= 0, $"Missing end label: {endLabel}");
        return value[start..end].Trim();
    }

    /// <summary>Counts non-overlapping occurrences of one expected marker.</summary>
    private static int CountOccurrences(string value, string marker)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }

        return count;
    }

    private sealed class WorkflowFixture
    {
        /// <summary>Creates a complete in-memory command workflow with deterministic collaborators.</summary>
        public WorkflowFixture(
            bool authorized,
            CommandExecutionResult? executionResult = null,
            bool cancelAuthorization = false)
        {
            var assessment = new CommandRiskAssessment(
                15,
                CommandRiskLevel.Low,
                "Read-only diagnostic command.",
                UsedAi: false,
                Advisory: null);
            RiskAssessment = new FakeRiskAssessmentService(assessment);
            Execution = new FakeCommandExecutionService(executionResult ?? new CommandExecutionResult(
                "Get-Location",
                0,
                "E:\\workspace",
                string.Empty,
                TimedOut: false,
                OutputTruncated: false,
                ElapsedMilliseconds: 5));
            Audit = new FakeActivityAuditService();
            CommandView = new FakeCommandAuthorizationView(authorized, cancelAuthorization);
            Shell = new FakeConsoleShellView();
            Workflow = new AuthorizedCommandWorkflow(
                RiskAssessment,
                Execution,
                Audit,
                new SensitiveDataRedactor(),
                CommandView,
                Shell,
                new FakeLocalizationService());
        }

        public AuthorizedCommandWorkflow Workflow { get; }

        public FakeRiskAssessmentService RiskAssessment { get; }

        public FakeCommandExecutionService Execution { get; }

        public FakeActivityAuditService Audit { get; }

        public FakeCommandAuthorizationView CommandView { get; }

        public FakeConsoleShellView Shell { get; }
    }

    private sealed class FakeRiskAssessmentService : ICommandRiskAssessmentService
    {
        private readonly CommandRiskAssessment _assessment;

        /// <summary>Creates a risk assessor that returns one fixed local assessment.</summary>
        public FakeRiskAssessmentService(CommandRiskAssessment assessment) => _assessment = assessment;

        /// <summary>Returns the configured assessment without making an AI request.</summary>
        public Task<CommandRiskAssessment> AssessAsync(
            string command,
            bool useAi,
            AppSettings settings,
            string language,
            CancellationToken cancellationToken) => Task.FromResult(_assessment);
    }

    private sealed class FakeCommandExecutionService : ICommandExecutionService
    {
        private readonly CommandExecutionResult _result;

        /// <summary>Creates an executor that returns a fixed result without starting a process.</summary>
        public FakeCommandExecutionService(CommandExecutionResult result) => _result = result;

        public int CallCount { get; private set; }

        /// <summary>Records one execution request and returns the in-memory result.</summary>
        public Task<CommandExecutionResult> ExecuteAsync(
            ApprovedCommand command,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeActivityAuditService : IActivityAuditService
    {
        public List<string> Calls { get; } = [];

        /// <summary>Rejects unexpected session starts in this focused workflow fixture.</summary>
        public Task StartSessionAsync(
            string sessionId,
            string kind,
            AppSettings settings,
            object? metadata,
            CancellationToken cancellationToken) => throw new InvalidOperationException("Session start is outside this workflow.");

        /// <summary>Rejects unexpected session closes in this focused workflow fixture.</summary>
        public Task CloseSessionAsync(
            string sessionId,
            string status,
            CancellationToken cancellationToken) => throw new InvalidOperationException("Session close is outside this workflow.");

        /// <summary>Records the ordered session event name without persisting data.</summary>
        public Task AppendSessionEventAsync(
            string sessionId,
            string eventType,
            object payload,
            CancellationToken cancellationToken)
        {
            Calls.Add($"event:{eventType}");
            return Task.CompletedTask;
        }

        /// <summary>Records the ordered activity type and outcome without persisting data.</summary>
        public Task RecordAsync(
            string activityType,
            string outcome,
            string? sessionId,
            object payload,
            CancellationToken cancellationToken)
        {
            Calls.Add($"activity:{activityType}:{outcome}");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCommandAuthorizationView : ICommandAuthorizationView
    {
        private readonly bool _authorized;
        private readonly bool _cancelAuthorization;

        /// <summary>Creates an authorization view with a deterministic decision.</summary>
        public FakeCommandAuthorizationView(bool authorized, bool cancelAuthorization)
        {
            _authorized = authorized;
            _cancelAuthorization = cancelAuthorization;
        }

        public int RenderCount { get; private set; }

        /// <summary>Returns an execution capability only when the fixture is configured to approve.</summary>
        public ApprovedCommand? PreviewAndAuthorize(string command, CommandRiskAssessment assessment)
        {
            if (_cancelAuthorization)
            {
                throw new InteractiveFlowCanceledException();
            }

            return _authorized ? ApprovedCommand.Create(command, assessment) : null;
        }

        /// <summary>Records rendering of the fixed execution result.</summary>
        public void RenderExecutionResult(CommandExecutionResult result) => RenderCount++;
    }

    private sealed class FakeConsoleShellView : IConsoleShellView
    {
        public ConsoleRenderOptions Options { get; private set; } = new(true, true);

        public int WarningCount { get; private set; }

        /// <summary>Stores rendering options for completeness.</summary>
        public void Configure(ConsoleRenderOptions options) => Options = options;

        /// <summary>Rejects unexpected header rendering in this focused workflow fixture.</summary>
        public void RenderHeader(string command, AppSettings? settings, bool hasApiKey) =>
            throw new InvalidOperationException("Header rendering is outside this workflow.");

        /// <summary>Rejects unexpected runtime-status rendering in this focused workflow fixture.</summary>
        public void RenderRuntimeStatus(ShellRuntimeStatus status) =>
            throw new InvalidOperationException("Runtime status is outside this workflow.");

        /// <summary>Runs the supplied in-memory action without terminal animation.</summary>
        public Task<T> RunWithStatusAsync<T>(string message, Func<Task<T>> action) => action();

        /// <summary>Rejects unexpected footer rendering in this focused workflow fixture.</summary>
        public void RenderFooter(string command) =>
            throw new InvalidOperationException("Footer rendering is outside this workflow.");

        /// <summary>Rejects unexpected error rendering in this focused workflow fixture.</summary>
        public void RenderError(string message) =>
            throw new InvalidOperationException("Error rendering is outside this workflow.");

        /// <summary>Rejects unexpected notice rendering in this focused workflow fixture.</summary>
        public void RenderNotice(string message) =>
            throw new InvalidOperationException("Notice rendering is outside this workflow.");

        /// <summary>Rejects unexpected success rendering in this focused workflow fixture.</summary>
        public void RenderSuccess(string message) =>
            throw new InvalidOperationException("Success rendering is outside this workflow.");

        /// <summary>Accepts cancellation warnings without writing to a terminal.</summary>
        public void RenderWarning(string message)
        {
            WarningCount++;
        }

        /// <summary>Rejects unexpected muted rendering in this focused workflow fixture.</summary>
        public void RenderMuted(string message) =>
            throw new InvalidOperationException("Muted rendering is outside this workflow.");

        /// <summary>Rejects unexpected section-title rendering in this focused workflow fixture.</summary>
        public void RenderSectionTitle(string message) =>
            throw new InvalidOperationException("Section titles are outside this workflow.");

        /// <summary>Rejects unexpected interactive input in this focused workflow fixture.</summary>
        public string ReadText(string prompt) =>
            throw new InvalidOperationException("Interactive input is outside this workflow.");

        /// <summary>Rejects unexpected version rendering in this focused workflow fixture.</summary>
        public void RenderVersion(string applicationVersion, string runtimeVersion, string runtimeIdentifier) =>
            throw new InvalidOperationException("Version rendering is outside this workflow.");

        /// <summary>Rejects unexpected blank-line rendering in this focused workflow fixture.</summary>
        public void WriteLine() =>
            throw new InvalidOperationException("Line rendering is outside this workflow.");
    }

    private sealed class FakeLocalizationService : ILocalizationService
    {
        public string Language { get; private set; } = "en";

        public System.Globalization.CultureInfo Culture => System.Globalization.CultureInfo.InvariantCulture;

        /// <summary>Stores the requested supported language for completeness.</summary>
        public void SetLanguage(string language) => Language = language;

        /// <summary>Returns stable localization keys without loading external resources.</summary>
        public string Text(string key, params object?[] args) => key;
    }
}

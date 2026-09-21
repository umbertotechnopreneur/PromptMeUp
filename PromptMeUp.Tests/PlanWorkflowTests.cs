// SPDX-License-Identifier: MIT

using System.Reflection;
using PromptMeUp.Application;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class PlanWorkflowTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hm-plan-tests-" + Guid.NewGuid().ToString("N"));
    private readonly LocalizationService _text = new();

    /// <summary>Stops after a failed action and never starts a later plan command.</summary>
    [Fact]
    public async Task RunStepsAsync_FailedAction_StopsBeforeLaterStep()
    {
        var fixture = CreateFixture([Result("first", 1)]);
        var plan = Plan("first", "second");

        Assert.Equal(1, await fixture.Workflow.RunStepsAsync(plan, false, AppSettings.Default, CancellationToken.None));
        Assert.Equal(["first"], fixture.Commands.Seen);
        Assert.Equal(PlanStepStatus.NeedsReview, plan.Steps[0].Status);
        Assert.Equal(PlanStepStatus.Pending, plan.Steps[1].Status);
    }

    /// <summary>Verifies an interrupted action on resume and does not replay the original command.</summary>
    [Fact]
    public async Task RunStepsAsync_InterruptedAction_VerifiesWithoutReplay()
    {
        var fixture = CreateFixture([Result("verify-first", 0)]);
        var plan = Plan("first") with { Steps = [new("Step 1", "first", "verify-first", "present", PlanStepStatus.Running)] };

        Assert.Equal(0, await fixture.Workflow.RunStepsAsync(plan, true, AppSettings.Default, CancellationToken.None));
        Assert.Equal(["verify-first"], fixture.Commands.Seen);
        Assert.Equal(PlanStepStatus.Completed, plan.Steps[0].Status);
    }

    /// <summary>Retains later completion checkpoints if an earlier resume verification fails.</summary>
    [Fact]
    public async Task RunStepsAsync_FailedRecheck_RetainsLaterCompletion()
    {
        var fixture = CreateFixture([Result("verify-first", 1)]);
        var plan = Plan("first", "second") with
        {
            Steps =
            [
                new("Step 1", "first", "verify-first", "present", PlanStepStatus.Completed),
                new("Step 2", "second", "verify-second", "present", PlanStepStatus.Completed)
            ]
        };

        Assert.Equal(1, await fixture.Workflow.RunStepsAsync(plan, true, AppSettings.Default, CancellationToken.None));
        Assert.Equal(PlanStepStatus.NeedsReview, plan.Steps[0].Status);
        Assert.Equal(PlanStepStatus.Completed, plan.Steps[1].Status);
    }

    /// <summary>Leaves a declined action pending and never treats denial as an uncertain execution.</summary>
    [Fact]
    public async Task RunStepsAsync_DeclinedAction_RemainsPending()
    {
        var fixture = CreateFixture([null]);
        var plan = Plan("first", "second");

        Assert.Equal(0, await fixture.Workflow.RunStepsAsync(plan, false, AppSettings.Default, CancellationToken.None));
        Assert.Equal(PlanStepStatus.Pending, plan.Steps[0].Status);
        Assert.Equal(["first"], fixture.Commands.Seen);
    }

    /// <summary>Uses the saved direct preference for both the plan action and its independent verification command.</summary>
    [Theory]
    [InlineData(true, CommandExecutionMode.Direct)]
    [InlineData(false, CommandExecutionMode.Confirm)]
    public async Task RunStepsAsync_UsesGlobalExecutionPreference(bool directModeEnabled, CommandExecutionMode expectedMode)
    {
        var fixture = CreateFixture([Result("first", 0), Result("verify-first", 0)]);

        await fixture.Workflow.RunStepsAsync(Plan("first"), false,
            AppSettings.Default with { DirectModeEnabled = directModeEnabled }, CancellationToken.None);

        Assert.Equal([expectedMode, expectedMode], fixture.Commands.Modes);
    }

    /// <summary>Shows the interim plan summary at eighty percent context usage even when the saved preference is off.</summary>
    [Fact]
    public async Task RunStepsAsync_ContextAtEightyPercent_RendersInterimSummaryWhenPreferenceIsOff()
    {
        var fixture = CreateFixture([Result("first", 0), Result("verify-first", 0), Result("second", 0), Result("verify-second", 0)]);
        var status = ShellRuntimeStatus.FromSettings(AppSettings.Default) with
        {
            ActiveContextTokens = 80,
            ContextBudgetTokens = 100
        };

        await fixture.Workflow.RunStepsAsync(Plan("first", "second"), false,
            AppSettings.Default with { ShowSessionSummaryDuringWork = false }, CancellationToken.None, status);

        Assert.Equal([status, status], fixture.Shell.Statuses);
    }

    /// <summary>Prevents concurrent guidance for the same durable plan identifier.</summary>
    [Fact]
    public void Acquire_SamePlanTwice_RejectsSecondLease()
    {
        var store = CreateStore();
        var id = Guid.NewGuid().ToString("N");
        using var lease = store.Acquire(id);
        Assert.Throws<InvalidOperationException>(() => store.Acquire(id));
    }

    /// <summary>Ignores any model-provided completion status and imports every new step as pending.</summary>
    [Fact]
    public void Parse_ModelStatus_DoesNotGrantProgress()
    {
        var json = """{"steps":[{"label":"one","command":"Get-Location","verification":"Get-Location","expected":"path","status":"completed"}]}""";
        Assert.Equal(PlanStepStatus.Pending, CreateStore().Parse(json, "inspect", _directory).Steps[0].Status);
    }

    /// <summary>Scopes resume IDs to generated compact GUIDs and forbids ambiguous goals.</summary>
    [Fact]
    public void Parse_ResumeOptions_AreStrict()
    {
        var parser = new CommandLineParser(_text);
        Assert.True(parser.Parse(["--plan", "prepare release"]).Succeeded);
        Assert.True(parser.Parse(["--plan", "--resume", Guid.NewGuid().ToString("N")]).Succeeded);
        Assert.False(parser.Parse(["--plan", "--resume", "..\\plan"]).Succeeded);
        Assert.False(parser.Parse(["--plan", "goal", "--resume", Guid.NewGuid().ToString("N")]).Succeeded);
    }

    /// <summary>Accepts plans of up to ten ordered steps.</summary>
    [Fact]
    public void Validate_TenSteps_IsSupported()
    {
        CreateStore().Validate(Plan(Enumerable.Range(0, 10).Select(index => "step-" + index).ToArray()));
    }

    /// <summary>Explains that deeper plans are not supported yet.</summary>
    [Fact]
    public void Validate_EleventhStep_ExplainsDepthLimit()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreateStore().Validate(Plan(Enumerable.Range(0, 11).Select(index => "step-" + index).ToArray())));

        Assert.Equal(_text.Text("Plan.Depth"), exception.Message);
    }

    /// <summary>Deletes only this test's random temporary state directory.</summary>
    public void Dispose()
    {
        var resolved = Path.GetFullPath(_directory);
        if (resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) && Directory.Exists(resolved))
        {
            Directory.Delete(resolved, recursive: true);
        }
    }

    /// <summary>Creates a workflow with deterministic command outcomes and passive collaborators.</summary>
    private Fixture CreateFixture(IEnumerable<CommandExecutionResult?> outcomes)
    {
        var commands = new StubCommands(outcomes);
        var store = CreateStore();
        var shell = DispatchProxy.Create<IConsoleShellView, RecordingShellProxy>();
        var recorder = (RecordingShellProxy)(object)shell;
        var workflow = new PlanWorkflow(null!, store, null!, commands, Proxy<IActivityAuditService>(),
            new StubPlanView(), shell, _text);
        return new Fixture(workflow, commands, recorder);
    }

    /// <summary>Creates an isolated durable state service with no shared application files.</summary>
    private PlanStore CreateStore() => new(
        new AppPaths(_directory, Path.Combine(_directory, "db"), Path.Combine(_directory, "logs"), Path.Combine(_directory, "logs", "x"), "prompt"),
        new SensitiveDataRedactor(), _text);

    /// <summary>Builds a simple pending plan with one verification per action.</summary>
    private ExecutionPlan Plan(params string[] commands) => new(1, Guid.NewGuid().ToString("N"), "goal", Path.GetFullPath(_directory),
        commands.Select((command, index) => new PlanStep($"Step {index + 1}", command, "verify-" + command, "present")).ToList());

    /// <summary>Builds one harmless typed command result for deterministic workflow tests.</summary>
    private static CommandExecutionResult Result(string command, int exitCode) => new(command, exitCode, "", "", false, false, 1);

    /// <summary>Creates a passive interface proxy for collaborators outside the state-machine assertion.</summary>
    private static T Proxy<T>() where T : class => DispatchProxy.Create<T, NoOpProxy>();

    private sealed record Fixture(PlanWorkflow Workflow, StubCommands Commands, RecordingShellProxy Shell);

    private sealed class StubPlanView : IPlanView
    {
        /// <summary>Suppresses rendering in state-machine tests.</summary>
        public void Render(ExecutionPlan plan) { }
        /// <summary>Accepts the plan-level guidance gate.</summary>
        public bool ConfirmStart() => true;
        /// <summary>Confirms the declared outcome after a successful verification result.</summary>
        public bool ConfirmOutcome(PlanStep step) => true;
    }

    private sealed class StubCommands(IEnumerable<CommandExecutionResult?> outcomes) : IAuthorizedCommandWorkflow
    {
        private readonly Queue<CommandExecutionResult?> _outcomes = new(outcomes);
        public List<string> Seen { get; } = [];
        public List<CommandExecutionMode> Modes { get; } = [];

        /// <summary>Returns deterministic outcomes while recording the exact command order.</summary>
        public Task<CommandExecutionResult?> RunForResultAsync(string sessionId, string command, AppSettings settings, CancellationToken cancellationToken,
            CommandExecutionMode executionMode = CommandExecutionMode.Confirm)
        {
            Seen.Add(command);
            Modes.Add(executionMode);
            return Task.FromResult(_outcomes.Dequeue());
        }

        /// <summary>The plan workflow does not use AI follow-up strings.</summary>
        public Task<string?> RunAsync(string sessionId, string command, AppSettings settings, CancellationToken cancellationToken,
            CommandExecutionMode executionMode = CommandExecutionMode.Confirm) =>
            throw new NotSupportedException();
    }

    public class RecordingShellProxy : NoOpProxy
    {
        public List<ShellRuntimeStatus> Statuses { get; } = [];

        /// <summary>Captures rendered session summaries while leaving the remaining plan shell output passive.</summary>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "RenderRuntimeStatus")
            {
                Statuses.Add((ShellRuntimeStatus)args![0]!);
                return null;
            }
            return base.Invoke(targetMethod, args);
        }
    }

    public class NoOpProxy : DispatchProxy
    {
        /// <summary>Returns completed tasks, default values, and no-op void calls for passive collaborators.</summary>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.ReturnType == typeof(void))
            {
                return null;
            }
            if (targetMethod?.ReturnType == typeof(Task))
            {
                return Task.CompletedTask;
            }
            if (targetMethod?.ReturnType == typeof(ConsoleRenderOptions))
            {
                return new ConsoleRenderOptions(true, true);
            }
            return targetMethod?.ReturnType.IsValueType == true ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }
}

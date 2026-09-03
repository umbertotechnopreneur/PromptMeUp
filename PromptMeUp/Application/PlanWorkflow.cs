// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed class PlanWorkflow(
    ArtifactAssistant assistant,
    PlanStore store,
    BoundedTextInput input,
    IAuthorizedCommandWorkflow commands,
    IActivityAuditService audit,
    IPlanView view,
    IConsoleShellView shell,
    ILocalizationService text)
{
    /// <summary>Creates or loads an exclusively locked plan and preserves its working directory on resume.</summary>
    public async Task<int> RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        ExecutionPlan plan;
        IDisposable lease;
        if (options.ResumeId is not null)
        {
            lease = store.Acquire(options.ResumeId);
            try
            {
                plan = await store.LoadAsync(options.ResumeId, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }
        else
        {
            var goal = input.Sanitize(options.Query!, settings.MaxMessageCharacters, fromArgument: true);
            var response = await assistant.SendAsync("plan-system", goal, settings, cancellationToken).ConfigureAwait(false);
            plan = store.Parse(response.Text, goal, Environment.CurrentDirectory);
            lease = store.Acquire(plan.Id);
        }
        using (lease)
        {
            if (!string.Equals(plan.Directory, Environment.CurrentDirectory, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                throw new InvalidOperationException(text.Text("Plan.Directory", plan.Directory));
            }
            await store.SaveAsync(plan, cancellationToken).ConfigureAwait(false);
            view.Render(plan);
            if (!view.ConfirmStart())
            {
                return 0;
            }
            return await RunStepsAsync(plan, options.ResumeId is not null, settings, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Runs pending commands once and verifies interrupted or completed steps before resuming.</summary>
    internal async Task<int> RunStepsAsync(ExecutionPlan plan, bool resuming, AppSettings settings, CancellationToken cancellationToken)
    {
        var session = Guid.NewGuid().ToString("N");
        await audit.StartSessionAsync(session, "plan", settings, new { plan.Id }, cancellationToken).ConfigureAwait(false);
        var status = "paused";
        try
        {
            for (var index = 0; index < plan.Steps.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = plan.Steps[index];
                if (step.Status == PlanStepStatus.Completed && !resuming)
                {
                    continue;
                }
                shell.RenderSectionTitle(step.Label);
                if (step.Status == PlanStepStatus.Pending)
                {
                    plan.Steps[index] = step with { Status = PlanStepStatus.Running };
                    await store.SaveAsync(plan, cancellationToken).ConfigureAwait(false);
                    var result = await commands.RunForResultAsync(session, step.Command, settings, cancellationToken).ConfigureAwait(false);
                    if (result is null)
                    {
                        plan.Steps[index] = step;
                        await store.SaveAsync(plan, cancellationToken).ConfigureAwait(false);
                        return 0;
                    }
                    if (result.TimedOut || result.ExitCode != 0)
                    {
                        await StopAsync(plan, index, cancellationToken).ConfigureAwait(false);
                        return 1;
                    }
                }
                else
                {
                    shell.RenderNotice(text.Text("Plan.Recheck"));
                }
                var check = await commands.RunForResultAsync(session, step.Verification, settings, cancellationToken).ConfigureAwait(false);
                if (check is null || check.TimedOut || check.OutputTruncated || check.ExitCode != 0 || !view.ConfirmOutcome(step))
                {
                    await StopAsync(plan, index, cancellationToken).ConfigureAwait(false);
                    return check is null ? 0 : 1;
                }
                plan.Steps[index] = step with { Status = PlanStepStatus.Completed };
                await store.SaveAsync(plan, cancellationToken).ConfigureAwait(false);
                view.Render(plan);
            }
            status = "completed";
            shell.RenderSuccess(text.Text("Plan.Completed"));
            return 0;
        }
        finally
        {
            await audit.CloseSessionAsync(session, status, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Stops on uncertain evidence while retaining later checkpoints so completed commands cannot be replayed.</summary>
    private async Task StopAsync(ExecutionPlan plan, int index, CancellationToken cancellationToken)
    {
        plan.Steps[index] = plan.Steps[index] with { Status = PlanStepStatus.NeedsReview };
        await store.SaveAsync(plan, cancellationToken).ConfigureAwait(false);
        shell.RenderWarning(text.Text("Plan.Stopped"));
        view.Render(plan);
    }
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed class ArtifactAssistant(
    IOpenAiService openAi,
    IActivityAuditService audit,
    BoundedTextInput input,
    IConsoleShellView shell,
    ILocalizationService text,
    ArtifactLimits? limits = null)
{
    /// <summary>Creates one bounded artifact request with an audited lifecycle, leaving visible summaries to the completed workflow.</summary>
    public async Task<AiResponse> SendAsync(
        string promptId,
        string request,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        request = promptId == "script-system"
            ? input.SanitizeUtf8(request, (limits ?? ArtifactLimits.Default).ScriptRequestBytes(settings.MaxMessageCharacters))
            : input.Sanitize(request, settings.MaxMessageCharacters);
        await using var session = await AuditSessionScope.StartAsync(
            audit, promptId, settings, new { invocation = promptId }, AuditSessionOutcome.Failed, cancellationToken).ConfigureAwait(false);
        var response = await shell.RunWithStatusAsync(text.Text("Status.Thinking"),
            () => openAi.SendAsync(promptId, session.Id, [new ChatMessage("user", request)], settings, settings.Language, cancellationToken)).ConfigureAwait(false);
        session.Outcome = AuditSessionOutcome.Completed;
        return response;
    }

    /// <summary>Shows a summary only after a caller has rendered an AI result that remains inside an active workflow.</summary>
    public bool RenderSummaryAfterVisibleTurn(AiResponse response, AppSettings settings, decimal? runningCost = null)
    {
        var snapshot = AiConversationWorkflow.CreateTurnSnapshot(response, settings, runningCost ?? response.EstimatedCostUsd ?? 0m);
        if (SessionSummaryPolicy.ShouldRender(settings, SessionSummaryTiming.AfterVisibleTurn)
            || AiConversationWorkflow.IsContextWarning(snapshot))
        {
            shell.RenderRuntimeStatus(snapshot);
            return true;
        }
        return false;
    }

    /// <summary>Shows the final summary after the caller has rendered the completed workflow outcome.</summary>
    public void RenderSummaryAtEnd(
        AiResponse response,
        AppSettings settings,
        bool summaryAlreadyRendered = false,
        decimal? runningCost = null)
    {
        if (!summaryAlreadyRendered && SessionSummaryPolicy.ShouldRender(settings, SessionSummaryTiming.WorkflowEnd))
        {
            RenderSummary(response, settings, runningCost);
        }
    }

    /// <summary>Builds and renders one provider-confirmed usage snapshot without inferring undisclosed usage.</summary>
    private void RenderSummary(AiResponse response, AppSettings settings, decimal? runningCost)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(settings);
        shell.RenderRuntimeStatus(AiConversationWorkflow.CreateTurnSnapshot(
            response, settings, runningCost ?? response.EstimatedCostUsd ?? 0m));
    }
}

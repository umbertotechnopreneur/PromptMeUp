// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed partial class AiConversationWorkflow
{
    /// <summary>Shows confirmed usage when the active session policy allows it.</summary>
    private void RenderTurnSnapshot(AiResponse response, AppSettings settings, decimal runningCost, SessionSummaryTiming timing)
    {
        if (SessionSummaryPolicy.ShouldRender(settings, timing))
        {
            _shell.RenderRuntimeStatus(CreateTurnSnapshot(response, settings, runningCost));
        }
    }

    /// <summary>Builds provider-confirmed context and cost values for one completed turn.</summary>
    internal static ShellRuntimeStatus CreateTurnSnapshot(AiResponse response, AppSettings settings, decimal runningCost)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(settings);
        decimal? promptCost = response.CostBreakdown is null
            ? null
            : response.CostBreakdown.InputUsd + response.CostBreakdown.CachedInputUsd + response.CostBreakdown.CacheWriteUsd;
        return new ShellRuntimeStatus(
            "OpenAI",
            response.Model,
            settings.ReasoningEffort,
            promptCost,
            response.CostBreakdown?.OutputUsd,
            runningCost,
            response.ContextUsage.InputTokens + response.ContextUsage.OutputTokens,
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.ContextUsage.ContextWindowTokens,
            false,
            response.Usage.CachedInputTokens,
            response.Usage.CacheWriteTokens)
        {
            ActiveContextTokens = response.ContextUsage.InputTokens,
            ContextBudgetTokens = response.ContextUsage.InputBudgetTokens,
            SystemInstructionTokens = response.ContextUsage.SystemInstructionTokens,
            GuideTokens = response.ContextUsage.GuideTokens,
            UserMessageTokens = response.ContextUsage.UserMessageTokens,
            ToolOutputTokens = response.ContextUsage.ToolOutputTokens,
            AssistantMessageTokens = response.ContextUsage.AssistantMessageTokens,
            HasContextBreakdown = response.ContextUsage.InputBudgetTokens > 0,
            TurnCostUsd = response.RequestCount > 1 ? response.TurnCostUsd : response.EstimatedCostUsd,
            HasTurnCost = true
        };
    }

    /// <summary>Applies visibility policy to a fresh session snapshot.</summary>
    private async Task RenderActiveSnapshotAsync(
        string sessionId,
        ConversationState memory,
        AppSettings settings,
        CancellationToken cancellationToken,
        bool force = false)
    {
        var snapshot = await CreateSessionSnapshotAsync(sessionId, memory, settings, cancellationToken).ConfigureAwait(false);
        if (memory.ShowSessionSummaryDuringWork || force || IsContextWarning(snapshot))
        {
            _shell.RenderRuntimeStatus(snapshot);
            memory.SummaryRenderedSinceLastResult = true;
        }
    }

    /// <summary>Combines local context with the session ledger, including failed calls and risk reviews.</summary>
    private async Task<ShellRuntimeStatus> CreateSessionSnapshotAsync(
        string sessionId, ConversationState memory, AppSettings settings, CancellationToken cancellationToken)
    {
        var context = await _openAi.EstimateContextAsync(
            memory.PromptId, CombineMessages(memory.Envelope, memory.Memory.Snapshot().Messages), settings, _text.Language,
            cancellationToken, memory.Guide).ConfigureAwait(false);
        var accounting = await _database.GetSessionAccountingAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var status = memory.LastResponse is null
            ? ShellRuntimeStatus.FromSettings(settings)
            : CreateTurnSnapshot(memory.LastResponse, settings, accounting.EstimatedCostUsd ?? 0m);
        return status with
        {
            RunningCostUsd = accounting.EstimatedCostUsd ?? 0m,
            SessionCostKnown = accounting.EstimatedCostUsd.HasValue,
            ActiveContextTokens = context.InputTokens,
            ContextWindowTokens = context.ContextWindowTokens,
            ContextBudgetTokens = context.InputBudgetTokens,
            SystemInstructionTokens = context.SystemInstructionTokens,
            GuideTokens = context.GuideTokens,
            UserMessageTokens = context.UserMessageTokens,
            ToolOutputTokens = context.ToolOutputTokens,
            AssistantMessageTokens = context.AssistantMessageTokens,
            HasContextBreakdown = true,
            MemoryTokens = memory.Envelope.Tokens,
            MemoryCount = memory.Envelope.Count,
            SessionInputTokens = accounting.Usage.InputTokens,
            SessionOutputTokens = accounting.Usage.OutputTokens,
            HasSessionUsage = true,
            ConversationMode = memory.PromptId switch
            {
                "diagnose-system" => ConversationDisplayMode.Diagnose,
                "explain-system" => ConversationDisplayMode.Explain,
                _ => ConversationDisplayMode.Chat
            }
        };
    }

    /// <summary>Forces the summary into view at eighty percent of the operating context budget.</summary>
    internal static bool IsContextWarning(ShellRuntimeStatus status) => status.ContextBudgetTokens > 0
        && status.ActiveContextTokens is { } used && (decimal)used * 100 >= (decimal)status.ContextBudgetTokens * 80;

    /// <summary>Reads final totals with a short deadline without hiding the original flow failure.</summary>
    private async Task RenderFinalSnapshotAsync(string sessionId, ConversationState memory, AppSettings settings)
    {
        if (memory.SummaryRenderedSinceLastResult)
        {
            return;
        }
        ShellRuntimeStatus snapshot;
        using var finalRead = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            snapshot = await CreateSessionSnapshotAsync(sessionId, memory, settings, finalRead.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Final session snapshot unavailable. ErrorType={ErrorType}", exception.GetType().Name);
            _shell.RenderWarning(_text.Text("Chat.SessionSummaryUnavailable"));
            snapshot = ShellRuntimeStatus.FromSettings(settings) with { SessionCostKnown = false };
        }
        _shell.RenderRuntimeStatus(snapshot);
    }
}

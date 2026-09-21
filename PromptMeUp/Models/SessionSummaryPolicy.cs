// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Identifies when a session summary could be displayed within an application workflow.</summary>
internal enum SessionSummaryTiming
{
    AfterVisibleTurn,
    WorkflowEnd
}

/// <summary>Applies the persisted visibility preference consistently without suppressing an end-of-workflow summary.</summary>
internal static class SessionSummaryPolicy
{
    /// <summary>Determines whether the configured policy permits a summary at the requested workflow point.</summary>
    internal static bool ShouldRender(AppSettings settings, SessionSummaryTiming timing)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return timing is SessionSummaryTiming.WorkflowEnd || settings.ShowSessionSummaryDuringWork;
    }
}

// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using Microsoft.Extensions.Logging;
using PromptMeUp.Services;

namespace PromptMeUp.Application;

/// <summary>Shares non-session audit recording while logging secondary persistence failures.</summary>
public sealed class ApplicationActivityRecorder(
    IActivityAuditService audit,
    ILogger<ApplicationActivityRecorder> logger)
{
    /// <summary>Records a non-session activity while allowing diagnostics to continue if auditing itself fails.</summary>
    public async Task TryRecordAsync(string activity, string outcome, string? sessionId, object payload)
    {
        try
        {
            await audit.RecordAsync(activity, outcome, sessionId, payload, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError("Activity audit persistence failed. Activity={Activity}, Outcome={Outcome}, ErrorType={ErrorType}", activity, outcome, exception.GetType().Name);
        }
    }
}

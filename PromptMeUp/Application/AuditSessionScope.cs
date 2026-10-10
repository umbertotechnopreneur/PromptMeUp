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


using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Application;

internal enum AuditSessionOutcome
{
    Failed,
    Cancelled,
    Paused,
    Completed
}

internal sealed class AuditSessionScope : IAsyncDisposable
{
    private readonly IActivityAuditService _audit;

    /// <summary>Retains an opened audit session and the workflow's explicit default outcome.</summary>
    private AuditSessionScope(IActivityAuditService audit, string id, AuditSessionOutcome outcome)
    {
        _audit = audit;
        Id = id;
        Outcome = outcome;
    }

    public string Id { get; }

    public AuditSessionOutcome Outcome { get; set; }

    /// <summary>Opens one audit session before returning the scope responsible for closing it.</summary>
    public static async Task<AuditSessionScope> StartAsync(
        IActivityAuditService audit,
        string kind,
        AppSettings settings,
        object metadata,
        AuditSessionOutcome initialOutcome,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audit);
        var session = new AuditSessionScope(audit, Guid.NewGuid().ToString("N"), initialOutcome);
        await audit.StartSessionAsync(session.Id, kind, settings, metadata, cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <summary>Closes the session through the audit service even after the workflow's cancellation token is cancelled.</summary>
    public async ValueTask DisposeAsync()
    {
        var status = Outcome switch
        {
            AuditSessionOutcome.Failed => "failed",
            AuditSessionOutcome.Cancelled => "cancelled",
            AuditSessionOutcome.Paused => "paused",
            AuditSessionOutcome.Completed => "completed",
            _ => throw new InvalidOperationException("The audit session outcome is unsupported.")
        };
        await _audit.CloseSessionAsync(Id, status, CancellationToken.None).ConfigureAwait(false);
    }
}

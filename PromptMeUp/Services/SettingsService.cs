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

namespace PromptMeUp.Services;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}

public sealed class SettingsService(IDatabaseService database) : ISettingsService
{
    /// <summary>Loads the current application settings through the database boundary.</summary>
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => database.LoadSettingsAsync(cancellationToken);

    /// <summary>Persists a complete validated application settings replacement.</summary>
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) => database.SaveSettingsAsync(settings, cancellationToken);
}

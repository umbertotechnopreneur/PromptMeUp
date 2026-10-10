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


namespace PromptMeUp.Models;

public enum PortablePathAction
{
    Install,
    Remove,
    Status
}

public sealed record PortablePathPlan(
    PortablePathAction Action,
    string ExecutableDirectory,
    string PersistenceTarget,
    string Preview,
    bool IsPresent,
    bool RequiresChange);

public sealed record PortablePathResult(
    PortablePathAction Action,
    string ExecutableDirectory,
    string PersistenceTarget,
    bool Changed,
    bool IsPresent);

public sealed record SecretStoreResult(string Guidance);

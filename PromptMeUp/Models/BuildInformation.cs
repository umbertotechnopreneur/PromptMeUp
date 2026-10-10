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


using System.Globalization;

namespace PromptMeUp.Models;

/// <summary>Describes the immutable application version, compiler identity, and source commit.</summary>
public sealed record BuildInformation(string Version, string MachineName, DateTimeOffset BuiltAtLocal, string GitCommit)
{
    /// <summary>Formats the compilation instant as an ISO 8601 timestamp with its local UTC offset.</summary>
    public string BuildTimestamp => BuiltAtLocal.ToString("O", CultureInfo.InvariantCulture);
}

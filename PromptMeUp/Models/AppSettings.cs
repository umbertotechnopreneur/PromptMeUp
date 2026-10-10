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

public sealed record AppSettings(
    bool SetupCompleted,
    string Language,
    bool AiEnabled,
    string Model,
    string ReasoningEffort,
    string OutputDetail,
    string CustomInstruction,
    bool IncludeWindowsLocation,
    bool ReviewCommandsWithAi,
    bool PromptCachingEnabled,
    int MaxConversationTurns,
    int MaxMessageCharacters,
    int MaxContextPercent,
    int MaxCommandOutputCharacters,
    int CommandTimeoutSeconds,
    string Endpoint,
    string ApiKeyVariable,
    string AdminKeyVariable,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Indicates that setup has never been saved successfully on this installation.</summary>
    public bool IsFirstRun => !SetupCompleted;

    public int ContextTokenBudget { get; init; } = 16_000;

    public string Theme { get; init; } = "cyan";

    public string PreferredName { get; init; } = string.Empty;

    public bool DirectModeEnabled { get; init; } = true;

    public bool ShowSessionSummaryDuringWork { get; init; }

    public ScriptLanguage ScriptLanguage { get; init; } = ScriptLanguage.PowerShell;

    public const string DefaultEndpoint = "https://api.openai.com/v1/responses";
    public const string DefaultApiKeyVariable = "OPENAI_API_KEY";
    public const string DefaultAdminKeyVariable = "OPENAI_ADMIN_KEY";

    public static AppSettings Default => new(
        SetupCompleted: false,
        Language: "en",
        AiEnabled: true,
        Model: "gpt-5.6-terra",
        ReasoningEffort: "medium",
        OutputDetail: "balanced",
        CustomInstruction: string.Empty,
        IncludeWindowsLocation: false,
        ReviewCommandsWithAi: true,
        PromptCachingEnabled: true,
        MaxConversationTurns: 12,
        MaxMessageCharacters: 16_000,
        MaxContextPercent: 70,
        MaxCommandOutputCharacters: 12_000,
        CommandTimeoutSeconds: 30,
        Endpoint: DefaultEndpoint,
        ApiKeyVariable: DefaultApiKeyVariable,
        AdminKeyVariable: DefaultAdminKeyVariable,
        UpdatedAt: DateTimeOffset.UtcNow);
}

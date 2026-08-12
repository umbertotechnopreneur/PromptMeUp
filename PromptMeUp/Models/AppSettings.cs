// SPDX-License-Identifier: MIT

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

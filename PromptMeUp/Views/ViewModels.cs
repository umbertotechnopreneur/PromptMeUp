// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Views;

public sealed record SetupViewState(
    AppSettings Settings,
    bool ApiKeyConfigured,
    bool AdminKeyConfigured)
{
    public SettingsSection InitialSection { get; init; } = SettingsSection.General;

    public bool ContextBudgetOverridden { get; init; }

    public CostOverview? Costs { get; init; }

    public SettingsFeatureOverview? FeatureOverview { get; init; }

    public Action? OpenMemories { get; init; }

    public bool SaveSucceeded { get; init; }
}

public enum SettingsSection
{
    General,
    Ai,
    Credentials,
    Context,
    Commands,
    Personalization,
    Theme,
    Skills,
    Learning,
    Memories,
    Privacy
}

public sealed record SetupSubmission(
    AppSettings Settings,
    string? ApiKey,
    string? AdminKey,
    bool TestConnection)
{
    public SettingsFeatureChanges? Features { get; init; }

    public bool KeepOpen { get; init; }

    public SettingsSection SelectedSection { get; init; } = SettingsSection.General;
}

public sealed record ConsoleRenderOptions(bool NoAnimation, bool NoEmoji, bool SuppressFooter = false);

public sealed record ShellRuntimeStatus(
    string Provider,
    string Model,
    string ThinkingLevel,
    decimal? PromptCostUsd,
    decimal? ResponseCostUsd,
    decimal RunningCostUsd,
    long ContextTotalTokens,
    long InputTokens,
    long OutputTokens,
    long ContextWindowTokens,
    bool ContextIsEstimated,
    long CachedInputTokens,
    long CacheWriteTokens)
{
    public long? ActiveContextTokens { get; init; }

    public long ContextBudgetTokens { get; init; }

    public long SystemInstructionTokens { get; init; }

    public long GuideTokens { get; init; }

    public long UserMessageTokens { get; init; }

    public long AssistantMessageTokens { get; init; }

    public bool HasContextBreakdown { get; init; }

    public decimal? TurnCostUsd { get; init; }

    public bool HasTurnCost { get; init; }

    public bool SessionCostKnown { get; init; } = true;

    public long MemoryTokens { get; init; }

    public int MemoryCount { get; init; }

    public long SessionInputTokens { get; init; }

    public long SessionOutputTokens { get; init; }

    public bool HasSessionUsage { get; init; }

    /// <summary>Creates an idle status-bar snapshot from persisted settings.</summary>
    public static ShellRuntimeStatus FromSettings(AppSettings? settings) => new(
        settings?.AiEnabled == false ? "local" : "OpenAI",
        settings?.Model ?? "—",
        settings?.ReasoningEffort ?? "—",
        0m,
        0m,
        0m,
        0,
        0,
        0,
        settings is null ? 0 : AiModelCatalog.Resolve(settings.Model).ContextWindowTokens,
        true,
        0,
        0);
}

public enum CommandSuggestionAction
{
    DoNotExecute,
    StartChat,
    SelectCommand
}

public sealed record CommandSuggestionDecision(
    CommandSuggestionAction Action,
    SuggestedCommand? SuggestedCommand);

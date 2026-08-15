// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Views;

public sealed record SetupViewState(
    AppSettings Settings,
    bool ApiKeyConfigured,
    bool AdminKeyConfigured);

public sealed record SetupSubmission(
    AppSettings Settings,
    string? ApiKey,
    string? AdminKey,
    bool TestConnection);

public sealed record ConsoleRenderOptions(bool NoAnimation, bool NoEmoji);

public sealed record ShellRuntimeStatus(
    string Provider,
    string Model,
    string ThinkingLevel,
    decimal? PromptCostUsd,
    decimal? ResponseCostUsd,
    decimal RunningCostUsd,
    long ContextInputTokens,
    long ContextWindowTokens,
    bool ContextIsEstimated,
    long CachedInputTokens,
    long CacheWriteTokens)
{
    /// <summary>Creates an idle status-bar snapshot from persisted settings.</summary>
    public static ShellRuntimeStatus FromSettings(AppSettings? settings) => new(
        settings?.AiEnabled == false ? "local" : "OpenAI",
        settings?.Model ?? "—",
        settings?.ReasoningEffort ?? "—",
        0m,
        0m,
        0m,
        0,
        settings is null ? 0 : AiModelCatalog.Resolve(settings.Model).ContextWindowTokens,
        true,
        0,
        0);
}

public enum MainMenuAction
{
    Query,
    Chat,
    Costs,
    Status,
    Setup,
    TestAi,
    Path,
    InstallFont,
    ThirdParty,
    Exit
}

// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public enum AppCommand
{
    Main,
    Help,
    Version,
    Setup,
    Status,
    Query,
    Diagnose,
    Script,
    Plan,
    Preview,
    Recipes,
    Chat,
    TestAi,
    Costs,
    ThirdParty,
    Where,
    InstallFont,
    Path
}

public sealed record CommandLineOptions(
    AppCommand Command,
    string? Query,
    string? Language,
    bool NoAnimation,
    bool NoEmoji,
    bool Yes,
    bool DryRun,
    string? PathAction,
    string? InputFile = null,
    string? OutputFile = null,
    string? ResumeId = null,
    string? PreviewAction = null,
    string? Prefix = null,
    string? Pattern = null,
    string? RecipeAction = null,
    string? RecipeName = null,
    string? SourcePlan = null);

public sealed record CommandLineParseResult(CommandLineOptions? Options, string? Error)
{
    public bool Succeeded => Options is not null && Error is null;
}

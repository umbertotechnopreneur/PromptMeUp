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
    string? OutputFile = null);

public sealed record CommandLineParseResult(CommandLineOptions? Options, string? Error)
{
    public bool Succeeded => Options is not null && Error is null;
}

// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public enum AppCommand
{
    Main,
    Help,
    Version,
    Setup,
    AiSettings,
    Theme,
    Status,
    Query,
    Direct,
    Diagnose,
    Script,
    Plan,
    Preview,
    Chat,
    Memories,
    Skills,
    Learning,
    Proposals,
    Dream,
    Heartbeat,
    Remember,
    Forget,
    TestAi,
    Costs,
    ThirdParty,
    Where,
    InstallFont,
    Path,
    Reset,
    Lenna,
    About
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
    string? Pattern = null);

public sealed record CommandLineParseResult(CommandLineOptions? Options, string? Error)
{
    public bool Succeeded => Options is not null && Error is null;
}

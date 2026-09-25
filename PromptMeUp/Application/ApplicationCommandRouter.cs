// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Application;

public interface IApplicationCommandRouter
{
    Task<int> DispatchAsync(CommandLineOptions options, AppSettings settings, int promptCount, CancellationToken cancellationToken);
}

/// <summary>Routes initialized commands to the handler that owns their feature area.</summary>
internal sealed class ApplicationCommandRouter(
    AiCommandHandler ai,
    MemoryCommandHandler memory,
    InformationCommandHandler information,
    SetupCommandHandler setup) : IApplicationCommandRouter
{
    /// <summary>Dispatches one command after common startup and first-run checks.</summary>
    public Task<int> DispatchAsync(CommandLineOptions options, AppSettings settings, int promptCount, CancellationToken cancellationToken) =>
        options.Command switch
        {
            AppCommand.Preview or AppCommand.Plan or AppCommand.Script or AppCommand.Diagnose or
                AppCommand.Query or AppCommand.Direct or AppCommand.Chat or AppCommand.TestAi =>
                ai.RunAsync(options, settings, cancellationToken),
            AppCommand.Skills or AppCommand.Learning or AppCommand.Dream or AppCommand.Heartbeat or
                AppCommand.Remember or AppCommand.Forget or AppCommand.Memories or AppCommand.Proposals =>
                memory.RunAsync(options, settings, cancellationToken),
            AppCommand.Lenna or AppCommand.About or AppCommand.Help or AppCommand.Version or
                AppCommand.Status or AppCommand.Costs or AppCommand.ThirdParty =>
                information.RunAsync(options, settings, promptCount, cancellationToken),
            AppCommand.Setup or AppCommand.AiSettings or AppCommand.Theme or AppCommand.Where or
                AppCommand.InstallFont or AppCommand.Path =>
                setup.RunAsync(options, settings, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(options), "Unsupported application command.")
        };
}

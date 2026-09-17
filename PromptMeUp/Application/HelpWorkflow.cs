// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Opens the command reference and coordinates local memory navigation without AI services.</summary>
public sealed class HelpWorkflow(IHelpView view, IConsoleShellView shell,
    MemoryManagerWorkflow? memories = null, IDatabaseService? database = null)
{
    /// <summary>Applies invocation preferences and displays adaptive fullscreen or scrolling help.</summary>
    public int Run(CommandLineOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Command != AppCommand.Help)
        {
            throw new ArgumentException("The help workflow only accepts the Help command.", nameof(options));
        }
        shell.Configure(new ConsoleRenderOptions(options.NoAnimation, options.NoEmoji, SuppressFooter: true));
        cancellationToken.ThrowIfCancellationRequested();
        view.Render(() => OpenMemoriesAsync(cancellationToken).GetAwaiter().GetResult());
        return 0;
    }

    /// <summary>Initializes local storage only when the user opens the memory manager from help.</summary>
    private async Task OpenMemoriesAsync(CancellationToken cancellationToken)
    {
        var storage = database ?? throw new InvalidOperationException("Memory storage is unavailable.");
        var workflow = memories ?? throw new InvalidOperationException("The memory manager is unavailable.");
        await storage.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await workflow.RunAsync(cancellationToken).ConfigureAwait(false);
    }
}

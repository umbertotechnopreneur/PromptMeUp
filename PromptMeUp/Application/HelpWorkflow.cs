// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Opens the command reference without resolving application settings or AI services.</summary>
public sealed class HelpWorkflow(IHelpView view, IConsoleShellView shell)
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
        view.Render();
        return 0;
    }
}

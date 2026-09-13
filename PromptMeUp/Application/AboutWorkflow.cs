// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Displays product information without resolving settings, storage, or AI services.</summary>
public sealed class AboutWorkflow(IAboutView view, IConsoleShellView shell, ILocalizationService text)
{
    /// <summary>Applies invocation preferences and displays the adaptive About screen.</summary>
    public int Run(CommandLineOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Command != AppCommand.About)
        {
            throw new ArgumentException("The About workflow only accepts the About command.", nameof(options));
        }
        if (options.Language is not null)
        {
            text.SetLanguage(options.Language);
        }
        shell.Configure(new ConsoleRenderOptions(options.NoAnimation, options.NoEmoji, SuppressFooter: true));
        cancellationToken.ThrowIfCancellationRequested();
        view.Render();
        return 0;
    }
}

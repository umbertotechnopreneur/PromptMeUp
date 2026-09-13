// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Displays the bundled portrait independently of settings, storage, and AI configuration.</summary>
public sealed class LennaWorkflow(
    ILennaImageService imageService,
    ILennaView view,
    IConsoleShellView shell,
    ILogger<LennaWorkflow> logger)
{
    /// <summary>Runs the local image command with a dedicated viewport and localized failure output.</summary>
    public int Run(CommandLineOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Command != AppCommand.Lenna)
        {
            throw new ArgumentException("The image workflow only accepts the Lenna command.", nameof(options));
        }
        shell.Configure(new ConsoleRenderOptions(options.NoAnimation, options.NoEmoji, SuppressFooter: true));
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            view.Render(imageService.Load());
            return 0;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning("The Lenna portrait could not be displayed. ErrorType={ErrorType}", exception.GetType().Name);
            shell.RenderError(exception.Message);
            return 1;
        }
    }
}

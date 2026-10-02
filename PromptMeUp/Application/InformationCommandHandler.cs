// SPDX-License-Identifier: MIT

using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Renders local help, version, status, and cost information.</summary>
internal sealed class InformationCommandHandler(
    LennaWorkflow lenna,
    AboutWorkflow about,
    HelpWorkflow help,
    IStatusView statusView,
    ICostsView costsView,
    IThirdPartyView thirdPartyView,
    IPricingService pricing,
    IEnvironmentSecretService secrets,
    ApplicationActivityRecorder activity,
    IConsoleShellView shell,
    AppPaths paths,
    ArtifactLimits artifactLimits)
{
    /// <summary>Runs one informational command and records status inspection when requested.</summary>
    internal async Task<int> RunAsync(CommandLineOptions options, AppSettings settings, int promptCount, CancellationToken cancellationToken)
    {
        switch (options.Command)
        {
            case AppCommand.Lenna:
                return lenna.Run(options, cancellationToken);
            case AppCommand.About:
                return about.Run(options, cancellationToken);
            case AppCommand.Help:
                return help.Run(options, cancellationToken);
            case AppCommand.Version:
                shell.RenderVersion(
                    BuildInformationReader.Read(),
                    Environment.Version.ToString(),
                    System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);
                return 0;
            case AppCommand.Status:
                await RunStatusAsync(settings, promptCount, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Costs:
                costsView.Render(await pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false));
                return 0;
            case AppCommand.ThirdParty:
                thirdPartyView.Render();
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), "Unsupported informational command.");
        }
    }

    /// <summary>Builds and renders current local status, then records the completed inspection.</summary>
    private async Task RunStatusAsync(AppSettings settings, int promptCount, CancellationToken cancellationToken)
    {
        var status = new AppStatus(
            settings,
            secrets.IsConfigured(settings.ApiKeyVariable),
            secrets.IsConfigured(settings.AdminKeyVariable),
            (await pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false)).LastPricingSync,
            paths.DatabasePath,
            paths.LogsDirectory,
            paths.PromptDirectory,
            promptCount)
        { ArtifactLimits = artifactLimits };
        statusView.Render(status);
        await activity.TryRecordAsync("status", "completed", null, new { promptCount }).ConfigureAwait(false);
    }
}

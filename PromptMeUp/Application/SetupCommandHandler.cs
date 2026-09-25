// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Runs setup screens and installation inspection or changes.</summary>
internal sealed class SetupCommandHandler(
    SetupWorkflow setup,
    InstallationWorkflow installation,
    ILocalizationService text)
{
    /// <summary>Runs one setup or installation command with the required terminal authorization.</summary>
    internal async Task<int> RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        switch (options.Command)
        {
            case AppCommand.Setup:
                return await RunSetupAsync(settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.AiSettings:
                return await RunSetupAsync(settings, cancellationToken, SettingsSection.Ai).ConfigureAwait(false);
            case AppCommand.Theme:
                return await RunSetupAsync(settings, cancellationToken, SettingsSection.Theme).ConfigureAwait(false);
            case AppCommand.Where:
                return await installation.RunWhereAsync(CommandPreconditions.IsInteractive, cancellationToken).ConfigureAwait(false);
            case AppCommand.InstallFont:
                CommandPreconditions.RequireInteractiveUnlessPreauthorized(text, options.Yes || options.DryRun);
                return await installation.RunFontAsync(options, cancellationToken).ConfigureAwait(false);
            case AppCommand.Path:
                CommandPreconditions.RequireInteractiveUnlessPreauthorized(text, options.Yes || options.PathAction == "status");
                return await installation.RunPathAsync(options, cancellationToken).ConfigureAwait(false);
            default:
                throw new ArgumentOutOfRangeException(nameof(options), "Unsupported setup command.");
        }
    }

    /// <summary>Opens the shared settings screen at the requested section in an interactive terminal.</summary>
    private async Task<int> RunSetupAsync(
        AppSettings current,
        CancellationToken cancellationToken,
        SettingsSection initialSection = SettingsSection.General)
    {
        CommandPreconditions.RequireInteractive(text);
        return await setup.RunAsync(current, cancellationToken, initialSection).ConfigureAwait(false);
    }
}

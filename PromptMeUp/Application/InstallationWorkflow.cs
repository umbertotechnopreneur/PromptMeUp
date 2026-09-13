// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Coordinates reviewed PATH, executable-location, and optional font operations.</summary>
public sealed class InstallationWorkflow(
    IPortablePathService pathService,
    IExecutableLocationService executableLocation,
    INerdFontInstallerService fontInstaller,
    ApplicationActivityRecorder activity,
    IPortablePathView pathView,
    IExecutableLocationView executableLocationView,
    INerdFontView fontView,
    IConsoleShellView shell,
    ILocalizationService text)
{
    /// <summary>Runs the persistent PATH status or mutation flow after displaying its exact target.</summary>
    public async Task<int> RunPathAsync(CommandLineOptions options, CancellationToken cancellationToken)
    {
        var action = options.PathAction is null
            ? pathView.SelectAction()
            : ParsePathAction(options.PathAction);
        var plan = pathService.CreatePlan(action);
        var confirmed = pathView.PreviewAndConfirm(plan, options.Yes);
        if (action != PortablePathAction.Status && plan.RequiresChange && !(confirmed || options.Yes))
        {
            await activity.TryRecordAsync("path", "cancelled", null, new { action, plan.ExecutableDirectory }).ConfigureAwait(false);
            return 0;
        }

        var result = await pathService.ApplyAsync(plan, cancellationToken).ConfigureAwait(false);
        pathView.RenderResult(result);
        await activity.TryRecordAsync("path", "completed", null, new { action, result.Changed, result.IsPresent, result.ExecutableDirectory }).ConfigureAwait(false);
        return 0;
    }

    /// <summary>Shows the running hm location and optionally opens its containing folder after exact authorization.</summary>
    public async Task<int> RunWhereAsync(bool isInteractive, CancellationToken cancellationToken)
    {
        var location = executableLocation.Resolve();
        var action = executableLocationView.RenderAndSelect(location, isInteractive);
        if (action == ExecutableLocationAction.DoNothing)
        {
            await activity.TryRecordAsync("where", "cancelled", null, new { location.ExecutablePath }).ConfigureAwait(false);
            return 0;
        }

        if (action == ExecutableLocationAction.OpenContainingFolder)
        {
            if (!executableLocationView.ConfirmOpen(location))
            {
                executableLocationView.RenderResult(location, ExecutableLocationAction.ShowChangeDirectoryCommand);
                await activity.TryRecordAsync("where", "cancelled", null, new { location.ExecutablePath }).ConfigureAwait(false);
                return 0;
            }

            cancellationToken.ThrowIfCancellationRequested();
            executableLocation.OpenContainingFolder(location);
        }

        executableLocationView.RenderResult(location, action);
        await activity.TryRecordAsync("where", "completed", null, new { action, location.ExecutablePath }).ConfigureAwait(false);
        return 0;
    }

    /// <summary>Runs the optional Nerd Font helper only after preview and authorization.</summary>
    public async Task<int> RunFontAsync(CommandLineOptions options, CancellationToken cancellationToken)
    {
        if (!fontView.PreviewAndConfirm(options.DryRun, options.Yes || options.DryRun))
        {
            await activity.TryRecordAsync("font", "cancelled", null, new { options.DryRun }).ConfigureAwait(false);
            return 0;
        }

        var result = await shell.RunWithStatusAsync(
            text.Text("Font.Progress"),
            () => fontInstaller.InstallAsync(options.DryRun, cancellationToken)).ConfigureAwait(false);
        fontView.RenderResult(result);
        await activity.TryRecordAsync("font", "completed", null, new { result.FontName, result.Changed, result.DryRun }).ConfigureAwait(false);
        return 0;
    }

    /// <summary>Maps a CLI PATH verb to the portable service action.</summary>
    private static PortablePathAction ParsePathAction(string action) => action switch
    {
        "install" => PortablePathAction.Install,
        "remove" => PortablePathAction.Remove,
        _ => PortablePathAction.Status
    };
}

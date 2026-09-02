// SPDX-License-Identifier: MIT

using System.Reflection;
using Microsoft.Extensions.Logging;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public interface IPromptMeUpApplication
{
    Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken);
}

public sealed class PromptMeUpApplication : IPromptMeUpApplication
{
    private readonly ICommandLineParser _parser;
    private readonly IDatabaseService _database;
    private readonly ISettingsService _settings;
    private readonly IEnvironmentSecretService _secrets;
    private readonly IPromptCatalogService _prompts;
    private readonly IPricingService _pricing;
    private readonly IAiConversationWorkflow _conversationWorkflow;
    private readonly IActivityAuditService _audit;
    private readonly IPortablePathService _pathService;
    private readonly IExecutableLocationService _executableLocation;
    private readonly INerdFontInstallerService _fontInstaller;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly ISetupView _setupView;
    private readonly IStatusView _statusView;
    private readonly ICostsView _costsView;
    private readonly IHelpView _helpView;
    private readonly IMainMenuView _mainMenuView;
    private readonly IThirdPartyView _thirdPartyView;
    private readonly IPortablePathView _pathView;
    private readonly IExecutableLocationView _executableLocationView;
    private readonly INerdFontView _fontView;
    private readonly AppPaths _paths;
    private readonly ILogger<PromptMeUpApplication> _logger;

    /// <summary>Creates the application orchestrator while keeping business services independent from Spectre views.</summary>
    public PromptMeUpApplication(
        ICommandLineParser parser,
        IDatabaseService database,
        ISettingsService settings,
        IEnvironmentSecretService secrets,
        IPromptCatalogService prompts,
        IPricingService pricing,
        IAiConversationWorkflow conversationWorkflow,
        IActivityAuditService audit,
        IPortablePathService pathService,
        IExecutableLocationService executableLocation,
        INerdFontInstallerService fontInstaller,
        ILocalizationService text,
        IConsoleShellView shell,
        ISetupView setupView,
        IStatusView statusView,
        ICostsView costsView,
        IHelpView helpView,
        IMainMenuView mainMenuView,
        IThirdPartyView thirdPartyView,
        IPortablePathView pathView,
        IExecutableLocationView executableLocationView,
        INerdFontView fontView,
        AppPaths paths,
        ILogger<PromptMeUpApplication> logger)
    {
        _parser = parser;
        _database = database;
        _settings = settings;
        _secrets = secrets;
        _prompts = prompts;
        _pricing = pricing;
        _conversationWorkflow = conversationWorkflow;
        _audit = audit;
        _pathService = pathService;
        _executableLocation = executableLocation;
        _fontInstaller = fontInstaller;
        _text = text;
        _shell = shell;
        _setupView = setupView;
        _statusView = statusView;
        _costsView = costsView;
        _helpView = helpView;
        _mainMenuView = mainMenuView;
        _thirdPartyView = thirdPartyView;
        _pathView = pathView;
        _executableLocationView = executableLocationView;
        _fontView = fontView;
        _paths = paths;
        _logger = logger;
    }

    /// <summary>Parses one invocation, initializes local state, and dispatches the selected CLI or interactive flow.</summary>
    public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        _text.SetLanguage(SupportedLanguages.ResolveSystemLanguage());
        var parse = _parser.Parse(args);
        if (!parse.Succeeded)
        {
            _shell.RenderHeader("?", null, false);
            _shell.RenderError(parse.Error ?? _text.Text("Cli.Invalid"));
            _helpView.Render();
            _shell.RenderFooter("invalid");
            return 2;
        }

        var options = parse.Options!;
        _shell.Configure(new ConsoleRenderOptions(options.NoAnimation, options.NoEmoji));
        await _database.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var promptCount = (await _prompts.ListAsync(cancellationToken).ConfigureAwait(false)).Count;
        var settings = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        _text.SetLanguage(options.Language ?? settings.Language);
        settings = settings with { Language = _text.Language };
        var hasApiKey = _secrets.IsConfigured(settings.ApiKeyVariable);
        var commandName = ToCommandName(options.Command);
        _shell.RenderHeader(commandName, settings, hasApiKey);

        try
        {
            if (options.Command == AppCommand.Main && !settings.SetupCompleted)
            {
                return await RunFirstSetupAsync(settings, cancellationToken).ConfigureAwait(false);
            }

            if (ShouldRefreshPricing(options.Command, settings))
            {
                await TryRefreshPricingAsync(settings, force: options.Command == AppCommand.Costs, cancellationToken).ConfigureAwait(false);
            }

            var exitCode = await DispatchAsync(options, settings, promptCount, cancellationToken).ConfigureAwait(false);
            _shell.RenderFooter(commandName);
            return exitCode;
        }
        catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _shell.RenderNotice(_text.Text("Common.Cancelled"));
            await TryAuditAsync(commandName, "cancelled", null, new { reason = "escape" }).ConfigureAwait(false);
            _shell.RenderFooter(commandName);
            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _shell.RenderNotice(_text.Text("Common.Cancelled"));
            _shell.RenderFooter(commandName);
            return 130;
        }
        catch (Exception exception) when (exception is OpenAiRequestException
                                          or HttpRequestException
                                          or TaskCanceledException
                                          or System.Text.Json.JsonException
                                          or InvalidOperationException
                                          or ConversationLimitException)
        {
            _logger.LogWarning("PromptMeUp command failed. Command={Command}, ErrorType={ErrorType}", commandName, exception.GetType().Name);
            _shell.RenderError(exception.Message);
            await TryAuditAsync(commandName, "failed", null, new { error = exception.GetType().Name }).ConfigureAwait(false);
            _shell.RenderFooter(commandName);
            return 1;
        }
    }

    /// <summary>Dispatches all explicit and interactive commands after common initialization.</summary>
    private async Task<int> DispatchAsync(
        CommandLineOptions options,
        AppSettings settings,
        int promptCount,
        CancellationToken cancellationToken)
    {
        switch (options.Command)
        {
            case AppCommand.Help:
                _helpView.Render();
                return 0;
            case AppCommand.Version:
                RenderVersion();
                return 0;
            case AppCommand.Setup:
                return await RunSetupAsync(settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.Status:
                await RunStatusAsync(settings, promptCount, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Query:
                EnsureAiReady(settings);
                await _conversationWorkflow.RunQueryAsync(
                    options.Query!,
                    settings,
                    renderQuery: true,
                    cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Chat:
                EnsureInteractive();
                EnsureAiReady(settings);
                await _conversationWorkflow.RunChatAsync(settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.TestAi:
                EnsureAiReady(settings);
                await _conversationWorkflow.RunConnectionTestAsync(settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Costs:
                _costsView.Render(await _pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false));
                return 0;
            case AppCommand.ThirdParty:
                _thirdPartyView.Render();
                return 0;
            case AppCommand.Where:
                return await RunWhereAsync(cancellationToken).ConfigureAwait(false);
            case AppCommand.InstallFont:
                EnsureInteractiveUnlessPreauthorized(options.Yes || options.DryRun);
                return await RunFontAsync(options, cancellationToken).ConfigureAwait(false);
            case AppCommand.Path:
                EnsureInteractiveUnlessPreauthorized(options.Yes || options.PathAction == "status");
                return await RunPathAsync(options, cancellationToken).ConfigureAwait(false);
            default:
                EnsureInteractive();
                return await RunMainMenuAsync(settings, promptCount, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Runs mandatory first-use setup or explains why redirected input cannot complete it.</summary>
    private async Task<int> RunFirstSetupAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        if (!IsInteractive)
        {
            _shell.RenderError(_text.Text("Error.SetupRequired"));
            _shell.RenderFooter("setup");
            return 2;
        }

        var result = await RunSetupAsync(settings, cancellationToken).ConfigureAwait(false);
        _shell.RenderFooter("setup");
        return result;
    }

    /// <summary>Collects setup settings, persists secrets safely, saves preferences, and optionally tests OpenAI.</summary>
    private async Task<int> RunSetupAsync(AppSettings current, CancellationToken cancellationToken)
    {
        EnsureInteractive();
        var submission = _setupView.Collect(new SetupViewState(
            current,
            _secrets.IsConfigured(current.ApiKeyVariable),
            _secrets.IsConfigured(current.AdminKeyVariable)));
        if (submission is null)
        {
            _shell.RenderNotice(_text.Text("Setup.Cancelled"));
            await TryAuditAsync("setup", "cancelled", null, new { }).ConfigureAwait(false);
            return 0;
        }

        var secretGuidance = new List<string>();
        if (submission.ApiKey is not null)
        {
            secretGuidance.Add(_secrets.StoreForCurrentUser(submission.Settings.ApiKeyVariable, submission.ApiKey).Guidance);
        }
        if (submission.AdminKey is not null)
        {
            secretGuidance.Add(_secrets.StoreForCurrentUser(submission.Settings.AdminKeyVariable, submission.AdminKey).Guidance);
        }

        await _settings.SaveAsync(submission.Settings, cancellationToken).ConfigureAwait(false);
        _text.SetLanguage(submission.Settings.Language);
        _shell.RenderSuccess(_text.Text("Setup.Saved"));
        foreach (var guidance in secretGuidance)
        {
            _shell.RenderMuted(guidance);
        }
        await TryAuditAsync(
            "setup",
            "completed",
            null,
            new
            {
                submission.Settings.Language,
                submission.Settings.Model,
                submission.Settings.ReasoningEffort,
                submission.Settings.OutputDetail,
                submission.Settings.PromptCachingEnabled,
                submission.Settings.MaxConversationTurns,
                submission.Settings.MaxContextPercent
            }).ConfigureAwait(false);
        if (submission.TestConnection)
        {
            await _conversationWorkflow.RunConnectionTestAsync(submission.Settings, cancellationToken).ConfigureAwait(false);
        }
        return 0;
    }

    /// <summary>Builds and renders the current application status from local services.</summary>
    private async Task RunStatusAsync(AppSettings settings, int promptCount, CancellationToken cancellationToken)
    {
        var status = new AppStatus(
            settings,
            _secrets.IsConfigured(settings.ApiKeyVariable),
            _secrets.IsConfigured(settings.AdminKeyVariable),
            (await _pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false)).LastPricingSync,
            _paths.DatabasePath,
            _paths.LogsDirectory,
            _paths.PromptDirectory,
            promptCount);
        _statusView.Render(status);
        await TryAuditAsync("status", "completed", null, new { promptCount }).ConfigureAwait(false);
    }

    /// <summary>Runs the persistent PATH status or mutation flow after displaying its exact target.</summary>
    private async Task<int> RunPathAsync(CommandLineOptions options, CancellationToken cancellationToken)
    {
        var action = options.PathAction is null
            ? _pathView.SelectAction()
            : ParsePathAction(options.PathAction);
        var plan = _pathService.CreatePlan(action);
        var confirmed = _pathView.PreviewAndConfirm(plan, options.Yes);
        if (action != PortablePathAction.Status && plan.RequiresChange && !(confirmed || options.Yes))
        {
            await TryAuditAsync("path", "cancelled", null, new { action, plan.ExecutableDirectory }).ConfigureAwait(false);
            return 0;
        }

        var result = await _pathService.ApplyAsync(plan, cancellationToken).ConfigureAwait(false);
        _pathView.RenderResult(result);
        await TryAuditAsync("path", "completed", null, new { action, result.Changed, result.IsPresent, result.ExecutableDirectory }).ConfigureAwait(false);
        return 0;
    }

    /// <summary>Shows the running hm location and optionally opens its containing folder after exact authorization.</summary>
    private async Task<int> RunWhereAsync(CancellationToken cancellationToken)
    {
        var location = _executableLocation.Resolve();
        var action = _executableLocationView.RenderAndSelect(location, IsInteractive);
        if (action == ExecutableLocationAction.DoNothing)
        {
            await TryAuditAsync("where", "cancelled", null, new { location.ExecutablePath }).ConfigureAwait(false);
            return 0;
        }

        if (action == ExecutableLocationAction.OpenContainingFolder)
        {
            if (!_executableLocationView.ConfirmOpen(location))
            {
                _executableLocationView.RenderResult(location, ExecutableLocationAction.ShowChangeDirectoryCommand);
                await TryAuditAsync("where", "cancelled", null, new { location.ExecutablePath }).ConfigureAwait(false);
                return 0;
            }

            cancellationToken.ThrowIfCancellationRequested();
            _executableLocation.OpenContainingFolder(location);
        }

        _executableLocationView.RenderResult(location, action);
        await TryAuditAsync("where", "completed", null, new { action, location.ExecutablePath }).ConfigureAwait(false);
        return 0;
    }

    /// <summary>Runs the optional Nerd Font helper only after preview and authorization.</summary>
    private async Task<int> RunFontAsync(CommandLineOptions options, CancellationToken cancellationToken)
    {
        if (!_fontView.PreviewAndConfirm(options.DryRun, options.Yes || options.DryRun))
        {
            await TryAuditAsync("font", "cancelled", null, new { options.DryRun }).ConfigureAwait(false);
            return 0;
        }

        var result = await _shell.RunWithStatusAsync(
            _text.Text("Font.Progress"),
            () => _fontInstaller.InstallAsync(options.DryRun, cancellationToken)).ConfigureAwait(false);
        _fontView.RenderResult(result);
        await TryAuditAsync("font", "completed", null, new { result.FontName, result.Changed, result.DryRun }).ConfigureAwait(false);
        return 0;
    }

    /// <summary>Runs the interactive command center until the user exits.</summary>
    private async Task<int> RunMainMenuAsync(AppSettings initialSettings, int promptCount, CancellationToken cancellationToken)
    {
        var settings = initialSettings;
        while (true)
        {
            MainMenuAction action;
            try
            {
                action = _mainMenuView.Select();
            }
            catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _shell.RenderNotice(_text.Text("Common.Cancelled"));
                return 0;
            }

            try
            {
                switch (action)
                {
                    case MainMenuAction.Query:
                        EnsureAiReady(settings);
                        var query = _shell.ReadText(_text.Text("Query.Prompt"));
                        await _conversationWorkflow.RunQueryAsync(
                            query,
                            settings,
                            renderQuery: false,
                            cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.Chat:
                        EnsureAiReady(settings);
                        await _conversationWorkflow.RunChatAsync(settings, cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.Costs:
                        await TryRefreshPricingAsync(settings, true, cancellationToken).ConfigureAwait(false);
                        _costsView.Render(await _pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false));
                        break;
                    case MainMenuAction.Status:
                        await RunStatusAsync(settings, promptCount, cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.Setup:
                        await RunSetupAsync(settings, cancellationToken).ConfigureAwait(false);
                        settings = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
                        _text.SetLanguage(settings.Language);
                        break;
                    case MainMenuAction.TestAi:
                        EnsureAiReady(settings);
                        await _conversationWorkflow.RunConnectionTestAsync(settings, cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.Where:
                        await RunWhereAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.Path:
                        await RunPathAsync(new CommandLineOptions(AppCommand.Path, null, null, false, false, false, false, null), cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.InstallFont:
                        await RunFontAsync(new CommandLineOptions(AppCommand.InstallFont, null, null, false, false, false, false, null), cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.ThirdParty:
                        _thirdPartyView.Render();
                        break;
                    default:
                        return 0;
                }
            }
            catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _shell.RenderNotice(_text.Text("Common.Cancelled"));
                await TryAuditAsync(
                    action.ToString().ToLowerInvariant(),
                    "cancelled",
                    null,
                    new { reason = "escape" }).ConfigureAwait(false);
            }
            _shell.WriteLine();
        }
    }

    /// <summary>Refreshes official pricing and optional organization costs without blocking unrelated app work on failure.</summary>
    private async Task TryRefreshPricingAsync(AppSettings settings, bool force, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _shell.RunWithStatusAsync(
                _text.Text("Costs.Refreshing"),
                () => _pricing.RefreshDailyIfNeededAsync(settings, force, cancellationToken)).ConfigureAwait(false);
            if (force)
            {
                _shell.RenderSuccess(_text.Text("Costs.Refreshed"));
            }

            if (force || result.PricesRefreshed || result.OrganizationCostRows > 0)
            {
                await TryAuditAsync("pricing_refresh", "completed", null, result).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or OpenAiRequestException or InvalidDataException or TaskCanceledException)
        {
            _logger.LogWarning("Daily pricing refresh failed; cached data remains available. ErrorType={ErrorType}", exception.GetType().Name);
            if (force)
            {
                _shell.RenderWarning(exception.Message);
            }
        }
    }

    /// <summary>Records a non-session activity while allowing diagnostics to continue if auditing itself fails.</summary>
    private async Task TryAuditAsync(string activity, string outcome, string? sessionId, object payload)
    {
        try
        {
            await _audit.RecordAsync(activity, outcome, sessionId, payload, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError("Activity audit persistence failed. Activity={Activity}, Outcome={Outcome}, ErrorType={ErrorType}", activity, outcome, exception.GetType().Name);
        }
    }

    /// <summary>Rejects AI work until setup, provider, and key prerequisites are satisfied.</summary>
    private void EnsureAiReady(AppSettings settings)
    {
        if (!settings.SetupCompleted || !settings.AiEnabled)
        {
            throw new InvalidOperationException(_text.Text("Error.SetupRequired"));
        }
        if (!_secrets.IsConfigured(settings.ApiKeyVariable))
        {
            throw new InvalidOperationException(_text.Text("Error.ApiKeyMissing", settings.ApiKeyVariable));
        }
    }

    /// <summary>Requires a live terminal for forms and authorization prompts.</summary>
    private void EnsureInteractive()
    {
        if (!IsInteractive)
        {
            throw new InvalidOperationException(_text.Text("Error.InteractiveRequired"));
        }
    }

    /// <summary>Allows explicitly parameterized non-interactive inspection while protecting prompt-only operations.</summary>
    private void EnsureInteractiveUnlessPreauthorized(bool preauthorized)
    {
        if (!IsInteractive && !preauthorized)
        {
            throw new InvalidOperationException(_text.Text("Error.InteractiveRequired"));
        }
    }

    /// <summary>Renders the semantic product and runtime version.</summary>
    private void RenderVersion()
    {
        var assembly = Assembly.GetExecutingAssembly().GetName();
        _shell.RenderVersion(
            assembly.Version?.ToString(3) ?? "0.1.5",
            Environment.Version.ToString(),
            System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);
    }

    /// <summary>Maps a CLI PATH verb to the portable service action.</summary>
    private static PortablePathAction ParsePathAction(string action) => action switch
    {
        "install" => PortablePathAction.Install,
        "remove" => PortablePathAction.Remove,
        _ => PortablePathAction.Status
    };

    /// <summary>Chooses commands where daily pricing is relevant and network access is expected.</summary>
    private static bool ShouldRefreshPricing(AppCommand command, AppSettings settings) =>
        settings.SetupCompleted && command is (AppCommand.Main or AppCommand.Status or AppCommand.Query or AppCommand.Chat or AppCommand.TestAi or AppCommand.Costs);

    /// <summary>Returns the stable status-bar name for a parsed command.</summary>
    private static string ToCommandName(AppCommand command) => command switch
    {
        AppCommand.TestAi => "test-ai",
        AppCommand.InstallFont => "install-font",
        AppCommand.ThirdParty => "third-party",
        _ => command.ToString().ToLowerInvariant()
    };

    private static bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;
}

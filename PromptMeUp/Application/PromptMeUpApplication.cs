// SPDX-License-Identifier: MIT

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
    private readonly DiagnosticWorkflow _diagnostics;
    private readonly ScriptWorkflow _scripts;
    private readonly PlanWorkflow _plans;
    private readonly FilePreviewWorkflow _filePreview;
    private readonly ApplicationActivityRecorder _activity;
    private readonly SetupWorkflow _setup;
    private readonly InstallationWorkflow _installation;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly IStatusView _statusView;
    private readonly ICostsView _costsView;
    private readonly IHelpView _helpView;
    private readonly HelpWorkflow _help;
    private readonly IThirdPartyView _thirdPartyView;
    private readonly IFirstRunView? _firstRunView;
    private readonly FirstRunWorkflow? _firstRun;
    private readonly IHomeView? _home;
    private readonly DiagnosticBundleService? _diagnosticBundles;
    private readonly AppPaths _paths;
    private readonly ILogger<PromptMeUpApplication> _logger;
    private readonly ArtifactLimits _artifactLimits;
    private readonly IThemeCatalogService? _themes;
    private readonly LennaWorkflow? _lenna;
    private readonly AboutWorkflow? _about;
    private readonly MemoryManagerWorkflow? _memories;
    private readonly MemoryCommandWorkflow? _memoryCommands;
    private readonly SkillsAndMemoryWorkflow? _skillsAndMemory;

    /// <summary>Creates the application orchestrator while keeping business services independent from Spectre views.</summary>
    public PromptMeUpApplication(
        ICommandLineParser parser,
        IDatabaseService database,
        ISettingsService settings,
        IEnvironmentSecretService secrets,
        IPromptCatalogService prompts,
        IPricingService pricing,
        IAiConversationWorkflow conversationWorkflow,
        DiagnosticWorkflow diagnostics,
        ScriptWorkflow scripts,
        PlanWorkflow plans,
        FilePreviewWorkflow filePreview,
        ApplicationActivityRecorder activity,
        SetupWorkflow setup,
        InstallationWorkflow installation,
        ILocalizationService text,
        IConsoleShellView shell,
        IStatusView statusView,
        ICostsView costsView,
        IHelpView helpView,
        IThirdPartyView thirdPartyView,
        AppPaths paths,
        ILogger<PromptMeUpApplication> logger,
        HelpWorkflow help,
        ArtifactLimits? artifactLimits = null,
        IThemeCatalogService? themes = null,
        LennaWorkflow? lenna = null,
        AboutWorkflow? about = null,
        MemoryManagerWorkflow? memories = null,
        MemoryCommandWorkflow? memoryCommands = null,
        SkillsAndMemoryWorkflow? skillsAndMemory = null,
        IFirstRunView? firstRunView = null,
        FirstRunWorkflow? firstRun = null,
        IHomeView? home = null,
        DiagnosticBundleService? diagnosticBundles = null)
    {
        _parser = parser;
        _database = database;
        _settings = settings;
        _secrets = secrets;
        _prompts = prompts;
        _pricing = pricing;
        _conversationWorkflow = conversationWorkflow;
        _diagnostics = diagnostics;
        _scripts = scripts;
        _plans = plans;
        _filePreview = filePreview;
        _activity = activity;
        _setup = setup;
        _installation = installation;
        _text = text;
        _shell = shell;
        _statusView = statusView;
        _costsView = costsView;
        _helpView = helpView;
        _help = help;
        _thirdPartyView = thirdPartyView;
        _paths = paths;
        _logger = logger;
        _artifactLimits = artifactLimits ?? ArtifactLimits.Default;
        _themes = themes;
        _lenna = lenna;
        _about = about;
        _memories = memories;
        _memoryCommands = memoryCommands;
        _skillsAndMemory = skillsAndMemory;
        _firstRunView = firstRunView;
        _firstRun = firstRun;
        _home = home;
        _diagnosticBundles = diagnosticBundles;
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
            _helpView.RenderStatic();
            return 2;
        }

        var options = parse.Options!;
        _shell.Configure(new ConsoleRenderOptions(options.NoAnimation, options.NoEmoji));
        if (options.Command == AppCommand.PrepareLogs)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var result = await (_diagnosticBundles ?? throw new InvalidOperationException("Diagnostic bundle service is unavailable."))
                .PrepareAsync(desktop, cancellationToken).ConfigureAwait(false);
            _shell.RenderSuccess(_text.Text("Logs.Prepared", result.Path, result.LogFileCount));
            _shell.RenderNotice(_text.Text("Logs.ReviewBeforeSending"));
            return 0;
        }
        await _database.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var promptCount = (await _prompts.ListAsync(cancellationToken).ConfigureAwait(false)).Count;
        var settings = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (_themes is not null)
        {
            TerminalTheme.Apply(_themes.Resolve(settings.Theme));
        }
        _text.SetLanguage(options.Language ?? settings.Language);
        if (options.Command is not (AppCommand.Setup or AppCommand.AiSettings or AppCommand.Theme))
        {
            settings = settings with { Language = _text.Language };
        }
        if (settings.IsFirstRun && options.Command == AppCommand.Help)
        {
            _helpView.RenderStatic();
            return 0;
        }
        var hasApiKey = _secrets.IsConfigured(settings.ApiKeyVariable);
        var commandName = ToCommandName(options.Command);
        _logger.LogInformation("Startup route selected. Command={Command}, FirstRun={FirstRun}, Interactive={Interactive}, Language={Language}",
            commandName, settings.IsFirstRun, IsInteractive, _text.Language);
        if (!settings.IsFirstRun && options.Command != AppCommand.Main)
        {
            _shell.RenderHeader(commandName, settings, hasApiKey);
        }

        try
        {
            if (options.Command == AppCommand.Reset)
            {
                if (options.ResetAll)
                {
                    await _database.ResetAsync(cancellationToken).ConfigureAwait(false);
                    settings = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
                }

                await _settings.SaveAsync(settings with
                {
                    SetupCompleted = false,
                    DirectModeEnabled = false,
                    UpdatedAt = DateTimeOffset.UtcNow
                }, cancellationToken).ConfigureAwait(false);
                return 0;
            }

            if (settings.IsFirstRun)
            {
                var firstRunView = _firstRunView
                    ?? throw new InvalidOperationException("The first-run view is unavailable.");
                if (!IsInteractive)
                {
                    firstRunView.RenderSetupRequired();
                    return 0;
                }

                EnsureInteractive();
                return await (_firstRun ?? throw new InvalidOperationException("The first-run workflow is unavailable."))
                    .RunAsync(settings, cancellationToken).ConfigureAwait(false);
            }

            if (ShouldRefreshPricing(options.Command, settings))
            {
                await TryRefreshPricingAsync(settings, force: options.Command == AppCommand.Costs, cancellationToken).ConfigureAwait(false);
            }

            var exitCode = await DispatchAsync(options, settings, promptCount, cancellationToken).ConfigureAwait(false);
            return exitCode;
        }
        catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _shell.RenderNotice(_text.Text("Common.Cancelled"));
            await _activity.TryRecordAsync(commandName, "cancelled", null, new { reason = "escape" }).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _shell.RenderNotice(_text.Text("Common.Cancelled"));
            return 130;
        }
        catch (Exception exception) when (exception is OpenAiRequestException
                                          or HttpRequestException
                                          or TaskCanceledException
                                          or System.Text.Json.JsonException
                                          or InvalidOperationException
                                          or ConversationLimitException)
        {
            _logger.LogWarning("PromptMeUp command failed. Command={Command}, ErrorType={ErrorType}, StackTrace={StackTrace}",
                commandName, exception.GetType().Name, new System.Diagnostics.StackTrace(exception, fNeedFileInfo: false).ToString());
            _shell.RenderError(FormatErrorMessage(exception, _text, OperatingSystem.IsWindows()));
            await _activity.TryRecordAsync(commandName, "failed", null, new { error = exception.GetType().Name }).ConfigureAwait(false);
            return 1;
        }
        finally
        {
            if (!settings.IsFirstRun)
            {
                await ResetDirectModeAfterInvocationAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Routes numbered home choices to the existing workflows and returns here when each one ends.</summary>
    private async Task<int> RunHomeAsync(CommandLineOptions options, int promptCount, CancellationToken cancellationToken)
    {
        var home = _home ?? throw new InvalidOperationException("The home view is unavailable.");
        if (!IsInteractive)
        {
            home.RenderNonInteractive();
            return 0;
        }
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
            _text.SetLanguage(options.Language ?? current.Language);
            current = current with { Language = _text.Language };
            if (_themes is not null)
            {
                TerminalTheme.Apply(_themes.Resolve(current.Theme));
            }
            _logger.LogInformation("Home menu opened.");
            var action = await home.ChooseAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Home menu choice. Action={Action}", action);
            if (action == HomeAction.Exit)
            {
                return 0;
            }
            try
            {
                string? query = null;
                if (action is HomeAction.Script or HomeAction.Diagnose)
                {
                    query = await home.ReadRequestAsync(action, current.MaxMessageCharacters, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(query))
                    {
                        continue;
                    }
                }
                var command = action switch
                {
                    HomeAction.Chat => AppCommand.Chat,
                    HomeAction.Script => AppCommand.Script,
                    HomeAction.Diagnose => AppCommand.Diagnose,
                    HomeAction.Memories => AppCommand.Memories,
                    HomeAction.Skills => AppCommand.Skills,
                    HomeAction.Settings => AppCommand.Setup,
                    HomeAction.Help => AppCommand.Help,
                    _ => throw new InvalidOperationException("Unsupported home action.")
                };
                if (ShouldRefreshPricing(command, current))
                {
                    await TryRefreshPricingAsync(current, force: false, cancellationToken).ConfigureAwait(false);
                }
                await DispatchAsync(options with { Command = command, Query = query }, current, promptCount, cancellationToken).ConfigureAwait(false);
            }
            catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _shell.RenderNotice(_text.Text("Common.Cancelled"));
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is
                OpenAiRequestException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException
                or InvalidOperationException or ConversationLimitException)
            {
                _logger.LogWarning("Home action failed. Action={Action}, ErrorType={ErrorType}, StackTrace={StackTrace}",
                    action, exception.GetType().Name, new System.Diagnostics.StackTrace(exception, fNeedFileInfo: false).ToString());
                _shell.RenderError(FormatErrorMessage(exception, _text, OperatingSystem.IsWindows()));
            }
        }
    }

    /// <summary>Turns off persisted direct mode after the current application session ends.</summary>
    private async Task ResetDirectModeAfterInvocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var persisted = await _database.LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
            if (!persisted.DirectModeEnabled)
            {
                return;
            }

            await _settings.SaveAsync(persisted with
            {
                DirectModeEnabled = false,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning("Could not reset direct mode after the application session. ErrorType={ErrorType}", exception.GetType().Name);
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
            case AppCommand.Lenna:
                return (_lenna ?? throw new InvalidOperationException(_text.Text("Lenna.LoadError")))
                    .Run(options, cancellationToken);
            case AppCommand.About:
                return (_about ?? throw new InvalidOperationException(_text.Text("About.Unavailable")))
                    .Run(options, cancellationToken);
            case AppCommand.Skills:
            case AppCommand.Learning:
            case AppCommand.Dream:
            case AppCommand.Heartbeat:
                EnsureInteractive();
                await (_skillsAndMemory ?? throw new InvalidOperationException(_text.Text("Lab.Invalid")))
                    .RunAsync(options.Command, settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Preview:
                return await _filePreview.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.Plan:
                EnsureInteractive();
                if (options.ResumeId is null)
                {
                    EnsureAiReady(settings);
                }
                return await _plans.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.Script:
                EnsureInteractive();
                EnsureAiReady(settings);
                await _scripts.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Diagnose:
                EnsureAiReady(settings);
                await _diagnostics.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Main:
                return await RunHomeAsync(options, promptCount, cancellationToken).ConfigureAwait(false);
            case AppCommand.Help:
                return _help.Run(options, cancellationToken);
            case AppCommand.Version:
                RenderVersion();
                return 0;
            case AppCommand.Setup:
                return await RunSetupAsync(settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.AiSettings:
                return await RunSetupAsync(settings, cancellationToken, SettingsSection.Ai).ConfigureAwait(false);
            case AppCommand.Theme:
                return await RunSetupAsync(settings, cancellationToken, SettingsSection.Theme).ConfigureAwait(false);
            case AppCommand.Status:
                await RunStatusAsync(settings, promptCount, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Query:
            case AppCommand.Direct:
                if (options.Command == AppCommand.Direct || settings.DirectModeEnabled)
                {
                    EnsureInteractive();
                }
                EnsureAiReady(settings);
                await _conversationWorkflow.RunQueryAsync(
                    options.Query!,
                    settings,
                    renderQuery: true,
                    cancellationToken,
                    executionMode: options.Command == AppCommand.Direct || settings.DirectModeEnabled
                        ? CommandExecutionMode.Direct : CommandExecutionMode.Confirm,
                    directModeOverride: options.Command == AppCommand.Direct).ConfigureAwait(false);
                return 0;
            case AppCommand.Remember:
            case AppCommand.Forget:
                if (options.Command == AppCommand.Forget)
                {
                    EnsureInteractive();
                }
                return await (_memoryCommands ?? throw new InvalidOperationException("Memory commands are unavailable."))
                    .RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.Memories:
                EnsureInteractive();
                await (_memories ?? throw new InvalidOperationException("The memory manager is unavailable."))
                    .RunAsync(cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Proposals:
                EnsureInteractive();
                await (_memories ?? throw new InvalidOperationException("The memory manager is unavailable."))
                    .RunAsync(cancellationToken, selectProposals: true).ConfigureAwait(false);
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
                return await _installation.RunWhereAsync(IsInteractive, cancellationToken).ConfigureAwait(false);
            case AppCommand.InstallFont:
                EnsureInteractiveUnlessPreauthorized(options.Yes || options.DryRun);
                return await _installation.RunFontAsync(options, cancellationToken).ConfigureAwait(false);
            case AppCommand.Path:
                EnsureInteractiveUnlessPreauthorized(options.Yes || options.PathAction == "status");
                return await _installation.RunPathAsync(options, cancellationToken).ConfigureAwait(false);
            default:
                throw new ArgumentOutOfRangeException(nameof(options), "Unsupported application command.");
        }
    }


    /// <summary>Requires a live terminal before opening the shared settings screen at the requested section.</summary>
    private async Task<int> RunSetupAsync(
        AppSettings current,
        CancellationToken cancellationToken,
        SettingsSection initialSection = SettingsSection.General)
    {
        EnsureInteractive();
        return await _setup.RunAsync(current, cancellationToken, initialSection).ConfigureAwait(false);
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
            promptCount)
        { ArtifactLimits = _artifactLimits };
        _statusView.Render(status);
        await _activity.TryRecordAsync("status", "completed", null, new { promptCount }).ConfigureAwait(false);
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
                await _activity.TryRecordAsync("pricing_refresh", "completed", null, result).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or OpenAiRequestException or InvalidDataException or TaskCanceledException)
        {
            _logger.LogWarning("Daily pricing refresh failed; cached data remains available. ErrorType={ErrorType}", exception.GetType().Name);
            if (force)
            {
                _shell.RenderWarning(FormatErrorMessage(exception, _text, OperatingSystem.IsWindows()));
            }
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
            throw new InvalidOperationException(AppendKeyRestartHint(
                _text.Text("Error.ApiKeyMissing", settings.ApiKeyVariable),
                _text,
                OperatingSystem.IsWindows()));
        }
    }

    /// <summary>Adds the localized terminal-restart guidance to Windows authentication failures.</summary>
    internal static string FormatErrorMessage(Exception exception, ILocalizationService text, bool isWindows)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(text);
        return exception is OpenAiRequestException { StatusCode: 401 }
            ? AppendKeyRestartHint(exception.Message, text, isWindows)
            : exception.Message;
    }

    /// <summary>Appends the localized key-refresh guidance when Windows may retain a stale process value.</summary>
    internal static string AppendKeyRestartHint(string message, ILocalizationService text, bool isWindows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(text);
        return isWindows
            ? $"{message}{Environment.NewLine}{text.Text("Error.KeyRestartRequired")}"
            : message;
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

    /// <summary>Renders the product version and immutable compilation provenance.</summary>
    private void RenderVersion()
    {
        _shell.RenderVersion(
            BuildInformationReader.Read(),
            Environment.Version.ToString(),
            System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);
    }

    /// <summary>Chooses commands where daily pricing is relevant and network access is expected.</summary>
    private static bool ShouldRefreshPricing(AppCommand command, AppSettings settings) =>
        settings.SetupCompleted && command is (AppCommand.Status or AppCommand.Query or AppCommand.Direct or AppCommand.Chat or AppCommand.TestAi or AppCommand.Costs);

    /// <summary>Returns the stable status-bar name for a parsed command.</summary>
    private static string ToCommandName(AppCommand command) => command switch
    {
        AppCommand.TestAi => "test-ai",
        AppCommand.AiSettings => "ai-settings",
        AppCommand.Theme => "theme",
        AppCommand.InstallFont => "install-font",
        AppCommand.PrepareLogs => "prepare-logs",
        AppCommand.ThirdParty => "third-party",
        _ => command.ToString().ToLowerInvariant()
    };

    private static bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;
}

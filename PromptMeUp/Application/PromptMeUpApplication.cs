// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
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
    private readonly IApplicationCommandRouter _commands;
    private readonly ApplicationActivityRecorder _activity;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly IHelpView _helpView;
    private readonly IFirstRunView _firstRunView;
    private readonly FirstRunWorkflow _firstRun;
    private readonly IHomeView _home;
    private readonly DiagnosticBundleService _diagnosticBundles;
    private readonly ILogger<PromptMeUpApplication> _logger;
    private readonly IThemeCatalogService _themes;

    /// <summary>Creates the application orchestrator while keeping business services independent from Spectre views.</summary>
    public PromptMeUpApplication(
        ICommandLineParser parser,
        IDatabaseService database,
        ISettingsService settings,
        IEnvironmentSecretService secrets,
        IPromptCatalogService prompts,
        IPricingService pricing,
        IApplicationCommandRouter commands,
        ApplicationActivityRecorder activity,
        ILocalizationService text,
        IConsoleShellView shell,
        IHelpView helpView,
        ILogger<PromptMeUpApplication> logger,
        IThemeCatalogService themes,
        IFirstRunView firstRunView,
        FirstRunWorkflow firstRun,
        IHomeView home,
        DiagnosticBundleService diagnosticBundles)
    {
        _parser = parser;
        _database = database;
        _settings = settings;
        _secrets = secrets;
        _prompts = prompts;
        _pricing = pricing;
        _commands = commands;
        _activity = activity;
        _text = text;
        _shell = shell;
        _helpView = helpView;
        _logger = logger;
        _themes = themes;
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
            var result = await _diagnosticBundles.PrepareAsync(desktop, cancellationToken).ConfigureAwait(false);
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
                if (!IsInteractive)
                {
                    _firstRunView.RenderSetupRequired();
                    return 0;
                }

                CommandPreconditions.RequireInteractive(_text);
                return await _firstRun.RunAsync(settings, cancellationToken).ConfigureAwait(false);
            }

            if (ShouldRefreshPricing(options.Command, settings))
            {
                await TryRefreshPricingAsync(settings, force: options.Command == AppCommand.Costs, cancellationToken).ConfigureAwait(false);
            }

            var exitCode = options.Command == AppCommand.Main
                ? await RunHomeAsync(options, promptCount, cancellationToken).ConfigureAwait(false)
                : await _commands.DispatchAsync(options, settings, promptCount, cancellationToken).ConfigureAwait(false);
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
        if (!IsInteractive)
        {
            _home.RenderNonInteractive();
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
            var action = await _home.ChooseAsync(cancellationToken).ConfigureAwait(false);
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
                    query = await _home.ReadRequestAsync(action, current.MaxMessageCharacters, cancellationToken).ConfigureAwait(false);
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
                await _commands.DispatchAsync(options with { Command = command, Query = query }, current, promptCount, cancellationToken).ConfigureAwait(false);
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

    private static bool IsInteractive => CommandPreconditions.IsInteractive;
}

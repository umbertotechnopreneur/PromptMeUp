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
    private readonly DiagnosticWorkflow _diagnostics;
    private readonly ScriptWorkflow _scripts;
    private readonly PlanWorkflow _plans;
    private readonly FilePreviewWorkflow _filePreview;
    private readonly RecipeWorkflow _recipes;
    private readonly ApplicationActivityRecorder _activity;
    private readonly SetupWorkflow _setup;
    private readonly InstallationWorkflow _installation;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly IStatusView _statusView;
    private readonly ICostsView _costsView;
    private readonly IHelpView _helpView;
    private readonly IMainMenuView _mainMenuView;
    private readonly IThirdPartyView _thirdPartyView;
    private readonly AppPaths _paths;
    private readonly ILogger<PromptMeUpApplication> _logger;
    private readonly ArtifactLimits _artifactLimits;

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
        RecipeWorkflow recipes,
        ApplicationActivityRecorder activity,
        SetupWorkflow setup,
        InstallationWorkflow installation,
        ILocalizationService text,
        IConsoleShellView shell,
        IStatusView statusView,
        ICostsView costsView,
        IHelpView helpView,
        IMainMenuView mainMenuView,
        IThirdPartyView thirdPartyView,
        AppPaths paths,
        ILogger<PromptMeUpApplication> logger,
        ArtifactLimits? artifactLimits = null)
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
        _recipes = recipes;
        _activity = activity;
        _setup = setup;
        _installation = installation;
        _text = text;
        _shell = shell;
        _statusView = statusView;
        _costsView = costsView;
        _helpView = helpView;
        _mainMenuView = mainMenuView;
        _thirdPartyView = thirdPartyView;
        _paths = paths;
        _logger = logger;
        _artifactLimits = artifactLimits ?? ArtifactLimits.Default;
    }

    /// <summary>Parses one invocation, initializes local state, and dispatches the selected CLI or interactive flow.</summary>
    public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        _text.SetLanguage(SupportedLanguages.ResolveSystemLanguage());
        var parse = _parser.Parse(args);
        if (!parse.Succeeded)
        {
            _shell.RenderHeader("?", null, false, Environment.CurrentDirectory);
            _shell.RenderError(parse.Error ?? _text.Text("Cli.Invalid"));
            _helpView.Render();
            return 2;
        }

        var options = parse.Options!;
        _shell.Configure(new ConsoleRenderOptions(options.NoAnimation, options.NoEmoji));
        await _database.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var promptCount = (await _prompts.ListAsync(cancellationToken).ConfigureAwait(false)).Count;
        var settings = await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        _text.SetLanguage(options.Language ?? settings.Language);
        if (options.Command != AppCommand.AiSettings)
        {
            settings = settings with { Language = _text.Language };
        }
        var hasApiKey = _secrets.IsConfigured(settings.ApiKeyVariable);
        var commandName = ToCommandName(options.Command);
        _shell.RenderHeader(commandName, settings, hasApiKey, Environment.CurrentDirectory);

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
            _logger.LogWarning("PromptMeUp command failed. Command={Command}, ErrorType={ErrorType}", commandName, exception.GetType().Name);
            _shell.RenderError(FormatErrorMessage(exception, _text, OperatingSystem.IsWindows()));
            await _activity.TryRecordAsync(commandName, "failed", null, new { error = exception.GetType().Name }).ConfigureAwait(false);
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
            case AppCommand.Recipes:
                if (options.RecipeAction is not ("list" or "show"))
                {
                    EnsureInteractive();
                }
                return await _recipes.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
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
            case AppCommand.Help:
                _helpView.Render();
                return 0;
            case AppCommand.Version:
                RenderVersion();
                return 0;
            case AppCommand.Setup:
                return await RunSetupAsync(settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.AiSettings:
                return await RunAiSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
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
                return await _installation.RunWhereAsync(IsInteractive, cancellationToken).ConfigureAwait(false);
            case AppCommand.InstallFont:
                EnsureInteractiveUnlessPreauthorized(options.Yes || options.DryRun);
                return await _installation.RunFontAsync(options, cancellationToken).ConfigureAwait(false);
            case AppCommand.Path:
                EnsureInteractiveUnlessPreauthorized(options.Yes || options.PathAction == "status");
                return await _installation.RunPathAsync(options, cancellationToken).ConfigureAwait(false);
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
            return 2;
        }

        var result = await RunSetupAsync(settings, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>Checks initial setup and terminal access before editing focused AI preferences.</summary>
    private async Task<int> RunAiSettingsAsync(AppSettings current, CancellationToken cancellationToken)
    {
        if (!current.SetupCompleted)
        {
            _shell.RenderError(_text.Text("Error.SetupRequired"));
            return 2;
        }

        EnsureInteractive();
        return await _setup.RunAiSettingsAsync(current, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Requires a live terminal before collecting setup settings and credentials.</summary>
    private async Task<int> RunSetupAsync(AppSettings current, CancellationToken cancellationToken)
    {
        EnsureInteractive();
        return await _setup.RunAsync(current, cancellationToken).ConfigureAwait(false);
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
                        await _installation.RunWhereAsync(IsInteractive, cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.Path:
                        await _installation.RunPathAsync(new CommandLineOptions(AppCommand.Path, null, null, false, false, false, false, null), cancellationToken).ConfigureAwait(false);
                        break;
                    case MainMenuAction.InstallFont:
                        await _installation.RunFontAsync(new CommandLineOptions(AppCommand.InstallFont, null, null, false, false, false, false, null), cancellationToken).ConfigureAwait(false);
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
                await _activity.TryRecordAsync(
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

    /// <summary>Renders the semantic product and runtime version.</summary>
    private void RenderVersion()
    {
        var assembly = Assembly.GetExecutingAssembly().GetName();
        _shell.RenderVersion(
            assembly.Version?.ToString(3) ?? "0.1.5",
            Environment.Version.ToString(),
            System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);
    }

    /// <summary>Chooses commands where daily pricing is relevant and network access is expected.</summary>
    private static bool ShouldRefreshPricing(AppCommand command, AppSettings settings) =>
        settings.SetupCompleted && command is (AppCommand.Main or AppCommand.Status or AppCommand.Query or AppCommand.Chat or AppCommand.TestAi or AppCommand.Costs);

    /// <summary>Returns the stable status-bar name for a parsed command.</summary>
    private static string ToCommandName(AppCommand command) => command switch
    {
        AppCommand.TestAi => "test-ai",
        AppCommand.AiSettings => "ai-settings",
        AppCommand.InstallFont => "install-font",
        AppCommand.ThirdParty => "third-party",
        _ => command.ToString().ToLowerInvariant()
    };

    private static bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;
}

// SPDX-License-Identifier: MIT

using System.Reflection;
using Microsoft.Extensions.Logging;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

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
    private readonly IOpenAiService _openAi;
    private readonly IConversationMemoryService _memoryService;
    private readonly ICommandRiskAssessmentService _riskAssessment;
    private readonly ICommandExecutionService _commandExecution;
    private readonly IActivityAuditService _audit;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly IPortablePathService _pathService;
    private readonly INerdFontInstallerService _fontInstaller;
    private readonly ILocalizationService _text;
    private readonly IConsoleShellView _shell;
    private readonly ISetupView _setupView;
    private readonly IStatusView _statusView;
    private readonly ICostsView _costsView;
    private readonly IChatView _chatView;
    private readonly ICommandAuthorizationView _commandView;
    private readonly IHelpView _helpView;
    private readonly IMainMenuView _mainMenuView;
    private readonly IThirdPartyView _thirdPartyView;
    private readonly IPortablePathView _pathView;
    private readonly INerdFontView _fontView;
    private readonly IAnsiConsole _console;
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
        IOpenAiService openAi,
        IConversationMemoryService memoryService,
        ICommandRiskAssessmentService riskAssessment,
        ICommandExecutionService commandExecution,
        IActivityAuditService audit,
        ISensitiveDataRedactor redactor,
        IPortablePathService pathService,
        INerdFontInstallerService fontInstaller,
        ILocalizationService text,
        IConsoleShellView shell,
        ISetupView setupView,
        IStatusView statusView,
        ICostsView costsView,
        IChatView chatView,
        ICommandAuthorizationView commandView,
        IHelpView helpView,
        IMainMenuView mainMenuView,
        IThirdPartyView thirdPartyView,
        IPortablePathView pathView,
        INerdFontView fontView,
        IAnsiConsole console,
        AppPaths paths,
        ILogger<PromptMeUpApplication> logger)
    {
        _parser = parser;
        _database = database;
        _settings = settings;
        _secrets = secrets;
        _prompts = prompts;
        _pricing = pricing;
        _openAi = openAi;
        _memoryService = memoryService;
        _riskAssessment = riskAssessment;
        _commandExecution = commandExecution;
        _audit = audit;
        _redactor = redactor;
        _pathService = pathService;
        _fontInstaller = fontInstaller;
        _text = text;
        _shell = shell;
        _setupView = setupView;
        _statusView = statusView;
        _costsView = costsView;
        _chatView = chatView;
        _commandView = commandView;
        _helpView = helpView;
        _mainMenuView = mainMenuView;
        _thirdPartyView = thirdPartyView;
        _pathView = pathView;
        _fontView = fontView;
        _console = console;
        _paths = paths;
        _logger = logger;
    }

    /// <summary>Parses one invocation, initializes local state, and dispatches the selected CLI or interactive flow.</summary>
    public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        var parse = _parser.Parse(args);
        if (!parse.Succeeded)
        {
            _text.SetLanguage(SupportedLanguages.ResolveSystemLanguage());
            _shell.RenderHeader("invalid", null, false);
            _shell.RenderError(parse.Error ?? "Invalid command line.");
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _shell.RenderNotice("Operation cancelled.");
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
            _logger.LogWarning(exception, "PromptMeUp command failed. Command={Command}, ErrorType={ErrorType}", commandName, exception.GetType().Name);
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
                await RunQueryAsync(options.Query!, settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Chat:
                EnsureInteractive();
                EnsureAiReady(settings);
                await RunChatAsync(settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.TestAi:
                EnsureAiReady(settings);
                await RunConnectionTestAsync(settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Costs:
                _costsView.Render(await _pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false));
                return 0;
            case AppCommand.ThirdParty:
                _thirdPartyView.Render();
                return 0;
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
        var submission = _setupView.Collect(current);
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
        _console.MarkupLine($"[green]{Markup.Escape(_text.Text("Setup.Saved"))}[/]");
        foreach (var guidance in secretGuidance)
        {
            _console.MarkupLine($"[grey]{Markup.Escape(guidance)}[/]");
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
            await RunConnectionTestAsync(submission.Settings, cancellationToken).ConfigureAwait(false);
        }
        return 0;
    }

    /// <summary>Runs a single-turn session and closes its ledger after the model response.</summary>
    private async Task RunQueryAsync(string query, AppSettings settings, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var memory = _memoryService.Create(settings);
        var runningCost = 0m;
        await _audit.StartSessionAsync(sessionId, "query", settings, new { invocation = "query" }, cancellationToken).ConfigureAwait(false);
        var status = "failed";
        try
        {
            await SendTurnAsync(sessionId, query, memory, settings, runningCost, cancellationToken).ConfigureAwait(false);
            status = "completed";
        }
        finally
        {
            await _audit.CloseSessionAsync(sessionId, status, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Runs a short interactive session with slash commands and a mandatory command-authorization gate.</summary>
    private async Task RunChatAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var memory = _memoryService.Create(settings);
        var runningCost = 0m;
        var status = "cancelled";
        await _audit.StartSessionAsync(sessionId, "chat", settings, new { invocation = "chat" }, cancellationToken).ConfigureAwait(false);
        _chatView.RenderIntro();
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = _chatView.ReadMessage().Trim();
                if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                {
                    status = "completed";
                    _console.MarkupLine($"[grey]{Markup.Escape(_text.Text("Chat.Exit"))}[/]");
                    break;
                }
                if (input.Equals("/clear", StringComparison.OrdinalIgnoreCase))
                {
                    memory.Clear();
                    await _audit.AppendSessionEventAsync(sessionId, "memory_cleared", new { }, cancellationToken).ConfigureAwait(false);
                    _console.MarkupLine($"[grey]{Markup.Escape(_text.Text("Chat.Cleared"))}[/]");
                    continue;
                }
                if (input.Equals("/costs", StringComparison.OrdinalIgnoreCase))
                {
                    _costsView.Render(await _pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false));
                    continue;
                }
                if (input.Equals("/status", StringComparison.OrdinalIgnoreCase))
                {
                    _shell.RenderRuntimeStatus(ShellRuntimeStatus.FromSettings(settings) with { RunningCostUsd = runningCost });
                    continue;
                }
                if (input.StartsWith("/run ", StringComparison.OrdinalIgnoreCase))
                {
                    var command = input[5..].Trim();
                    if (command.Length == 0)
                    {
                        _shell.RenderError("/run requires a command.");
                        continue;
                    }

                    var commandFollowUp = await RunAuthorizedCommandAsync(
                        sessionId,
                        command,
                        settings,
                        cancellationToken).ConfigureAwait(false);
                    if (commandFollowUp is not null)
                    {
                        runningCost += await SendTurnAsync(sessionId, commandFollowUp, memory, settings, runningCost, cancellationToken).ConfigureAwait(false);
                    }
                    continue;
                }
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                runningCost += await SendTurnAsync(sessionId, input, memory, settings, runningCost, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await _audit.CloseSessionAsync(sessionId, status, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Adds one bounded user turn, renders preflight context, calls OpenAI, and updates session metrics.</summary>
    private async Task<decimal> SendTurnAsync(
        string sessionId,
        string userText,
        ConversationMemory memory,
        AppSettings settings,
        decimal runningCost,
        CancellationToken cancellationToken)
    {
        var update = memory.Add("user", userText);
        await AuditPruningAsync(sessionId, update.PrunedMessages, cancellationToken).ConfigureAwait(false);
        var before = await _openAi.EstimateContextAsync(
            "chat-system",
            update.Snapshot.Messages,
            settings,
            _text.Language,
            cancellationToken).ConfigureAwait(false);
        _shell.RenderRuntimeStatus(new ShellRuntimeStatus(
            "OpenAI",
            settings.Model,
            settings.ReasoningEffort,
            null,
            null,
            runningCost,
            before.InputTokens,
            before.ContextWindowTokens,
            true,
            0,
            0));

        var response = await _shell.RunWithStatusAsync(
            _text.Text("Status.Thinking"),
            () => _openAi.SendAsync(
                "chat-system",
                sessionId,
                update.Snapshot.Messages,
                settings,
                _text.Language,
                cancellationToken)).ConfigureAwait(false);
        var assistantUpdate = memory.Add("assistant", response.Text);
        await AuditPruningAsync(sessionId, assistantUpdate.PrunedMessages, cancellationToken).ConfigureAwait(false);
        _chatView.RenderAssistant(response.Text, animate: false, cancellationToken);
        var turnCost = response.EstimatedCostUsd ?? 0m;
        decimal? promptCost = response.CostBreakdown is null
            ? null
            : response.CostBreakdown.InputUsd + response.CostBreakdown.CachedInputUsd + response.CostBreakdown.CacheWriteUsd;
        _shell.RenderRuntimeStatus(new ShellRuntimeStatus(
            "OpenAI",
            response.Model,
            settings.ReasoningEffort,
            promptCost,
            response.CostBreakdown?.OutputUsd,
            runningCost + turnCost,
            response.ContextUsage.InputTokens + response.ContextUsage.OutputTokens,
            response.ContextUsage.ContextWindowTokens,
            false,
            response.Usage.CachedInputTokens,
            response.Usage.CacheWriteTokens));
        return turnCost;
    }

    /// <summary>Assesses, previews, authorizes, executes, audits, and prepares bounded command output for the next AI turn.</summary>
    private async Task<string?> RunAuthorizedCommandAsync(
        string sessionId,
        string command,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var assessment = await _riskAssessment.AssessAsync(
            command,
            settings.ReviewCommandsWithAi,
            settings,
            _text.Language,
            cancellationToken).ConfigureAwait(false);
        await _audit.AppendSessionEventAsync(
            sessionId,
            "command_preview",
            new { command, assessment },
            cancellationToken).ConfigureAwait(false);
        var approved = _commandView.PreviewAndAuthorize(command, assessment);
        if (approved is null)
        {
            await _audit.RecordAsync("command_authorization", "denied", sessionId, new { command, assessment.Score }, cancellationToken).ConfigureAwait(false);
            return null;
        }

        await _audit.RecordAsync("command_authorization", "approved", sessionId, new { command, assessment.Score }, cancellationToken).ConfigureAwait(false);
        var result = await _shell.RunWithStatusAsync(
            _text.Text("Command.Running"),
            () => _commandExecution.ExecuteAsync(
                approved,
                TimeSpan.FromSeconds(settings.CommandTimeoutSeconds),
                cancellationToken)).ConfigureAwait(false);
        _commandView.RenderExecutionResult(result);
        var boundedOutput = Limit(_redactor.Redact(result.StandardOutput), settings.MaxCommandOutputCharacters);
        var boundedError = Limit(_redactor.Redact(result.StandardError), settings.MaxCommandOutputCharacters);
        var redactedCommand = _redactor.Redact(command);
        await _audit.AppendSessionEventAsync(
            sessionId,
            "command_output",
            new
            {
                result.Command,
                result.ExitCode,
                standardOutput = boundedOutput,
                standardError = boundedError,
                result.TimedOut,
                result.OutputTruncated,
                result.ElapsedMilliseconds
            },
            cancellationToken).ConfigureAwait(false);
        var followUp = $"""
            I explicitly authorized and ran this PowerShell command:
            {redactedCommand}

            Exit code: {result.ExitCode?.ToString() ?? "timeout"}
            Standard output:
            {boundedOutput}

            Standard error:
            {boundedError}

            Analyze this result and explain the next useful step. Do not imply that any additional command has run.
            """;
        return Limit(followUp, settings.MaxMessageCharacters);
    }

    /// <summary>Runs the YAML diagnostic prompt and renders its response with a teletype effect.</summary>
    private async Task RunConnectionTestAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        _console.MarkupLine($"[bold deepskyblue1]{Markup.Escape(_text.Text("Test.Title"))}[/]");
        var prompt = await _prompts.GetAsync("connection-test", cancellationToken).ConfigureAwait(false);
        _chatView.RenderUser(prompt.ResolveText(_text.Language));
        var result = await _shell.RunWithStatusAsync(
            _text.Text("Status.Thinking"),
            () => _openAi.TestConnectionAsync(settings, _text.Language, cancellationToken)).ConfigureAwait(false);
        _chatView.RenderAssistant(result.Response.Text, animate: true, cancellationToken);
        _console.MarkupLine($"[green]{Markup.Escape(_text.Text("Test.Success", result.Response.ElapsedMilliseconds))}[/]");
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
            var action = _mainMenuView.Select();
            switch (action)
            {
                case MainMenuAction.Query:
                    EnsureAiReady(settings);
                    var query = _console.Prompt(new TextPrompt<string>(Markup.Escape(_text.Text("Query.Prompt"))));
                    await RunQueryAsync(query, settings, cancellationToken).ConfigureAwait(false);
                    break;
                case MainMenuAction.Chat:
                    EnsureAiReady(settings);
                    await RunChatAsync(settings, cancellationToken).ConfigureAwait(false);
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
                    await RunConnectionTestAsync(settings, cancellationToken).ConfigureAwait(false);
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
            _console.WriteLine();
        }
    }

    /// <summary>Refreshes official pricing and optional organization costs without blocking unrelated app work on failure.</summary>
    private async Task TryRefreshPricingAsync(AppSettings settings, bool force, CancellationToken cancellationToken)
    {
        try
        {
            await _shell.RunWithStatusAsync(
                _text.Text("Costs.Refreshing"),
                () => _pricing.RefreshDailyIfNeededAsync(settings, force, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or OpenAiRequestException or InvalidDataException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Daily pricing refresh failed; cached data remains available.");
            if (force)
            {
                _console.MarkupLine($"[yellow]{Markup.Escape(exception.Message)}[/]");
            }
        }
    }

    /// <summary>Records context-pruning activity only when the active memory actually changed.</summary>
    private async Task AuditPruningAsync(string sessionId, int prunedMessages, CancellationToken cancellationToken)
    {
        if (prunedMessages <= 0)
        {
            return;
        }

        _chatView.RenderMemoryPruned(prunedMessages);
        await _audit.AppendSessionEventAsync(sessionId, "memory_pruned", new { prunedMessages }, cancellationToken).ConfigureAwait(false);
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
            _logger.LogError(exception, "Activity audit persistence failed. Activity={Activity}, Outcome={Outcome}", activity, outcome);
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
        _console.MarkupLine($"[bold]PromptMeUp[/] {Markup.Escape(assembly.Version?.ToString(3) ?? "0.1.0")}");
        _console.MarkupLine($"[grey].NET {Markup.Escape(Environment.Version.ToString())} · {Markup.Escape(System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier)}[/]");
    }

    /// <summary>Limits output retained and transmitted after an authorized command.</summary>
    private static string Limit(string value, int maximumCharacters)
    {
        if (value.Length <= maximumCharacters)
        {
            return value;
        }

        const string suffix = "\n[truncated by PromptMeUp]";
        return maximumCharacters <= suffix.Length
            ? value[..maximumCharacters]
            : value[..(maximumCharacters - suffix.Length)] + suffix;
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

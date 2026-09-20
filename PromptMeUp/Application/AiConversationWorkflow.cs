// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public interface IAiConversationWorkflow
{
    Task RunQueryAsync(
        string query,
        AppSettings settings,
        bool renderQuery,
        CancellationToken cancellationToken,
        string promptId = "query-system",
        CommandExecutionMode executionMode = CommandExecutionMode.Confirm);

    Task RunChatAsync(AppSettings settings, CancellationToken cancellationToken);

    Task RunConnectionTestAsync(AppSettings settings, CancellationToken cancellationToken);
}

public sealed class AiConversationWorkflow : IAiConversationWorkflow
{
    private static readonly JsonSerializerOptions MemoryJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
    private readonly IConversationMemoryService _memoryService;
    private readonly IOpenAiService _openAi;
    private readonly IPromptCatalogService _prompts;
    private readonly IPricingService _pricing;
    private readonly IActivityAuditService _audit;
    private readonly IAuthorizedCommandWorkflow _commandWorkflow;
    private readonly IChatView _chatView;
    private readonly ICommandSuggestionView _suggestionView;
    private readonly ICostsView _costsView;
    private readonly IConsoleShellView _shell;
    private readonly ILocalizationService _text;
    private readonly PersistentMemoryService _persistentMemory;
    private readonly IMemoryView _memoryView;
    private readonly IDatabaseService _database;
    private readonly IAppGuideService _appGuide;
    private readonly ILogger<AiConversationWorkflow> _logger;
    private readonly SkillCatalogService? _skills;
    private readonly ExperimentalStore? _experiments;
    private readonly ExperimentalWorkflow? _experimentalWorkflow;
    private readonly ReminderService? _reminders;

    /// <summary>Creates the focused query, chat, connection-test, and session-lifecycle workflow.</summary>
    public AiConversationWorkflow(
        IConversationMemoryService memoryService,
        IOpenAiService openAi,
        IPromptCatalogService prompts,
        IPricingService pricing,
        IActivityAuditService audit,
        IAuthorizedCommandWorkflow commandWorkflow,
        IChatView chatView,
        ICommandSuggestionView suggestionView,
        ICostsView costsView,
        IConsoleShellView shell,
        ILocalizationService text,
        PersistentMemoryService persistentMemory,
        IMemoryView memoryView,
        IDatabaseService database,
        IAppGuideService appGuide,
        ILogger<AiConversationWorkflow> logger,
        SkillCatalogService? skills = null,
        ExperimentalStore? experiments = null,
        ExperimentalWorkflow? experimentalWorkflow = null,
        ReminderService? reminders = null)
    {
        _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        _openAi = openAi ?? throw new ArgumentNullException(nameof(openAi));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _commandWorkflow = commandWorkflow ?? throw new ArgumentNullException(nameof(commandWorkflow));
        _chatView = chatView ?? throw new ArgumentNullException(nameof(chatView));
        _suggestionView = suggestionView ?? throw new ArgumentNullException(nameof(suggestionView));
        _costsView = costsView ?? throw new ArgumentNullException(nameof(costsView));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _persistentMemory = persistentMemory ?? throw new ArgumentNullException(nameof(persistentMemory));
        _memoryView = memoryView ?? throw new ArgumentNullException(nameof(memoryView));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _appGuide = appGuide ?? throw new ArgumentNullException(nameof(appGuide));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _skills = skills;
        _experiments = experiments;
        _experimentalWorkflow = experimentalWorkflow;
        _reminders = reminders;
    }

    /// <summary>Runs a single-turn session and offers a safe continuation into chat after the model response.</summary>
    public async Task RunQueryAsync(
        string query,
        AppSettings settings,
        bool renderQuery,
        CancellationToken cancellationToken,
        string promptId = "query-system",
        CommandExecutionMode executionMode = CommandExecutionMode.Confirm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(settings);
        var memory = new ConversationState(_memoryService.Create(settings), promptId) { ExecutionMode = executionMode };
        await using var session = await AuditSessionScope.StartAsync(
            _audit, promptId, settings, new { invocation = promptId, executionMode }, AuditSessionOutcome.Failed, cancellationToken).ConfigureAwait(false);
        try
        {
            if (executionMode == CommandExecutionMode.Direct)
            {
                _shell.RenderNotice(_text.Text("Direct.Active"));
            }
            if (renderQuery)
            {
                _chatView.RenderMemoryHint();
                _chatView.RenderUser(query);
            }
            var startChat = await SendAndOfferActionsAsync(
                session.Id,
                query,
                memory,
                settings,
                offerChatContinuation: IsInteractive,
                promptId,
                cancellationToken,
                captureObservation: promptId == "query-system").ConfigureAwait(false);
            if (startChat)
            {
                _chatView.RenderIntro(includeMemoryHints: !renderQuery);
                session.Outcome = await RunChatLoopAsync(session.Id, memory, settings, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                session.Outcome = AuditSessionOutcome.Completed;
            }
        }
        finally
        {
            await RenderFinalSnapshotAsync(session.Id, memory, settings).ConfigureAwait(false);
        }
    }

    /// <summary>Runs a short interactive session with slash commands and a mandatory command-authorization gate.</summary>
    public async Task RunChatAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var memory = new ConversationState(_memoryService.Create(settings), "chat-system")
        {
            ExecutionMode = settings.DirectModeEnabled ? CommandExecutionMode.Direct : CommandExecutionMode.Confirm
        };
        await using var session = await AuditSessionScope.StartAsync(
            _audit, "chat", settings, new { invocation = "chat" }, AuditSessionOutcome.Cancelled, cancellationToken).ConfigureAwait(false);
        try
        {
            _chatView.RenderIntro();
            if (memory.ExecutionMode == CommandExecutionMode.Direct)
            {
                _shell.RenderNotice(_text.Text("Direct.Active"));
            }
            if (_experiments is not null)
            {
                var experiments = await _experiments.SettingsAsync(cancellationToken).ConfigureAwait(false);
                var lastRun = await _experiments.GetAsync("last-heartbeat", cancellationToken).ConfigureAwait(false);
                if (experiments.Enabled && experiments.MaintenanceReminder
                    && (lastRun is null || !DateTimeOffset.TryParse(lastRun, out var last) || DateTimeOffset.UtcNow - last > TimeSpan.FromDays(7)))
                {
                    _shell.RenderNotice(_text.Text("Lab.Due"));
                }
            }
            session.Outcome = await RunChatLoopAsync(session.Id, memory, settings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await RenderFinalSnapshotAsync(session.Id, memory, settings).ConfigureAwait(false);
        }
    }

    /// <summary>Runs user chat turns until exit while keeping every command path behind the authorization workflow.</summary>
    private async Task<AuditSessionOutcome> RunChatLoopAsync(
        string sessionId,
        ConversationState memory,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (_reminders is not null)
                {
                    await _reminders.DeliverDueAsync(reminder =>
                        _shell.RenderNotice(_text.Text("Reminder.Due", reminder.DueAt.ToLocalTime()
                            .ToString("yyyy-MM-dd HH:mm zzz", System.Globalization.CultureInfo.InvariantCulture), reminder.Message)),
                        cancellationToken).ConfigureAwait(false);
                }
                var input = _chatView.ReadMessage(settings.MaxMessageCharacters, settings.PreferredName).Trim();
                if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                {
                    _shell.RenderMuted(_text.Text("Chat.Exit"));
                    return AuditSessionOutcome.Completed;
                }
                if (input.Equals("/clear", StringComparison.OrdinalIgnoreCase))
                {
                    memory.Memory.Clear();
                    memory.Guide = AppGuideContext.Empty;
                    memory.Envelope = new MemoryEnvelope(null, 0, 0);
                    await _audit.AppendSessionEventAsync(sessionId, "memory_cleared", new { }, cancellationToken).ConfigureAwait(false);
                    _shell.RenderMuted(_text.Text("Chat.Cleared"));
                    await RenderActiveSnapshotAsync(sessionId, memory, settings, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                if (input.Equals("/costs", StringComparison.OrdinalIgnoreCase))
                {
                    _costsView.Render(await _pricing.GetOverviewAsync(cancellationToken).ConfigureAwait(false));
                    continue;
                }
                if (input.Equals("/status", StringComparison.OrdinalIgnoreCase)
                    || input.Equals("/context", StringComparison.OrdinalIgnoreCase))
                {
                    await RenderActiveSnapshotAsync(sessionId, memory, settings, cancellationToken, force: true).ConfigureAwait(false);
                    continue;
                }
                if (await HandleExperimentalCommandAsync(input, settings, cancellationToken).ConfigureAwait(false)
                    || await HandleMemoryCommandAsync(input, cancellationToken).ConfigureAwait(false))
                {
                    await RenderActiveSnapshotAsync(sessionId, memory, settings, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                if (TryParseRunCommand(input, out var command))
                {
                    if (command.Length == 0)
                    {
                        _shell.RenderError(_text.Text("Chat.RunRequired"));
                        continue;
                    }

                    var commandFollowUp = await _commandWorkflow.RunAsync(
                        sessionId,
                        command,
                        settings,
                        cancellationToken,
                        memory.ExecutionMode).ConfigureAwait(false);
                    if (commandFollowUp is not null)
                    {
                        await SendAndOfferActionsAsync(
                            sessionId,
                            commandFollowUp,
                            memory,
                            settings,
                            offerChatContinuation: false,
                            "chat-system",
                            cancellationToken).ConfigureAwait(false);
                    }
                    continue;
                }
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                await SendAndOfferActionsAsync(
                    sessionId,
                    input,
                    memory,
                    settings,
                    offerChatContinuation: false,
                    "chat-system",
                    cancellationToken,
                    classifyDisplayIntent: true,
                    captureObservation: true).ConfigureAwait(false);
            }
            catch (ConversationLimitException)
            {
                _shell.RenderError(_text.Text("Chat.ContextLimit"));
            }
        }
    }

    /// <summary>Sends one turn and offers its actions while retaining completed-turn cost if a follow-up fails.</summary>
    private async Task<bool> SendAndOfferActionsAsync(
        string sessionId,
        string userText,
        ConversationState memory,
        AppSettings settings,
        bool offerChatContinuation,
        string promptId,
        CancellationToken cancellationToken,
        bool classifyDisplayIntent = false,
        bool captureObservation = false)
    {
        var response = await SendTurnAsync(
            sessionId, userText, memory, settings, promptId, cancellationToken, classifyDisplayIntent, captureObservation).ConfigureAwait(false);
        return await OfferSuggestedActionsAsync(
            sessionId, response, memory, settings, offerChatContinuation, promptId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Shares command selection, execution and result analysis while direct mode replaces single-command selection with a countdown.</summary>
    private async Task<bool> OfferSuggestedActionsAsync(
        string sessionId,
        AiResponse response,
        ConversationState memory,
        AppSettings settings,
        bool offerChatContinuation,
        string promptId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        if ((!IsInteractive && memory.ExecutionMode != CommandExecutionMode.Direct)
            || ((!memory.ShowCommandSuggestions || response.SuggestedCommands.Count == 0) && !offerChatContinuation))
        {
            return false;
        }

        var activeResponse = response;
        var canStartChat = offerChatContinuation;
        var directCommands = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<SuggestedCommand> suggestions = memory.ShowCommandSuggestions ? activeResponse.SuggestedCommands : [];
            if (suggestions.Count == 0 && !canStartChat)
            {
                return false;
            }

            await _audit.AppendSessionEventAsync(
                sessionId,
                "command_suggestions_presented",
                new { count = suggestions.Count, canStartChat },
                cancellationToken).ConfigureAwait(false);
            if (memory.ExecutionMode == CommandExecutionMode.Direct && suggestions.Count > 0 && directCommands >= 8)
            {
                _shell.RenderWarning(_text.Text("Direct.StepLimit"));
                return offerChatContinuation;
            }
            var selection = memory.ExecutionMode == CommandExecutionMode.Direct && suggestions.Count == 1
                ? new CommandSuggestionDecision(CommandSuggestionAction.SelectCommand, suggestions[0])
                : _suggestionView.Select(suggestions, canStartChat);
            switch (selection.Action)
            {
                case CommandSuggestionAction.DoNotExecute:
                    await _audit.AppendSessionEventAsync(
                        sessionId,
                        "command_suggestion_declined",
                        new { count = suggestions.Count },
                        cancellationToken).ConfigureAwait(false);
                    return false;
                case CommandSuggestionAction.StartChat when canStartChat:
                    await _audit.AppendSessionEventAsync(
                        sessionId,
                        "chat_continued_from_answer",
                        new { },
                        cancellationToken).ConfigureAwait(false);
                    return true;
                case CommandSuggestionAction.SelectCommand when selection.SuggestedCommand is not null:
                    var command = ResolveSuggestedCommand(suggestions, selection.SuggestedCommand);
                    await _audit.AppendSessionEventAsync(
                        sessionId,
                        "command_suggestion_selected",
                        new { command.Label },
                        cancellationToken).ConfigureAwait(false);
                    var followUp = await _commandWorkflow.RunAsync(
                        sessionId,
                        command.Command,
                        settings,
                        cancellationToken,
                        memory.ExecutionMode).ConfigureAwait(false);
                    if (followUp is null)
                    {
                        return false;
                    }
                    directCommands++;

                    activeResponse = await SendTurnAsync(
                        sessionId,
                        followUp,
                        memory,
                        settings,
                        promptId,
                        cancellationToken).ConfigureAwait(false);
                    continue;
                default:
                    throw new InvalidOperationException("The command suggestion view returned an unsupported action.");
            }
        }
    }

    /// <summary>Confirms that a view decision refers to a command emitted by the current parsed model response.</summary>
    private static SuggestedCommand ResolveSuggestedCommand(
        IReadOnlyList<SuggestedCommand> suggestions,
        SuggestedCommand selected) => suggestions.SingleOrDefault(candidate =>
            string.Equals(candidate.Label, selected.Label, StringComparison.Ordinal)
            && string.Equals(candidate.Command, selected.Command, StringComparison.Ordinal))
        ?? throw new InvalidOperationException("The selected command was not part of the current model response.");

    /// <summary>Recognizes the exact or argument-bearing run directive without accepting similarly prefixed prompts.</summary>
    internal static bool TryParseRunCommand(string input, out string command)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Equals("/run", StringComparison.OrdinalIgnoreCase))
        {
            command = string.Empty;
            return true;
        }

        if (input.StartsWith("/run ", StringComparison.OrdinalIgnoreCase))
        {
            command = input[4..].Trim();
            return true;
        }

        command = string.Empty;
        return false;
    }

    /// <summary>Runs the YAML diagnostic prompt and renders its localized response status.</summary>
    public async Task RunConnectionTestAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _shell.RenderSectionTitle(_text.Text("Test.Title"));
        var prompt = await _prompts.GetAsync("connection-test", cancellationToken).ConfigureAwait(false);
        _chatView.RenderUser(prompt.ResolveText(_text.Language));
        var result = await _shell.RunWithStatusAsync(
            _text.Text("Status.Thinking"),
            () => _openAi.TestConnectionAsync(settings, _text.Language, cancellationToken)).ConfigureAwait(false);
        _chatView.RenderAssistant(result.Response.Text, animate: true, cancellationToken);
        _shell.RenderSuccess(_text.Text("Test.Success", result.Response.ElapsedMilliseconds));
        RenderTurnSnapshot(result.Response, settings, result.Response.EstimatedCostUsd ?? 0m);
    }

    /// <summary>Adds one bounded user turn, calls OpenAI, renders the parsed answer, and updates the session snapshot.</summary>
    private async Task<AiResponse> SendTurnAsync(
        string sessionId,
        string userText,
        ConversationState memory,
        AppSettings settings,
        string promptId,
        CancellationToken cancellationToken,
        bool classifyDisplayIntent = false,
        bool captureObservation = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        var captureRevision = captureObservation && _experiments is not null
            ? await _experiments.RevisionAsync(cancellationToken).ConfigureAwait(false) : null;
        if (userText.Length > settings.MaxMessageCharacters)
        {
            throw new ConversationLimitException(_text.Text("Chat.InputTooLong", settings.MaxMessageCharacters));
        }
        var envelope = await PrepareMemoryAsync(
            sessionId, memory, settings, promptId, userText, cancellationToken, pendingMessage: userText).ConfigureAwait(false);
        if (envelope.Skills.Count > 0)
        {
            _shell.RenderActiveSkills(envelope.Skills);
        }
        foreach (var warning in envelope.SkillWarnings)
        {
            _shell.RenderWarning(warning);
        }
        var update = memory.Memory.Add("user", userText);
        await AuditPruningAsync(sessionId, update.PrunedMessages, cancellationToken).ConfigureAwait(false);
        var response = await _shell.RunWithStatusAsync(
            _text.Text("Status.Thinking"),
            () => SendWithDisplayIntentAsync(sessionId, memory, settings, promptId, envelope, userText,
                classifyDisplayIntent, cancellationToken)).ConfigureAwait(false);
        memory.LastResponse = response;
        var assistantUpdate = memory.Memory.Add("assistant", response.Text);
        _chatView.RenderAssistant(response.Text, animate: true, cancellationToken);
        if (captureObservation && _experiments is not null)
        {
            await _experiments.CaptureAsync(sessionId, userText, cancellationToken, captureRevision).ConfigureAwait(false);
        }
        await AuditPruningAsync(sessionId, assistantUpdate.PrunedMessages, cancellationToken).ConfigureAwait(false);
        await RenderActiveSnapshotAsync(sessionId, memory, settings, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <summary>Classifies only directly typed chat requests and accounts for the bounded display call before any answer.</summary>
    private async Task<AiResponse> SendWithDisplayIntentAsync(
        string sessionId,
        ConversationState memory,
        AppSettings settings,
        string promptId,
        MemoryEnvelope envelope,
        string userText,
        bool classifyDisplayIntent,
        CancellationToken cancellationToken)
    {
        if (!classifyDisplayIntent)
        {
            return await SendWithGuideAsync(sessionId, memory, settings, promptId, envelope, userText,
                cancellationToken).ConfigureAwait(false);
        }

        // Recalled notes, earlier answers, and generated command output cannot request display changes.
        var classification = await _openAi.SendAsync("chat-display-intent", sessionId,
            [new ChatMessage("user", userText)], settings with { ReasoningEffort = "low", OutputDetail = "compact" },
            _text.Language, cancellationToken).ConfigureAwait(false);
        memory.LastResponse = classification with { TurnCostUsd = classification.EstimatedCostUsd };
        var intent = ChatDisplayIntentParser.Parse(classification.Text, classification.HttpStatusCode);
        var response = classification with { TurnCostUsd = classification.EstimatedCostUsd };
        if (intent.ContinueChat)
        {
            var answer = await SendWithGuideAsync(sessionId, memory, settings, promptId, envelope, userText,
                cancellationToken).ConfigureAwait(false);
            response = answer with
            {
                RequestCount = classification.RequestCount + answer.RequestCount,
                TurnCostUsd = classification.EstimatedCostUsd.HasValue && answer.TurnCostUsd.HasValue
                    ? classification.EstimatedCostUsd.Value + answer.TurnCostUsd.Value
                    : null
            };
        }

        // Apply mixed requests only after the answer succeeds, so a failed turn cannot silently change visibility.
        var confirmation = await ApplyDisplayIntentAsync(sessionId, memory, intent, cancellationToken).ConfigureAwait(false);
        return response with
        {
            Text = !intent.ContinueChat ? confirmation
                : confirmation.Length == 0 ? response.Text : $"{confirmation}\n\n{response.Text}"
        };
    }

    /// <summary>Applies validated session-only visibility changes and confirms them with local translated text.</summary>
    private async Task<string> ApplyDisplayIntentAsync(
        string sessionId,
        ConversationState memory,
        ChatDisplayIntent intent,
        CancellationToken cancellationToken)
    {
        if (!intent.HasChanges)
        {
            return string.Empty;
        }

        await _audit.AppendSessionEventAsync(sessionId, "chat_display_changed", new
        {
            showSessionSummary = intent.ShowSessionSummary ?? memory.ShowSessionSummary,
            showCommandSuggestions = intent.ShowCommandSuggestions ?? memory.ShowCommandSuggestions
        }, cancellationToken).ConfigureAwait(false);
        var confirmations = new List<string>();
        if (intent.ShowSessionSummary is { } showSummary)
        {
            memory.ShowSessionSummary = showSummary;
            confirmations.Add(_text.Text(showSummary ? "Chat.SessionSummaryShown" : "Chat.SessionSummaryHidden"));
        }
        if (intent.ShowCommandSuggestions is { } showCommands)
        {
            memory.ShowCommandSuggestions = showCommands;
            confirmations.Add(_text.Text(showCommands ? "Chat.CommandSuggestionsShown" : "Chat.CommandSuggestionsHidden"));
        }
        var icon = TerminalTheme.IconPrefix(_shell.Options, "✅", "+");
        return string.Join("\n\n", confirmations.Select(confirmation => icon + confirmation));
    }

    /// <summary>Resolves at most one bounded guide request per turn and retains each completed call's cost immediately.</summary>
    private async Task<AiResponse> SendWithGuideAsync(
        string sessionId,
        ConversationState memory,
        AppSettings settings,
        string promptId,
        MemoryEnvelope envelope,
        string userText,
        CancellationToken cancellationToken)
    {
        var first = await _openAi.SendAsync(promptId, sessionId,
            CombineMessages(envelope, memory.Memory.Snapshot().Messages), settings, _text.Language,
            cancellationToken, memory.Guide).ConfigureAwait(false);
        memory.LastResponse = first with { TurnCostUsd = first.EstimatedCostUsd };
        if (first.GuideTopics.Count == 0)
        {
            return first with { TurnCostUsd = first.EstimatedCostUsd };
        }

        if (promptId is not ("chat-system" or "query-system")
            || first.GuideTopics.All(topic => memory.Guide.Topics.Contains(topic, StringComparer.Ordinal)))
        {
            throw new OpenAiRequestException("The AI requested an unavailable or already supplied app guide.",
                "invalid_app_guide_request", first.HttpStatusCode);
        }

        var combinedTopics = memory.Guide.Topics.Concat(first.GuideTopics).Distinct(StringComparer.Ordinal).ToArray();
        var topics = combinedTopics.Length <= AppGuideService.MaxTopics ? combinedTopics : first.GuideTopics;
        var previousGuide = memory.Guide;
        var guide = await _appGuide.LoadAsync(topics, _text.Language, cancellationToken).ConfigureAwait(false);
        memory.Guide = guide;
        try
        {
            // Rebudget before sending the chapters, keeping the current question while dropping complete older turns.
            envelope = await PrepareMemoryAsync(sessionId, memory, settings, promptId, userText,
                cancellationToken, pendingMessage: userText).ConfigureAwait(false);
        }
        catch
        {
            memory.Guide = previousGuide;
            throw;
        }

        await _audit.AppendSessionEventAsync(sessionId, "app_guide_loaded",
            new { topics = guide.Topics, estimatedSystemTokens = guide.Tokens }, cancellationToken).ConfigureAwait(false);
        var final = await _openAi.SendAsync(promptId, sessionId,
            CombineMessages(envelope, memory.Memory.Snapshot().Messages), settings, _text.Language,
            cancellationToken, guide).ConfigureAwait(false);
        var completed = final with
        {
            RequestCount = 2,
            TurnCostUsd = first.EstimatedCostUsd.HasValue && final.EstimatedCostUsd.HasValue
                ? first.EstimatedCostUsd.Value + final.EstimatedCostUsd.Value
                : null
        };
        memory.LastResponse = completed;
        if (final.GuideTopics.Count != 0)
        {
            throw new OpenAiRequestException("The AI requested another guide instead of completing its answer.",
                "app_guide_round_limit", final.HttpStatusCode);
        }
        return completed;
    }

    /// <summary>Renders provider-confirmed context, input, output, costs, and cache counters after every completed AI response.</summary>
    private void RenderTurnSnapshot(AiResponse response, AppSettings settings, decimal runningCost)
    {
        _shell.RenderRuntimeStatus(CreateTurnSnapshot(response, settings, runningCost));
    }

    /// <summary>Builds the immutable provider-confirmed session snapshot shown after a query, chat turn, or connection test.</summary>
    internal static ShellRuntimeStatus CreateTurnSnapshot(AiResponse response, AppSettings settings, decimal runningCost)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(settings);
        decimal? promptCost = response.CostBreakdown is null
            ? null
            : response.CostBreakdown.InputUsd + response.CostBreakdown.CachedInputUsd + response.CostBreakdown.CacheWriteUsd;
        return new ShellRuntimeStatus(
            "OpenAI",
            response.Model,
            settings.ReasoningEffort,
            promptCost,
            response.CostBreakdown?.OutputUsd,
            runningCost,
            response.ContextUsage.InputTokens + response.ContextUsage.OutputTokens,
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.ContextUsage.ContextWindowTokens,
            false,
            response.Usage.CachedInputTokens,
            response.Usage.CacheWriteTokens)
        {
            TurnCostUsd = response.RequestCount > 1 ? response.TurnCostUsd : response.EstimatedCostUsd,
            HasTurnCost = true
        };
    }

    /// <summary>Records context-pruning activity only when the active memory actually changed.</summary>
    private async Task AuditPruningAsync(string sessionId, int prunedMessages, CancellationToken cancellationToken)
    {
        if (prunedMessages <= 0)
        {
            return;
        }

        _chatView.RenderMemoryPruned(prunedMessages);
        await _audit.AppendSessionEventAsync(
            sessionId,
            "memory_pruned",
            new { prunedMessages },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Keeps explicit experiment administration out of chat context and automatic provider requests.</summary>
    private async Task<bool> HandleExperimentalCommandAsync(string input, AppSettings settings, CancellationToken ct)
    {
        var parts = input.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts.FirstOrDefault()?.ToLowerInvariant() switch
        {
            "/skills" => AppCommand.Skills,
            "/learning" => AppCommand.Learning,
            "/proposals" => AppCommand.Proposals,
            "/dream" => AppCommand.Dream,
            "/heartbeat" => AppCommand.Heartbeat,
            _ => (AppCommand?)null
        };
        if (command is null)
        {
            return false;
        }
        if (parts.Length != 1 || _experimentalWorkflow is null)
        {
            _shell.RenderError(_text.Text("Lab.Invalid"));
            return true;
        }
        try
        {
            await _experimentalWorkflow.RunAsync(command.Value, settings, ct).ConfigureAwait(false);
        }
        catch (InteractiveFlowCanceledException) when (!ct.IsCancellationRequested)
        {
            _shell.RenderNotice(_text.Text("Common.Cancelled"));
        }
        catch (InvalidOperationException exception)
        {
            _shell.RenderError(exception.Message);
        }
        return true;
    }

    /// <summary>Handles explicit note commands locally so neither note administration nor invalid syntax reaches the provider.</summary>
    private async Task<bool> HandleMemoryCommandAsync(string input, CancellationToken cancellationToken)
    {
        var parts = input.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }
        var command = parts[0].ToLowerInvariant();
        if (command is not ("/remember" or "/memories" or "/forget"))
        {
            return false;
        }
        try
        {
            switch (command)
            {
                case "/memories" when parts.Length == 1:
                    _memoryView.Render(await _persistentMemory.ListAsync(cancellationToken).ConfigureAwait(false));
                    break;
                case "/remember" when parts.Length == 2:
                    var note = parts[1].Trim();
                    var scope = note.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (string.Equals(scope.FirstOrDefault(), "global", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(scope.FirstOrDefault(), "project", StringComparison.OrdinalIgnoreCase))
                    {
                        note = scope.Length == 2 ? scope[1].Trim() : string.Empty;
                    }
                    var saved = await _persistentMemory.RememberAsync(note, true, cancellationToken).ConfigureAwait(false);
                    _shell.RenderSuccess(_text.Text("Memory.Saved", saved.Id));
                    break;
                case "/forget" when parts.Length == 2:
                    if (_experimentalWorkflow is not null && !_experimentalWorkflow.ConfirmMemoryForget())
                    {
                        _shell.RenderNotice(_text.Text("Common.Cancelled"));
                        break;
                    }
                    var removed = await _persistentMemory.ForgetAsync(parts[1].Trim(), cancellationToken).ConfigureAwait(false);
                    if (removed)
                    {
                        _shell.RenderSuccess(_text.Text("Memory.Forgotten"));
                    }
                    else
                    {
                        _shell.RenderWarning(_text.Text("Memory.NotFound"));
                    }
                    break;
                default:
                    _shell.RenderError(_text.Text("Memory.Syntax"));
                    break;
            }
        }
        catch (MemoryValidationException exception)
        {
            _shell.RenderError(exception.Message);
        }
        return true;
    }

    /// <summary>Loads selected notes once into a bounded low-trust YAML envelope without adding them to conversation history.</summary>
    private async Task<MemoryEnvelope> LoadMemoryEnvelopeAsync(string query, CancellationToken cancellationToken)
    {
        var selection = await _persistentMemory.SelectAsync(query, cancellationToken).ConfigureAwait(false);
        var notes = selection.Memories.ToList();
        var template = notes.Count == 0 ? string.Empty
            : (await _prompts.GetAsync("memory-context", cancellationToken).ConfigureAwait(false)).ResolveText(_text.Language);
        var memoryText = string.Empty;
        while (notes.Count > 0)
        {
            var payload = JsonSerializer.Serialize(notes.Select(note => note.Text), MemoryJsonOptions);
            var message = new ChatMessage("user", template.Replace("{memories}", payload, StringComparison.Ordinal));
            var tokens = ContextTokenEstimator.Messages([message]);
            if (tokens <= 800)
            {
                memoryText = message.Content;
                break;
            }
            notes.RemoveAt(notes.Count - 1);
        }
        var skillWarnings = new List<string>();
        var selectedSkills = _skills is null ? [] : await _skills.SelectAsync(query, cancellationToken, skillWarnings).ConfigureAwait(false);
        var combined = memoryText;
        if (selectedSkills.Count > 0)
        {
            combined += "\n\n" + await _skills!.ContextAsync(selectedSkills, cancellationToken).ConfigureAwait(false);
        }
        if (string.IsNullOrWhiteSpace(combined))
        {
            return new MemoryEnvelope(null, 0, 0) { SkillWarnings = skillWarnings };
        }
        var envelopeMessage = new ChatMessage("user", combined.Trim());
        var envelopeTokens = ContextTokenEstimator.Messages([envelopeMessage]);
        if (envelopeTokens > 3200)
        {
            throw new ConversationLimitException(_text.Text("Chat.ContextLimit"));
        }
        return new MemoryEnvelope(envelopeMessage, notes.Count, envelopeTokens) { Skills = selectedSkills, SkillWarnings = skillWarnings };
    }

    /// <summary>Reserves actual populated instructions and recalled notes before pruning the recent-turn window.</summary>
    private async Task<MemoryEnvelope> PrepareMemoryAsync(
        string sessionId,
        ConversationState memory,
        AppSettings settings,
        string promptId,
        string query,
        CancellationToken cancellationToken,
        string? pendingMessage = null)
    {
        var envelope = await LoadMemoryEnvelopeAsync(query, cancellationToken).ConfigureAwait(false);
        var baseline = await _openAi.EstimateContextAsync(
            promptId, CombineMessages(envelope, []), settings, _text.Language, cancellationToken, memory.Guide).ConfigureAwait(false);
        var pendingTokens = pendingMessage is null
            ? 0
            : ContextTokenEstimator.Messages([new ChatMessage("user", pendingMessage)]);
        if (baseline.InputTokens + pendingTokens > baseline.InputBudgetTokens)
        {
            throw new ConversationLimitException(_text.Text("Input.ContextBudget",
                baseline.InputTokens + pendingTokens, baseline.InputBudgetTokens, baseline.ReservedOutputTokens));
        }
        var update = memory.Memory.SetTokenBudget(baseline.InputBudgetTokens - baseline.InputTokens);
        await AuditPruningAsync(sessionId, update.PrunedMessages, cancellationToken).ConfigureAwait(false);
        memory.PromptId = promptId;
        memory.Envelope = envelope;
        return envelope;
    }

    /// <summary>Applies visibility policy to a fresh snapshot without changing conversation memory.</summary>
    private async Task RenderActiveSnapshotAsync(
        string sessionId,
        ConversationState memory,
        AppSettings settings,
        CancellationToken cancellationToken,
        bool force = false)
    {
        var snapshot = await CreateSessionSnapshotAsync(sessionId, memory, settings, cancellationToken).ConfigureAwait(false);
        if (memory.ShowSessionSummary || force || IsContextWarning(snapshot))
        {
            _shell.RenderRuntimeStatus(snapshot);
        }
    }

    /// <summary>Combines current local context with one authoritative ledger read, including failed calls and risk reviews.</summary>
    private async Task<ShellRuntimeStatus> CreateSessionSnapshotAsync(
        string sessionId, ConversationState memory, AppSettings settings, CancellationToken cancellationToken)
    {
        var context = await _openAi.EstimateContextAsync(
            memory.PromptId, CombineMessages(memory.Envelope, memory.Memory.Snapshot().Messages), settings, _text.Language,
            cancellationToken, memory.Guide).ConfigureAwait(false);
        var accounting = await _database.GetSessionAccountingAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var status = memory.LastResponse is null
            ? ShellRuntimeStatus.FromSettings(settings)
            : CreateTurnSnapshot(memory.LastResponse, settings, accounting.EstimatedCostUsd ?? 0m);
        return status with
        {
            RunningCostUsd = accounting.EstimatedCostUsd ?? 0m,
            SessionCostKnown = accounting.EstimatedCostUsd.HasValue,
            ActiveContextTokens = context.InputTokens,
            ContextWindowTokens = context.ContextWindowTokens,
            ContextBudgetTokens = context.InputBudgetTokens,
            SystemInstructionTokens = context.SystemInstructionTokens,
            GuideTokens = context.GuideTokens,
            UserMessageTokens = context.UserMessageTokens,
            AssistantMessageTokens = context.AssistantMessageTokens,
            HasContextBreakdown = true,
            MemoryTokens = memory.Envelope.Tokens,
            MemoryCount = memory.Envelope.Count,
            SessionInputTokens = accounting.Usage.InputTokens,
            SessionOutputTokens = accounting.Usage.OutputTokens,
            HasSessionUsage = true
        };
    }

    /// <summary>Overrides hidden summaries at eighty percent of the effective operating context budget.</summary>
    internal static bool IsContextWarning(ShellRuntimeStatus status) => status.ContextBudgetTokens > 0
        && status.ActiveContextTokens is { } used && (decimal)used * 100 >= (decimal)status.ContextBudgetTokens * 80;

    /// <summary>Refreshes local totals at exit with a bounded shutdown-independent read and never hides the original flow failure.</summary>
    private async Task RenderFinalSnapshotAsync(string sessionId, ConversationState memory, AppSettings settings)
    {
        ShellRuntimeStatus snapshot;
        using var finalRead = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            snapshot = await CreateSessionSnapshotAsync(sessionId, memory, settings, finalRead.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Final session snapshot unavailable. ErrorType={ErrorType}", exception.GetType().Name);
            _shell.RenderWarning(_text.Text("Chat.SessionSummaryUnavailable"));
            snapshot = ShellRuntimeStatus.FromSettings(settings) with { SessionCostKnown = false };
        }
        _shell.RenderRuntimeStatus(snapshot);
    }

    /// <summary>Prepends recalled data exactly once while keeping the latest user message last for provider accounting.</summary>
    private static IReadOnlyList<ChatMessage> CombineMessages(MemoryEnvelope envelope, IReadOnlyList<ChatMessage> messages) =>
        envelope.Message is null ? messages : [envelope.Message, .. messages];

    private sealed class ConversationState(ConversationMemory memory, string promptId)
    {
        public ConversationMemory Memory { get; } = memory;

        public CommandExecutionMode ExecutionMode { get; init; }

        public AiResponse? LastResponse { get; set; }

        public string PromptId { get; set; } = promptId;

        public AppGuideContext Guide { get; set; } = AppGuideContext.Empty;

        public MemoryEnvelope Envelope { get; set; } = new(null, 0, 0);

        public bool ShowSessionSummary { get; set; }

        public bool ShowCommandSuggestions { get; set; } = true;
    }

    private sealed record MemoryEnvelope(ChatMessage? Message, int Count, long Tokens)
    {
        public IReadOnlyList<SkillDefinition> Skills { get; init; } = [];
        public IReadOnlyList<string> SkillWarnings { get; init; } = [];
    }

    private static bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;
}

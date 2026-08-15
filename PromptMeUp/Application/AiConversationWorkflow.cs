// SPDX-License-Identifier: MIT

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
        CancellationToken cancellationToken);

    Task RunChatAsync(AppSettings settings, CancellationToken cancellationToken);

    Task RunConnectionTestAsync(AppSettings settings, CancellationToken cancellationToken);
}

public sealed class AiConversationWorkflow : IAiConversationWorkflow
{
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
        ILocalizationService text)
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
    }

    /// <summary>Runs a single-turn session and offers a safe continuation into chat after the model response.</summary>
    public async Task RunQueryAsync(
        string query,
        AppSettings settings,
        bool renderQuery,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(settings);
        var sessionId = Guid.NewGuid().ToString("N");
        var memory = _memoryService.Create(settings);
        await _audit.StartSessionAsync(sessionId, "query", settings, new { invocation = "query" }, cancellationToken).ConfigureAwait(false);
        var status = "failed";
        var runningCost = 0m;
        try
        {
            if (renderQuery)
            {
                _chatView.RenderUser(query);
            }
            var turn = await SendTurnAsync(
                sessionId,
                query,
                memory,
                settings,
                runningCost,
                "query-system",
                cancellationToken).ConfigureAwait(false);
            runningCost += turn.Cost;
            var action = await OfferSuggestedActionsAsync(
                sessionId,
                turn.Response,
                memory,
                settings,
                runningCost,
                offerChatContinuation: IsInteractive,
                promptId: "query-system",
                cancellationToken: cancellationToken).ConfigureAwait(false);
            runningCost = action.RunningCost;
            if (action.StartChat)
            {
                _chatView.RenderIntro();
                status = await RunChatLoopAsync(sessionId, memory, settings, runningCost, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                status = "completed";
            }
        }
        finally
        {
            await _audit.CloseSessionAsync(sessionId, status, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Runs a short interactive session with slash commands and a mandatory command-authorization gate.</summary>
    public async Task RunChatAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var sessionId = Guid.NewGuid().ToString("N");
        var memory = _memoryService.Create(settings);
        var runningCost = 0m;
        var status = "cancelled";
        await _audit.StartSessionAsync(sessionId, "chat", settings, new { invocation = "chat" }, cancellationToken).ConfigureAwait(false);
        _chatView.RenderIntro();
        try
        {
            status = await RunChatLoopAsync(sessionId, memory, settings, runningCost, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _audit.CloseSessionAsync(sessionId, status, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Runs user chat turns until exit while keeping every command path behind the authorization workflow.</summary>
    private async Task<string> RunChatLoopAsync(
        string sessionId,
        ConversationMemory memory,
        AppSettings settings,
        decimal initialRunningCost,
        CancellationToken cancellationToken)
    {
        var runningCost = initialRunningCost;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var input = _chatView.ReadMessage().Trim();
            if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                _shell.RenderMuted(_text.Text("Chat.Exit"));
                return "completed";
            }
            if (input.Equals("/clear", StringComparison.OrdinalIgnoreCase))
            {
                memory.Clear();
                await _audit.AppendSessionEventAsync(sessionId, "memory_cleared", new { }, cancellationToken).ConfigureAwait(false);
                _shell.RenderMuted(_text.Text("Chat.Cleared"));
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
                    cancellationToken).ConfigureAwait(false);
                if (commandFollowUp is not null)
                {
                    var turn = await SendTurnAsync(
                        sessionId,
                        commandFollowUp,
                        memory,
                        settings,
                        runningCost,
                        "chat-system",
                        cancellationToken).ConfigureAwait(false);
                    runningCost += turn.Cost;
                    var action = await OfferSuggestedActionsAsync(
                        sessionId,
                        turn.Response,
                        memory,
                        settings,
                        runningCost,
                        offerChatContinuation: false,
                        promptId: "chat-system",
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    runningCost = action.RunningCost;
                }
                continue;
            }
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var userTurn = await SendTurnAsync(
                sessionId,
                input,
                memory,
                settings,
                runningCost,
                "chat-system",
                cancellationToken).ConfigureAwait(false);
            runningCost += userTurn.Cost;
            var selectedAction = await OfferSuggestedActionsAsync(
                sessionId,
                userTurn.Response,
                memory,
                settings,
                runningCost,
                offerChatContinuation: false,
                promptId: "chat-system",
                cancellationToken: cancellationToken).ConfigureAwait(false);
            runningCost = selectedAction.RunningCost;
        }
    }

    /// <summary>Presents model-suggested commands as inert choices and optionally converts a one-shot answer into chat.</summary>
    private async Task<PostResponseAction> OfferSuggestedActionsAsync(
        string sessionId,
        AiResponse response,
        ConversationMemory memory,
        AppSettings settings,
        decimal runningCost,
        bool offerChatContinuation,
        string promptId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        if (!IsInteractive || (response.SuggestedCommands.Count == 0 && !offerChatContinuation))
        {
            return new PostResponseAction(false, runningCost);
        }

        var activeResponse = response;
        var canStartChat = offerChatContinuation;
        while (true)
        {
            if (activeResponse.SuggestedCommands.Count == 0 && !canStartChat)
            {
                return new PostResponseAction(false, runningCost);
            }

            await _audit.AppendSessionEventAsync(
                sessionId,
                "command_suggestions_presented",
                new { count = activeResponse.SuggestedCommands.Count, canStartChat },
                cancellationToken).ConfigureAwait(false);
            var selection = _suggestionView.Select(activeResponse.SuggestedCommands, canStartChat);
            switch (selection.Action)
            {
                case CommandSuggestionAction.DoNotExecute:
                    await _audit.AppendSessionEventAsync(
                        sessionId,
                        "command_suggestion_declined",
                        new { count = activeResponse.SuggestedCommands.Count },
                        cancellationToken).ConfigureAwait(false);
                    return new PostResponseAction(false, runningCost);
                case CommandSuggestionAction.StartChat when canStartChat:
                    await _audit.AppendSessionEventAsync(
                        sessionId,
                        "chat_continued_from_answer",
                        new { },
                        cancellationToken).ConfigureAwait(false);
                    return new PostResponseAction(true, runningCost);
                case CommandSuggestionAction.SelectCommand when selection.SuggestedCommand is not null:
                    var command = ResolveSuggestedCommand(activeResponse.SuggestedCommands, selection.SuggestedCommand);
                    await _audit.AppendSessionEventAsync(
                        sessionId,
                        "command_suggestion_selected",
                        new { command.Label },
                        cancellationToken).ConfigureAwait(false);
                    var followUp = await _commandWorkflow.RunAsync(
                        sessionId,
                        command.Command,
                        settings,
                        cancellationToken).ConfigureAwait(false);
                    if (followUp is null)
                    {
                        return new PostResponseAction(false, runningCost);
                    }

                    var nextTurn = await SendTurnAsync(
                        sessionId,
                        followUp,
                        memory,
                        settings,
                        runningCost,
                        promptId,
                        cancellationToken).ConfigureAwait(false);
                    runningCost += nextTurn.Cost;
                    activeResponse = nextTurn.Response;
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
    private async Task<TurnResult> SendTurnAsync(
        string sessionId,
        string userText,
        ConversationMemory memory,
        AppSettings settings,
        decimal runningCost,
        string promptId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        var update = memory.Add("user", userText);
        await AuditPruningAsync(sessionId, update.PrunedMessages, cancellationToken).ConfigureAwait(false);
        var response = await _shell.RunWithStatusAsync(
            _text.Text("Status.Thinking"),
            () => _openAi.SendAsync(
                promptId,
                sessionId,
                update.Snapshot.Messages,
                settings,
                _text.Language,
                cancellationToken)).ConfigureAwait(false);
        var assistantUpdate = memory.Add("assistant", response.Text);
        await AuditPruningAsync(sessionId, assistantUpdate.PrunedMessages, cancellationToken).ConfigureAwait(false);
        _chatView.RenderAssistant(response.Text, animate: false, cancellationToken);
        var turnCost = response.EstimatedCostUsd ?? 0m;
        RenderTurnSnapshot(response, settings, runningCost + turnCost);
        return new TurnResult(response, turnCost);
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
            response.Usage.CacheWriteTokens);
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

    private sealed record TurnResult(AiResponse Response, decimal Cost);

    private sealed record PostResponseAction(bool StartChat, decimal RunningCost);

    private static bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;
}

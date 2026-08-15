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
        _costsView = costsView ?? throw new ArgumentNullException(nameof(costsView));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Runs a single-turn session and closes its ledger after the model response.</summary>
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
        try
        {
            if (renderQuery)
            {
                _chatView.RenderUser(query);
            }
            await SendTurnAsync(sessionId, query, memory, settings, 0m, cancellationToken).ConfigureAwait(false);
            status = "completed";
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
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = _chatView.ReadMessage().Trim();
                if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                {
                    status = "completed";
                    _shell.RenderMuted(_text.Text("Chat.Exit"));
                    break;
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
                        runningCost += await SendTurnAsync(
                            sessionId,
                            commandFollowUp,
                            memory,
                            settings,
                            runningCost,
                            cancellationToken).ConfigureAwait(false);
                    }
                    continue;
                }
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                runningCost += await SendTurnAsync(
                    sessionId,
                    input,
                    memory,
                    settings,
                    runningCost,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await _audit.CloseSessionAsync(sessionId, status, CancellationToken.None).ConfigureAwait(false);
        }
    }

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
}

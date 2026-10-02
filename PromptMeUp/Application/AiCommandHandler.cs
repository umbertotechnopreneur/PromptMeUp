// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Application;

/// <summary>Runs AI conversations, artifacts, and diagnostics after their prerequisites are checked.</summary>
internal sealed class AiCommandHandler(
    IAiConversationWorkflow conversationWorkflow,
    DiagnosticWorkflow diagnostics,
    ScriptWorkflow scripts,
    PlanWorkflow plans,
    FilePreviewWorkflow filePreview,
    IEnvironmentSecretService secrets,
    ILocalizationService text)
{
    /// <summary>Runs one AI-related command with the same interaction and credential rules as the CLI.</summary>
    internal async Task<int> RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        switch (options.Command)
        {
            case AppCommand.Preview:
                return await filePreview.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.Plan:
                CommandPreconditions.RequireInteractive(text);
                if (options.ResumeId is null)
                {
                    CommandPreconditions.RequireAiReady(settings, secrets, text);
                }
                return await plans.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.Script:
                CommandPreconditions.RequireInteractive(text);
                CommandPreconditions.RequireAiReady(settings, secrets, text);
                await scripts.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Diagnose:
                CommandPreconditions.RequireAiReady(settings, secrets, text);
                await diagnostics.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Query:
            case AppCommand.Direct:
                if (options.Command == AppCommand.Direct || settings.DirectModeEnabled)
                {
                    CommandPreconditions.RequireInteractive(text);
                }
                CommandPreconditions.RequireAiReady(settings, secrets, text);
                await conversationWorkflow.RunQueryAsync(
                    options.Query!,
                    settings,
                    renderQuery: true,
                    cancellationToken,
                    executionMode: options.Command == AppCommand.Direct || settings.DirectModeEnabled
                        ? CommandExecutionMode.Direct : CommandExecutionMode.Confirm,
                    directModeOverride: options.Command == AppCommand.Direct).ConfigureAwait(false);
                return 0;
            case AppCommand.Chat:
                CommandPreconditions.RequireInteractive(text);
                CommandPreconditions.RequireAiReady(settings, secrets, text);
                await conversationWorkflow.RunChatAsync(settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.TestAi:
                CommandPreconditions.RequireAiReady(settings, secrets, text);
                await conversationWorkflow.RunConnectionTestAsync(settings, cancellationToken).ConfigureAwait(false);
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), "Unsupported AI command.");
        }
    }
}

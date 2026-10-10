// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Application;

/// <summary>Runs global skills, learning, and persistent-memory commands.</summary>
internal sealed class MemoryCommandHandler(
    SkillsAndMemoryWorkflow skillsAndMemory,
    MemoryCommandWorkflow memoryCommands,
    MemoryManagerWorkflow memories,
    ILocalizationService text)
{
    /// <summary>Runs one global skills or memory command with its terminal requirements.</summary>
    internal async Task<int> RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        switch (options.Command)
        {
            case AppCommand.Skills:
            case AppCommand.Learning:
            case AppCommand.Dream:
            case AppCommand.Heartbeat:
                CommandPreconditions.RequireInteractive(text);
                await skillsAndMemory.RunAsync(options.Command, settings, cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Remember:
            case AppCommand.Forget:
                if (options.Command == AppCommand.Forget)
                {
                    CommandPreconditions.RequireInteractive(text);
                }
                return await memoryCommands.RunAsync(options, settings, cancellationToken).ConfigureAwait(false);
            case AppCommand.Memories:
                CommandPreconditions.RequireInteractive(text);
                await memories.RunAsync(cancellationToken).ConfigureAwait(false);
                return 0;
            case AppCommand.Proposals:
                CommandPreconditions.RequireInteractive(text);
                await memories.RunAsync(cancellationToken, selectProposals: true).ConfigureAwait(false);
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), "Unsupported memory command.");
        }
    }
}

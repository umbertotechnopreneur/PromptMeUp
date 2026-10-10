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
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Shares exact-command copying and its visible receipt across command interaction views.</summary>
internal static class CommandCopyView
{
    /// <summary>Reports clipboard success and requests a graceful exit only after a successful copy.</summary>
    /// <param name="console">The console receiving the copy receipt.</param>
    /// <param name="text">The localized success and failure messages.</param>
    /// <param name="clipboard">The host clipboard service.</param>
    /// <param name="command">The exact selected or previewed command.</param>
    /// <param name="exitAfterCopy">Whether successful copying closes the application.</param>
    /// <exception cref="ApplicationExitRequestedException">The command was copied and exit was requested.</exception>
    internal static void Copy(IAnsiConsole console, ILocalizationService text, ICommandClipboard clipboard,
        string? command, bool exitAfterCopy)
    {
        var copied = !string.IsNullOrWhiteSpace(command) && clipboard.TryCopy(command);
        console.MarkupLine($"  [{(copied ? TerminalTheme.Success : TerminalTheme.Warning)}]{Markup.Escape(text.Text(copied ? "Command.Copied" : "Command.CopyFailed"))}[/]");
        if (copied && exitAfterCopy)
        {
            throw new ApplicationExitRequestedException();
        }
    }
}

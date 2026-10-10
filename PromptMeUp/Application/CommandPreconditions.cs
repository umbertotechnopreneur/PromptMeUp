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

/// <summary>Applies the shared terminal and AI prerequisites before a command starts.</summary>
internal static class CommandPreconditions
{
    internal static bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;

    /// <summary>Requires a live terminal for forms and authorization prompts.</summary>
    internal static void RequireInteractive(ILocalizationService text)
    {
        if (!IsInteractive)
        {
            throw new InvalidOperationException(text.Text("Error.InteractiveRequired"));
        }
    }

    /// <summary>Allows non-interactive inspection only when the operation is already authorized.</summary>
    internal static void RequireInteractiveUnlessPreauthorized(ILocalizationService text, bool preauthorized)
    {
        if (!IsInteractive && !preauthorized)
        {
            throw new InvalidOperationException(text.Text("Error.InteractiveRequired"));
        }
    }

    /// <summary>Rejects AI work until setup, provider, and key prerequisites are satisfied.</summary>
    internal static void RequireAiReady(AppSettings settings, IEnvironmentSecretService secrets, ILocalizationService text)
    {
        if (!settings.SetupCompleted || !settings.AiEnabled)
        {
            throw new InvalidOperationException(text.Text("Error.SetupRequired"));
        }
        if (!secrets.IsConfigured(settings.ApiKeyVariable))
        {
            throw new InvalidOperationException(PromptMeUpApplication.AppendKeyRestartHint(
                text.Text("Error.ApiKeyMissing", settings.ApiKeyVariable), text, OperatingSystem.IsWindows()));
        }
    }
}

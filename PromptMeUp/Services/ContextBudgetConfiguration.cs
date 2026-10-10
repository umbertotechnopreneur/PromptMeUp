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


using System.Globalization;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public static class ContextBudgetConfiguration
{
    /// <summary>Loads the bounded ordinary-chat input budget without persisting environment configuration.</summary>
    public static ConversationContextLimits Load(Func<string, string?> readVariable, ILocalizationService text)
    {
        ArgumentNullException.ThrowIfNull(readVariable);
        ArgumentNullException.ThrowIfNull(text);
        var value = readVariable("PROMPTMEUP_CONTEXT_TOKENS");
        if (value is null)
        {
            return ConversationContextLimits.Default;
        }
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var tokens)
            || tokens is < 4_000 or > 200_000)
        {
            throw new InvalidOperationException(text.Text("Context.InvalidBudget", 4_000, 200_000));
        }
        return new ConversationContextLimits(tokens);
    }
}

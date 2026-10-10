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

internal static class ModelPricingPolicy
{
    private const long LongContextInputThreshold = 272_000;

    /// <summary>Resolves documented model families and dated snapshots to their applicable standard pricing band.</summary>
    internal static (string Model, string ContextWindow)? Resolve(string model, long inputTokens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        var descriptor = AiModelCatalog.Models.FirstOrDefault(candidate =>
            string.Equals(model, candidate.Id, StringComparison.OrdinalIgnoreCase)
            || (model.StartsWith(candidate.Id + "-", StringComparison.OrdinalIgnoreCase)
                && DateOnly.TryParseExact(model[(candidate.Id.Length + 1)..], "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _)));
        if (descriptor is null)
        {
            return null;
        }

        // The >272K input threshold applies to these families, not to every model with a large context window.
        var hasLongBand = descriptor.Id is "gpt-5.6-sol" or "gpt-5.6-terra" or "gpt-5.6-luna" or "gpt-5.5" or "gpt-5.4";
        return (descriptor.Id, hasLongBand && inputTokens > LongContextInputThreshold ? "long" : "short");
    }
}

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


using System.Buffers;
using System.Globalization;
using System.Text;

namespace PromptMeUp.Services;

/// <summary>Normalizes optional display-name data without treating its contents as AI instructions.</summary>
public static class PreferredNamePolicy
{
    public const int MaximumLength = 80;

    /// <summary>Accepts a bounded Unicode name while rejecting hidden controls, malformed text, and recognizable credentials.</summary>
    public static string Normalize(string? value, ISensitiveDataRedactor redactor)
    {
        ArgumentNullException.ThrowIfNull(redactor);
        if (value is null)
        {
            return string.Empty;
        }
        for (var index = 0; index < value.Length;)
        {
            if (Rune.DecodeFromUtf16(value.AsSpan(index), out var rune, out var consumed) != OperationStatus.Done
                || Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.Format
                    or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
            {
                throw Invalid();
            }
            index += consumed;
        }
        var normalized = value.Trim().Normalize(NormalizationForm.FormC);
        if (normalized.Length > MaximumLength || !string.Equals(normalized, redactor.Redact(normalized), StringComparison.Ordinal))
        {
            throw Invalid();
        }
        return normalized;
    }

    /// <summary>Returns one generic validation error without retaining or echoing the supplied name.</summary>
    private static ArgumentException Invalid() => new("The preferred name is invalid.", "value");
}

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


using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class LocalizationTests
{
    /// <summary>Verifies that every advertised language contains every product UI key.</summary>
    [Fact]
    public void Catalogs_AllSupportedLanguages_AreComplete()
    {
        Assert.NotEmpty(UiTextCatalog.Entries);
        foreach (var (key, translations) in UiTextCatalog.Entries)
        {
            foreach (var language in SupportedLanguages.Codes)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(translations.ForLanguage(language)),
                    $"Missing {language} translation for {key}.");
            }
        }
    }
}

// SPDX-License-Identifier: MIT

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

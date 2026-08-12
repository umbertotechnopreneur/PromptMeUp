// SPDX-License-Identifier: MIT

using System.Reflection;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class LocalizationTests
{
    /// <summary>Verifies that every advertised language contains every product UI key.</summary>
    [Fact]
    public void Catalogs_AllSupportedLanguages_AreComplete()
    {
        var type = typeof(LocalizationService);
        var english = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(
            type.GetField("English", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null));
        var overrides = Assert.IsAssignableFrom<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(
            type.GetField("Overrides", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null));

        foreach (var language in SupportedLanguages.Codes.Where(code => code != "en"))
        {
            Assert.True(overrides.TryGetValue(language, out var localized));
            Assert.Empty(english.Keys.Except(localized!.Keys, StringComparer.Ordinal));
        }
    }
}

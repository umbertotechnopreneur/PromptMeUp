// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    internal static IReadOnlyDictionary<string, LocalizedText> Entries { get; } = CreateEntries();

    /// <summary>Resolves a UI string for one supported language without changing the active interface culture.</summary>
    internal static bool TryGet(string key, string language, out string value)
    {
        if (!Entries.TryGetValue(key, out var translations))
        {
            value = string.Empty;
            return false;
        }
        value = translations.ForLanguage(language);
        return true;
    }

    /// <summary>Combines each functional catalog while rejecting duplicate UI keys.</summary>
    private static IReadOnlyDictionary<string, LocalizedText> CreateEntries()
    {
        var entries = new Dictionary<string, LocalizedText>(StringComparer.Ordinal);
        AddAboutEntries(entries);
        AddApplicationEntries(entries);
        AddArtifactsEntries(entries);
        AddCommandsEntries(entries);
        AddConversationEntries(entries);
        AddContextBreakdownEntries(entries);
        AddCostsEntries(entries);
        AddErrorsEntries(entries);
        AddExperimentalEntries(entries);
        AddReflectionEntries(entries);
        AddFormEntries(entries);
        AddHelpBrowserEntries(entries);
        AddLennaEntries(entries);
        AddMemoryManagerEntries(entries);
        AddMemoryCliEntries(entries);
        AddMemoryManagerValidationEntries(entries);
        AddSettingsEntries(entries);
        AddSetupEntries(entries);
        AddStatusEntries(entries);
        AddSystemIntegrationEntries(entries);
        AddThemeEntries(entries);
        return entries;
    }
}

/// <summary>Stores one required UI translation for each supported language.</summary>
/// <summary>Stores all six named translations for one authoritative UI key.</summary>
internal sealed record LocalizedText(
    string English,
    string Italian,
    string French,
    string German,
    string Spanish,
    string Vietnamese)
{
    /// <summary>Resolves one explicitly named translation for a supported language.</summary>
    internal string ForLanguage(string language) => language switch
    {
        "en" => English,
        "it" => Italian,
        "fr" => French,
        "de" => German,
        "es" => Spanish,
        "vi" => Vietnamese,
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language.")
    };
}

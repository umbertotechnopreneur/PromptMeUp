// SPDX-License-Identifier: MIT

using System.Globalization;

namespace PromptMeUp.Services;

public sealed record SupportedLanguage(string Code, string NativeName, string CultureName, string Flag);

public static class SupportedLanguages
{
    public static IReadOnlyList<SupportedLanguage> All { get; } =
    [
        new("it", "Italiano", "it-IT", "🇮🇹"),
        new("en", "English", "en-US", "🇺🇸"),
        new("fr", "Français", "fr-FR", "🇫🇷"),
        new("de", "Deutsch", "de-DE", "🇩🇪"),
        new("es", "Español", "es-ES", "🇪🇸"),
        new("vi", "Tiếng Việt", "vi-VN", "🇻🇳")
    ];

    public static IReadOnlyList<string> Codes { get; } = All.Select(language => language.Code).ToArray();

    /// <summary>Checks whether a two-letter interface language is advertised by the product.</summary>
    public static bool IsSupported(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && All.Any(language => string.Equals(language.Code, value.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the canonical lower-case code for one supported language.</summary>
    public static string Normalize(string value) =>
        All.First(language => string.Equals(language.Code, value.Trim(), StringComparison.OrdinalIgnoreCase)).Code;

    /// <summary>Uses the current UI culture when supported and otherwise selects English.</summary>
    public static string ResolveSystemLanguage()
    {
        var twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return IsSupported(twoLetter) ? Normalize(twoLetter) : "en";
    }

    /// <summary>Resolves the display culture used for localized numbers, dates, and text formatting.</summary>
    public static CultureInfo Culture(string language) =>
        CultureInfo.GetCultureInfo(All.First(item => item.Code == Normalize(language)).CultureName);
}

public interface ILocalizationService
{
    string Language { get; }

    CultureInfo Culture { get; }

    void SetLanguage(string language);

    string Text(string key, params object?[] args);
}

public sealed class LocalizationService : ILocalizationService
{
    public string Language { get; private set; } = "en";

    public CultureInfo Culture => SupportedLanguages.Culture(Language);

    /// <summary>Applies one supported interface language for the current invocation.</summary>
    public void SetLanguage(string language) => Language = SupportedLanguages.Normalize(language);

    /// <summary>Resolves one UI string and formats its values using the selected language culture.</summary>
    public string Text(string key, params object?[] args)
    {
        var template = UiTextCatalog.TryGet(key, Language, out var translation)
            ? translation
            : key;
        return args.Length == 0 ? template : string.Format(Culture, template, args);
    }
}

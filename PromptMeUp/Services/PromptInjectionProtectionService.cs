// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PromptMeUp.Services;

public sealed record PromptPreambleProtectionResult(
    string SanitizedText,
    int WordCount,
    bool IsSafe,
    bool IsWithinWordLimit);

public interface IPromptInjectionProtectionService
{
    PromptPreambleProtectionResult Protect(string? preamble);
}

public sealed class PromptInjectionProtectionService : IPromptInjectionProtectionService
{
    public const int MaximumPreambleWords = 500;

    private static readonly Regex WordPattern = new(
        @"[\p{L}\p{M}\p{N}]+(?:['’\-][\p{L}\p{M}\p{N}]+)*",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex HorizontalWhitespacePattern = new(
        @"[^\S\r\n]+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ExcessLineBreakPattern = new(
        @"\n{3,}",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex[] InjectionPatterns =
    [
        Pattern(@"\b(ignore|disregard|forget|override|bypass)\b.{0,80}\b(previous|prior|above|system|developer|instructions?|rules?|prompt)\b"),
        Pattern(@"\b(ignora|dimentica|sovrascrivi|aggira|eludi)\b.{0,80}\b(istruzioni|regole|prompt|sistema|precedenti|sviluppatore)\b"),
        Pattern(@"\b(ignore|oublie|remplace|contourne)\b.{0,80}\b(instructions?|regles?|invite|systeme|precedentes?|developpeur)\b"),
        Pattern(@"\b(ignoriere|vergiss|uberschreibe|umgehe)\b.{0,80}\b(anweisungen?|regeln?|system|systemprompt|vorherigen?|entwickler)\b"),
        Pattern(@"\b(ignora|olvida|reemplaza|anula|elude)\b.{0,80}\b(instrucciones?|reglas?|sistema|prompt|anteriores?|desarrollador)\b"),
        Pattern(@"\b(bo\s+qua|quen|ghi\s+de|vuot\s+qua)\b.{0,80}\b(huong\s+dan|quy\s+tac|loi\s+nhac|he\s+thong|truoc|nha\s+phat\s+trien)\b"),
        Pattern(@"\b(reveal|show|print|repeat|display|mostra|rivela|affiche|revele|zeige|enthulle|muestra|revela|hien\s+thi|tiet\s+lo)\b.{0,80}\b(system|developer|sistema|systeme|entwickler|he\s+thong)\b.{0,40}\b(prompt|instructions?|istruzioni|invite|anweisungen?|instrucciones?|loi\s+nhac|huong\s+dan)\b"),
        Pattern(@"(?:^|\n)\s*(?:#{1,6}\s*)?(system|developer|assistant|user|sistema|systeme|entwickler|desarrollador|he\s+thong|nha\s+phat\s+trien)\s*[:>]"),
        Pattern(@"<\s*/?\s*user-configured-preamble\b")
    ];

    /// <summary>Normalizes a configured preamble, counts Unicode words, and rejects multilingual instruction-override patterns.</summary>
    public PromptPreambleProtectionResult Protect(string? preamble)
    {
        var sanitized = Sanitize(preamble ?? string.Empty);
        var wordCount = WordPattern.Matches(sanitized).Count;
        var scanText = RemoveDiacritics(sanitized).ToLowerInvariant();
        var isSafe = InjectionPatterns.All(pattern => !pattern.IsMatch(scanText));
        return new PromptPreambleProtectionResult(
            sanitized,
            wordCount,
            isSafe,
            wordCount <= MaximumPreambleWords);
    }

    /// <summary>Creates one compiled, culture-invariant detector over normalized lowercase text.</summary>
    private static Regex Pattern(string expression) => new(
        expression,
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>Removes control and formatting characters while preserving intentional line breaks.</summary>
    private static string Sanitize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var builder = new StringBuilder(normalized.Length);
        foreach (var rune in normalized.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category == UnicodeCategory.Format
                || (category == UnicodeCategory.Control && rune.Value is not ('\n' or '\t')))
            {
                continue;
            }

            builder.Append(rune.ToString());
        }

        var compact = HorizontalWhitespacePattern.Replace(builder.ToString(), " ");
        return ExcessLineBreakPattern.Replace(compact, "\n\n").Trim();
    }

    /// <summary>Produces an accent-insensitive scan representation without altering the persisted preamble.</summary>
    private static string RemoveDiacritics(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var rune in decomposed.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(rune.ToString());
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

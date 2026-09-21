// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public sealed class ScriptArtifactService(
    ISensitiveDataRedactor redactor,
    ILocalizationService text,
    IScriptLanguageCatalog languages,
    ArtifactLimits? limits = null)
{
    private readonly ArtifactLimits _limits = limits ?? ArtifactLimits.Default;

    /// <summary>Rejects malformed, oversized, or credential-bearing generated artifacts before saving.</summary>
    public ScriptArtifact Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var explanation = root.GetProperty("explanation").GetString();
            var source = root.GetProperty("source").GetString();
            if (string.IsNullOrWhiteSpace(explanation) || string.IsNullOrWhiteSpace(source))
            {
                throw new JsonException();
            }
            ValidateSource(source);
            return new ScriptArtifact(explanation, source);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidOperationException(text.Text("Script.Invalid"));
        }
    }

    /// <summary>Reads a bounded existing script while rejecting embedded credentials rather than altering the local preview.</summary>
    public async Task<string> ReadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await BoundedArtifactFile.ReadAsync(path, _limits.MaxScriptBytes, text, cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(new MemoryStream(bytes), new UTF8Encoding(false, true), true);
            var source = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            ValidateSource(source);
            return source;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DecoderFallbackException)
        {
            throw new InvalidOperationException(text.Text("Input.FileError"));
        }
    }

    /// <summary>Writes a reviewed artifact to a new language-matched script file and never overwrites an existing file.</summary>
    public async Task SaveAsync(string path, string source, ScriptLanguage language, CancellationToken cancellationToken)
    {
        ValidateSource(source);
        var definition = languages.Get(language);
        if (!string.Equals(Path.GetExtension(path), definition.FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(text.Text("Script.OutputOption"));
        }
        var fullPath = Path.GetFullPath(path);
        try
        {
            await AtomicFileWriter.WriteAllTextAsync(fullPath, source,
                overwrite: false, cancellationToken, new UTF8Encoding(false)).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(text.Text("Script.SaveError"));
        }
    }

    /// <summary>Suggests a new current-directory filename with a stable language extension and no existing collision.</summary>
    public string SuggestOutputPath(string request, ScriptLanguage language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request);
        var definition = languages.Get(language);
        var directory = Path.GetFullPath(Environment.CurrentDirectory);
        var stem = CreateSafeStem(request);
        for (var suffix = 1; suffix <= 1_000; suffix++)
        {
            var fileName = suffix == 1
                ? stem + definition.FileExtension
                : stem + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture) + definition.FileExtension;
            var candidate = Path.Combine(directory, fileName);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(text.Text("Script.SaveError"));
    }

    /// <summary>Retains the PowerShell parser helper for callers that only need the legacy language-specific check.</summary>
    public static string BuildValidationCommand(string source) => ScriptLanguageCatalog.BuildPowerShellValidationCommand(source);

    /// <summary>Quotes literal data without permitting interpolation or evaluation by the runner.</summary>
    public static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    /// <summary>Converts a natural-language request into a safe, readable file stem without path separators or reserved Windows names.</summary>
    private static string CreateSafeStem(string request)
    {
        var normalized = request.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var separatorPending = false;
        foreach (var character in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                if (separatorPending && builder.Length > 0)
                {
                    builder.Append('-');
                }
                builder.Append(char.ToLowerInvariant(character));
                separatorPending = false;
            }
            else
            {
                separatorPending = builder.Length > 0;
            }
        }

        var stem = builder.ToString().Trim('-', '.', ' ');
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "script";
        }
        if (stem.Length > 56)
        {
            stem = stem[..56].TrimEnd('-', '.', ' ');
        }
        if (IsReservedWindowsFileName(stem))
        {
            stem = "script-" + stem;
        }

        return stem;
    }

    /// <summary>Rejects the Windows device-name stems even when the current platform can create them.</summary>
    private static bool IsReservedWindowsFileName(string value)
    {
        var normalized = value.TrimEnd('.', ' ');
        if (normalized is "con" or "prn" or "aux" or "nul")
        {
            return true;
        }
        return normalized.Length == 4
            && (normalized.StartsWith("com", StringComparison.Ordinal) || normalized.StartsWith("lpt", StringComparison.Ordinal))
            && normalized[3] is >= '1' and <= '9';
    }

    /// <summary>Rejects unsafe artifact shapes and recognizable credentials without concealing the reason in logs.</summary>
    private void ValidateSource(string source)
    {
        BoundedArtifactFile.CheckSize(Encoding.UTF8.GetByteCount(source), _limits.MaxScriptBytes, text);
        if (string.IsNullOrWhiteSpace(source)
            || source.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t'))
            || redactor.Redact(source) != source || source.Contains("[redacted", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(text.Text("Script.Invalid"));
        }
    }
}

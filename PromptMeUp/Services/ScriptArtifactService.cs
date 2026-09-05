// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public sealed class ScriptArtifactService(ISensitiveDataRedactor redactor, ILocalizationService text, ArtifactLimits? limits = null)
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

    /// <summary>Writes a reviewed artifact to a new script file, never overwriting an existing file.</summary>
    public async Task SaveAsync(string path, string source, CancellationToken cancellationToken)
    {
        ValidateSource(source);
        if (!string.Equals(Path.GetExtension(path), ".ps1", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(text.Text("Script.OutputOption"));
        }
        var fullPath = Path.GetFullPath(path);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, source, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            File.Move(temporary, fullPath, overwrite: false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(text.Text("Script.SaveError"));
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    /// <summary>Builds a visible PowerShell parser and optional built-in analyzer invocation that never evaluates the source.</summary>
    public static string BuildValidationCommand(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return "$source = " + Quote(source) + "; $tokens = $null; $parseErrors = $null; " +
            "$null = [System.Management.Automation.Language.Parser]::ParseInput($source, [ref]$tokens, [ref]$parseErrors); " +
            "$diagnostics = @($parseErrors | ForEach-Object { [pscustomobject]@{ Line = $_.Extent.StartLineNumber; Message = $_.Message } }); " +
            "$analyzerAvailable = [bool](Get-Module -ListAvailable PSScriptAnalyzer); " +
            "if ($analyzerAvailable) { $diagnostics += @(PSScriptAnalyzer\\Invoke-ScriptAnalyzer -ScriptDefinition $source | Select-Object Line,Severity,Message) }; " +
            "[pscustomobject]@{ SyntaxValid = ($parseErrors.Count -eq 0); AnalyzerAvailable = $analyzerAvailable; Diagnostics = $diagnostics } | ConvertTo-Json -Depth 5; " +
            "if ($parseErrors.Count -gt 0) { exit 1 }";
    }

    /// <summary>Quotes literal data without permitting interpolation or evaluation by the runner.</summary>
    public static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

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

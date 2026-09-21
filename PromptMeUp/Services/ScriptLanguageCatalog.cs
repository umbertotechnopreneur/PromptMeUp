// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Resolves script-language metadata, local runtimes, syntax checks, and safe temporary launch commands.</summary>
public interface IScriptLanguageCatalog
{
    ScriptLanguageDefinition Get(ScriptLanguage language);

    IReadOnlyList<ScriptLanguageDefinition> List();

    ScriptRuntimeAvailability GetAvailability(ScriptLanguage language);

    ScriptLanguage DetectPreferredLanguage();

    string BuildValidationCommand(ScriptLanguage language, string source);

    string BuildExecutionCommand(ScriptLanguage language, string source);
}

/// <summary>Keeps the language-specific script contract in one place while commands continue through normal authorization.</summary>
public sealed class ScriptLanguageCatalog : IScriptLanguageCatalog
{
    private static readonly IReadOnlyDictionary<ScriptLanguage, ScriptLanguageDefinition> Definitions =
        new Dictionary<ScriptLanguage, ScriptLanguageDefinition>
        {
            [ScriptLanguage.PowerShell] = new(
                ScriptLanguage.PowerShell,
                "powershell",
                "PowerShell 7",
                ".ps1",
                "Generate a PowerShell 7 (.ps1) script. Use named parameters, input validation, Set-StrictMode, and deliberate error handling. For destructive effects use CmdletBinding(SupportsShouldProcess) and guard effects with ShouldProcess.",
                true,
                ["pwsh"]),
            [ScriptLanguage.Batch] = new(
                ScriptLanguage.Batch,
                "batch",
                "Batch / CMD",
                ".cmd",
                "Generate a Windows Batch/CMD (.cmd) script. Use setlocal EnableExtensions DisableDelayedExpansion, validate arguments, quote paths, check errorlevel, and never use PowerShell-only syntax. State explicitly that CMD has no universal syntax-only validation or dry-run primitive.",
                false,
                ["cmd.exe"]),
            [ScriptLanguage.Bash] = new(
                ScriptLanguage.Bash,
                "bash",
                "Bash",
                ".sh",
                "Generate a Bash (.sh) script. Use a portable Bash shebang, set -euo pipefail where compatible with the task, quote expansions, validate arguments, and handle failures deliberately. For destructive effects provide an explicit dry-run mode when practical.",
                true,
                ["bash"]),
            [ScriptLanguage.Python] = new(
                ScriptLanguage.Python,
                "python",
                "Python 3",
                ".py",
                "Generate a Python 3 (.py) script. Use argparse, pathlib where appropriate, a main entry point, input validation, and explicit exception handling. For destructive effects provide an explicit dry-run mode when practical.",
                true,
                ["python3", "python"]),
            [ScriptLanguage.JavaScript] = new(
                ScriptLanguage.JavaScript,
                "javascript",
                "JavaScript / Node.js",
                ".js",
                "Generate a Node.js JavaScript (.js) script. Use process.argv, validate arguments, handle asynchronous failures, and use explicit exit codes. For destructive effects provide an explicit dry-run mode when practical.",
                true,
                ["node"])
        };

    /// <summary>Gets the complete fixed definition for one supported language.</summary>
    public ScriptLanguageDefinition Get(ScriptLanguage language) => Definitions.TryGetValue(language, out var definition)
        ? definition
        : throw new ArgumentOutOfRangeException(nameof(language));

    /// <summary>Lists language definitions in the stable setup-menu order.</summary>
    public IReadOnlyList<ScriptLanguageDefinition> List() =>
        new[] { ScriptLanguage.PowerShell, ScriptLanguage.Batch, ScriptLanguage.Bash, ScriptLanguage.Python, ScriptLanguage.JavaScript }
            .Select(Get)
            .ToArray();

    /// <summary>Finds the selected interpreter without running it or reading user-provided source.</summary>
    public ScriptRuntimeAvailability GetAvailability(ScriptLanguage language)
    {
        var definition = Get(language);
        if (language == ScriptLanguage.Batch && !OperatingSystem.IsWindows())
        {
            return new ScriptRuntimeAvailability(false, null);
        }

        var executablePath = FindExecutable(definition.ExecutableCandidates);
        return new ScriptRuntimeAvailability(executablePath is not null, executablePath);
    }

    /// <summary>Uses an explicitly exposed shell first, then the available supported runtime order for a new installation.</summary>
    public ScriptLanguage DetectPreferredLanguage()
    {
        var shell = Environment.GetEnvironmentVariable("SHELL");
        var shellLanguage = shell?.Contains("bash", StringComparison.OrdinalIgnoreCase) == true
            ? ScriptLanguage.Bash
            : shell?.Contains("pwsh", StringComparison.OrdinalIgnoreCase) == true
                || shell?.Contains("powershell", StringComparison.OrdinalIgnoreCase) == true
                ? ScriptLanguage.PowerShell
                : shell?.Contains("cmd", StringComparison.OrdinalIgnoreCase) == true
                    ? ScriptLanguage.Batch
                    : (ScriptLanguage?)null;
        if (shellLanguage is { } detected && GetAvailability(detected).IsAvailable)
        {
            return detected;
        }

        foreach (var language in new[]
                 {
                     ScriptLanguage.PowerShell,
                     ScriptLanguage.Bash,
                     ScriptLanguage.Python,
                     ScriptLanguage.JavaScript,
                     ScriptLanguage.Batch
                 })
        {
            if (GetAvailability(language).IsAvailable)
            {
                return language;
            }
        }

        // A persistent setting still needs a deterministic value when the PATH is temporarily unavailable.
        return OperatingSystem.IsWindows() ? ScriptLanguage.Batch : ScriptLanguage.Bash;
    }

    /// <summary>Builds a non-executing syntax validation command for a locally available selected language.</summary>
    public string BuildValidationCommand(ScriptLanguage language, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var definition = Get(language);
        if (!definition.SupportsValidation)
        {
            throw new InvalidOperationException("The selected script language does not provide a syntax-only validation command.");
        }

        if (language == ScriptLanguage.PowerShell)
        {
            return BuildPowerShellValidationCommand(source);
        }

        var runtime = RequireRuntime(language);
        return BuildTemporaryScriptCommand(definition, source, runtime, BuildValidationInvocation(language));
    }

    /// <summary>Builds a reviewed command that writes the source to a unique temporary file and starts only the selected runtime.</summary>
    public string BuildExecutionCommand(ScriptLanguage language, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var definition = Get(language);
        var runtime = RequireRuntime(language);
        return BuildTemporaryScriptCommand(definition, source, runtime, BuildExecutionInvocation(language));
    }

    /// <summary>Maps a persisted setting value to the fixed language identifier.</summary>
    public static ScriptLanguage ParseStorageValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var definition in Definitions.Values)
        {
            if (string.Equals(definition.StorageValue, value, StringComparison.OrdinalIgnoreCase))
            {
                return definition.Language;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported script language.");
    }

    /// <summary>Returns the stable database value for a selected language.</summary>
    public static string ToStorageValue(ScriptLanguage language) => Definitions.TryGetValue(language, out var definition)
        ? definition.StorageValue
        : throw new ArgumentOutOfRangeException(nameof(language));

    /// <summary>Builds the PowerShell parser-only check without evaluating the supplied source.</summary>
    public static string BuildPowerShellValidationCommand(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return "$source = " + ScriptArtifactService.Quote(source) + "; $tokens = $null; $parseErrors = $null; " +
            "$null = [System.Management.Automation.Language.Parser]::ParseInput($source, [ref]$tokens, [ref]$parseErrors); " +
            "$diagnostics = @($parseErrors | ForEach-Object { [pscustomobject]@{ Line = $_.Extent.StartLineNumber; Message = $_.Message } }); " +
            "$analyzerAvailable = [bool](Get-Module -ListAvailable PSScriptAnalyzer); " +
            "if ($analyzerAvailable) { $diagnostics += @(PSScriptAnalyzer\\Invoke-ScriptAnalyzer -ScriptDefinition $source | Select-Object Line,Severity,Message) }; " +
            "[pscustomobject]@{ SyntaxValid = ($parseErrors.Count -eq 0); AnalyzerAvailable = $analyzerAvailable; Diagnostics = $diagnostics } | ConvertTo-Json -Depth 5; " +
            "if ($parseErrors.Count -gt 0) { exit 1 }";
    }

    /// <summary>Requires a current executable path before a user can authorize validation or execution.</summary>
    private string RequireRuntime(ScriptLanguage language) => GetAvailability(language).ExecutablePath
        ?? throw new InvalidOperationException("The selected script runtime is unavailable.");

    /// <summary>Creates the selected runtime invocation using only fixed arguments and an internally generated temporary path.</summary>
    private static string BuildExecutionInvocation(ScriptLanguage language) => language switch
    {
        ScriptLanguage.PowerShell => "& $runner '-NoLogo' '-NoProfile' '-NonInteractive' '-File' $scriptPath",
        ScriptLanguage.Batch => "& $runner '/d' '/c' ('call \"{0}\"' -f $scriptPath)",
        ScriptLanguage.Bash => "& $runner '--noprofile' '--norc' $scriptPath",
        ScriptLanguage.Python => "& $runner '-I' $scriptPath",
        ScriptLanguage.JavaScript => "& $runner $scriptPath",
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };

    /// <summary>Creates an interpreter syntax-check invocation that never runs the generated source.</summary>
    private static string BuildValidationInvocation(ScriptLanguage language) => language switch
    {
        ScriptLanguage.Bash => "& $runner '--noprofile' '--norc' '-n' $scriptPath",
        ScriptLanguage.Python => "& $runner '-I' '-c' 'import ast, pathlib, sys; ast.parse(pathlib.Path(sys.argv[1]).read_text(encoding=\"utf-8\"), filename=sys.argv[1])' $scriptPath",
        ScriptLanguage.JavaScript => "& $runner '--check' $scriptPath",
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };

    /// <summary>Writes source to a unique temp file with its real extension, invokes the fixed runtime, and removes both paths in finally.</summary>
    private static string BuildTemporaryScriptCommand(
        ScriptLanguageDefinition definition,
        string source,
        string executablePath,
        string invocation)
    {
        return string.Join(Environment.NewLine,
        [
            "$ErrorActionPreference = 'Stop'",
            "$temporaryPath = [IO.Path]::GetTempFileName()",
            "$scriptPath = $null",
            "$exitCode = 1",
            "try {",
            "    $scriptPath = [IO.Path]::ChangeExtension($temporaryPath, " + ScriptArtifactService.Quote(definition.FileExtension) + ")",
            "    [IO.File]::Move($temporaryPath, $scriptPath)",
            "    [IO.File]::WriteAllText($scriptPath, " + ScriptArtifactService.Quote(source) + ", [Text.UTF8Encoding]::new($false))",
            "    $runner = " + ScriptArtifactService.Quote(executablePath),
            "    " + invocation,
            "    if ($null -ne $LASTEXITCODE) { $exitCode = $LASTEXITCODE } else { $exitCode = 0 }",
            "}",
            "finally {",
            "    if ($null -ne $scriptPath -and [IO.File]::Exists($scriptPath)) { [IO.File]::Delete($scriptPath) }",
            "    if ([IO.File]::Exists($temporaryPath)) { [IO.File]::Delete($temporaryPath) }",
            "}",
            "exit $exitCode"
        ]);
    }

    /// <summary>Finds the first fixed executable candidate on PATH without spawning a process.</summary>
    private static string? FindExecutable(IEnumerable<string> candidates)
    {
        var pathDirectories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var candidate in candidates)
        {
            foreach (var name in ExpandExecutableNames(candidate))
            {
                if (Path.IsPathRooted(name))
                {
                    if (File.Exists(name))
                    {
                        return Path.GetFullPath(name);
                    }
                    continue;
                }

                foreach (var directory in pathDirectories)
                {
                    try
                    {
                        var path = Path.Combine(directory, name);
                        if (File.Exists(path))
                        {
                            return Path.GetFullPath(path);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Ignore malformed PATH entries rather than making a new installation unusable.
                    }
                }
            }
        }

        return null;
    }

    /// <summary>Adds the platform executable suffixes only when a candidate did not already specify one.</summary>
    private static IEnumerable<string> ExpandExecutableNames(string candidate)
    {
        yield return candidate;
        if (!OperatingSystem.IsWindows() || Path.HasExtension(candidate))
        {
            yield break;
        }

        foreach (var extension in (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
                     .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return candidate + extension.ToLowerInvariant();
        }
    }
}

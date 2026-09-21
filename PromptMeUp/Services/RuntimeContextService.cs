// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.RegularExpressions;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IRuntimeContextService
{
    RuntimeContext GetCurrent(ScriptLanguage preferredScriptLanguage);
}

/// <summary>Builds portable runtime context while withholding machine identities and exposing a sanitized working path only to operational prompts.</summary>
public sealed class RuntimeContextService : IRuntimeContextService
{
    private const int MaximumWorkingDirectoryLength = 240;
    private const string ApprovedCommandShell = "PowerShell 7 (pwsh -NoLogo -NoProfile -NonInteractive)";
    private readonly IScriptLanguageCatalog _scriptLanguages;
    private readonly ISensitiveDataRedactor _redactor;

    /// <summary>Creates the runtime-context service with the fixed script catalog and shared credential redactor.</summary>
    public RuntimeContextService(IScriptLanguageCatalog scriptLanguages, ISensitiveDataRedactor redactor)
    {
        _scriptLanguages = scriptLanguages ?? throw new ArgumentNullException(nameof(scriptLanguages));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
    }

    /// <summary>Returns the OS, effective shell, selected script interpreter, and an account-safe local working directory.</summary>
    public RuntimeContext GetCurrent(ScriptLanguage preferredScriptLanguage)
    {
        if (!Enum.IsDefined(preferredScriptLanguage))
        {
            throw new ArgumentOutOfRangeException(nameof(preferredScriptLanguage));
        }

        var definition = _scriptLanguages.Get(preferredScriptLanguage);
        var availability = _scriptLanguages.GetAvailability(preferredScriptLanguage).IsAvailable;
        return Build(
            new RuntimeContextSnapshot(
                DetectPlatform(),
                Environment.OSVersion.Version,
                Environment.CurrentDirectory,
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
            definition.DisplayName,
            availability,
            _redactor);
    }

    /// <summary>Converts a small trusted platform snapshot into the exact provider-bound runtime context.</summary>
    internal static RuntimeContext Build(
        RuntimeContextSnapshot snapshot,
        string preferredScriptInterpreter,
        bool isPreferredScriptInterpreterAvailable,
        ISensitiveDataRedactor redactor)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredScriptInterpreter);
        ArgumentNullException.ThrowIfNull(redactor);

        return new RuntimeContext(
            DescribeOperatingSystem(snapshot.Platform, snapshot.OperatingSystemVersion),
            ApprovedCommandShell,
            preferredScriptInterpreter.Trim(),
            isPreferredScriptInterpreterAvailable,
            SanitizeWorkingDirectory(snapshot.CurrentDirectory, snapshot.UserProfile, redactor));
    }

    /// <summary>Identifies the supported platform family without reading a user or machine identity.</summary>
    private static RuntimePlatform DetectPlatform() => OperatingSystem.IsWindows()
        ? RuntimePlatform.Windows
        : OperatingSystem.IsMacOS()
            ? RuntimePlatform.MacOS
            : OperatingSystem.IsLinux()
                ? RuntimePlatform.Linux
                : RuntimePlatform.Other;

    /// <summary>Formats a family and numeric platform version from managed runtime APIs only.</summary>
    private static string DescribeOperatingSystem(RuntimePlatform platform, Version? version)
    {
        var family = platform switch
        {
            RuntimePlatform.Windows => "Windows",
            RuntimePlatform.MacOS => "macOS",
            RuntimePlatform.Linux => "Linux",
            _ => "Unknown operating system"
        };
        if (version is null || version.Major < 0 || version.Minor < 0)
        {
            return family;
        }

        var numericVersion = version.Build < 0
            ? $"{version.Major}.{version.Minor}"
            : version.Revision < 0
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        return $"{family} {numericVersion}";
    }

    /// <summary>Returns a bounded path with account names and recognizable credentials removed before any provider-bound use.</summary>
    private static string SanitizeWorkingDirectory(string? currentDirectory, string? userProfile, ISensitiveDataRedactor redactor)
    {
        var path = NormalizePath(currentDirectory);
        if (string.IsNullOrWhiteSpace(path))
        {
            return "unavailable";
        }
        if (IsNetworkPath(path))
        {
            return "network path withheld";
        }

        var home = NormalizePath(userProfile);
        if (!string.IsNullOrWhiteSpace(home) && IsPathWithin(path, home))
        {
            var suffix = path[home.Length..].TrimStart('\\', '/');
            path = string.IsNullOrEmpty(suffix) ? "~" : $"~/{suffix.Replace('\\', '/')}";
        }
        else
        {
            path = ReplaceConventionalHomePrefix(path);
        }

        var redacted = redactor.Redact(path);
        return redacted.Length <= MaximumWorkingDirectoryLength
            ? redacted
            : string.Concat(redacted.AsSpan(0, MaximumWorkingDirectoryLength - 1), "…");
    }

    /// <summary>Normalizes a local path for safe display without treating it as an instruction or preserving control characters.</summary>
    private static string? NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim())
        {
            if (!char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    /// <summary>Replaces conventional home-directory roots when the runtime cannot resolve the current account profile.</summary>
    private static string ReplaceConventionalHomePrefix(string path)
    {
        var windowsPrefix = Regex.Match(
            path,
            @"^(?<drive>[A-Za-z]:)[\\/](?:Users|Documents and Settings)[\\/][^\\/]+(?<rest>[\\/].*)?$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (windowsPrefix.Success)
        {
            return $"~{windowsPrefix.Groups["rest"].Value.Replace('\\', '/')}";
        }

        var unixPrefix = Regex.Match(
            path,
            @"^/(?:home|Users)/[^/]+(?<rest>/.*)?$",
            RegexOptions.CultureInvariant);
        return unixPrefix.Success ? $"~{unixPrefix.Groups["rest"].Value}" : path;
    }

    /// <summary>Checks a path prefix across slash styles without relying on the host platform's path parser.</summary>
    private static bool IsPathWithin(string path, string root)
    {
        var normalizedPath = path.Replace('\\', '/').TrimEnd('/');
        var normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
        return string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith($"{normalizedRoot}/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Withholds UNC and mapped-network paths so a server identity is never sent to the provider.</summary>
    private static bool IsNetworkPath(string path)
    {
        if (path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            var root = Path.GetPathRoot(path);
            return !string.IsNullOrWhiteSpace(root)
                && new DriveInfo(root).DriveType == DriveType.Network;
        }
        catch (Exception) when (Path.IsPathRooted(path))
        {
            return false;
        }
    }
}

/// <summary>Captures only trusted platform and local-path facts before prompt-safe formatting.</summary>
internal sealed record RuntimeContextSnapshot(
    RuntimePlatform Platform,
    Version? OperatingSystemVersion,
    string? CurrentDirectory,
    string? UserProfile);

/// <summary>Represents the supported console platform families plus an explicit fallback.</summary>
internal enum RuntimePlatform
{
    Windows,
    Linux,
    MacOS,
    Other
}

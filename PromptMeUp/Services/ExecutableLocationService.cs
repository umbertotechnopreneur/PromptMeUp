// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IExecutableLocationService
{
    ExecutableLocationInfo Resolve();

    void OpenContainingFolder(ExecutableLocationInfo location);
}

public sealed class ExecutableLocationService : IExecutableLocationService
{
    /// <summary>Resolves the running hm binary and prepares exact, platform-appropriate action previews.</summary>
    public ExecutableLocationInfo Resolve()
    {
        var executablePath = ResolveExecutablePath();
        var directoryPath = Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException("The hm executable directory could not be resolved.");
        var openFolderPreview = OperatingSystem.IsWindows()
            ? $"explorer.exe /select, {QuoteForWindows(executablePath)}"
            : OperatingSystem.IsMacOS()
                ? $"open -R {QuoteForPosixShell(executablePath)}"
                : $"xdg-open {QuoteForPosixShell(directoryPath)}";
        var changeDirectoryCommand = OperatingSystem.IsWindows()
            ? $"Set-Location -LiteralPath {QuoteForPowerShell(directoryPath)}"
            : $"cd {QuoteForPosixShell(directoryPath)}";

        return new ExecutableLocationInfo(executablePath, directoryPath, openFolderPreview, changeDirectoryCommand);
    }

    /// <summary>Opens the native file manager after the view has shown and authorized the exact action.</summary>
    public void OpenContainingFolder(ExecutableLocationInfo location)
    {
        ArgumentNullException.ThrowIfNull(location);
        var startInfo = new ProcessStartInfo { UseShellExecute = false };
        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = "explorer.exe";
            startInfo.ArgumentList.Add("/select,");
            startInfo.ArgumentList.Add(location.ExecutablePath);
        }
        else if (OperatingSystem.IsMacOS())
        {
            startInfo.FileName = "open";
            startInfo.ArgumentList.Add("-R");
            startInfo.ArgumentList.Add(location.ExecutablePath);
        }
        else
        {
            startInfo.FileName = "xdg-open";
            startInfo.ArgumentList.Add(location.DirectoryPath);
        }

        try
        {
            _ = Process.Start(startInfo)
                ?? throw new InvalidOperationException("The system file manager could not be started.");
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("The system file manager could not be started.", exception);
        }
    }

    /// <summary>Prefers the packaged hm app host and falls back to the entry assembly or current process.</summary>
    private static string ResolveExecutablePath()
    {
        var packagedName = OperatingSystem.IsWindows() ? "hm.exe" : "hm";
        var packagedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, packagedName));
        if (File.Exists(packagedPath))
        {
            return packagedPath;
        }

        var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(entryAssemblyPath))
        {
            return Path.GetFullPath(entryAssemblyPath);
        }

        return !string.IsNullOrWhiteSpace(Environment.ProcessPath)
            ? Path.GetFullPath(Environment.ProcessPath)
            : throw new InvalidOperationException("The hm executable path could not be resolved.");
    }

    /// <summary>Quotes one path for an exact Windows command preview.</summary>
    private static string QuoteForWindows(string value) => '"' + value.Replace("\"", "\\\"", StringComparison.Ordinal) + '"';

    /// <summary>Quotes one literal path for a PowerShell Set-Location command.</summary>
    private static string QuoteForPowerShell(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    /// <summary>Quotes one literal path for a POSIX-compatible shell command.</summary>
    private static string QuoteForPosixShell(string value) => "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
}

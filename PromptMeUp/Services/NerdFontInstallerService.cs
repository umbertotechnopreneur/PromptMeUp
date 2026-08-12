// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface INerdFontInstallerService
{
    Task<FontInstallResult> InstallAsync(bool dryRun, CancellationToken cancellationToken);
}

public sealed class NerdFontInstallerService : INerdFontInstallerService
{
    private const string FontName = "JetBrainsMono Nerd Font";
    private readonly ILogger<NerdFontInstallerService> _logger;

    /// <summary>Creates the opt-in terminal font helper.</summary>
    public NerdFontInstallerService(ILogger<NerdFontInstallerService> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Installs JetBrainsMono through an existing Oh My Posh CLI; it never installs PromptMeUp itself.</summary>
    public async Task<FontInstallResult> InstallAsync(bool dryRun, CancellationToken cancellationToken)
    {
        if (dryRun)
        {
            return new FontInstallResult(false, true, FontName, "Would run: oh-my-posh font install JetBrainsMono --headless");
        }

        if (!OperatingSystem.IsWindows())
        {
            return new FontInstallResult(
                false,
                false,
                FontName,
                "Automatic Nerd Font installation is Windows-only; install JetBrainsMono Nerd Font with your platform font manager.");
        }

        if (!await CommandExistsAsync("oh-my-posh", cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("oh-my-posh was not found. Install it first or install JetBrainsMono Nerd Font manually.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "oh-my-posh",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("font");
        startInfo.ArgumentList.Add("install");
        startInfo.ArgumentList.Add("JetBrainsMono");
        startInfo.ArgumentList.Add("--headless");
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("oh-my-posh could not be started.");
        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var output = (await standardOutput.ConfigureAwait(false)).Trim();
        var error = (await standardError.ConfigureAwait(false)).Trim();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"oh-my-posh font installation failed with exit code {process.ExitCode}."
                : error);
        }

        _logger.LogInformation("Nerd Font installation completed. Font={Font}", FontName);
        return new FontInstallResult(true, false, FontName, string.IsNullOrWhiteSpace(output) ? "Font installation completed." : output);
    }

    /// <summary>Checks whether a command resolves without relying on shell aliases.</summary>
    private static async Task<bool> CommandExistsAsync(string command, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add($"[bool](Get-Command -Name '{command}' -CommandType Application -ErrorAction SilentlyContinue)");
        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0 && output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }
}

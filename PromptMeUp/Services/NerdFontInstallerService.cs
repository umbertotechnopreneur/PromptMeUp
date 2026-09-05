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
    private readonly ILocalizationService _text;

    /// <summary>Creates the opt-in terminal font helper.</summary>
    public NerdFontInstallerService(ILogger<NerdFontInstallerService> logger, ILocalizationService? text = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _text = text ?? new LocalizationService();
    }

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
        var result = await BoundedProcessRunner.RunAsync(startInfo, "oh-my-posh font install JetBrainsMono --headless",
            TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        if (result.TimedOut)
        {
            throw new InvalidOperationException(_text.Text("Font.Timeout"));
        }
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StandardError)
                ? $"oh-my-posh font installation failed with exit code {result.ExitCode}."
                : result.StandardError);
        }

        _logger.LogInformation("Nerd Font installation completed. Font={Font}", FontName);
        return new FontInstallResult(true, false, FontName, result.StandardOutput);
    }

    /// <summary>Checks whether a command resolves without relying on shell aliases.</summary>
    private async Task<bool> CommandExistsAsync(string command, CancellationToken cancellationToken)
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
        var result = await BoundedProcessRunner.RunAsync(startInfo, "Get-Command oh-my-posh",
            TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
        if (result.TimedOut)
        {
            throw new InvalidOperationException(_text.Text("Font.LookupTimeout"));
        }
        return result.ExitCode == 0 && result.StandardOutput.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }
}

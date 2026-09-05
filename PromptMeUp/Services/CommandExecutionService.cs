// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface ICommandExecutionService
{
    Task<CommandExecutionResult> ExecuteAsync(
        ApprovedCommand command,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public sealed class CommandExecutionService : ICommandExecutionService
{
    private readonly ILogger<CommandExecutionService> _logger;

    /// <summary>Creates the restricted child-process runner.</summary>
    public CommandExecutionService(ILogger<CommandExecutionService> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Runs a recently authorized command in non-elevated PowerShell and captures bounded output.</summary>
    public async Task<CommandExecutionResult> ExecuteAsync(
        ApprovedCommand command,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.AuthorizationId)
            || DateTimeOffset.UtcNow - command.ApprovedAt > TimeSpan.FromMinutes(10))
        {
            throw new InvalidOperationException("Command authorization is missing or expired.");
        }

        var streamSource = command.Text.Length > 8_000;
        _logger.LogInformation("Authorized command starting. AuthorizationId={AuthorizationId}, RiskScore={RiskScore}", command.AuthorizationId, command.Assessment.Score);
        var result = await BoundedProcessRunner.RunAsync(BuildStartInfo(command.Text), command.Text, timeout, cancellationToken,
            streamSource ? command.Text : null).ConfigureAwait(false);
        _logger.LogInformation("Authorized command completed. AuthorizationId={AuthorizationId}, ExitCode={ExitCode}, TimedOut={TimedOut}, ElapsedMs={ElapsedMs}",
            command.AuthorizationId, result.ExitCode, result.TimedOut, result.ElapsedMilliseconds);
        return result;
    }

    /// <summary>Builds a non-interactive, non-elevated PowerShell child-process definition.</summary>
    private static ProcessStartInfo BuildStartInfo(string command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            WorkingDirectory = Environment.CurrentDirectory
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command.Length > 8_000
            ? "[Console]::InputEncoding = [Text.UTF8Encoding]::new($false); Invoke-Expression ([Console]::In.ReadToEnd()); if (-not $?) { exit 1 }"
            : command);
        return startInfo;
    }

}

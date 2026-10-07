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
        CancellationToken cancellationToken,
        Action<CommandOutputUpdate>? output = null,
        CancellationToken stopToken = default);
}

public sealed class CommandExecutionService : ICommandExecutionService
{
    private readonly ILogger<CommandExecutionService> _logger;

    /// <summary>Creates the restricted child-process runner.</summary>
    public CommandExecutionService(ILogger<CommandExecutionService> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Runs a recently authorized command in non-elevated PowerShell and captures bounded output.</summary>
    /// <param name="command">The exact command authorized by the user.</param>
    /// <param name="timeout">Total deadline, or InfiniteTimeSpan for supervised foreground execution.</param>
    /// <param name="cancellationToken">Application cancellation.</param>
    /// <param name="output">Receives normalized live output while the process is running.</param>
    /// <param name="stopToken">Stops only this command and returns its partial result.</param>
    /// <exception cref="InvalidOperationException">The authorization is missing or expired.</exception>
    public async Task<CommandExecutionResult> ExecuteAsync(
        ApprovedCommand command,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Action<CommandOutputUpdate>? output = null,
        CancellationToken stopToken = default)
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
            streamSource ? command.Text : null, output, stopToken).ConfigureAwait(false);
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
            ? "[Console]::InputEncoding = [Text.UTF8Encoding]::new($false); [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false); Invoke-Expression ([Console]::In.ReadToEnd()); if (-not $?) { exit 1 }"
            : command);
        return startInfo;
    }

}

// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Text;
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
    private const int MaximumCapturedCharactersPerStream = 32_768;
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

        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(5))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Command timeout must be between zero and five minutes.");
        }

        using var process = new Process
        {
            StartInfo = BuildStartInfo(command.Text),
            EnableRaisingEvents = true
        };
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Authorized command starting. AuthorizationId={AuthorizationId}, RiskScore={RiskScore}",
            command.AuthorizationId,
            command.Assessment.Score);
        if (!process.Start())
        {
            throw new InvalidOperationException("PowerShell could not be started.");
        }

        process.StandardInput.Close();
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(timeout);
        var outputTask = ReadBoundedAsync(process.StandardOutput, timeoutCancellation.Token);
        var errorTask = ReadBoundedAsync(process.StandardError, timeoutCancellation.Token);
        var timedOut = false;

        try
        {
            await Task.WhenAll(
                process.WaitForExitAsync(timeoutCancellation.Token),
                outputTask,
                errorTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            timedOut = timeoutCancellation.IsCancellationRequested;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
        }
        catch
        {
            await timeoutCancellation.CancelAsync().ConfigureAwait(false);
            TryKill(process);
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
            throw;
        }

        if (timedOut)
        {
            TryKill(process);
        }

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        stopwatch.Stop();
        _logger.LogInformation(
            "Authorized command completed. AuthorizationId={AuthorizationId}, ExitCode={ExitCode}, TimedOut={TimedOut}, ElapsedMs={ElapsedMs}",
            command.AuthorizationId,
            timedOut ? null : process.ExitCode,
            timedOut,
            stopwatch.ElapsedMilliseconds);
        return new CommandExecutionResult(
            command.Text,
            timedOut ? null : process.ExitCode,
            output.Text,
            error.Text,
            timedOut,
            output.Truncated || error.Truncated,
            stopwatch.ElapsedMilliseconds);
    }

    /// <summary>Builds a non-interactive, non-elevated PowerShell child-process definition.</summary>
    private static ProcessStartInfo BuildStartInfo(string command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            WorkingDirectory = Environment.CurrentDirectory,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);
        return startInfo;
    }

    /// <summary>Drains one redirected stream while retaining only a safe, useful prefix.</summary>
    private static async Task<CapturedStream> ReadBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        var buffer = new char[4096];
        var truncated = false;
        while (true)
        {
            int read;
            try
            {
                read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                truncated = true;
                break;
            }
            if (read == 0)
            {
                break;
            }

            var remaining = MaximumCapturedCharactersPerStream - builder.Length;
            if (remaining > 0)
            {
                builder.Append(buffer, 0, Math.Min(read, remaining));
            }

            truncated |= read > remaining;
        }

        return new CapturedStream(builder.ToString().TrimEnd(), truncated);
    }

    /// <summary>Stops only the authorized child process tree after timeout or cancellation.</summary>
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process ended between the state check and the kill request.
        }
    }

    private sealed record CapturedStream(string Text, bool Truncated);
}

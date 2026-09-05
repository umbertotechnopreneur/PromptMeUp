// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Text;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

internal static class BoundedProcessRunner
{
    private const int MaximumCapturedCharactersPerStream = 32_768;

    /// <summary>Runs a caller-approved process with one deadline for input, exit, and bounded output collection.</summary>
    internal static async Task<CommandExecutionResult> RunAsync(
        ProcessStartInfo startInfo, string displayCommand, TimeSpan timeout, CancellationToken cancellationToken, string? standardInput = null)
    {
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(5))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        cancellationToken.ThrowIfCancellationRequested();
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardInputEncoding = new UTF8Encoding(false);
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        using var process = new Process { StartInfo = startInfo };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        var stopwatch = Stopwatch.StartNew();
        if (!process.Start())
        {
            throw new InvalidOperationException("The requested process could not be started.");
        }
        var outputTask = ReadBoundedAsync(process.StandardOutput, deadline.Token);
        var errorTask = ReadBoundedAsync(process.StandardError, deadline.Token);
        var inputTask = WriteInputAsync(process.StandardInput, standardInput, deadline.Token);
        var timedOut = false;
        try
        {
            await Task.WhenAll(process.WaitForExitAsync(deadline.Token), inputTask, outputTask, errorTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            timedOut = deadline.IsCancellationRequested;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
        }
        catch
        {
            await deadline.CancelAsync().ConfigureAwait(false);
            TryKill(process);
            throw;
        }
        finally
        {
            if (deadline.IsCancellationRequested)
            {
                TryKill(process);
            }
        }
        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        return new CommandExecutionResult(displayCommand, timedOut ? null : process.ExitCode,
            output.Text, error.Text, timedOut, output.Truncated || error.Truncated, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>Transmits large approved source without command-line length limits or temporary source files.</summary>
    private static async Task WriteInputAsync(StreamWriter writer, string? input, CancellationToken cancellationToken)
    {
        try
        {
            if (input is not null)
            {
                await writer.BaseStream.WriteAsync(Encoding.UTF8.GetBytes(input), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            writer.Close();
        }
    }

    /// <summary>Drains a redirected stream while retaining only a bounded prefix, including on cancellation.</summary>
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

    /// <summary>Stops the started process tree when a deadline, cancellation, or I/O failure interrupts execution.</summary>
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
            // A process can exit between the state check and the kill request.
        }
    }

    private sealed record CapturedStream(string Text, bool Truncated);
}

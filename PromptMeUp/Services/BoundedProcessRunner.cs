// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using System.Diagnostics;
using System.Text;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

internal static class BoundedProcessRunner
{
    /// <summary>Runs a caller-approved process with one deadline for input, exit, and bounded output collection.</summary>
    /// <param name="startInfo">The process and argument list to launch.</param>
    /// <param name="displayCommand">The command retained in the result.</param>
    /// <param name="timeout">Total deadline, or InfiniteTimeSpan for foreground execution.</param>
    /// <param name="cancellationToken">Cancels the application.</param>
    /// <param name="standardInput">Optional approved source transported through stdin.</param>
    /// <param name="output">Receives serialized, normalized live updates.</param>
    /// <param name="stopToken">Interrupts this command without cancelling the application.</param>
    /// <exception cref="ArgumentOutOfRangeException">The deadline is not positive or infinite.</exception>
    /// <exception cref="InvalidOperationException">The process could not be started.</exception>
    internal static async Task<CommandExecutionResult> RunAsync(
        ProcessStartInfo startInfo, string displayCommand, TimeSpan timeout, CancellationToken cancellationToken,
        string? standardInput = null, Action<CommandOutputUpdate>? output = null, CancellationToken stopToken = default)
    {
        if (timeout != Timeout.InfiniteTimeSpan && (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > uint.MaxValue - 1))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        cancellationToken.ThrowIfCancellationRequested();
        stopToken.ThrowIfCancellationRequested();
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardInputEncoding = new UTF8Encoding(false);
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        using var process = new Process { StartInfo = startInfo };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopToken);
        deadline.CancelAfter(timeout);
        var stopwatch = Stopwatch.StartNew();
        if (!process.Start())
        {
            throw new InvalidOperationException("The requested process could not be started.");
        }
        var outputGate = new object();
        Action<CommandOutputUpdate>? publish = output is null ? null : update =>
        {
            lock (outputGate) output(update);
        };
        var outputTask = ReadBoundedAsync(process.StandardOutput, false, publish, deadline);
        var errorTask = ReadBoundedAsync(process.StandardError, true, publish, deadline);
        var inputTask = WriteInputAsync(process.StandardInput, standardInput, deadline);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            // A descendant retaining a pipe must not keep the UI waiting after the launched process exits.
            if (timeout == Timeout.InfiniteTimeSpan) deadline.CancelAfter(TimeSpan.FromSeconds(2));
            await Task.WhenAll(inputTask, outputTask, errorTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            timedOut = deadline.IsCancellationRequested && !stopToken.IsCancellationRequested;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = !stopToken.IsCancellationRequested;
        }
        catch
        {
            await deadline.CancelAsync().ConfigureAwait(false);
            TryKill(process);
            await ObserveCompletionAsync(inputTask, outputTask, errorTask).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (deadline.IsCancellationRequested)
            {
                TryKill(process);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }
        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
        var capturedOutput = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        try { await inputTask.ConfigureAwait(false); }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested) { }
        cancellationToken.ThrowIfCancellationRequested();
        var cancelled = stopToken.IsCancellationRequested;
        return new CommandExecutionResult(displayCommand, timedOut || cancelled ? null : process.ExitCode,
            capturedOutput.Text, error.Text, timedOut, capturedOutput.Truncated || error.Truncated, stopwatch.ElapsedMilliseconds)
        {
            Cancelled = cancelled,
            OutputWasStreamed = publish is not null
        };
    }

    /// <summary>Transmits large approved source without command-line length limits or temporary source files.</summary>
    /// <param name="writer">The child process input stream.</param>
    /// <param name="input">Optional approved source to transmit.</param>
    /// <param name="deadline">The shared lifetime cancelled if input transmission fails.</param>
    private static async Task WriteInputAsync(StreamWriter writer, string? input, CancellationTokenSource deadline)
    {
        try
        {
            if (input is not null)
            {
                await writer.BaseStream.WriteAsync(Encoding.UTF8.GetBytes(input), deadline.Token).ConfigureAwait(false);
            }
        }
        catch
        {
            await deadline.CancelAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            writer.Close();
        }
    }

    /// <summary>Drains a stream continuously while retaining bounded normalized first/last evidence.</summary>
    /// <param name="reader">The redirected stream to drain.</param>
    /// <param name="isError">Whether this is stderr.</param>
    /// <param name="publish">The optional serialized display callback.</param>
    /// <param name="deadline">The shared deadline cancelled if reading or rendering fails.</param>
    private static async Task<CapturedStream> ReadBoundedAsync(StreamReader reader, bool isError,
        Action<CommandOutputUpdate>? publish, CancellationTokenSource deadline)
    {
        var capture = new SimpleCommandOutput(isError, publish);
        var buffer = new char[4096];
        var truncated = false;
        try
        {
            while (true)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(), deadline.Token).ConfigureAwait(false);
                if (read == 0) break;
                capture.Append(buffer.AsSpan(0, read));
            }
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            truncated = true;
        }
        catch
        {
            await deadline.CancelAsync().ConfigureAwait(false);
            throw;
        }
        var text = capture.Complete();
        return new CapturedStream(text, truncated || capture.Truncated);
    }

    /// <summary>Observes cancelled I/O tasks after teardown so they cannot outlive the execution scope.</summary>
    /// <param name="tasks">Outstanding input/output operations.</param>
    private static async Task ObserveCompletionAsync(params Task[] tasks)
    {
        try { await Task.WhenAll(tasks).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (IOException) { }
    }

    /// <summary>Stops the started process tree when a deadline, cancellation, or I/O failure interrupts execution.</summary>
    /// <param name="process">The process whose descendants belong to this execution.</param>
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

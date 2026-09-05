// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class CommandExecutionServiceTests
{
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaximumReturnTime = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan WatchdogDeadline = TimeSpan.FromSeconds(45);

    /// <summary>Large source transport preserves multiline text, Unicode, and explicit or implicit command exit behavior.</summary>
    [Theory]
    [InlineData("[Console]::WriteLine('Tiếng Việt'); exit 7", 7)]
    [InlineData("throw 'Synthetic failure'", 1)]
    [InlineData("Write-Error 'Synthetic error'; 'done'", 0)]
    [InlineData("[Console]::WriteLine('Tiếng Việt')", 0)]
    public async Task ExecuteAsync_LargeSource_PreservesSemantics(string source, int expectedExitCode)
    {
        var command = "#" + new string('x', 40_000) + "\n" + source;
        var result = await new CommandExecutionService(NullLogger<CommandExecutionService>.Instance)
            .ExecuteAsync(Approve(command), TimeSpan.FromSeconds(15), default);
        Assert.Equal(expectedExitCode, result.ExitCode);
        Assert.Equal(command, result.Command);
        if (source.Contains("Tiếng Việt", StringComparison.Ordinal))
        {
            Assert.Equal("Tiếng Việt", result.StandardOutput);
        }
    }

    /// <summary>The shared helper runner bounds both output streams and honors cancellation before process startup.</summary>
    [Fact]
    public async Task SharedRunner_BoundsBothStreams_AndRejectsPrecancelledStart()
    {
        var startInfo = new ProcessStartInfo("pwsh");
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-Command", "[Console]::Write(('x' * 40000)); [Console]::Error.Write(('y' * 40000))" })
        {
            startInfo.ArgumentList.Add(argument);
        }
        var result = await BoundedProcessRunner.RunAsync(startInfo, "Synthetic helper", TimeSpan.FromSeconds(15), default);
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.OutputTruncated);
        Assert.Equal(32768, result.StandardOutput.Length);
        Assert.Equal(32768, result.StandardError.Length);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => BoundedProcessRunner.RunAsync(
            new ProcessStartInfo("synthetic-executable-that-does-not-exist"), "No start", TimeSpan.FromSeconds(1), new CancellationToken(true)));
    }

    /// <summary>Verifies a still-running process is terminated at its deadline while retaining output already received.</summary>
    [Fact]
    public async Task ExecuteAsync_ParentStillRunning_TimesOutWithPartialOutput()
    {
        var service = new CommandExecutionService(NullLogger<CommandExecutionService>.Instance);
        using var watchdog = new CancellationTokenSource(WatchdogDeadline);

        var result = await service.ExecuteAsync(Approve("[Console]::Out.WriteLine('started'); [Console]::Out.Flush(); [Threading.Thread]::Sleep(30000)"),
            ProcessDeadline, watchdog.Token);

        Assert.True(result.TimedOut);
        Assert.Contains("started", result.StandardOutput, StringComparison.Ordinal);
        Assert.Null(result.ExitCode);
        Assert.True(result.ElapsedMilliseconds < MaximumReturnTime.TotalMilliseconds, result.ElapsedMilliseconds.ToString());
    }

    /// <summary>Verifies external cancellation is propagated instead of returning a misleading command timeout.</summary>
    [Fact]
    public async Task ExecuteAsync_CallerCancels_PropagatesCancellation()
    {
        var service = new CommandExecutionService(NullLogger<CommandExecutionService>.Instance);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecuteAsync(Approve("Start-Sleep -Seconds 10"),
            TimeSpan.FromSeconds(10), cancellation.Token));
    }

    /// <summary>Verifies inherited output pipes cannot extend a command deadline after its parent has exited.</summary>
    [Fact]
    public async Task ExecuteAsync_DescendantHoldsOutput_DeadlineBoundsStreamDrain()
    {
        const string command = """
            $child = [Diagnostics.ProcessStartInfo]::new()
            $child.FileName = [Environment]::ProcessPath
            $child.UseShellExecute = $false
            $child.CreateNoWindow = $true
            $child.ArgumentList.Add('-NoProfile')
            $child.ArgumentList.Add('-NonInteractive')
            $child.ArgumentList.Add('-Command')
            $child.ArgumentList.Add('[Threading.Thread]::Sleep(30000)')
            $started = [Diagnostics.Process]::Start($child)
            [Console]::Out.WriteLine("child-id=$($started.Id)")
            [Console]::Out.WriteLine('parent-finished')
            [Console]::Out.Flush()
            """;
        var service = new CommandExecutionService(NullLogger<CommandExecutionService>.Instance);
        using var watchdog = new CancellationTokenSource(WatchdogDeadline);
        var stopwatch = Stopwatch.StartNew();

        // Allow cold CI startup while keeping the return bound below the child's independent 30-second lifetime.
        var result = await service.ExecuteAsync(Approve(command), ProcessDeadline, watchdog.Token);

        try
        {
            Assert.True(result.TimedOut);
            Assert.True(result.OutputTruncated);
            Assert.Null(result.ExitCode);
            Assert.Contains("parent-finished", result.StandardOutput, StringComparison.Ordinal);
            Assert.True(stopwatch.Elapsed < MaximumReturnTime, stopwatch.Elapsed.ToString());
        }
        finally
        {
            var pidLine = result.StandardOutput.Split('\n').FirstOrDefault(line => line.StartsWith("child-id=", StringComparison.Ordinal));
            if (pidLine is not null && int.TryParse(pidLine[9..], out var childId))
            {
                StopFixtureChild(childId);
            }
        }
    }

    /// <summary>Verifies the normal successful path captures both streams and retains the process exit code.</summary>
    [Fact]
    public async Task ExecuteAsync_ShortCommand_PreservesOutputAndExitCode()
    {
        var service = new CommandExecutionService(NullLogger<CommandExecutionService>.Instance);

        var result = await service.ExecuteAsync(Approve("[Console]::WriteLine('synthetic-out'); [Console]::Error.WriteLine('synthetic-error'); exit 7"),
            TimeSpan.FromSeconds(10), default);

        Assert.False(result.TimedOut);
        Assert.False(result.OutputTruncated);
        Assert.Equal(7, result.ExitCode);
        Assert.Equal("synthetic-out", result.StandardOutput);
        Assert.Equal("synthetic-error", result.StandardError);
    }

    /// <summary>Creates the test-only authorization capability for the exact safe fixture command.</summary>
    private static ApprovedCommand Approve(string command) => ApprovedCommand.Create(command,
        CommandRiskAssessmentService.AssessLocally(command, "en"));

    /// <summary>Stops only the synthetic sleeping child whose identifier was returned by this fixture.</summary>
    private static void StopFixtureChild(int childId)
    {
        try
        {
            using var child = Process.GetProcessById(childId);
            child.Kill(entireProcessTree: true);
            child.WaitForExit(5_000);
        }
        catch (ArgumentException)
        {
            // The synthetic sleeper already finished before cleanup.
        }
        catch (InvalidOperationException)
        {
            // The synthetic sleeper exited while cleanup inspected it.
        }
    }
}

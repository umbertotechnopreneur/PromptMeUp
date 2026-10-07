// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class CommandCountdownViewTests
{
    /// <summary>Completes only after all five seconds elapse when no key is pressed.</summary>
    [Fact]
    public async Task Countdown_NoKeys_WaitsForFiveSeconds()
    {
        var clock = new AdvancingTimeProvider();
        var view = CreateView([], clock);
        Assert.True(await view.WaitAsync(default));
        Assert.True(clock.Timestamp >= 6_000);
    }

    /// <summary>Executes immediately for Enter and refuses both cancellation shortcuts without waiting for the deadline.</summary>
    [Theory]
    [InlineData(ConsoleKey.Enter, false, true)]
    [InlineData(ConsoleKey.Escape, false, false)]
    [InlineData(ConsoleKey.C, true, false)]
    public async Task Countdown_Keyboard_ActsBeforeDeadline(ConsoleKey key, bool control, bool expected)
    {
        var clock = new AdvancingTimeProvider();
        var view = CreateView([new ConsoleKeyInfo('\0', key, false, false, control)], clock);
        Assert.Equal(expected, await view.WaitAsync(default));
        Assert.Equal(1_000, clock.Timestamp);
    }

    /// <summary>Checks buffered Escape even if another unrelated key arrives before it at the deadline.</summary>
    [Fact]
    public async Task Countdown_UnrelatedKeyBeforeEscape_Cancels()
    {
        var view = CreateView([new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false),
            new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false)], new AdvancingTimeProvider());
        Assert.False(await view.WaitAsync(default));
    }

    /// <summary>Checks that copying cancels the countdown and Ctrl+C never triggers the copy callback.</summary>
    /// <param name="control">Whether Control is held with the C key.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Countdown_CopyShortcut_NeverApprovesExecution(bool control)
    {
        var clock = new AdvancingTimeProvider();
        var view = CreateView([new ConsoleKeyInfo('c', ConsoleKey.C, false, false, control)], clock);
        var copyRequested = false;

        var authorized = await view.WaitAsync(default, () => copyRequested = true);

        Assert.False(authorized);
        Assert.Equal(!control, copyRequested);
        Assert.Equal(1_000, clock.Timestamp);
    }

    /// <summary>Lets successful copy-and-exit leave the countdown before it can approve execution.</summary>
    [Fact]
    public async Task Countdown_CopyAndExit_PropagatesGracefulExitBeforeDeadline()
    {
        var clock = new AdvancingTimeProvider();
        var view = CreateView([new ConsoleKeyInfo('c', ConsoleKey.C, false, false, false)], clock);

        await Assert.ThrowsAsync<PromptMeUp.Models.ApplicationExitRequestedException>(() =>
            view.WaitAsync(default, () => throw new PromptMeUp.Models.ApplicationExitRequestedException()));

        Assert.Equal(1_000, clock.Timestamp);
    }

    /// <summary>Honors application shutdown before producing an execution decision.</summary>
    [Fact]
    public async Task Countdown_Shutdown_NeverApproves()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var view = CreateView([], new AdvancingTimeProvider());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => view.WaitAsync(cancellation.Token));
    }

    /// <summary>Creates an isolated terminal renderer with scripted keys and a deterministic monotonic clock.</summary>
    private static CommandCountdownView CreateView(IEnumerable<ConsoleKeyInfo> keys, TimeProvider clock)
    {
        var queued = new Queue<ConsoleKeyInfo>(keys);
        var input = TestProxy.Create<IAnsiConsoleInput>((method, _) => method.Name switch
        {
            "IsKeyAvailable" => queued.Count > 0,
            "ReadKeyAsync" => Task.FromResult<ConsoleKeyInfo?>(queued.Dequeue()),
            _ => throw new NotSupportedException(method.Name)
        });
        var rendering = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(new StringWriter())
        });
        var console = TestProxy.Create<IAnsiConsole>((method, args) =>
            method.Name == "get_Input" ? input : method.Invoke(rendering, args));
        return new CommandCountdownView(console, new LocalizationService(), clock, isInteractive: true);
    }

    private sealed class AdvancingTimeProvider : TimeProvider
    {
        public long Timestamp { get; private set; }

        public override long TimestampFrequency => 1_000;

        /// <summary>Advances one synthetic second per observation without delaying the test for real seconds.</summary>
        public override long GetTimestamp() => Timestamp += 1_000;
    }
}

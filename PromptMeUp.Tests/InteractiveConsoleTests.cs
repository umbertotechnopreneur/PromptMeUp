// SPDX-License-Identifier: MIT

using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class InteractiveConsoleTests
{
    /// <summary>Verifies that ordinary input passes through without changing the selected key.</summary>
    [Fact]
    public async Task ReadKeyAsync_OrdinaryKey_ReturnsKey()
    {
        var expected = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        var input = new EscapeAwareConsoleInput(
            new StubConsoleInput(_ => Task.FromResult<ConsoleKeyInfo?>(expected)),
            CancellationToken.None);

        var actual = await input.ReadKeyAsync(intercept: true, CancellationToken.None);

        Assert.Equal(expected, actual);
    }

    /// <summary>Verifies that Escape raises the dedicated current-flow cancellation signal.</summary>
    [Fact]
    public async Task ReadKeyAsync_Escape_ThrowsInteractiveFlowCancellation()
    {
        var escape = new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false);
        var input = new EscapeAwareConsoleInput(
            new StubConsoleInput(_ => Task.FromResult<ConsoleKeyInfo?>(escape)),
            CancellationToken.None);

        await Assert.ThrowsAsync<InteractiveFlowCanceledException>(
            () => input.ReadKeyAsync(intercept: true, CancellationToken.None));
    }

    /// <summary>Verifies that global shutdown interrupts a prompt even when its local token is not cancelled.</summary>
    [Fact]
    public async Task ReadKeyAsync_ShutdownCancellation_StopsPendingRead()
    {
        using var shutdown = new CancellationTokenSource();
        var input = new EscapeAwareConsoleInput(
            new StubConsoleInput(WaitForCancellationAsync),
            shutdown.Token);

        var read = input.ReadKeyAsync(intercept: true, CancellationToken.None);
        shutdown.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
    }

    /// <summary>Waits indefinitely until the supplied input cancellation token is cancelled.</summary>
    private static async Task<ConsoleKeyInfo?> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return null;
    }

    private sealed class StubConsoleInput : IAnsiConsoleInput
    {
        private readonly Func<CancellationToken, Task<ConsoleKeyInfo?>> _read;

        /// <summary>Creates a deterministic console input around the supplied asynchronous read.</summary>
        public StubConsoleInput(Func<CancellationToken, Task<ConsoleKeyInfo?>> read) =>
            _read = read ?? throw new ArgumentNullException(nameof(read));

        /// <summary>Reports that the stub has input ready for prompt code.</summary>
        public bool IsKeyAvailable() => true;

        /// <summary>Returns the configured key synchronously.</summary>
        public ConsoleKeyInfo? ReadKey(bool intercept) =>
            _read(CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>Returns the configured key while forwarding cancellation.</summary>
        public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken) =>
            _read(cancellationToken);
    }
}

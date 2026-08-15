// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

internal sealed class InteractiveFlowCanceledException : OperationCanceledException
{
    /// <summary>Creates the signal raised when Escape cancels only the active interactive flow.</summary>
    public InteractiveFlowCanceledException()
        : base("The current interactive flow was cancelled.")
    {
    }
}

internal sealed class EscapeAwareAnsiConsole : IAnsiConsole
{
    private readonly IAnsiConsole _inner;

    /// <summary>Creates a console facade whose input supports Escape and application shutdown cancellation.</summary>
    public EscapeAwareAnsiConsole(IAnsiConsole inner, CancellationToken shutdownToken)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        Input = new EscapeAwareConsoleInput(inner.Input, shutdownToken);
    }

    public Profile Profile => _inner.Profile;

    public IAnsiConsoleCursor Cursor => _inner.Cursor;

    public IAnsiConsoleInput Input { get; }

    public IExclusivityMode ExclusivityMode => _inner.ExclusivityMode;

    public RenderPipeline Pipeline => _inner.Pipeline;

    /// <summary>Intentionally ignores clear requests so every application flow preserves terminal scrollback.</summary>
    public void Clear(bool home)
    {
        _ = home;
    }

    /// <summary>Writes one renderable through the wrapped console.</summary>
    public void Write(IRenderable renderable) => _inner.Write(renderable);

    /// <summary>Writes ANSI instructions through the wrapped console.</summary>
    public void WriteAnsi(Action<AnsiWriter> action) => _inner.WriteAnsi(action);
}

internal sealed class EscapeAwareConsoleInput : IAnsiConsoleInput
{
    private readonly IAnsiConsoleInput _inner;
    private readonly CancellationToken _shutdownToken;

    /// <summary>Creates an input facade that separates flow cancellation from application shutdown.</summary>
    public EscapeAwareConsoleInput(IAnsiConsoleInput inner, CancellationToken shutdownToken)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _shutdownToken = shutdownToken;
    }

    /// <summary>Reports whether a key is ready without consuming it.</summary>
    public bool IsKeyAvailable() => _inner.IsKeyAvailable();

    /// <summary>Reads one key while allowing Ctrl+C to cancel a synchronously rendered prompt.</summary>
    public ConsoleKeyInfo? ReadKey(bool intercept) =>
        ReadKeyAsync(intercept, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Reads one key and maps Escape to cancellation of the current interactive flow.</summary>
    public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownToken);
        var key = await _inner.ReadKeyAsync(intercept, linkedCancellation.Token).ConfigureAwait(false);
        return EnsureNotEscape(key);
    }

    /// <summary>Returns ordinary keys and raises the dedicated flow-cancellation signal for Escape.</summary>
    internal static ConsoleKeyInfo? EnsureNotEscape(ConsoleKeyInfo? key)
    {
        if (key?.Key == ConsoleKey.Escape)
        {
            throw new InteractiveFlowCanceledException();
        }

        return key;
    }
}

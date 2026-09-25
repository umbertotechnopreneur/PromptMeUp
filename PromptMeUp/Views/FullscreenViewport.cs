// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Owns the disposable alternate-buffer lifecycle shared by fullscreen terminal surfaces.</summary>
internal static class FullscreenViewport
{
    /// <summary>Checks whether the active terminal can safely host an interactive alternate-buffer surface.</summary>
    internal static bool CanUse(IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);
        return console.Profile.Capabilities.Ansi && console.Profile.Capabilities.AlternateBuffer
            && console.Profile.Capabilities.Interactive && console.Profile.Out.IsTerminal
            && !Console.IsInputRedirected && !Console.IsOutputRedirected
            && console.Profile.Width >= 60 && console.Profile.Height >= 20;
    }

    /// <summary>Runs an interaction in the alternate buffer while always restoring the cursor and terminal style.</summary>
    internal static void Run(IAnsiConsole console, Action interaction)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(interaction);
        console.AlternateScreen(() =>
        {
            console.Cursor.Hide();
            try
            {
                interaction();
            }
            finally
            {
                console.WriteAnsi(writer => writer.ResetStyle());
                console.Cursor.Show();
            }
        });
    }

    /// <summary>Moves to the top of the disposable surface and clears it only when its frame or theme changed.</summary>
    internal static FullscreenFrame BeginFrame(IAnsiConsole console, FullscreenFrame? previous)
    {
        ArgumentNullException.ThrowIfNull(console);
        var frame = new FullscreenFrame(console.Profile.Width, console.Profile.Height, TerminalTheme.Current.Id);
        console.WriteAnsi(writer =>
        {
            if (!previous.HasValue || previous.Value != frame)
            {
                // The caller is already inside the alternate buffer, so this cannot erase user scrollback.
                writer.Background(Style.Parse(TerminalTheme.Background).Foreground);
                writer.EraseInDisplay(2);
            }
            writer.CursorHome();
        });
        return frame;
    }
}

/// <summary>Identifies the terminal shape and visual theme used for the most recent fullscreen render.</summary>
internal readonly record struct FullscreenFrame(int Width, int Height, string Theme);

/// <summary>Waits for one key while preserving a pending read across resize and numeric-shortcut timeouts.</summary>
internal sealed class FullscreenInput(IAnsiConsole console, ILocalizationService text)
{
    private Task<ConsoleKeyInfo?>? _pendingRead;

    /// <summary>Reads one key, repaints on resize, and returns null when a caller-owned deadline expires.</summary>
    internal ConsoleKeyInfo? ReadKey(Action repaint, DateTimeOffset? deadline = null)
    {
        ArgumentNullException.ThrowIfNull(repaint);
        var pending = _pendingRead ??= ReadKeyAsync();
        var dimensions = (console.Profile.Width, console.Profile.Height);
        while (!pending.IsCompleted)
        {
            var remaining = deadline is { } value ? value - DateTimeOffset.UtcNow : TimeSpan.FromMilliseconds(100);
            if (remaining <= TimeSpan.Zero)
            {
                return null;
            }
            Task.WhenAny(pending, Task.Delay(TimeSpan.FromMilliseconds(Math.Min(100, remaining.TotalMilliseconds))))
                .GetAwaiter().GetResult();
            var current = (console.Profile.Width, console.Profile.Height);
            if (current != dimensions)
            {
                dimensions = current;
                repaint();
            }
        }
        _pendingRead = null;
        return pending.GetAwaiter().GetResult()
            ?? throw new IOException(text.Text("Form.EndOfInput"));
    }

    /// <summary>Forgets a completed surface's key task before the next independent interaction starts.</summary>
    internal void Reset() => _pendingRead = null;

    /// <summary>Uses the cancellation-aware input wrapper supplied by the application host.</summary>
    private async Task<ConsoleKeyInfo?> ReadKeyAsync() =>
        await console.Input.ReadKeyAsync(true, CancellationToken.None).ConfigureAwait(false);
}

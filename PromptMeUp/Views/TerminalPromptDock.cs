// SPDX-License-Identifier: MIT

using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Places an active prompt near the terminal bottom without replacing the normal scrollback buffer.</summary>
internal static class TerminalPromptDock
{
    /// <summary>Adds only the space needed for a prompt, with an opt-out for surfaces that keep their own layout.</summary>
    internal static void Align(IAnsiConsole console, int reservedRows, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reservedRows);
        if (!enabled || Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            return;
        }

        int blankRows;
        try
        {
            var height = Math.Min(console.Profile.Height, Console.WindowHeight);
            var row = Console.CursorTop - Console.WindowTop;
            if (height < 8 || row < 0 || row >= height)
            {
                return;
            }
            blankRows = Math.Max(0, height - Math.Min(reservedRows, height) - row);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or PlatformNotSupportedException)
        {
            // Cursor position is unavailable in some terminals; retain the ordinary flowing prompt there.
            return;
        }

        for (var index = 0; index < blankRows; index++)
        {
            console.WriteLine();
        }
    }
}

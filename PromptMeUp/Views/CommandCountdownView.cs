// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

internal sealed class CommandCountdownView(IAnsiConsole console, ILocalizationService text, TimeProvider? timeProvider = null)
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <summary>Shows a five-second progress bar and returns early for Enter or cancellation without clearing scrollback.</summary>
    internal async Task<bool> WaitAsync(CancellationToken cancellationToken)
    {
        if (!console.Profile.Capabilities.Interactive)
        {
            throw new InvalidOperationException(text.Text("Error.InteractiveRequired"));
        }
        console.MarkupLine($"[{TerminalTheme.Warning}]{Markup.Escape(text.Text("Direct.Keys"))}[/]");
        console.WriteLine();
        return await console.Progress().AutoClear(false).HideCompleted(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn
            {
                Width = Math.Clamp(console.Profile.Width / 3, 4, 40),
                CompletedStyle = Style.Parse(TerminalTheme.Accent),
                RemainingStyle = Style.Parse(TerminalTheme.Divider)
            })
            .StartAsync(async context =>
            {
                var task = context.AddTask(Description(5), maxValue: Duration.TotalMilliseconds);
                var started = _time.GetTimestamp();
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Poll availability so no pending read can steal input from the following chat turn.
                    while (console.Input.IsKeyAvailable())
                    {
                        var key = await console.Input.ReadKeyAsync(true, cancellationToken).ConfigureAwait(false);
                        var decision = InterpretKey(key);
                        if (decision.HasValue)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            task.Value = task.MaxValue;
                            task.Description = Markup.Escape(text.Text(decision.Value ? "Command.Running" : "Command.Cancelled"));
                            return decision.Value;
                        }
                    }

                    var elapsed = _time.GetElapsedTime(started);
                    var remaining = Duration - elapsed;
                    if (remaining <= TimeSpan.Zero)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        task.Value = task.MaxValue;
                        task.Description = Markup.Escape(text.Text("Command.Running"));
                        return true;
                    }
                    task.Value = elapsed.TotalMilliseconds;
                    task.Description = Description((int)Math.Ceiling(remaining.TotalSeconds));
                    await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
    }

    /// <summary>Recognizes immediate execution and both cancellation shortcuts, treating end of input as cancellation.</summary>
    internal static bool? InterpretKey(ConsoleKeyInfo? key) => key switch
    {
        null => false,
        { Key: ConsoleKey.Escape } => false,
        { Key: ConsoleKey.C, Modifiers: var modifiers } when modifiers.HasFlag(ConsoleModifiers.Control) => false,
        { Key: ConsoleKey.Enter, Modifiers: 0 } => true,
        _ => null
    };

    /// <summary>Formats a high-contrast localized countdown label.</summary>
    private string Description(int seconds) =>
        $"[bold {TerminalTheme.Accent}]{Markup.Escape(text.Text("Direct.Countdown", seconds))}[/]";
}

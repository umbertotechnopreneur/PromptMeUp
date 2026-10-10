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


using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

internal sealed class CommandCountdownView(
    IAnsiConsole console,
    ILocalizationService text,
    TimeProvider? timeProvider = null,
    bool? isInteractive = null)
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <summary>Shows a five-second progress bar and returns early for Enter or cancellation without clearing scrollback.</summary>
    /// <param name="cancellationToken">The token used to cancel the countdown.</param>
    /// <param name="copyAndExit">An optional action for copying the command and closing the application.</param>
    /// <exception cref="InvalidOperationException">The terminal is not interactive.</exception>
    /// <exception cref="OperationCanceledException">The countdown was cancelled.</exception>
    internal async Task<bool> WaitAsync(CancellationToken cancellationToken, Action? copyAndExit = null)
    {
        if (!(isInteractive ?? console.Profile.Capabilities.Interactive))
        {
            throw new InvalidOperationException(text.Text("Error.InteractiveRequired"));
        }
        ConversationText.Write(console, new Markup($"[{TerminalTheme.Warning}]{Markup.Escape(text.Text("Direct.Keys"))}[/]"));
        if (copyAndExit is not null)
        {
            ConversationText.Write(console, new Markup($"[bold {TerminalTheme.Info}]C[/] [{TerminalTheme.Primary}]{Markup.Escape(text.Text("Command.CopyAndExit"))}[/]"));
        }
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
                        if (key is { Key: ConsoleKey.C, Modifiers: 0 } && copyAndExit is not null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            task.StopTask();
                            copyAndExit();
                            return false;
                        }
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
        $"  [bold {TerminalTheme.Accent}]{Markup.Escape(text.Text("Direct.Countdown", seconds))}[/]";
}

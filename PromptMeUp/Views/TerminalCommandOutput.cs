// SPDX-License-Identifier: MIT

using System.Diagnostics;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface ICommandOutputSession : IDisposable
{
    CancellationToken StopToken { get; }

    void Write(CommandOutputUpdate update);
}

/// <summary>Shows live simple output without allowing child processes to address the surrounding terminal.</summary>
internal sealed class TerminalCommandOutput : ICommandOutputSession
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly TimeSpan _silenceNoticeInterval;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private readonly object _gate = new();
    private readonly Task _monitor;
    private string _rendered = "";
    private bool? _stream;
    private int _renderWidth;
    private long _lastOutput;
    private long _lastNotice;
    private bool _disposed;

    /// <summary>Starts a command-owned output region and a bounded keyboard/status polling loop.</summary>
    /// <param name="console">The terminal receiving live output.</param>
    /// <param name="text">Localized status messages.</param>
    /// <param name="silenceNoticeInterval">Delay before a status notice when the child has no new output.</param>
    internal TerminalCommandOutput(IAnsiConsole console, ILocalizationService text, TimeSpan silenceNoticeInterval)
    {
        _console = console;
        _text = text;
        _silenceNoticeInterval = silenceNoticeInterval;
        _console.MarkupLine($"[{TerminalTheme.Info}]{Markup.Escape(text.Text("Command.Running"))}[/]");
        if (console.Profile.Capabilities.Interactive)
            _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Command.LiveKeys"))}[/]");
        _console.WriteLine();
        _monitor = Task.Run(MonitorAsync);
    }

    public CancellationToken StopToken => _stop.Token;

    /// <summary>Appends new text or redraws only a simple line owned by this command.</summary>
    /// <param name="update">Normalized spans containing no terminal control characters.</param>
    public void Write(CommandOutputUpdate update)
    {
        lock (_gate)
        {
            if (_disposed) return;
            var plain = string.Concat(update.Spans.Select(span => span.Text));
            var skip = 0;
            if (_stream == update.IsError && plain.StartsWith(_rendered, StringComparison.Ordinal))
            {
                skip = _rendered.Length;
            }
            else if (_stream.HasValue)
            {
                if (_stream == update.IsError && CanRewrite())
                {
                    // These fixed controls only erase our current physical row; child escape bytes never reach the console.
                    _console.Profile.Out.Writer.Write("\r\u001b[2K");
                }
                else _console.WriteLine();
            }
            var rendered = new Paragraph();
            foreach (var span in update.Spans)
            {
                var offset = Math.Min(skip, span.Text.Length);
                skip -= offset;
                if (offset == span.Text.Length) continue;
                var foreground = Foreground(span.Foreground, update.IsError);
                rendered.Append(span.Text[offset..], Style.Parse(span.Bold ? $"bold {foreground}" : foreground));
            }
            _console.Write(rendered);
            _console.Profile.Out.Writer.Flush();
            _stream = update.IsError;
            _rendered = plain;
            _renderWidth = _console.Profile.Width;
            _lastOutput = _elapsed.ElapsedMilliseconds;
            if (update.CompleteLine) FinishLine();
        }
    }

    /// <summary>Stops polling before the next prompt can acquire terminal input.</summary>
    public void Dispose()
    {
        _shutdown.Cancel();
        try { _monitor.GetAwaiter().GetResult(); }
        finally
        {
            lock (_gate)
            {
                if (_stream.HasValue) FinishLine();
                _disposed = true;
            }
            _shutdown.Dispose();
            _stop.Dispose();
        }
    }

    /// <summary>Polls Escape without leaving pending reads and reports elapsed time during silence.</summary>
    private async Task MonitorAsync()
    {
        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                if (_console.Profile.Capabilities.Interactive && !_stop.IsCancellationRequested)
                {
                    while (_console.Input.IsKeyAvailable())
                    {
                        // Escape belongs to this command session rather than the surrounding chat flow.
                        var key = await (_console.Input is EscapeAwareConsoleInput wrapped
                            ? wrapped.ReadRawKeyAsync(true, _shutdown.Token)
                            : _console.Input.ReadKeyAsync(true, _shutdown.Token)).ConfigureAwait(false);
                        if (key is { Key: ConsoleKey.Escape })
                        {
                            await _stop.CancelAsync().ConfigureAwait(false);
                            break;
                        }
                    }
                }
                lock (_gate)
                {
                    var milliseconds = _elapsed.ElapsedMilliseconds;
                    if (milliseconds - _lastOutput >= _silenceNoticeInterval.TotalMilliseconds
                        && milliseconds - _lastNotice >= _silenceNoticeInterval.TotalMilliseconds)
                    {
                        if (_stream.HasValue) FinishLine();
                        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Command.Quiet", _elapsed.Elapsed.ToString(@"hh\:mm\:ss")))}[/]");
                        _lastNotice = milliseconds;
                    }
                }
                await Task.Delay(50, _shutdown.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
        catch
        {
            await _stop.CancelAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Restricts erasure to unwrapped ASCII rows with unchanged terminal dimensions.</summary>
    private bool CanRewrite() => _console.Profile.Capabilities.Ansi
        && _renderWidth == _console.Profile.Width
        && _rendered.Length < Math.Max(1, _renderWidth - 1)
        && _rendered.All(character => character is >= ' ' and <= '~');

    /// <summary>Closes the current child-owned line without clearing terminal history.</summary>
    private void FinishLine()
    {
        _console.WriteLine();
        _stream = null;
        _rendered = "";
    }

    /// <summary>Maps supported SGR foreground colors to trusted Spectre styles.</summary>
    /// <param name="code">A normalized basic ANSI foreground code.</param>
    /// <param name="isError">Whether the text originated on stderr.</param>
    private static string Foreground(int code, bool isError) => code switch
    {
        30 or 90 => TerminalTheme.Muted,
        31 => "red",
        32 => "green",
        33 => "yellow",
        34 => "blue",
        35 => "purple",
        36 => "teal",
        37 => "silver",
        91 => "red1",
        92 => "green1",
        93 => "yellow1",
        94 => "dodgerblue1",
        95 => "magenta1",
        96 => "cyan1",
        97 => "white",
        _ => isError ? TerminalTheme.Warning : TerminalTheme.Primary
    };
}

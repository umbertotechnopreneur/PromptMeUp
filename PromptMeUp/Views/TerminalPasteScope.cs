// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace PromptMeUp.Views;

internal sealed class TerminalPasteScope : IDisposable
{
    private const uint EnableVirtualTerminalInput = 0x0200;
    private readonly IAnsiConsole _console;
    private readonly IntPtr _inputHandle;
    private readonly uint _originalInputMode;
    private bool _restoreInputMode;
    private bool _disposed;

    /// <summary>Enables bracketed paste and Windows modifier reporting while retaining the original console input mode.</summary>
    public TerminalPasteScope(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        if (!console.Profile.Capabilities.Ansi || !console.Profile.Capabilities.Interactive
            || !console.Profile.Out.IsTerminal || Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            _inputHandle = GetStdHandle(-10);
            if (!GetConsoleMode(_inputHandle, out _originalInputMode))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot read the terminal input mode for paste handling.");
            }

            // Windows otherwise removes paste boundaries before Console.ReadKey can receive them.
            if (!SetConsoleMode(_inputHandle, _originalInputMode | EnableVirtualTerminalInput))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot enable terminal input for paste handling.");
            }

            _restoreInputMode = true;
        }

        try
        {
            _console.WriteAnsi(writer => writer.Write("\u001b[?2004h"));
            if (OperatingSystem.IsWindows())
            {
                // VT input alone flattens Shift+Enter; Windows key records preserve its modifier.
                _console.WriteAnsi(writer => writer.Write("\u001b[?9001h"));
            }
            Enabled = true;
        }
        catch
        {
            RestoreInputMode();
            throw;
        }
    }

    public bool Enabled { get; }

    /// <summary>Disables temporary keyboard and paste modes and restores native input even if terminal output fails.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (Enabled)
            {
                if (OperatingSystem.IsWindows())
                {
                    _console.WriteAnsi(writer => writer.Write("\u001b[?9001l"));
                }
                _console.WriteAnsi(writer => writer.Write("\u001b[?2004l"));
            }
        }
        finally
        {
            RestoreInputMode();
        }
    }

    /// <summary>Restores the native input mode captured before enabling virtual terminal input.</summary>
    private void RestoreInputMode()
    {
        if (!_restoreInputMode)
        {
            return;
        }

        if (!SetConsoleMode(_inputHandle, _originalInputMode))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot restore the terminal input mode after paste handling.");
        }

        _restoreInputMode = false;
    }

    /// <summary>Returns the native handle for the selected Windows standard stream.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int standardHandle);

    /// <summary>Reads the current mode of the Windows console input buffer.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr consoleHandle, out uint mode);

    /// <summary>Changes the mode of the Windows console input buffer.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(IntPtr consoleHandle, uint mode);
}

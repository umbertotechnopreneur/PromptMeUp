# Adapted for PromptMeUp from the author's CLI-Intelligence Windows screen capture tool.
[CmdletBinding()]
param(
    [string]$Mode = 'full_screen',
    [string]$OutputPath = '',
    [string]$_ValidationError = 'Capture failed: choose a supported mode and a new local PNG path with an existing, unlinked parent directory.'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $Mode -cnotin @('full_screen', 'active_window', 'active_monitor') -or [string]::IsNullOrWhiteSpace($OutputPath) -or $OutputPath -match '[\x00-\x1F\x7F]') { throw $_ValidationError }
$destination = [IO.Path]::GetFullPath($OutputPath)
$root = [IO.Path]::GetPathRoot($destination)
$fileName = [IO.Path]::GetFileName($destination)
if ($root -notmatch '^[A-Za-z]:\\$' -or $destination.Length -gt 260 -or $destination.Substring(2).Contains(':') -or $fileName.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or $fileName.EndsWith(' ') -or $fileName.EndsWith('.') -or $fileName -match '^(?i:CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|$)' -or [IO.Path]::GetExtension($destination) -ine '.png') { throw $_ValidationError }
$drive = [IO.DriveInfo]::new($root)
if ($drive.DriveType -notin @([IO.DriveType]::Fixed, [IO.DriveType]::Removable) -or -not $drive.IsReady) { throw $_ValidationError }
$parent = [IO.Path]::GetDirectoryName($destination)
if (-not [IO.Directory]::Exists($parent) -or (Test-Path -LiteralPath $destination)) { throw $_ValidationError }
function Assert-CapturePath {
    $ancestor = $parent
    while ($ancestor) {
        if ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw $_ValidationError }
        $ancestor = [IO.Path]::GetDirectoryName([IO.Path]::TrimEndingDirectorySeparator($ancestor))
    }
}
Assert-CapturePath
# These assemblies ship with supported Windows PowerShell 7; fail before writing if unavailable.
Add-Type -AssemblyName System.Drawing.Common
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

/// <summary>Reads Windows screen geometry without introducing a desktop framework dependency.</summary>
public static class PromptMeUpCaptureBounds
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rectangle { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rectangle Monitor;
        public Rectangle Work;
        public uint Flags;
    }
    /// <summary>Changes only the executing thread's DPI context for physical-pixel capture.</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    /// <summary>Gets the foreground window handle.</summary>
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    /// <summary>Gets visible window bounds.</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out Rectangle bounds);
    /// <summary>Detects a minimized foreground window.</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);
    /// <summary>Reads virtual screen coordinates and dimensions.</summary>
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
    /// <summary>Finds the monitor containing the foreground window.</summary>
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    /// <summary>Reads monitor geometry.</summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    /// <summary>Returns bounded visible capture coordinates without falling back to a broader capture.</summary>
    public static int[] GetBounds(string mode)
    {
        long left = GetSystemMetrics(76), top = GetSystemMetrics(77);
        long right = left + GetSystemMetrics(78), bottom = top + GetSystemMetrics(79);
        if (mode != "full_screen")
        {
            var window = GetForegroundWindow();
            if (window == IntPtr.Zero || IsIconic(window)) throw new InvalidOperationException();
            Rectangle rectangle;
            if (mode == "active_window")
            {
                if (!GetWindowRect(window, out rectangle)) throw new InvalidOperationException();
            }
            else if (mode == "active_monitor")
            {
                var monitor = MonitorFromWindow(window, 0);
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) throw new InvalidOperationException();
                rectangle = info.Monitor;
            }
            else throw new InvalidOperationException();
            left = Math.Max(left, rectangle.Left);
            top = Math.Max(top, rectangle.Top);
            right = Math.Min(right, rectangle.Right);
            bottom = Math.Min(bottom, rectangle.Bottom);
        }
        var width = right - left;
        var height = bottom - top;
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384 || width * height > 16777216)
            throw new InvalidOperationException();
        return new[] { checked((int)left), checked((int)top), (int)width, (int)height };
    }
}
'@
$previousDpi = [PromptMeUpCaptureBounds]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
if ($previousDpi -eq [IntPtr]::Zero) { throw $_ValidationError }
$bitmap = $null
$graphics = $null
$encoded = $null
try {
    $bounds = [PromptMeUpCaptureBounds]::GetBounds($Mode)
    $bitmap = [Drawing.Bitmap]::new($bounds[2], $bounds[3], [Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($bounds[0], $bounds[1], 0, 0, [Drawing.Size]::new($bounds[2], $bounds[3]), [Drawing.CopyPixelOperation]::SourceCopy)
    $encoded = [IO.MemoryStream]::new()
    $bitmap.Save($encoded, [Drawing.Imaging.ImageFormat]::Png)
    if ($encoded.Length -gt 67108864) { throw $_ValidationError }
    Assert-CapturePath
    # CreateNew also rejects a destination created after the preview; never overwrite an image.
    $output = [IO.FileStream]::new($destination, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $encoded.Position = 0
        $encoded.CopyTo($output)
        $output.Flush()
    }
    finally { $output.Dispose() }
    [pscustomobject]@{ Path = $destination; Width = $bounds[2]; Height = $bounds[3]; Bytes = $encoded.Length; Mode = $Mode } | ConvertTo-Json -Compress
}
finally {
    if ($encoded) { $encoded.Dispose() }
    if ($graphics) { $graphics.Dispose() }
    if ($bitmap) { $bitmap.Dispose() }
    [void][PromptMeUpCaptureBounds]::SetThreadDpiAwarenessContext($previousDpi)
}

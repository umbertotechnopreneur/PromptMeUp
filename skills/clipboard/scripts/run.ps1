# Adapted for PromptMeUp from the author's CLI-Intelligence clipboard tool.
[CmdletBinding()]
param(
    [string]$Action = 'read',
    [string]$Text = '',
    [string]$_ValidationError = 'Clipboard access failed or the selected text is invalid or too large.'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $Action -cnotin @('read', 'write')) { throw $_ValidationError }
if ($Action -ceq 'write') {
    if (-not $PSBoundParameters.ContainsKey('Text') -or $Text.Contains([char]0) -or [Text.Encoding]::UTF8.GetByteCount($Text) -gt 4096) { throw $_ValidationError }
    Set-Clipboard -Value $Text
    [pscustomobject]@{ Action = 'write'; Characters = $Text.Length } | ConvertTo-Json -Compress
    return
}
if ($PSBoundParameters.ContainsKey('Text')) { throw $_ValidationError }
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

/// <summary>Reads only a bounded Unicode clipboard prefix without allocating its full contents.</summary>
public static class PromptMeUpClipboardReader
{
    /// <summary>Opens the clipboard for bounded text access.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr owner);
    /// <summary>Releases the clipboard lock.</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();
    /// <summary>Obtains the Unicode clipboard handle.</summary>
    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint format);
    /// <summary>Locks a clipboard allocation for inspection.</summary>
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr handle);
    /// <summary>Releases the allocation lock.</summary>
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr handle);
    /// <summary>Reads allocation size without copying its contents.</summary>
    [DllImport("kernel32.dll")]
    private static extern UIntPtr GlobalSize(IntPtr handle);

    /// <summary>Returns at most 2049 UTF-16 characters, including one truncation sentinel.</summary>
    public static string Read()
    {
        if (!OpenClipboard(IntPtr.Zero)) throw new InvalidOperationException();
        try
        {
            var handle = GetClipboardData(13);
            if (handle == IntPtr.Zero) throw new InvalidOperationException();
            var count = (int)Math.Min(GlobalSize(handle).ToUInt64() / 2, 2049UL);
            if (count == 0) return string.Empty;
            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero) throw new InvalidOperationException();
            try
            {
                var value = Marshal.PtrToStringUni(pointer, count);
                var terminator = value.IndexOf('\0');
                return terminator < 0 ? value : value.Substring(0, terminator);
            }
            finally { GlobalUnlock(handle); }
        }
        finally { CloseClipboard(); }
    }
}
'@
$content = [PromptMeUpClipboardReader]::Read()
$truncated = $content.Length -gt 2048
if ($truncated) { $content = $content.Substring(0, 2048) }
while ($content.Length -gt 0 -and ([char]::IsHighSurrogate($content[$content.Length - 1]) -or [Text.Encoding]::UTF8.GetByteCount($content) -gt 4096)) {
    $content = $content.Substring(0, $content.Length - 1)
    $truncated = $true
}
[pscustomobject]@{ Action = 'read'; Text = $content; Truncated = $truncated } | ConvertTo-Json -Compress

// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PromptMeUp.Services;

/// <summary>Stores generic credentials in the current Windows user's operating-system vault.</summary>
[SupportedOSPlatform("windows")]
internal static class WindowsCredentialStore
{
    private const uint Generic = 1;
    private const uint LocalMachine = 2;
    private const int NotFound = 1168;

    /// <summary>Reads a vault entry, distinguishing a missing credential from a vault failure.</summary>
    internal static string? Read(string name)
    {
        if (!CredRead("PromptMeUp/" + name, Generic, 0, out var pointer))
        {
            var error = Marshal.GetLastWin32Error();
            return error == NotFound ? null : throw new Win32Exception(error);
        }
        try
        {
            var credential = Marshal.PtrToStructure<Credential>(pointer);
            if (credential.Blob == IntPtr.Zero || credential.BlobSize == 0
                || credential.BlobSize > OpenAiKeyPolicy.MaximumLength * 2 || credential.BlobSize % 2 != 0)
            {
                throw new InvalidDataException("The Windows credential has an invalid format.");
            }
            return Marshal.PtrToStringUni(credential.Blob, checked((int)credential.BlobSize / 2));
        }
        finally
        {
            CredFree(pointer);
        }
    }

    /// <summary>Writes a UTF-16 credential and clears the unmanaged secret buffer on every exit.</summary>
    internal static void Write(string name, string secret)
    {
        var pointer = Marshal.StringToCoTaskMemUni(secret);
        try
        {
            var credential = new Credential
            {
                Type = Generic,
                TargetName = "PromptMeUp/" + name,
                BlobSize = checked((uint)(secret.Length * 2)),
                Blob = pointer,
                Persist = LocalMachine,
                UserName = "PromptMeUp"
            };
            if (!CredWrite(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Marshal.ZeroFreeCoTaskMemUnicode(pointer);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public string? TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint BlobSize;
        public IntPtr Blob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    /// <summary>Reads one generic credential through the Windows credential API.</summary>
    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    /// <summary>Writes one generic credential through the Windows credential API.</summary>
    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref Credential credential, uint flags);

    /// <summary>Releases memory allocated by the Windows credential API.</summary>
    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr credential);
}

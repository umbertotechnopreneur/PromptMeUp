// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PromptMeUp.Infrastructure;

namespace PromptMeUp.Services;

/// <summary>Reports whether an explicitly requested desktop shortcut was created or already exists.</summary>
public enum DesktopLauncherResult { Created, AlreadyExists }

/// <summary>Creates an optional current-user desktop launcher without changing terminal or shell preferences.</summary>
public interface IDesktopLauncherService
{
    bool IsAvailable { get; }
    DesktopLauncherResult Create();
}

/// <summary>Uses a stable execution alias so the desktop shortcut survives MSIX version updates.</summary>
public sealed class DesktopLauncherService(AppPaths paths) : IDesktopLauncherService
{
    public bool IsAvailable => OperatingSystem.IsWindows() && File.Exists(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "hm.exe"));

    /// <summary>Creates a shortcut only when the Windows execution alias is installed.</summary>
    public DesktopLauncherResult Create()
    {
        if (!OperatingSystem.IsWindows() || !IsAvailable)
        {
            throw new PlatformNotSupportedException("The Windows hm execution alias is required for the desktop launcher.");
        }
        return CreateWindowsShortcut();
    }

    /// <summary>Writes only the requested user's shortcut and its managed launcher assets.</summary>
    [SupportedOSPlatform("windows")]
    private DesktopLauncherResult CreateWindowsShortcut()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
        {
            throw new DirectoryNotFoundException("The current user's desktop is unavailable.");
        }
        var shortcutPath = Path.Combine(desktop, "PromptMeUp.lnk");
        if (File.Exists(shortcutPath))
        {
            return DesktopLauncherResult.AlreadyExists;
        }
        var directory = Path.Combine(paths.DataDirectory, "desktop-launcher");
        Directory.CreateDirectory(directory);
        var icon = Path.Combine(directory, "PromptMeUp.ico");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "PromptMeUp.ico"), icon, overwrite: true);
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows shortcut creation is unavailable.");
        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows shortcut creation is unavailable.");
            shortcut = ((dynamic)shell).CreateShortcut(shortcutPath);
            dynamic link = shortcut;
            link.TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WindowsApps", "hm.exe");
            link.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            link.IconLocation = icon;
            link.Description = "PromptMeUp";
            link.Save();
            return DesktopLauncherResult.Created;
        }
        finally
        {
            if (shortcut is not null) { Marshal.FinalReleaseComObject(shortcut); }
            if (shell is not null) { Marshal.FinalReleaseComObject(shell); }
        }
    }
}

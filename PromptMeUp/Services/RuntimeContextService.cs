// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IRuntimeContextService
{
    RuntimeContext GetCurrent();
}

/// <summary>Builds a minimal, cross-platform hardware and directory context without executing a shell command.</summary>
public sealed class RuntimeContextService : IRuntimeContextService
{
    private const int MaximumValueLength = 240;
    private readonly ISensitiveDataRedactor _redactor;

    /// <summary>Creates the runtime-context service with the shared credential redactor.</summary>
    public RuntimeContextService(ISensitiveDataRedactor redactor) =>
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));

    /// <summary>Returns the current sanitized working-directory, platform, and hardware context.</summary>
    public RuntimeContext GetCurrent() => Build(CollectSnapshot(), _redactor);

    /// <summary>Converts a deterministic probe snapshot into the exact safe context supplied to the model.</summary>
    internal static RuntimeContext Build(RuntimeContextSnapshot snapshot, ISensitiveDataRedactor redactor)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(redactor);

        var platform = ResolvePlatform(snapshot.Platform);
        var cpuModel = SanitizeValue(snapshot.CpuModel, redactor);
        var cpu = string.IsNullOrWhiteSpace(cpuModel)
            ? $"{snapshot.ProcessorCount} logical processor(s), {snapshot.ProcessArchitecture}"
            : $"{cpuModel}; {snapshot.ProcessorCount} logical processor(s), {snapshot.ProcessArchitecture}";
        var memory = snapshot.TotalMemoryBytes is > 0
            ? FormatBytes(snapshot.TotalMemoryBytes.Value)
            : "physical total unavailable to the portable runtime";
        var gpuNames = snapshot.Gpus
            .Select(gpu => SanitizeValue(gpu, redactor))
            .Where(gpu => !string.IsNullOrWhiteSpace(gpu))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        return new RuntimeContext(
            SanitizeWorkingDirectory(snapshot.CurrentDirectory, snapshot.UserProfile, redactor),
            SanitizeValue(snapshot.OperatingSystem, redactor, "unavailable") ?? "unavailable",
            ResolveCommandEnvironment(platform),
            cpu,
            memory,
            gpuNames.Length == 0 ? "not exposed by the portable runtime" : string.Join("; ", gpuNames));
    }

    /// <summary>Collects facts through managed APIs, native platform APIs, and read-only system files only.</summary>
    private static RuntimeContextSnapshot CollectSnapshot()
    {
        var platform = DetectPlatform();
        return new RuntimeContextSnapshot(
            Environment.CurrentDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            RuntimeInformation.OSDescription,
            platform,
            RuntimeInformation.ProcessArchitecture.ToString(),
            Math.Max(1, Environment.ProcessorCount),
            ReadCpuModel(platform),
            ReadTotalMemoryBytes(platform),
            ReadGpuNames(platform));
    }

    /// <summary>Identifies the platform family used to select portable shell guidance and probes.</summary>
    private static RuntimePlatform DetectPlatform() => OperatingSystem.IsWindows()
        ? RuntimePlatform.Windows
        : OperatingSystem.IsMacOS()
            ? RuntimePlatform.MacOS
            : OperatingSystem.IsLinux()
                ? RuntimePlatform.Linux
                : RuntimePlatform.Other;

    /// <summary>Maps the internal platform identifier to a stable prompt-facing operating-system family.</summary>
    private static string ResolvePlatform(RuntimePlatform platform) => platform switch
    {
        RuntimePlatform.Windows => "Windows",
        RuntimePlatform.MacOS => "macOS",
        RuntimePlatform.Linux => "Linux",
        _ => "unknown platform"
    };

    /// <summary>Describes the shell syntax the assistant should use for commands on the active platform.</summary>
    private static string ResolveCommandEnvironment(string platform) => platform switch
    {
        "Windows" => "Windows console; prefer PowerShell 7 syntax and paths",
        "macOS" => "macOS terminal; use PowerShell 7 syntax with macOS paths; approved commands run in pwsh",
        "Linux" => "Linux terminal; use PowerShell 7 syntax with Linux paths; approved commands run in pwsh",
        _ => "operating-system family unavailable; approved commands run in PowerShell 7 (pwsh); ask before assuming paths"
    };

    /// <summary>Reads a CPU model label without invoking a shell or retaining device identifiers.</summary>
    private static string? ReadCpuModel(RuntimePlatform platform)
    {
        try
        {
            if (platform == RuntimePlatform.Windows && OperatingSystem.IsWindows())
            {
                return ReadWindowsCpuModel();
            }

            return platform switch
            {
                RuntimePlatform.Linux => ReadKeyValueFile("/proc/cpuinfo", "model name")
                    ?? ReadKeyValueFile("/proc/cpuinfo", "Hardware"),
                RuntimePlatform.MacOS => ReadMacString("machdep.cpu.brand_string"),
                _ => null
            };
        }
        catch (Exception) when (platform is RuntimePlatform.Windows or RuntimePlatform.Linux or RuntimePlatform.MacOS)
        {
            return null;
        }
    }

    /// <summary>Reads the Windows CPU display name after the caller has confirmed the current platform.</summary>
    [SupportedOSPlatform("windows")]
    private static string? ReadWindowsCpuModel() => Registry.GetValue(
        @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
        "ProcessorNameString",
        null) as string;

    /// <summary>Reads total physical memory where the active platform provides it without a subprocess.</summary>
    private static ulong? ReadTotalMemoryBytes(RuntimePlatform platform)
    {
        try
        {
            var total = platform switch
            {
                RuntimePlatform.Windows => ReadWindowsTotalMemoryBytes(),
                RuntimePlatform.Linux => ReadLinuxTotalMemoryBytes(),
                RuntimePlatform.MacOS => ReadMacUnsignedInteger("hw.memsize"),
                _ => null
            };
            if (total is > 0)
            {
                return total;
            }
        }
        catch (Exception) when (platform is RuntimePlatform.Windows or RuntimePlatform.Linux or RuntimePlatform.MacOS)
        {
            // Hardware introspection is optional and must never block a terminal answer.
        }

        return null;
    }

    /// <summary>Reads GPU display labels where a safe platform-native source is available.</summary>
    private static IReadOnlyList<string> ReadGpuNames(RuntimePlatform platform)
    {
        try
        {
            return platform switch
            {
                RuntimePlatform.Windows => ReadWindowsGpuNames(),
                RuntimePlatform.Linux => ReadLinuxGpuNames(),
                _ => []
            };
        }
        catch (Exception) when (platform is RuntimePlatform.Windows or RuntimePlatform.Linux)
        {
            return [];
        }
    }

    /// <summary>Returns the first non-empty value for a colon-delimited key in a small system information file.</summary>
    private static string? ReadKeyValueFile(string path, string expectedKey)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        foreach (var line in File.ReadLines(path).Take(256))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0
                || !string.Equals(line[..separator].Trim(), expectedKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = line[(separator + 1)..].Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    /// <summary>Reads Linux MemTotal in bytes from procfs without treating it as a user file.</summary>
    private static ulong? ReadLinuxTotalMemoryBytes()
    {
        var totalKb = ReadKeyValueFile("/proc/meminfo", "MemTotal");
        if (string.IsNullOrWhiteSpace(totalKb))
        {
            return null;
        }

        var numeric = totalKb.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return ulong.TryParse(numeric, NumberStyles.None, CultureInfo.InvariantCulture, out var kibibytes)
            ? kibibytes * 1_024UL
            : null;
    }

    /// <summary>Reads Windows physical memory through the documented native memory-status API.</summary>
    private static ulong? ReadWindowsTotalMemoryBytes()
    {
        var memoryStatus = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };
        return GlobalMemoryStatusEx(ref memoryStatus) ? memoryStatus.TotalPhysical : null;
    }

    /// <summary>Reads display-device labels from Windows without WMI, command execution, or unique identifiers.</summary>
    private static IReadOnlyList<string> ReadWindowsGpuNames()
    {
        var names = new List<string>();
        for (uint index = 0; index < 16; index++)
        {
            var displayDevice = new DisplayDevice
            {
                Size = Marshal.SizeOf<DisplayDevice>()
            };
            if (!EnumDisplayDevices(null, index, ref displayDevice, 0))
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(displayDevice.DeviceString))
            {
                names.Add(displayDevice.DeviceString);
            }
        }

        return names;
    }

    /// <summary>Reads Linux GPU model labels when Nvidia exposes them, otherwise maps safe DRM vendor identifiers.</summary>
    private static IReadOnlyList<string> ReadLinuxGpuNames()
    {
        var names = new List<string>();
        const string nvidiaDirectory = "/proc/driver/nvidia/gpus";
        if (Directory.Exists(nvidiaDirectory))
        {
            foreach (var path in Directory.EnumerateFiles(nvidiaDirectory, "information", SearchOption.AllDirectories).Take(3))
            {
                var model = ReadKeyValueFile(path, "Model");
                if (!string.IsNullOrWhiteSpace(model))
                {
                    names.Add(model);
                }
            }
        }

        if (names.Count > 0 || !Directory.Exists("/sys/class/drm"))
        {
            return names;
        }

        foreach (var vendorPath in Directory.EnumerateFiles("/sys/class/drm", "vendor", SearchOption.AllDirectories).Take(12))
        {
            var vendor = File.ReadAllText(vendorPath).Trim();
            var name = vendor switch
            {
                "0x10de" => "NVIDIA GPU",
                "0x1002" or "0x1022" => "AMD GPU",
                "0x8086" => "Intel GPU",
                "0x1af4" => "VirtIO GPU",
                "0x1234" => "QEMU virtual GPU",
                _ => string.Empty
            };
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    /// <summary>Reads a UTF-8 sysctl string on macOS without starting a child process.</summary>
    private static string? ReadMacString(string name)
    {
        nuint length = 0;
        if (SysctlByName(name, IntPtr.Zero, ref length, IntPtr.Zero, 0) != 0 || length == 0 || length > 4_096)
        {
            return null;
        }

        var buffer = Marshal.AllocHGlobal(checked((int)length));
        try
        {
            return SysctlByName(name, buffer, ref length, IntPtr.Zero, 0) == 0
                ? Marshal.PtrToStringUTF8(buffer)
                : null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>Reads an unsigned 64-bit sysctl value on macOS without a shell dependency.</summary>
    private static ulong? ReadMacUnsignedInteger(string name)
    {
        nuint length = (nuint)sizeof(ulong);
        var buffer = Marshal.AllocHGlobal(sizeof(long));
        try
        {
            if (SysctlByName(name, buffer, ref length, IntPtr.Zero, 0) != 0 || length != (nuint)sizeof(ulong))
            {
                return null;
            }

            return unchecked((ulong)Marshal.ReadInt64(buffer));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>Replaces an account home prefix and recognizable credentials before a working path reaches the provider.</summary>
    private static string SanitizeWorkingDirectory(string? value, string? userProfile, ISensitiveDataRedactor redactor)
    {
        var path = NormalizeValue(value);
        if (string.IsNullOrWhiteSpace(path))
        {
            return "unavailable";
        }

        if (IsNetworkPath(path))
        {
            return "network working directory (path withheld)";
        }

        var home = NormalizeValue(userProfile);
        if (!string.IsNullOrWhiteSpace(home) && IsPathWithin(path, home))
        {
            var suffix = path[home.Length..].TrimStart('\\', '/');
            path = string.IsNullOrEmpty(suffix) ? "~" : $"~/{suffix.Replace('\\', '/')}";
        }
        else
        {
            path = ReplaceConventionalHomePrefix(path);
        }

        return SanitizeValue(path, redactor, "unavailable") ?? "unavailable";
    }

    /// <summary>Recognizes common home-directory conventions when the runtime cannot resolve the profile path.</summary>
    private static string ReplaceConventionalHomePrefix(string path)
    {
        var windowsPrefix = System.Text.RegularExpressions.Regex.Match(
            path,
            @"^(?<drive>[A-Za-z]:)[\\/](?:Users|Documents and Settings)[\\/][^\\/]+(?<rest>[\\/].*)?$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (windowsPrefix.Success)
        {
            return $"~{windowsPrefix.Groups["rest"].Value.Replace('\\', '/')}";
        }

        var unixPrefix = System.Text.RegularExpressions.Regex.Match(
            path,
            @"^/(?:home|Users)/[^/]+(?<rest>/.*)?$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        return unixPrefix.Success ? $"~{unixPrefix.Groups["rest"].Value}" : path;
    }

    /// <summary>Checks a path prefix on either slash style without relying on the current host platform.</summary>
    private static bool IsPathWithin(string path, string root)
    {
        var normalizedPath = path.Replace('\\', '/').TrimEnd('/');
        var normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
        return string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith($"{normalizedRoot}/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Withholds UNC and mapped-network paths so a server identity never enters provider context.</summary>
    private static bool IsNetworkPath(string path)
    {
        if (path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            var root = Path.GetPathRoot(path);
            return !string.IsNullOrWhiteSpace(root)
                && new DriveInfo(root).DriveType == DriveType.Network;
        }
        catch (Exception) when (Path.IsPathRooted(path))
        {
            return false;
        }
    }

    /// <summary>Normalizes display text, removes controls, redacts credentials, and enforces a small provider-bound limit.</summary>
    private static string? SanitizeValue(string? value, ISensitiveDataRedactor redactor, string? fallback = null)
    {
        var normalized = NormalizeValue(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        var redacted = redactor.Redact(normalized);
        return redacted.Length <= MaximumValueLength
            ? redacted
            : string.Concat(redacted.AsSpan(0, MaximumValueLength - 1), "…");
    }

    /// <summary>Removes control characters and collapses whitespace before runtime values enter prompt text.</summary>
    private static string? NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new System.Text.StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value.Trim())
        {
            if (char.IsControl(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>Formats physical bytes as an intentionally rounded binary-unit value for operational guidance.</summary>
    private static string FormatBytes(ulong bytes) => string.Format(
        CultureInfo.InvariantCulture,
        "{0:0.0} GiB physical memory",
        bytes / (1024d * 1024d * 1024d));

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int Size;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string? DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? DeviceString;

        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? DeviceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? DeviceKey;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(
        string? device,
        uint deviceIndex,
        ref DisplayDevice displayDevice,
        uint flags);

    [DllImport("libSystem.B.dylib", EntryPoint = "sysctlbyname", SetLastError = true)]
    private static extern int SysctlByName(
        string name,
        IntPtr oldValue,
        ref nuint oldValueLength,
        IntPtr newValue,
        nuint newValueLength);
}

/// <summary>Captures raw probe values before the runtime-context service filters them for provider use.</summary>
internal sealed record RuntimeContextSnapshot(
    string? CurrentDirectory,
    string? UserProfile,
    string? OperatingSystem,
    RuntimePlatform Platform,
    string ProcessArchitecture,
    int ProcessorCount,
    string? CpuModel,
    ulong? TotalMemoryBytes,
    IReadOnlyList<string> Gpus);

/// <summary>Represents the three supported console platform families plus an explicit fallback.</summary>
internal enum RuntimePlatform
{
    Windows,
    Linux,
    MacOS,
    Other
}

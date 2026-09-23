// SPDX-License-Identifier: MIT

using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PromptMeUp.Infrastructure;

namespace PromptMeUp.Services;

/// <summary>Describes the local support archive without exposing its contents to another service.</summary>
public sealed record DiagnosticBundleResult(string Path, int LogFileCount, long Bytes);

/// <summary>Creates a bounded, redacted support archive that the user can inspect and attach to an email.</summary>
public sealed class DiagnosticBundleService(AppPaths paths, ISensitiveDataRedactor redactor,
    ILogger<DiagnosticBundleService> logger)
{
    private const int MaximumLogFiles = 14;
    private const int MaximumBytesPerLog = 4 * 1024 * 1024;

    /// <summary>Writes machine metadata and recent redacted logs to a new ZIP in the requested directory.</summary>
    public async Task<DiagnosticBundleResult> PrepareAsync(string outputDirectory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var destination = Path.GetFullPath(outputDirectory);
        if (!Directory.Exists(destination))
        {
            throw new DirectoryNotFoundException("The output directory is unavailable.");
        }

        var logFiles = Directory.Exists(paths.LogsDirectory)
            ? new DirectoryInfo(paths.LogsDirectory).EnumerateFiles("promptmeup-*.log", SearchOption.TopDirectoryOnly)
                .Where(file => (file.Attributes & FileAttributes.ReparsePoint) == 0)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(MaximumLogFiles)
                .ToArray()
            : [];
        var zipPath = ReservePath(destination);
        try
        {
            await using var stream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                bufferSize: 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8))
            {
                await WriteTextAsync(archive, "README.txt", Readme(logFiles.Length), cancellationToken).ConfigureAwait(false);
                await WriteTextAsync(archive, "diagnostics.json", DiagnosticsJson(logFiles.Length), cancellationToken).ConfigureAwait(false);
                foreach (var log in logFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var content = await ReadLogTailAsync(log.FullName, cancellationToken).ConfigureAwait(false);
                    content = Sanitize(content);
                    await WriteTextAsync(archive, "logs/" + log.Name, content, cancellationToken).ConfigureAwait(false);
                }
            }
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            var bytes = stream.Length;
            logger.LogInformation("Support bundle prepared. LogFileCount={LogFileCount}, ZipBytes={ZipBytes}", logFiles.Length, bytes);
            return new(zipPath, logFiles.Length, bytes);
        }
        catch
        {
            try { File.Delete(zipPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    /// <summary>Reserves a timestamped filename without replacing an existing support archive.</summary>
    private static string ReservePath(string directory)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        for (var suffix = 0; suffix < 100; suffix++)
        {
            var extra = suffix == 0 ? string.Empty : $"-{suffix}";
            var candidate = Path.Combine(directory, $"PromptMeUp-support-{timestamp}{extra}.zip");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new IOException("Could not reserve a unique support archive name.");
    }

    /// <summary>Builds a privacy-limited machine snapshot without names, addresses, serials, or environment values.</summary>
    private static string DiagnosticsJson(int logFileCount) => JsonSerializer.Serialize(new
    {
        generatedAtUtc = DateTimeOffset.UtcNow,
        application = new
        {
            name = "PromptMeUp",
            version = typeof(DiagnosticBundleService).Assembly.GetName().Version?.ToString(),
            packaged = AppContext.BaseDirectory.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase)
        },
        system = new
        {
            operatingSystem = RuntimeInformation.OSDescription,
            osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            framework = RuntimeInformation.FrameworkDescription,
            runtimeIdentifier = RuntimeInformation.RuntimeIdentifier,
            is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
            processorCount = Environment.ProcessorCount,
            uiCulture = CultureInfo.CurrentUICulture.Name,
            timeZone = TimeZoneInfo.Local.Id
        },
        terminal = new
        {
            inputRedirected = Console.IsInputRedirected,
            outputRedirected = Console.IsOutputRedirected,
            errorRedirected = Console.IsErrorRedirected
        },
        logs = new { includedFiles = logFileCount, maximumFiles = MaximumLogFiles, maximumBytesPerFile = MaximumBytesPerLog },
        excluded = new[] { "API keys", "environment variables", "database", "chat history", "memories", "user name", "computer name", "network addresses" }
    }, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>Explains the archive contents and asks the user to inspect it before sending.</summary>
    private static string Readme(int logFileCount) => $"""
        PromptMeUp support bundle

        This archive was created locally by hm --prepare-logs.
        It contains diagnostics.json and {logFileCount} available diagnostic log file(s).
        Each log is redacted again while the archive is created. Large logs contain only their most recent 4 MiB.

        It does not include the PromptMeUp database, conversations, memories, API keys, environment variables,
        user or computer names, network addresses, or an inventory of personal files.

        Please inspect the archive before attaching it to an email. Creating it does not send anything.
        """;

    /// <summary>Reads at most the recent bounded portion of a shared log and marks a truncated prefix.</summary>
    private static async Task<string> ReadLogTailAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var truncated = stream.Length > MaximumBytesPerLog;
        if (truncated)
        {
            stream.Seek(-MaximumBytesPerLog, SeekOrigin.End);
        }
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024, leaveOpen: false);
        if (truncated)
        {
            await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return truncated ? "[Earlier log content omitted by support bundle size limit.]\n" + content : content;
    }

    /// <summary>Applies credential redaction and replaces current-user path roots with neutral tokens.</summary>
    private string Sanitize(string content)
    {
        var safe = redactor.Redact(content);
        var roots = new[]
        {
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "%USERPROFILE%"),
            (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "%LOCALAPPDATA%"),
            (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "%APPDATA%")
        };
        foreach (var (root, replacement) in roots.OrderByDescending(item => item.Item1.Length))
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
                safe = safe.Replace(root, replacement, StringComparison.OrdinalIgnoreCase);
            }
        }
        return safe;
    }

    /// <summary>Writes one UTF-8 text entry without exposing a temporary uncompressed copy.</summary>
    private static async Task WriteTextAsync(ZipArchive archive, string name, string content,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}

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
    /// <param name="path">Log file to read while allowing its writer to continue.</param>
    /// <param name="cancellationToken">Cancels the snapshot read.</param>
    /// <exception cref="IOException">The log could not be opened or read.</exception>
    /// <exception cref="OperationCanceledException">The read was cancelled.</exception>
    private static async Task<string> ReadLogTailAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await ReadLogTailAsync(stream, MaximumBytesPerLog, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads a fixed byte window so a growing log cannot extend the snapshot.</summary>
    /// <param name="stream">Caller-owned seekable log stream.</param>
    /// <param name="maximumBytes">Maximum source bytes retained before decoding and redaction.</param>
    /// <param name="cancellationToken">Cancels reading or decoding.</param>
    /// <exception cref="ArgumentNullException">The stream is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The byte limit is not positive.</exception>
    /// <exception cref="IOException">The snapshot could not be read.</exception>
    /// <exception cref="NotSupportedException">The stream does not support seeking or reading.</exception>
    /// <exception cref="OperationCanceledException">The read was cancelled.</exception>
    internal static async Task<string> ReadLogTailAsync(Stream stream, int maximumBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotEnd = stream.Length;
        var byteCount = (int)Math.Min(snapshotEnd, maximumBytes);
        var truncated = snapshotEnd > byteCount;
        stream.Seek(snapshotEnd - byteCount, SeekOrigin.Begin);

        // The writer can keep going. This read stops at the original end of the log.
        var bytes = new byte[byteCount];
        var bytesRead = await stream.ReadAtLeastAsync(bytes, byteCount, throwOnEndOfStream: false,
            cancellationToken).ConfigureAwait(false);
        using var snapshot = new MemoryStream(bytes, 0, bytesRead, writable: false);
        using var reader = new StreamReader(snapshot, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
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

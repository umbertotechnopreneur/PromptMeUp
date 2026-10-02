// SPDX-License-Identifier: MIT

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class DiagnosticBundleServiceTests
{
    /// <summary>Creates an inspectable archive while excluding persistence and redacting credentials and user paths.</summary>
    [Fact]
    public async Task PrepareAsync_CreatesBoundedRedactedArchive()
    {
        using var fixture = new RegressionFixture();
        var key = "sk-" + new string('s', 32);
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        await File.WriteAllTextAsync(Path.Combine(fixture.Paths.LogsDirectory, "promptmeup-20260924.log"),
            $"startup path={profile} authorization=Bearer {key}\n");
        await File.WriteAllTextAsync(fixture.Paths.DatabasePath, "database must stay outside the archive");
        var service = new DiagnosticBundleService(fixture.Paths, new SensitiveDataRedactor(),
            NullLogger<DiagnosticBundleService>.Instance);

        var result = await service.PrepareAsync(fixture.Paths.DataDirectory, default);

        Assert.True(File.Exists(result.Path));
        Assert.Equal(1, result.LogFileCount);
        using var archive = ZipFile.OpenRead(result.Path);
        Assert.Equal(["README.txt", "diagnostics.json", "logs/promptmeup-20260924.log"],
            archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal));
        var log = await ReadEntryAsync(archive, "logs/promptmeup-20260924.log");
        Assert.DoesNotContain(key, log, StringComparison.Ordinal);
        Assert.DoesNotContain(profile, log, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%USERPROFILE%", log, StringComparison.Ordinal);
        var diagnostics = await ReadEntryAsync(archive, "diagnostics.json");
        Assert.DoesNotContain(Environment.MachineName, diagnostics, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.UserName, diagnostics, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database must stay outside", string.Join('\n', archive.Entries.Select(entry => entry.FullName)), StringComparison.Ordinal);
    }

    /// <summary>Excludes bytes appended after the log's length was captured.</summary>
    /// <param name="initial">Log contents at the start of the snapshot.</param>
    /// <param name="maximumBytes">Maximum source bytes to retain.</param>
    /// <param name="expected">Text remaining after partial-line removal.</param>
    [Theory]
    [InlineData("first\n", 128, "first\n")]
    [InlineData("first\nlast\n", 5, "[Earlier log content omitted by support bundle size limit.]\n")]
    public async Task ReadLogTail_GrowingStream_StopsAtSnapshotEnd(string initial, int maximumBytes, string expected)
    {
        using var stream = new GrowingLogStream(initial, "This line arrived later.\n");

        var actual = await DiagnosticBundleService.ReadLogTailAsync(stream, maximumBytes, default);

        Assert.Equal(expected, actual);
        Assert.True(stream.CanRead);
    }

    /// <summary>Drops a partial UTF-8 line without damaging the complete line that follows it.</summary>
    [Fact]
    public async Task ReadLogTail_Utf8Cut_PreservesFollowingLine()
    {
        const string retained = "Keep this emoji: 😀\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("older\n€\n" + retained));
        var maximumBytes = Encoding.UTF8.GetByteCount("€\n" + retained) - 1;

        var actual = await DiagnosticBundleService.ReadLogTailAsync(stream, maximumBytes, default);

        Assert.Equal("[Earlier log content omitted by support bundle size limit.]\n" + retained, actual);
        Assert.DoesNotContain("\uFFFD", actual);
    }

    /// <summary>A log with no line break cannot make partial-line skipping read past the byte window.</summary>
    [Fact]
    public async Task ReadLogTail_LongLine_RemainsBounded()
    {
        using var stream = new GrowingLogStream(new string('x', 512), new string('y', 512));

        var actual = await DiagnosticBundleService.ReadLogTailAsync(stream, 64, default);

        Assert.Equal("[Earlier log content omitted by support bundle size limit.]\n", actual);
        Assert.Equal(512, stream.Position);
    }

    /// <summary>Stops before touching a caller-owned stream when cancellation was already requested.</summary>
    [Fact]
    public async Task ReadLogTail_Cancelled_DoesNotRead()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("unchanged"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            DiagnosticBundleService.ReadLogTailAsync(stream, 64, cancellation.Token));

        Assert.Equal(0, stream.Position);
    }

    /// <summary>Simulates a log writer appending data after the reader captures the file length.</summary>
    private sealed class GrowingLogStream : MemoryStream
    {
        private readonly byte[] _addition;
        private bool _appended;

        /// <summary>Creates an expandable stream with a pending concurrent append.</summary>
        /// <param name="initial">Bytes visible when the snapshot begins.</param>
        /// <param name="addition">Text appended immediately before the first read.</param>
        public GrowingLogStream(string initial, string addition)
        {
            Write(Encoding.UTF8.GetBytes(initial));
            Position = 0;
            _addition = Encoding.UTF8.GetBytes(addition);
        }

        /// <summary>Appends once while preserving the reader's position.</summary>
        /// <param name="buffer">Destination whose size bounds the read.</param>
        /// <param name="cancellationToken">Cancels the underlying read.</param>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_appended)
            {
                var position = Position;
                Position = Length;
                Write(_addition);
                Position = position;
                _appended = true;
            }
            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    /// <summary>Reads a named UTF-8 archive entry for focused content assertions.</summary>
    private static async Task<string> ReadEntryAsync(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name) ?? throw new InvalidOperationException("Missing archive entry: " + name);
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

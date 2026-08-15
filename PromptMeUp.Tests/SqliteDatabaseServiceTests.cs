// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Infrastructure;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SqliteDatabaseServiceTests : IDisposable
{
    private readonly string _dataDirectory;
    private readonly AppPaths _paths;

    /// <summary>Creates an isolated filesystem-backed database location for each test.</summary>
    public SqliteDatabaseServiceTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "PromptMeUp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataDirectory);
        _paths = new AppPaths(
            _dataDirectory,
            Path.Combine(_dataDirectory, "promptmeup.db"),
            Path.Combine(_dataDirectory, "logs"),
            Path.Combine(_dataDirectory, "logs", "promptmeup-.log"),
            Path.Combine(_dataDirectory, "prompt"));
    }

    /// <summary>Removes the disposable database and releases pooled SQLite file handles.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }

    /// <summary>Verifies that an unversioned database receives the schema and version only after initialization.</summary>
    [Fact]
    public async Task InitializeAsync_UnversionedDatabase_CreatesCurrentSchema()
    {
        var service = CreateService();

        await service.InitializeAsync(CancellationToken.None);

        await using var connection = await OpenDatabaseAsync();
        Assert.Equal(1, await ReadSchemaVersionAsync(connection));
        Assert.Equal(1L, await ExecuteScalarInt64Async(connection, "SELECT COUNT(*) FROM app_settings WHERE id = 1;"));
    }

    /// <summary>Verifies that failed initialization leaves an unversioned database eligible for a later retry.</summary>
    [Fact]
    public async Task InitializeAsync_UnversionedIncompatibleDatabase_DoesNotSetVersion()
    {
        await using (var connection = await OpenDatabaseAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE ai_requests (id TEXT PRIMARY KEY);";
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<SqliteException>(
            () => CreateService().InitializeAsync(CancellationToken.None));

        await using var verificationConnection = await OpenDatabaseAsync();
        Assert.Equal(0, await ReadSchemaVersionAsync(verificationConnection));
        Assert.Equal(
            0L,
            await ExecuteScalarInt64Async(
                verificationConnection,
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'app_settings';"));
    }

    /// <summary>Verifies that current-schema initialization preserves settings and restores a missing index.</summary>
    [Fact]
    public async Task InitializeAsync_CurrentDatabase_PreservesExistingSettings()
    {
        var service = CreateService();
        await service.InitializeAsync(CancellationToken.None);
        await using (var connection = await OpenDatabaseAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE app_settings SET language = 'fr' WHERE id = 1;
                DROP INDEX ix_ai_requests_occurred;
                """;
            await command.ExecuteNonQueryAsync();
        }

        await service.InitializeAsync(CancellationToken.None);

        var settings = await service.LoadSettingsAsync(CancellationToken.None);
        Assert.Equal("fr", settings.Language);
        await using var verificationConnection = await OpenDatabaseAsync();
        Assert.Equal(1, await ReadSchemaVersionAsync(verificationConnection));
        Assert.Equal(
            1L,
            await ExecuteScalarInt64Async(
                verificationConnection,
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'ix_ai_requests_occurred';"));
    }

    /// <summary>Verifies that a newer database is rejected before initialization creates any schema objects.</summary>
    [Fact]
    public async Task InitializeAsync_FutureDatabase_RejectsWithoutApplyingDdl()
    {
        await using (var connection = await OpenDatabaseAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 2;";
            await command.ExecuteNonQueryAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().InitializeAsync(CancellationToken.None));

        Assert.Contains("'2'", exception.Message, StringComparison.Ordinal);
        await using var verificationConnection = await OpenDatabaseAsync();
        Assert.Equal(2, await ReadSchemaVersionAsync(verificationConnection));
        Assert.Equal(
            0L,
            await ExecuteScalarInt64Async(
                verificationConnection,
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'app_settings';"));
    }

    /// <summary>Creates the service under test with a no-op logger.</summary>
    private SqliteDatabaseService CreateService() =>
        new(_paths, NullLogger<SqliteDatabaseService>.Instance);

    /// <summary>Opens the isolated test database without connection pooling.</summary>
    private async Task<SqliteConnection> OpenDatabaseAsync()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString());
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>Reads the SQLite schema version from the supplied connection.</summary>
    private static async Task<int> ReadSchemaVersionAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>Executes a scalar query and converts its result to a 64-bit integer.</summary>
    private static async Task<long> ExecuteScalarInt64Async(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}

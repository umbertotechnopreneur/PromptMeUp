// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;

namespace PromptMeUp.Tests;

internal static class SqliteTestPool
{
    /// <summary>Releases only the production connection pool for one isolated test database.</summary>
    internal static void Clear(string databasePath)
    {
        // Match the service's pool key without clearing pools owned by other concurrent fixtures.
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString());
        SqliteConnection.ClearPool(connection);
    }
}

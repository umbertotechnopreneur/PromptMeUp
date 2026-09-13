// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;

namespace PromptMeUp.Tests;

internal static class SqliteTestPool
{
    /// <summary>Releases the database and memory connection pools for one isolated test database.</summary>
    internal static void Clear(string databasePath)
    {
        // Match each service's pool key without clearing other concurrent fixtures' pools.
        var settings = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        };
        ClearPool(settings);

        settings.Mode = SqliteOpenMode.ReadWrite;
        settings.DefaultTimeout = 5;
        ClearPool(settings);
    }

    /// <summary>Closes idle handles belonging to one exact connection-string pool.</summary>
    private static void ClearPool(SqliteConnectionStringBuilder settings)
    {
        using var connection = new SqliteConnection(settings.ToString());
        SqliteConnection.ClearPool(connection);
    }
}

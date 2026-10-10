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

        settings.ForeignKeys = true;
        ClearPool(settings);
    }

    /// <summary>Closes idle handles belonging to one exact connection-string pool.</summary>
    private static void ClearPool(SqliteConnectionStringBuilder settings)
    {
        using var connection = new SqliteConnection(settings.ToString());
        SqliteConnection.ClearPool(connection);
    }
}

// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services.Sqlite;

/// <summary>Defines the current SQLite schema version and its initialization SQL.</summary>
internal static class SqliteSchema
{
    /// <summary>Identifies the schema version supported by this application build.</summary>
    internal const int Version = 3;

    /// <summary>Enables the persistent write-ahead logging mode before schema creation starts.</summary>
    internal const string EnableWriteAheadLoggingSql = "PRAGMA journal_mode = WAL;";

    /// <summary>Creates every table and index belonging to the current schema.</summary>
    internal const string CreateSql = """
        CREATE TABLE IF NOT EXISTS app_settings (
            id INTEGER NOT NULL PRIMARY KEY CHECK (id = 1),
            setup_completed INTEGER NOT NULL CHECK (setup_completed IN (0, 1)),
            language TEXT NOT NULL,
            theme TEXT NOT NULL DEFAULT 'cyan' CHECK (length(theme) BETWEEN 1 AND 32),
            ai_enabled INTEGER NOT NULL CHECK (ai_enabled IN (0, 1)),
            model TEXT NOT NULL,
            reasoning_effort TEXT NOT NULL,
            output_detail TEXT NOT NULL,
            custom_instruction TEXT NOT NULL,
            include_windows_location INTEGER NOT NULL CHECK (include_windows_location IN (0, 1)),
            review_commands_with_ai INTEGER NOT NULL CHECK (review_commands_with_ai IN (0, 1)),
            prompt_caching_enabled INTEGER NOT NULL CHECK (prompt_caching_enabled IN (0, 1)),
            max_conversation_turns INTEGER NOT NULL CHECK (max_conversation_turns BETWEEN 2 AND 50),
            max_message_characters INTEGER NOT NULL CHECK (max_message_characters BETWEEN 500 AND 100000),
            max_context_percent INTEGER NOT NULL CHECK (max_context_percent BETWEEN 10 AND 95),
            context_token_budget INTEGER NOT NULL DEFAULT 16000 CHECK (context_token_budget BETWEEN 4000 AND 200000),
            max_command_output_characters INTEGER NOT NULL CHECK (max_command_output_characters BETWEEN 1000 AND 32768),
            command_timeout_seconds INTEGER NOT NULL CHECK (command_timeout_seconds BETWEEN 5 AND 300),
            endpoint TEXT NOT NULL,
            api_key_variable TEXT NOT NULL,
            admin_key_variable TEXT NOT NULL,
            updated_unix INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ai_requests (
            id TEXT NOT NULL PRIMARY KEY,
            conversation_id TEXT NOT NULL,
            prompt_id TEXT NOT NULL,
            occurred_unix INTEGER NOT NULL,
            completed_unix INTEGER NULL,
            endpoint_host TEXT NOT NULL,
            requested_model TEXT NOT NULL,
            returned_model TEXT NULL,
            user_prompt TEXT NOT NULL,
            assistant_response TEXT NULL,
            input_tokens INTEGER NOT NULL CHECK (input_tokens >= 0),
            cached_input_tokens INTEGER NOT NULL CHECK (cached_input_tokens >= 0),
            cache_write_tokens INTEGER NOT NULL CHECK (cache_write_tokens >= 0),
            output_tokens INTEGER NOT NULL CHECK (output_tokens >= 0),
            reasoning_tokens INTEGER NOT NULL CHECK (reasoning_tokens >= 0),
            total_tokens INTEGER NOT NULL CHECK (total_tokens >= 0),
            estimated_cost_microusd INTEGER NULL CHECK (estimated_cost_microusd >= 0),
            http_status INTEGER NULL CHECK (http_status BETWEEN 100 AND 599),
            elapsed_ms INTEGER NULL CHECK (elapsed_ms >= 0),
            provider_response_id TEXT NULL,
            provider_request_id TEXT NULL,
            success INTEGER NOT NULL CHECK (success IN (0, 1)),
            failure_code TEXT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_ai_requests_occurred ON ai_requests (occurred_unix);
        CREATE INDEX IF NOT EXISTS ix_ai_requests_conversation ON ai_requests (conversation_id, occurred_unix);

        CREATE TABLE IF NOT EXISTS persistent_memories (
            id TEXT NOT NULL PRIMARY KEY CHECK (length(id) = 32),
            scope_key TEXT NOT NULL CHECK (scope_key = 'global' OR length(scope_key) = 64),
            body TEXT NOT NULL CHECK (length(body) BETWEEN 1 AND 1000),
            updated_unix INTEGER NOT NULL CHECK (updated_unix >= 0)
        );
        CREATE INDEX IF NOT EXISTS ix_persistent_memories_scope ON persistent_memories (scope_key, updated_unix);

        CREATE TABLE IF NOT EXISTS ai_sessions (
            id TEXT NOT NULL PRIMARY KEY,
            started_unix INTEGER NOT NULL,
            ended_unix INTEGER NULL,
            language TEXT NOT NULL,
            model TEXT NOT NULL,
            kind TEXT NOT NULL,
            status TEXT NOT NULL,
            metadata_json TEXT NOT NULL CHECK (json_valid(metadata_json))
        );
        CREATE INDEX IF NOT EXISTS ix_ai_sessions_started ON ai_sessions (started_unix);

        CREATE TABLE IF NOT EXISTS ai_session_events (
            id TEXT NOT NULL PRIMARY KEY,
            session_id TEXT NOT NULL,
            sequence INTEGER NOT NULL CHECK (sequence > 0),
            occurred_unix INTEGER NOT NULL,
            event_type TEXT NOT NULL,
            payload_json TEXT NOT NULL CHECK (json_valid(payload_json)),
            FOREIGN KEY (session_id) REFERENCES ai_sessions (id) ON DELETE RESTRICT,
            UNIQUE (session_id, sequence)
        );
        CREATE INDEX IF NOT EXISTS ix_ai_session_events_session ON ai_session_events (session_id, sequence);

        CREATE TABLE IF NOT EXISTS activity_audit (
            id TEXT NOT NULL PRIMARY KEY,
            occurred_unix INTEGER NOT NULL,
            session_id TEXT NULL,
            activity_type TEXT NOT NULL,
            outcome TEXT NOT NULL,
            payload_json TEXT NOT NULL CHECK (json_valid(payload_json)),
            FOREIGN KEY (session_id) REFERENCES ai_sessions (id) ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS ix_activity_audit_occurred ON activity_audit (occurred_unix);
        CREATE INDEX IF NOT EXISTS ix_activity_audit_session ON activity_audit (session_id, occurred_unix);

        CREATE TABLE IF NOT EXISTS ai_model_pricing (
            provider TEXT NOT NULL,
            model TEXT NOT NULL,
            service_tier TEXT NOT NULL,
            context_window TEXT NOT NULL,
            currency TEXT NOT NULL CHECK (currency = 'usd'),
            input_microusd_per_million INTEGER NOT NULL CHECK (input_microusd_per_million >= 0),
            cached_input_microusd_per_million INTEGER NULL CHECK (cached_input_microusd_per_million >= 0),
            cache_write_microusd_per_million INTEGER NULL CHECK (cache_write_microusd_per_million >= 0),
            output_microusd_per_million INTEGER NOT NULL CHECK (output_microusd_per_million >= 0),
            source_url TEXT NOT NULL,
            retrieved_unix INTEGER NOT NULL,
            PRIMARY KEY (provider, model, service_tier, context_window)
        );

        CREATE TABLE IF NOT EXISTS organization_costs (
            id TEXT NOT NULL PRIMARY KEY,
            bucket_start_unix INTEGER NOT NULL,
            bucket_end_unix INTEGER NOT NULL,
            amount_microusd INTEGER NOT NULL CHECK (amount_microusd >= 0),
            currency TEXT NOT NULL,
            line_item TEXT NULL,
            project_id TEXT NULL,
            retrieved_unix INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_organization_costs_bucket ON organization_costs (bucket_start_unix);

        CREATE TABLE IF NOT EXISTS experimental_settings (
            scope_key TEXT NOT NULL CHECK(length(scope_key) = 64),
            name TEXT NOT NULL CHECK(length(name) BETWEEN 1 AND 120),
            value TEXT NOT NULL CHECK(length(value) <= 4096),
            PRIMARY KEY(scope_key, name)
        );
        CREATE TABLE IF NOT EXISTS learning_observations (
            id TEXT NOT NULL PRIMARY KEY CHECK(length(id) = 32),
            scope_key TEXT NOT NULL CHECK(length(scope_key) = 64),
            session_id TEXT NOT NULL CHECK(length(session_id) = 32),
            body TEXT NOT NULL CHECK(length(body) BETWEEN 1 AND 4000),
            created_unix INTEGER NOT NULL CHECK(created_unix >= 0)
        );
        CREATE INDEX IF NOT EXISTS ix_learning_scope ON learning_observations(scope_key, created_unix);
        CREATE TABLE IF NOT EXISTS memory_proposals (
            id TEXT NOT NULL PRIMARY KEY CHECK(length(id) = 32),
            scope_key TEXT NOT NULL CHECK(length(scope_key) = 64),
            payload_json TEXT NOT NULL CHECK(length(payload_json) <= 32768 AND json_valid(payload_json)),
            status TEXT NOT NULL CHECK(status IN ('pending', 'approved', 'rejected', 'expired')),
            created_unix INTEGER NOT NULL CHECK(created_unix >= 0)
        );
        CREATE INDEX IF NOT EXISTS ix_proposals_scope ON memory_proposals(scope_key, status, created_unix);
        CREATE TABLE IF NOT EXISTS memory_provenance (
            memory_id TEXT NOT NULL PRIMARY KEY,
            kind TEXT NOT NULL CHECK(kind IN ('memory', 'lesson', 'correction', 'preference')),
            sources_json TEXT NOT NULL CHECK(length(sources_json) <= 1024 AND json_valid(sources_json)),
            proposal_id TEXT NOT NULL CHECK(length(proposal_id) = 32),
            FOREIGN KEY(memory_id) REFERENCES persistent_memories(id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS learning_revisions (
            scope_key TEXT NOT NULL PRIMARY KEY CHECK(scope_key = 'global' OR length(scope_key) = 64),
            revision TEXT NOT NULL CHECK(length(revision) = 32)
        );

        CREATE TABLE IF NOT EXISTS sync_state (
            name TEXT NOT NULL PRIMARY KEY,
            value TEXT NOT NULL
        );

        """;
}

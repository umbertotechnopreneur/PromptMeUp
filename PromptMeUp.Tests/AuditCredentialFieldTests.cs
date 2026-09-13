// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Tests;

public sealed class AuditCredentialFieldTests
{
    /// <summary>Removes nested credential fields from every audit entry point while preserving valid JSON and typed usage metadata.</summary>
    [Fact]
    public async Task Audit_CredentialFields_RedactsAllPersistenceSurfaces()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var payload = new
        {
            accessToken = "synthetic-access-value",
            Credentials = new[]
            {
                new Dictionary<string, object>
                {
                    ["SecretAccessKey"] = "synthetic-secret-value",
                    ["SessionToken"] = "synthetic-session-value",
                    ["refresh-token"] = "synthetic-refresh-value",
                    ["OPENAI_ADMIN_KEY"] = "synthetic-admin-value",
                    ["tokenType"] = "Bearer",
                    ["accessTokenExpiresAt"] = "synthetic-expiry-metadata",
                    ["inputTokens"] = 42,
                    ["output_tokens"] = 9
                }
            },
            safe = "keep"
        };

        await fixture.Audit.StartSessionAsync("credential-fields", "chat", AppSettings.Default, payload, default);
        await fixture.Audit.AppendSessionEventAsync("credential-fields", "command-output", payload, default);
        await fixture.Audit.RecordAsync("command", "completed", "credential-fields", payload, default);

        foreach (var query in new[]
                 {
                     "SELECT metadata_json FROM ai_sessions;",
                     "SELECT payload_json FROM ai_session_events;",
                     "SELECT payload_json FROM activity_audit;"
                 })
        {
            var stored = Assert.IsType<string>(await fixture.ScalarAsync(query));
            Assert.DoesNotContain("synthetic-access-value", stored, StringComparison.Ordinal);
            Assert.DoesNotContain("synthetic-secret-value", stored, StringComparison.Ordinal);
            Assert.DoesNotContain("synthetic-session-value", stored, StringComparison.Ordinal);
            Assert.DoesNotContain("synthetic-refresh-value", stored, StringComparison.Ordinal);
            Assert.DoesNotContain("synthetic-admin-value", stored, StringComparison.Ordinal);

            using var json = JsonDocument.Parse(stored);
            var root = json.RootElement;
            Assert.Equal("[redacted]", root.GetProperty("accessToken").GetString());
            Assert.Equal("keep", root.GetProperty("safe").GetString());
            var credentials = root.GetProperty("Credentials")[0];
            Assert.Equal("[redacted]", credentials.GetProperty("SecretAccessKey").GetString());
            Assert.Equal("[redacted]", credentials.GetProperty("SessionToken").GetString());
            Assert.Equal("[redacted]", credentials.GetProperty("refresh-token").GetString());
            Assert.Equal("[redacted]", credentials.GetProperty("OPENAI_ADMIN_KEY").GetString());
            Assert.Equal("Bearer", credentials.GetProperty("tokenType").GetString());
            Assert.Equal("synthetic-expiry-metadata", credentials.GetProperty("accessTokenExpiresAt").GetString());
            Assert.Equal(42, credentials.GetProperty("inputTokens").GetInt32());
            Assert.Equal(9, credentials.GetProperty("output_tokens").GetInt32());
        }
    }
}

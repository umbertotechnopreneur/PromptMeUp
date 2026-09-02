// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class PrivacyRegressionTests
{
    /// <summary>Verifies credential values in shell assignments and quoted JSON are fully redacted without damaging adjacent data.</summary>
    [Theory]
    [InlineData("{\"password\":\"synthetic value\",\"safe\":\"keep\"}")]
    [InlineData("{\"api_key\":\"synthetic value\",\"safe\":\"keep\"}")]
    [InlineData("{\"access_token\":\"synthetic\\\"value\",\"safe\":\"keep\"}")]
    [InlineData("PASSWORD='synthetic value'; safe=keep")]
    [InlineData("PASSWORD=synthetic-value; safe=keep")]
    [InlineData("{\\\"password\\\":\\\"synthetic value\\\",\\\"safe\\\":\\\"keep\\\"}")]
    public void Redact_QuotedCredentials_PreservesSafeTextAndIsIdempotent(string text)
    {
        var redactor = new SensitiveDataRedactor();

        var safe = redactor.Redact(text);

        Assert.DoesNotContain("synthetic", safe, StringComparison.Ordinal);
        Assert.Contains("keep", safe, StringComparison.Ordinal);
        Assert.Contains("[redacted-credential]", safe, StringComparison.Ordinal);
        Assert.Equal(safe, redactor.Redact(safe));
        if (text.StartsWith("{\"", StringComparison.Ordinal))
        {
            using var json = JsonDocument.Parse(safe);
            Assert.Equal("keep", json.RootElement.GetProperty("safe").GetString());
        }
    }

    /// <summary>Verifies Unicode-escaped quotes in a serialized JSON string cannot bypass credential detection.</summary>
    [Fact]
    public void Redact_SerializedJson_DecodesBeforeCredentialDetection()
    {
        var text = JsonSerializer.Serialize("{\"password\":\"synthetic-value\",\"safe\":\"keep\"}");
        var redactor = new SensitiveDataRedactor();

        var safe = redactor.Redact(text);

        Assert.DoesNotContain("synthetic-value", safe, StringComparison.Ordinal);
        using var inner = JsonDocument.Parse(JsonSerializer.Deserialize<string>(safe)!);
        Assert.Equal("[redacted-credential]", inner.RootElement.GetProperty("password").GetString());
        Assert.Equal("keep", inner.RootElement.GetProperty("safe").GetString());
        Assert.Equal(safe, redactor.Redact(safe));
    }

    /// <summary>Verifies nested JSON command output reaches SQLite as valid JSON with credential values removed.</summary>
    [Fact]
    public async Task AppendSessionEventAsync_JsonOutput_RedactsNestedText()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.Audit.StartSessionAsync("privacy", "chat", AppSettings.Default, null, default);

        await fixture.Audit.AppendSessionEventAsync("privacy", "command-output",
            new { stdout = "{\"password\":\"synthetic-value\",\"safe\":\"keep\"}" }, default);

        var stored = Assert.IsType<string>(await fixture.ScalarAsync("SELECT payload_json FROM ai_session_events;"));
        Assert.DoesNotContain("synthetic-value", stored, StringComparison.Ordinal);
        using var outer = JsonDocument.Parse(stored);
        using var inner = JsonDocument.Parse(outer.RootElement.GetProperty("stdout").GetString()!);
        Assert.Equal("[redacted-credential]", inner.RootElement.GetProperty("password").GetString());
        Assert.Equal("keep", inner.RootElement.GetProperty("safe").GetString());
    }

    /// <summary>Verifies new credential-bearing preambles are rejected without changing persisted settings.</summary>
    [Fact]
    public async Task SaveSettingsAsync_CredentialPreamble_RejectsWithoutPersistence()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Database.SaveSettingsAsync(
            AppSettings.Default with { CustomInstruction = "password=synthetic-value" }, default));

        Assert.Equal(string.Empty, await fixture.ScalarAsync("SELECT custom_instruction FROM app_settings;"));
    }

    /// <summary>Verifies legacy preambles are scrubbed in the settings row and before being returned for use.</summary>
    [Fact]
    public async Task LoadSettingsAsync_LegacyCredentialPreamble_SanitizesBeforeUse()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        await fixture.ScalarAsync("UPDATE app_settings SET custom_instruction = $preamble;",
            ("$preamble", "Use concise answers. password=synthetic-value"));

        var settings = await fixture.Database.LoadSettingsAsync(default);

        Assert.DoesNotContain("synthetic-value", settings.CustomInstruction, StringComparison.Ordinal);
        Assert.Contains("Use concise answers.", settings.CustomInstruction, StringComparison.Ordinal);
        Assert.Equal(settings.CustomInstruction, await fixture.ScalarAsync("SELECT custom_instruction FROM app_settings;"));
        Assert.Equal(settings.CustomInstruction, (await fixture.Database.LoadSettingsAsync(default)).CustomInstruction);
    }
}

// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SensitiveDataStructureRegressionTests
{
    /// <summary>Preserves safe duplicate JSON properties and their original formatting without materialization failures.</summary>
    [Fact]
    public void Redact_DuplicateSafeFields_PreservesOriginalJson()
    {
        const string input = "{ \"safe\": \"first\", \"safe\": \"second\" }";

        Assert.Equal(input, new SensitiveDataRedactor().Redact(input));
    }

    /// <summary>Redacts every occurrence of duplicate credential properties without discarding safe JSON fields.</summary>
    [Fact]
    public void Redact_DuplicateCredentialFields_RedactsEveryValue()
    {
        const string input = "{\"accessToken\":\"synthetic-first\",\"accessToken\":\"synthetic-second\",\"safe\":\"keep\"}";
        var redactor = new SensitiveDataRedactor();

        var result = redactor.Redact(input);

        using var json = JsonDocument.Parse(result);
        var credentials = json.RootElement.EnumerateObject().Where(property => property.Name == "accessToken").ToArray();
        Assert.Equal(2, credentials.Length);
        Assert.All(credentials, property => Assert.Equal("[redacted-credential]", property.Value.GetString()));
        Assert.Equal("keep", json.RootElement.GetProperty("safe").GetString());
        Assert.DoesNotContain("synthetic", result, StringComparison.Ordinal);
        Assert.Equal(result, redactor.Redact(result));
    }

    /// <summary>Removes recognizable synthetic credentials used as JSON property names while retaining their safe values.</summary>
    [Fact]
    public void Redact_CredentialsInPropertyNames_RedactsNames()
    {
        var key = string.Concat("sk-", "proj-", new string('x', 32));
        var bearer = string.Concat("Bearer ", new string('y', 32));
        var input = JsonSerializer.Serialize(new Dictionary<string, string> { [key] = "keep-key", [bearer] = "keep-bearer" });
        var redactor = new SensitiveDataRedactor();

        var result = redactor.Redact(input);

        using var json = JsonDocument.Parse(result);
        Assert.Equal("keep-key", json.RootElement.GetProperty("[redacted-openai-key]").GetString());
        Assert.Equal("keep-bearer", json.RootElement.GetProperty("Bearer [redacted-bearer-token]").GetString());
        Assert.DoesNotContain(key, result, StringComparison.Ordinal);
        Assert.DoesNotContain(bearer, result, StringComparison.Ordinal);
        Assert.Equal(result, redactor.Redact(result));
    }

    /// <summary>Redacts a complete credential object or array atomically and leaves neighboring token counts typed.</summary>
    [Theory]
    [InlineData("{\"accessToken\":[\"synthetic-first\",\"synthetic-second\"],\"inputTokens\":42,\"safe\":\"keep\"}")]
    [InlineData("{\"accessToken\":{\"value\":\"synthetic-value\"},\"inputTokens\":42,\"safe\":\"keep\"}")]
    public void Redact_AggregateCredentialValue_RemovesWholeContainer(string input)
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.Redact(input);

        using var json = JsonDocument.Parse(result);
        Assert.Equal("[redacted-credential]", json.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal(42, json.RootElement.GetProperty("inputTokens").GetInt32());
        Assert.Equal("keep", json.RootElement.GetProperty("safe").GetString());
        Assert.DoesNotContain("synthetic", result, StringComparison.Ordinal);
        Assert.Equal(result, redactor.Redact(result));
    }

    /// <summary>Classifies decoded JSON field names consistently across escaped and spaced property spellings.</summary>
    [Theory]
    [InlineData("{\"access\\u0054oken\":\"synthetic-value\",\"safe\":\"keep\"}", "accessToken")]
    [InlineData("{\"api key\":\"synthetic-value\",\"safe\":\"keep\"}", "api key")]
    [InlineData("{\"Secret Access Key\":\"synthetic-value\",\"safe\":\"keep\"}", "Secret Access Key")]
    public void Redact_DecodedCredentialField_RedactsValue(string input, string field)
    {
        var result = new SensitiveDataRedactor().Redact(input);

        using var json = JsonDocument.Parse(result);
        Assert.Equal("[redacted-credential]", json.RootElement.GetProperty(field).GetString());
        Assert.Equal("keep", json.RootElement.GetProperty("safe").GetString());
        Assert.DoesNotContain("synthetic", result, StringComparison.Ordinal);
    }

    /// <summary>Consumes complete PowerShell here-strings despite interior quotes and preserves the following safe assignment.</summary>
    [Theory]
    [InlineData("PASSWORD=@'\nsynthetic's unsafetail\n'@; safe=keep")]
    [InlineData("accessToken=@\"\r\nsynthetic\" unsafetail\r\n\"@; safe=keep")]
    public void Redact_HereStringWithInteriorQuote_RemovesWholeValue(string input)
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.Redact(input);

        Assert.DoesNotContain("synthetic", result, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafetail", result, StringComparison.Ordinal);
        Assert.Contains("safe=keep", result, StringComparison.Ordinal);
        Assert.Equal(result, redactor.Redact(result));
    }

    /// <summary>Redacts all remaining here-string content when output truncation removes its line terminator.</summary>
    [Theory]
    [InlineData("PASSWORD=@'\nsynthetic's unsafetail")]
    [InlineData("accessToken=@\"\r\nsynthetic\" unsafetail")]
    public void Redact_TruncatedHereString_RemovesRemainder(string input)
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.Redact(input);

        Assert.DoesNotContain("synthetic", result, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafetail", result, StringComparison.Ordinal);
        Assert.Contains("[redacted-credential]", result, StringComparison.Ordinal);
        Assert.Equal(result, redactor.Redact(result));
    }
}

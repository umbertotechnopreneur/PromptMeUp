// SPDX-License-Identifier: MIT

using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

internal sealed class RegressionFixture : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "PromptMeUp.Tests", Guid.NewGuid().ToString("N"));
    public AppPaths Paths { get; }
    public SqliteDatabaseService Database { get; }
    public ActivityAuditService Audit { get; }
    public IEnvironmentSecretService Secrets { get; }

    /// <summary>Creates isolated persistence and synthetic collaborators that never access real credentials or the network.</summary>
    internal RegressionFixture()
    {
        Directory.CreateDirectory(_directory);
        Paths = new AppPaths(_directory, Path.Combine(_directory, "test.db"), _directory,
            Path.Combine(_directory, "test.log"), Path.Combine(AppContext.BaseDirectory, "prompt"));
        Database = new SqliteDatabaseService(Paths, NullLogger<SqliteDatabaseService>.Instance,
            new PromptInjectionProtectionService(), new SensitiveDataRedactor());
        Audit = new ActivityAuditService(Database, new SensitiveDataRedactor());
        Secrets = TestProxy.Create<IEnvironmentSecretService>((method, _) => method.Name switch
        {
            "Load" => "sk-" + new string('x', 32),
            "LooksLikeOpenAiKey" or "IsConfigured" => true,
            _ => throw new NotSupportedException(method.Name)
        });
    }

    /// <summary>Constructs the actual provider service with local HTTP responses and deterministic runtime facts.</summary>
    internal OpenAiService CreateOpenAi(HttpClient http, ILogger<OpenAiService>? logger = null) => new(
        http, Secrets,
        TestProxy.Create<IPromptCatalogService>((_, _) => Task.FromResult(new PromptDefinition(
            "query-system", 1, "Synthetic regression prompt", [],
            new Dictionary<string, string> { ["en"] = "Answer the user's question." }, new Dictionary<string, string>()))),
        TestProxy.Create<IRuntimeContextService>((_, _) => new RuntimeContext("~", "test", "PowerShell 7", "test", "test", "test")),
        Database, new AiCostCalculator(), Audit, new SensitiveDataRedactor(), new PromptInjectionProtectionService(),
        logger ?? NullLogger<OpenAiService>.Instance);

    /// <summary>Executes fixture-only SQL with bound values and returns its scalar result.</summary>
    internal async Task<object?> ScalarAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Paths.DatabasePath,
            Pooling = false
        }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }
        return await command.ExecuteScalarAsync();
    }

    /// <summary>Provides synthetic, visibly test-only prices for checking band selection independently of live prices.</summary>
    internal static AiModelPrice Price(string band, decimal input = 1m, string model = "gpt-5.6-terra") =>
        new("openai", model, "standard", band, "usd", input, input / 10, input * 1.25m, 0,
            "https://example.invalid/synthetic-prices", DateTimeOffset.UtcNow);

    /// <summary>Builds a local Responses envelope with usage and structured assistant content.</summary>
    internal static string ResponseJson(string text = "Answer", long inputTokens = 20, string model = "gpt-5.6-terra") =>
        JsonSerializer.Serialize(new
        {
            id = "synthetic-response",
            model,
            status = "completed",
            output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = JsonSerializer.Serialize(new { answer_markdown = text, commands = Array.Empty<object>() }) } } } },
            usage = new { input_tokens = inputTokens, output_tokens = 1, total_tokens = inputTokens + 1 }
        });

    /// <summary>Releases pooled handles and removes only this fixture's temporary directory.</summary>
    public void Dispose()
    {
        SqliteTestPool.Clear(Paths.DatabasePath);
        Directory.Delete(_directory, recursive: true);
    }
}

public class TestProxy : DispatchProxy
{
    private Func<MethodInfo, object?[], object?> _handler = null!;

    /// <summary>Creates an explicit interface fake that rejects any unexpected collaborator call.</summary>
    internal static T Create<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        var instance = Create<T, TestProxy>();
        ((TestProxy)(object)instance)._handler = handler;
        return instance;
    }

    /// <summary>Routes the invocation to the test's explicit behavior.</summary>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => _handler(targetMethod!, args ?? []);
}

internal sealed class SyntheticHttpHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
{
    /// <summary>Returns only in-memory content without opening a network connection.</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(response());
}

internal sealed class DelayedHttpContent(string body, TimeSpan delay) : HttpContent
{
    /// <summary>Delegates serialization to the cancellation-aware implementation.</summary>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    /// <summary>Models headers arriving immediately followed by a delayed response body.</summary>
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(body), cancellationToken);
    }

    /// <summary>Models a chunked response without a declared content length.</summary>
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}

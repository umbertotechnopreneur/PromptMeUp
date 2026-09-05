// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Services.OpenAi;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class ArtifactLimitTests
{
    /// <summary>The larger output budget reserves space inside the model window before any provider request.</summary>
    [Fact]
    public async Task Artifact_ContextBudget_ReservesOutputBeforeTransport()
    {
        using var fixture = new RegressionFixture();
        using var http = new HttpClient(new SyntheticHttpHandler(() => throw new InvalidOperationException("Transport must not run.")));
        var limits = new ArtifactLimits(maxScriptBytes: 2 * ArtifactLimits.Mebibyte, maxOutputTokens: 65_536);
        var service = fixture.CreateOpenAi(http, promptId: "script-system", limits: limits);
        await Assert.ThrowsAsync<ConversationLimitException>(() => service.SendAsync("script-system", "context-test",
            [new ChatMessage("user", new string('x', 1_400_000))],
            AppSettings.Default with { Model = "gpt-5.4-mini", MaxContextPercent = 95 }, "en", default));
    }

    /// <summary>Provider artifact bodies larger than the old two-MiB ceiling retain their complete Unicode source.</summary>
    [Fact]
    public async Task Artifact_LargeProviderEnvelope_PreservesCompleteSource()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var source = "# " + new string('é', 350_000);
        var body = JsonSerializer.Serialize(new
        {
            id = "large-artifact",
            model = "gpt-5.6-terra",
            status = "completed",
            output = new[] { new { content = new[] { new { type = "output_text", text = JsonSerializer.Serialize(new { explanation = "Synthetic artifact", source }) } } } },
            usage = new { input_tokens = 20, output_tokens = 10, total_tokens = 30 }
        });
        Assert.True(Encoding.UTF8.GetByteCount(body) > 2 * ArtifactLimits.Mebibyte);
        using var http = new HttpClient(new SyntheticHttpHandler(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        { Content = new StringContent(body, Encoding.UTF8) }));
        var response = await fixture.CreateOpenAi(http, promptId: "script-system").SendAsync("script-system", "large-response",
            [new ChatMessage("user", "Generate source")], AppSettings.Default, "en", default);
        Assert.Equal(source, new ScriptArtifactService(new SensitiveDataRedactor(), new LocalizationService()).Parse(response.Text).Source);
    }

    /// <summary>A complete one-MiB script round-trips through the same UTF-8 read and write limit.</summary>
    [Fact]
    public async Task Script_OneMebibyte_RoundTrips()
    {
        using var fixture = new RegressionFixture();
        var service = new ScriptArtifactService(new SensitiveDataRedactor(), new LocalizationService());
        var source = "#" + new string('x', ArtifactLimits.Mebibyte - 1);
        var path = Path.Combine(fixture.Paths.DataDirectory, "large.ps1");
        await service.SaveAsync(path, source, default);
        Assert.Equal(ArtifactLimits.Mebibyte, new FileInfo(path).Length);
        Assert.Equal(source, await service.ReadAsync(path, default));
        Assert.Equal(source, service.Parse(JsonSerializer.Serialize(new { explanation = "Synthetic source", source })).Source);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(path + ".ps1", source + "x", default));
        Assert.False(File.Exists(path + ".ps1"));
    }

    /// <summary>Measures UTF-8 bytes, supports overrides, and rejects a file larger than the configured budget.</summary>
    [Fact]
    public async Task Script_UnicodeAndOverride_UseByteLimits()
    {
        using var fixture = new RegressionFixture();
        var text = new LocalizationService();
        var small = new ScriptArtifactService(new SensitiveDataRedactor(), text, new ArtifactLimits(maxScriptBytes: 8));
        var path = Path.Combine(fixture.Paths.DataDirectory, "unicode.ps1");
        await small.SaveAsync(path, "éééé", default);
        Assert.Equal("éééé", await small.ReadAsync(path, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => small.SaveAsync(path + ".ps1", "ééééx", default));
        await File.WriteAllTextAsync(path, "ééééx", new UTF8Encoding(false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => small.ReadAsync(path, default));
        var larger = new ScriptArtifactService(new SensitiveDataRedactor(), text, new ArtifactLimits(maxScriptBytes: 16));
        Assert.Equal("ééééx", await larger.ReadAsync(path, default));
    }

    /// <summary>A serialized multilingual plan above one MiB can be reopened and retains its checkpoint.</summary>
    [Fact]
    public async Task Plan_LargeUnicode_RoundTripsAndRejectsBeforeReplacing()
    {
        using var fixture = new RegressionFixture();
        var text = new LocalizationService();
        var store = new PlanStore(fixture.Paths, new SensitiveDataRedactor(), text);
        var smallStore = new PlanStore(fixture.Paths, new SensitiveDataRedactor(), text, new ArtifactLimits(maxPlanBytes: ArtifactLimits.Mebibyte));
        var plan = new ExecutionPlan(1, Guid.NewGuid().ToString("N"), "Synthetic goal", Path.GetTempPath(),
            [new PlanStep("Inspect", "Get-Date", "Get-Date", new string('é', 200_000), PlanStepStatus.Completed)]);
        await store.SaveAsync(plan, default);
        var path = Path.Combine(fixture.Paths.DataDirectory, "plans", plan.Id + ".json");
        Assert.True(new FileInfo(path).Length > ArtifactLimits.Mebibyte);
        var loaded = await store.LoadAsync(plan.Id, default);
        Assert.Equal(plan.Steps[0], loaded.Steps[0]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => smallStore.LoadAsync(plan.Id, default));
        var original = await File.ReadAllBytesAsync(path);
        await Assert.ThrowsAsync<InvalidOperationException>(() => smallStore.SaveAsync(plan, default));
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    /// <summary>Rejects inconsistent or malformed configuration without silently changing requested values.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("65")]
    [InlineData("1.5")]
    [InlineData("")]
    [InlineData("invalid")]
    public void Configuration_InvalidValue_FailsExplicitly(string value) =>
        Assert.Throws<InvalidOperationException>(() => ArtifactLimitConfiguration.Load(_ => value, new LocalizationService()));

    /// <summary>Configuration changes file limits and the separate artifact token budget while preserving chat defaults.</summary>
    [Fact]
    public void Configuration_Overrides_AreApplied()
    {
        var limits = ArtifactLimitConfiguration.Load(name => name switch
        {
            "PROMPTMEUP_MAX_SCRIPT_MIB" => "2",
            "PROMPTMEUP_MAX_PLAN_MIB" => "16",
            "PROMPTMEUP_MAX_ARTIFACT_OUTPUT_TOKENS" => "32768",
            _ => null
        }, new LocalizationService());
        Assert.Equal(2 * ArtifactLimits.Mebibyte, limits.MaxScriptBytes);
        Assert.Equal(16 * ArtifactLimits.Mebibyte, limits.MaxPlanBytes);
        var prompt = new PromptDefinition("script-system", 2, "Test", [],
            new Dictionary<string, string> { ["en"] = "Limit {max_script_bytes}" }, new Dictionary<string, string>());
        Assert.Equal(32768, OpenAiRequestBuilder.ResolveMaxOutputTokens(prompt, "compact", limits));
        Assert.Equal(900, OpenAiRequestBuilder.ResolveMaxOutputTokens(prompt with { Id = "query-system" }, "compact", limits));
        Assert.Equal("Limit 2097152", OpenAiRequestBuilder.BuildInstructions(prompt, AppSettings.Default, "en", limits: limits));
    }

    /// <summary>Large escaped script input crosses the artifact workflow without inheriting the ordinary chat limit.</summary>
    [Fact]
    public async Task ArtifactAssistant_LargeEscapedRequest_PreservesCompleteInput()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var original = "# " + new string('é', 200_000);
        var request = JsonSerializer.Serialize(new { request = "Review this script", original });
        Assert.True(request.Length > ArtifactLimits.Mebibyte);
        string? received = null;
        var provider = TestProxy.Create<IOpenAiService>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            received = Assert.IsAssignableFrom<IReadOnlyList<ChatMessage>>(args[2])[0].Content;
            return Task.FromResult(OpenAiResponseParser.ParseResponse(RegressionFixture.ResponseJson(), 200, 1, null, true));
        });
        var shell = TestProxy.Create<IConsoleShellView>((method, args) => method.Name == "RunWithStatusAsync"
            ? ((Func<Task<AiResponse>>)args[1]!)() : null);
        var text = new LocalizationService();
        var assistant = new ArtifactAssistant(provider, fixture.Audit, new BoundedTextInput(new SensitiveDataRedactor(), text), shell, text);
        await assistant.SendAsync("script-system", request, AppSettings.Default, default);
        Assert.Equal(request, received);
        Assert.Equal(original, JsonDocument.Parse(received!).RootElement.GetProperty("original").GetString());
    }

    /// <summary>Validates a one-MiB source through the real runner without putting source in process arguments or executing it.</summary>
    [Fact]
    public async Task Validation_OneMebibyteSource_PassesThroughStandardInput()
    {
        var source = "#" + new string('x', ArtifactLimits.Mebibyte - 48) + "\nthrow 'This source must not be executed'";
        var command = ScriptArtifactService.BuildValidationCommand(source);
        var approved = ApprovedCommand.Create(command, new CommandRiskAssessment(35, CommandRiskLevel.Medium, "Parser only", false, null));
        var result = await new CommandExecutionService(NullLogger<CommandExecutionService>.Instance)
            .ExecuteAsync(approved, TimeSpan.FromSeconds(30), default);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(command, result.Command);
        using var document = JsonDocument.Parse(result.StandardOutput);
        Assert.True(document.RootElement.GetProperty("SyntaxValid").GetBoolean());
    }
}

// SPDX-License-Identifier: MIT

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class RecipeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hm-recipes-" + Guid.NewGuid().ToString("N"));
    private readonly LocalizationService _text = new();

    /// <summary>Never stores invocation parameter values back into the reusable recipe definition.</summary>
    [Fact]
    public async Task Bind_SaveDefinition_DoesNotPersistInvocationValues()
    {
        var recipe = Recipe();
        var store = Store();
        var plan = store.Bind(recipe, new Dictionary<string, string> { ["message"] = "one-run-value" }, _directory);
        await store.SaveAsync(recipe, CancellationToken.None);
        var json = File.ReadAllText(Path.Combine(_directory, "recipes", "echo-message.json"));
        Assert.DoesNotContain("one-run-value", json, StringComparison.Ordinal);
        Assert.Contains("one-run-value", plan.Steps[0].Command, StringComparison.Ordinal);
        Assert.Equal(PlanStepStatus.Pending, plan.Steps[0].Status);
    }

    /// <summary>Treats apostrophes and PowerShell expressions in parameter input as data rather than executable source.</summary>
    [Fact]
    public async Task Bind_QuotedParameter_DoesNotEvaluateInput()
    {
        var marker = Path.Combine(_directory, "not-executed");
        var value = "'; [IO.File]::WriteAllText(" + ScriptArtifactService.Quote(marker) + ", 'unexpected'); '";
        var plan = Store().Bind(Recipe(), new Dictionary<string, string> { ["message"] = value }, _directory);
        var approved = ApprovedCommand.Create(plan.Steps[0].Command,
            new CommandRiskAssessment(35, CommandRiskLevel.Medium, "Literal data test.", false, null));
        var result = await new CommandExecutionService(NullLogger<CommandExecutionService>.Instance)
            .ExecuteAsync(approved, TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(value, result.StandardOutput.TrimEnd('\r', '\n'));
        Assert.False(File.Exists(marker));
    }

    /// <summary>Rejects missing or credential-shaped parameters before constructing any runnable plan.</summary>
    [Fact]
    public void Bind_InvalidParameters_FailsClosed()
    {
        Assert.Throws<InvalidOperationException>(() => Store().Bind(Recipe(), new Dictionary<string, string>(), _directory));
        Assert.Throws<InvalidOperationException>(() => Store(new MarkerRedactor()).Bind(Recipe(),
            new Dictionary<string, string> { ["message"] = "restricted-value" }, _directory));
    }

    /// <summary>Requires a confirmed completed plan, then resets reusable steps to pending.</summary>
    [Fact]
    public void FromCompletedPlan_RequiresSuccessAndResetsProgress()
    {
        var store = Store();
        var step = Recipe().Steps[0];
        var pending = new ExecutionPlan(1, Guid.NewGuid().ToString("N"), "inspect", _directory, [step]);
        Assert.Throws<InvalidOperationException>(() => store.FromCompletedPlan("inspect", pending));
        var complete = pending with { Steps = [step with { Status = PlanStepStatus.Completed }] };
        Assert.Equal(PlanStepStatus.Pending, store.FromCompletedPlan("inspect", complete).Steps[0].Status);
    }

    /// <summary>Preserves a previously saved recipe when an import or save uses the same name.</summary>
    [Fact]
    public async Task SaveAsync_ExistingRecipe_IsNotOverwritten()
    {
        var store = Store();
        await store.SaveAsync(Recipe(), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(Recipe() with { Description = "changed" }, CancellationToken.None));
        Assert.Equal("Print a message.", (await store.LoadAsync("echo-message", CancellationToken.None)).Description);
    }

    /// <summary>Discards untrusted saved completion states on import and rejects unsupported schema versions.</summary>
    [Fact]
    public async Task ReadFileAsync_DiscardsSavedProgressAndRejectsVersions()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "import.json");
        var recipe = Recipe() with { Steps = [Recipe().Steps[0] with { Status = PlanStepStatus.Completed }] };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(recipe, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal(PlanStepStatus.Pending, (await Store().ReadFileAsync(path, CancellationToken.None)).Steps[0].Status);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(recipe with { Version = 99 }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store().ReadFileAsync(path, CancellationToken.None));
    }

    /// <summary>Keeps recipe names local and scopes import/export/from-plan options to their specific actions.</summary>
    [Fact]
    public void Parse_RecipeActions_AreStrict()
    {
        var parser = new CommandLineParser(_text);
        Assert.True(parser.Parse(["--recipes"]).Succeeded);
        Assert.True(parser.Parse(["--recipes", "run", "echo-message"]).Succeeded);
        Assert.True(parser.Parse(["--recipes", "import", "--file", "recipe.json"]).Succeeded);
        Assert.False(parser.Parse(["--recipes", "run", "../recipe"]).Succeeded);
        Assert.False(parser.Parse(["--recipes", "save", "name"]).Succeeded);
        Assert.False(parser.Parse(["--recipes", "show", "name", "--file", "recipe.json"]).Succeeded);
    }

    /// <summary>Removes only this fixture's random temporary recipe directory.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>Builds a valid portable recipe with one data-only parameter.</summary>
    private static CommandRecipe Recipe() => new(1, "echo-message", "Print a message.", null, ["PowerShell 7 is available."],
        [new RecipeParameter("message", "Text to display.")],
        [new PlanStep("Display", "Write-Output $hmParameters['message']", "Write-Output $hmParameters['message']", "The text is shown.")]);

    /// <summary>Creates isolated recipe and plan services without real settings or provider calls.</summary>
    private RecipeStore Store(ISensitiveDataRedactor? redactor = null)
    {
        var paths = new AppPaths(_directory, Path.Combine(_directory, "db"), Path.Combine(_directory, "logs"), Path.Combine(_directory, "logs", "x"), "prompt");
        redactor ??= new SensitiveDataRedactor();
        return new RecipeStore(paths, new PlanStore(paths, redactor, _text), redactor, _text);
    }

    private sealed class MarkerRedactor : ISensitiveDataRedactor
    {
        /// <summary>Exercises rejection at the credential boundary without including credential fixtures.</summary>
        public string Redact(string value) => value.Replace("restricted-value", "[removed]", StringComparison.Ordinal);
    }
}

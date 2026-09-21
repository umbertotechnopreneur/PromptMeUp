// SPDX-License-Identifier: MIT

using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class ScriptWorkflowTests
{
    /// <summary>Passes the saved confirmation preference to one-time script execution through the normal authorization gate.</summary>
    [Theory]
    [InlineData(true, CommandExecutionMode.Direct)]
    [InlineData(false, CommandExecutionMode.Confirm)]
    public async Task RunAsync_UsesGlobalExecutionPreference(bool directModeEnabled, CommandExecutionMode expectedMode)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var text = new LocalizationService();
        var definition = new ScriptLanguageDefinition(
            ScriptLanguage.PowerShell,
            "powershell",
            "PowerShell 7",
            ".ps1",
            "Generate a PowerShell script.",
            true,
            ["pwsh"]);
        var provider = TestProxy.Create<IOpenAiService>((method, _) =>
        {
            Assert.Equal("SendAsync", method.Name);
            return Task.FromResult(Response());
        });
        var shell = TestProxy.Create<IConsoleShellView>((method, args) => method.Name switch
        {
            "RunWithStatusAsync" => ((Func<Task<AiResponse>>)args[1]!).Invoke(),
            "RenderNotice" or "RenderRuntimeStatus" or "RenderSuccess" => null,
            _ => throw new NotSupportedException(method.Name)
        });
        var languages = TestProxy.Create<IScriptLanguageCatalog>((method, _) => method.Name switch
        {
            "Get" => definition,
            "GetAvailability" => new ScriptRuntimeAvailability(true, "pwsh"),
            "BuildExecutionCommand" => "Get-Location",
            _ => throw new NotSupportedException(method.Name)
        });
        CommandExecutionMode? mode = null;
        var commands = TestProxy.Create<IAuthorizedCommandWorkflow>((method, args) =>
        {
            Assert.Equal("RunForResultAsync", method.Name);
            mode = (CommandExecutionMode)args[4]!;
            return Task.FromResult<CommandExecutionResult?>(new CommandExecutionResult("Get-Location", 0, "", "", false, false, 1));
        });
        var view = TestProxy.Create<IScriptView>((method, _) => method.Name switch
        {
            "Render" => null,
            "Choose" => ScriptAction.Execute,
            _ => throw new NotSupportedException(method.Name)
        });
        var assistant = new ArtifactAssistant(provider, fixture.Audit, new BoundedTextInput(new SensitiveDataRedactor(), text), shell, text);
        var workflow = new ScriptWorkflow(
            assistant,
            new ScriptArtifactService(new SensitiveDataRedactor(), text, languages),
            languages,
            new BoundedTextInput(new SensitiveDataRedactor(), text),
            commands,
            fixture.Audit,
            view,
            shell,
            text);

        await workflow.RunAsync(new CommandLineOptions(AppCommand.Script, "Show the current folder.", "en", true, true, false, false, null),
            AppSettings.Default with { DirectModeEnabled = directModeEnabled }, default);

        Assert.True(mode.HasValue);
        Assert.Equal(expectedMode, mode.Value);
    }

    /// <summary>Creates an artifact response with a valid JSON script and ordinary provider accounting.</summary>
    private static AiResponse Response() => new(
        "response-id",
        "gpt-5.6-terra",
        """{"explanation":"Synthetic script.","source":"Write-Output 'ok'"}""",
        new AiUsageMetrics(10, 1, 0, 0, 0, 11),
        new AiContextUsage(10, 1, 0, 0, 0, 1_000, false) { InputBudgetTokens = 900 },
        null,
        0m,
        200,
        0,
        "provider-request-id");
}

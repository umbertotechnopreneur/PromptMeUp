// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class ApplicationErrorTests
{
    /// <summary>Verifies that explicit cost refresh failures show authentication guidance when appropriate and still render cached costs.</summary>
    [Theory]
    [InlineData(401)]
    [InlineData(503)]
    public async Task RunAsync_CostRefreshFailure_RendersExpectedWarningAndCachedCosts(int statusCode)
    {
        var text = new LocalizationService();
        var settings = AppSettings.Default with { SetupCompleted = true, Language = "it" };
        var exception = new OpenAiRequestException("Organization costs request failed.", "organization_costs_failed", statusCode);
        var overview = new CostOverview(null, null, 0, 0, null, 0, 0, 0, 0, []);
        var warnings = new List<string>();
        var cachedCostsRendered = false;
        var pricing = TestProxy.Create<IPricingService>((method, args) =>
        {
            if (method.Name == "RefreshDailyIfNeededAsync")
            {
                Assert.True((bool)args[1]!);
                return Task.FromException<PricingRefreshResult>(exception);
            }
            return method.Name == "GetOverviewAsync"
                ? Task.FromResult(overview)
                : throw new NotSupportedException(method.Name);
        });
        var shell = TestProxy.Create<IConsoleShellView>((method, args) =>
        {
            if (method.Name == "RenderWarning")
            {
                warnings.Add((string)args[0]!);
                return null;
            }
            return method.Name switch
            {
                "Configure" or "RenderHeader" => null,
                "RunWithStatusAsync" => ((Func<Task<PricingRefreshResult>>)args[1]!)(),
                _ => throw new NotSupportedException(method.Name)
            };
        });
        var app = new PromptMeUpApplication(
            parser: new CommandLineParser(text),
            database: TestProxy.Create<IDatabaseService>((method, _) => method.Name == "InitializeAsync"
                ? Task.CompletedTask : throw new NotSupportedException(method.Name)),
            settings: TestProxy.Create<ISettingsService>((method, _) => method.Name == "LoadAsync"
                ? Task.FromResult(settings) : throw new NotSupportedException(method.Name)),
            secrets: TestProxy.Create<IEnvironmentSecretService>((method, _) => method.Name == "IsConfigured"
                ? true : throw new NotSupportedException(method.Name)),
            prompts: TestProxy.Create<IPromptCatalogService>((method, _) => method.Name == "ListAsync"
                ? Task.FromResult<IReadOnlyList<PromptDefinition>>([]) : throw new NotSupportedException(method.Name)),
            pricing: pricing,
            conversationWorkflow: null!,
            diagnostics: null!,
            scripts: null!,
            plans: null!,
            filePreview: null!,
            recipes: null!,
            activity: null!,
            setup: null!,
            installation: null!,
            text: text,
            shell: shell,
            statusView: null!,
            costsView: TestProxy.Create<ICostsView>((method, args) =>
            {
                Assert.Equal("Render", method.Name);
                Assert.Same(overview, args[0]);
                cachedCostsRendered = true;
                return null;
            }),
            helpView: null!,
            thirdPartyView: null!,
            paths: null!,
            logger: NullLogger<PromptMeUpApplication>.Instance);

        var exitCode = await app.RunAsync(["--costs"], default);

        Assert.Equal(0, exitCode);
        Assert.True(cachedCostsRendered);
        var expectedWarning = statusCode == 401 && OperatingSystem.IsWindows()
            ? $"{exception.Message}{Environment.NewLine}{text.Text("Error.KeyRestartRequired")}"
            : exception.Message;
        Assert.Equal(expectedWarning, Assert.Single(warnings));
    }

    /// <summary>Verifies that a Windows authentication failure explains how to activate a recently changed key.</summary>
    [Fact]
    public void FormatErrorMessage_UnauthorizedOnWindows_AppendsLocalizedRestartGuidance()
    {
        var text = new LocalizationService();
        text.SetLanguage("it");
        var exception = new OpenAiRequestException("Incorrect API key provided.", "responses_api_failed", 401);

        var message = PromptMeUpApplication.FormatErrorMessage(exception, text, isWindows: true);

        Assert.Contains("Incorrect API key provided.", message, StringComparison.Ordinal);
        Assert.Contains("chiudi completamente e riapri l'applicazione del terminale", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("l'IDE se usi un terminale integrato", message, StringComparison.Ordinal);
    }

    /// <summary>Verifies that unrelated provider failures do not receive misleading credential guidance.</summary>
    [Fact]
    public void FormatErrorMessage_NonAuthenticationFailure_RemainsUnchanged()
    {
        var text = new LocalizationService();
        var exception = new OpenAiRequestException("Service unavailable.", "responses_api_failed", 503);

        var message = PromptMeUpApplication.FormatErrorMessage(exception, text, isWindows: true);

        Assert.Equal(exception.Message, message);
    }

    /// <summary>Verifies that non-Windows authentication failures retain their original message.</summary>
    [Fact]
    public void FormatErrorMessage_UnauthorizedOutsideWindows_RemainsUnchanged()
    {
        var text = new LocalizationService();
        var exception = new OpenAiRequestException("Incorrect API key provided.", "responses_api_failed", 401);

        var message = PromptMeUpApplication.FormatErrorMessage(exception, text, isWindows: false);

        Assert.Equal(exception.Message, message);
    }
}

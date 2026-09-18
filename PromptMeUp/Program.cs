// SPDX-License-Identifier: MIT

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PromptMeUp.Application;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Serilog;
using Spectre.Console;

namespace PromptMeUp;

internal static class Program
{
    /// <summary>Bootstraps UTF-8 output, Serilog, dependency injection, cancellation, and the PromptMeUp application.</summary>
    private static async Task<int> Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        var paths = AppPaths.Create();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(
                paths.LogFilePattern,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        using var shutdown = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;
        IConsoleShellView? shell = null;

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services, paths, shutdown.Token);
            await using var provider = services.BuildServiceProvider();
            shell = provider.GetRequiredService<IConsoleShellView>();
            var text = provider.GetRequiredService<ILocalizationService>();
            text.SetLanguage(SupportedLanguages.ResolveSystemLanguage());
            var parse = provider.GetRequiredService<ICommandLineParser>().Parse(args);
            if (parse.Succeeded && parse.Options!.Command == AppCommand.Lenna)
            {
                return provider.GetRequiredService<LennaWorkflow>().Run(parse.Options, shutdown.Token);
            }
            if (parse.Succeeded && parse.Options!.Command == AppCommand.Help)
            {
                return provider.GetRequiredService<HelpWorkflow>().Run(parse.Options, shutdown.Token);
            }
            if (parse.Succeeded && parse.Options!.Command == AppCommand.About)
            {
                return provider.GetRequiredService<AboutWorkflow>().Run(parse.Options, shutdown.Token);
            }
            return await provider.GetRequiredService<IPromptMeUpApplication>().RunAsync(args, shutdown.Token);
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            return 130;
        }
        catch (InteractiveFlowCanceledException)
        {
            return 0;
        }
        catch (Exception exception)
        {
            Log.Fatal("PromptMeUp terminated unexpectedly. ExceptionType={ExceptionType}", exception.GetType().Name);
            AnsiConsole.MarkupLine($"[red]PromptMeUp failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            try
            {
                shell?.RenderFooter();
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }

    /// <summary>Registers the model, service, and view layers used by the lightweight console host.</summary>
    private static void ConfigureServices(
        IServiceCollection services,
        AppPaths paths,
        CancellationToken shutdownToken)
    {
        services.AddSingleton(paths);
        services.AddSingleton(_ => BuildInformationReader.Read());
        services.AddSingleton<IAnsiConsole>(new EscapeAwareAnsiConsole(AnsiConsole.Console, shutdownToken));
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: false);
        });

        services.AddSingleton<ICommandLineParser, CommandLineParser>();
        services.AddSingleton<ILennaImageService, LennaImageService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton(provider => ArtifactLimitConfiguration.Load(
            Environment.GetEnvironmentVariable, provider.GetRequiredService<ILocalizationService>()));
        services.AddSingleton(provider => ContextBudgetConfiguration.Load(
            Environment.GetEnvironmentVariable, provider.GetRequiredService<ILocalizationService>()));
        services.AddSingleton<IDatabaseService, SqliteDatabaseService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeCatalogService>(_ => new ThemeCatalogService(Path.Combine(AppContext.BaseDirectory, "themes")));
        services.AddSingleton<IEnvironmentSecretService, EnvironmentSecretService>();
        services.AddSingleton<ISensitiveDataRedactor, SensitiveDataRedactor>();
        services.AddSingleton<IPromptInjectionProtectionService, PromptInjectionProtectionService>();
        services.AddSingleton<IRuntimeContextService, RuntimeContextService>();
        services.AddSingleton<IPromptCatalogService, YamlPromptCatalogService>();
        services.AddSingleton<IAppGuideService, AppGuideService>();
        services.AddSingleton<IAiCostCalculator, AiCostCalculator>();
        services.AddSingleton<IActivityAuditService, ActivityAuditService>();
        services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();
        services.AddSingleton<PersistentMemoryService>();
        services.AddSingleton<ExperimentalStore>();
        services.AddSingleton<MemoryReflectionService>();
        services.AddSingleton<ReminderService>();
        services.AddSingleton<SkillCatalogService>();
        services.AddSingleton<SkillActionService>();
        services.AddSingleton<ExperimentalView>();
        services.AddSingleton<ExperimentalWorkflow>();
        services.AddSingleton<ICommandRiskAssessmentService, CommandRiskAssessmentService>();
        services.AddSingleton<ICommandExecutionService, CommandExecutionService>();
        services.AddSingleton<IPortablePathService, PortablePathService>();
        services.AddSingleton<IExecutableLocationService, ExecutableLocationService>();
        services.AddSingleton<INerdFontInstallerService, NerdFontInstallerService>();

        services.AddHttpClient<IOpenAiService, OpenAiService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PromptMeUp/0.1");
        });
        services.AddHttpClient<IPricingService, OpenAiPricingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PromptMeUp/0.1");
        });

        services.AddSingleton<IConsoleShellView, ConsoleShellView>();
        services.AddSingleton<IPoorMarkdownRenderer, PoorMarkdownRenderer>();
        services.AddSingleton<IThemeView, ThemeView>();
        services.AddSingleton<SetupView>();
        services.AddSingleton<FullscreenSetupView>();
        services.AddSingleton<ISetupView, AdaptiveSetupView>();
        services.AddSingleton<IStatusView, StatusView>();
        services.AddSingleton<ICostsView, CostsView>();
        services.AddSingleton<IChatView, ChatView>();
        services.AddSingleton<IMemoryView, MemoryView>();
        services.AddSingleton<IMemoryManagerView, MemoryManagerView>();
        services.AddSingleton<IMemoryForgetView, MemoryForgetView>();
        services.AddSingleton<ICommandSuggestionView, CommandSuggestionView>();
        services.AddSingleton<IHelpView, HelpView>();
        services.AddSingleton<IAboutView, AboutView>();
        services.AddSingleton<ILennaView, LennaView>();
        services.AddSingleton<ICommandAuthorizationView, CommandAuthorizationView>();
        services.AddSingleton<IThirdPartyView, ThirdPartyView>();
        services.AddSingleton<IPortablePathView, PortablePathView>();
        services.AddSingleton<IExecutableLocationView, ExecutableLocationView>();
        services.AddSingleton<INerdFontView, NerdFontView>();

        services.AddSingleton<IAuthorizedCommandWorkflow, AuthorizedCommandWorkflow>();
        services.AddSingleton<IAiConversationWorkflow, AiConversationWorkflow>();
        services.AddSingleton<BoundedTextInput>();
        services.AddSingleton<DiagnosticWorkflow>();
        services.AddSingleton<ArtifactAssistant>();
        services.AddSingleton<ScriptArtifactService>();
        services.AddSingleton<IScriptView, ScriptView>();
        services.AddSingleton<ScriptWorkflow>();
        services.AddSingleton<PlanStore>();
        services.AddSingleton<IPlanView, PlanView>();
        services.AddSingleton<PlanWorkflow>();
        services.AddSingleton<FilePreviewService>();
        services.AddSingleton<IFilePreviewView, FilePreviewView>();
        services.AddSingleton<FilePreviewWorkflow>();
        services.AddSingleton<RecipeStore>();
        services.AddSingleton<IRecipeView, RecipeView>();
        services.AddSingleton<RecipeWorkflow>();
        services.AddSingleton<ApplicationActivityRecorder>();
        services.AddSingleton<SetupWorkflow>();
        services.AddSingleton<MemoryManagerWorkflow>();
        services.AddSingleton<MemoryCommandWorkflow>();
        services.AddSingleton<InstallationWorkflow>();
        services.AddSingleton<LennaWorkflow>();
        services.AddSingleton<HelpWorkflow>();
        services.AddSingleton<AboutWorkflow>();
        services.AddSingleton<IPromptMeUpApplication, PromptMeUpApplication>();
    }
}

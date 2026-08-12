// SPDX-License-Identifier: MIT

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PromptMeUp.Application;
using PromptMeUp.Infrastructure;
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

        try
        {
            using var shutdown = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;
            var services = new ServiceCollection();
            ConfigureServices(services, paths);
            await using var provider = services.BuildServiceProvider();
            try
            {
                return await provider.GetRequiredService<IPromptMeUpApplication>().RunAsync(args, shutdown.Token);
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "PromptMeUp terminated unexpectedly. ExceptionType={ExceptionType}", exception.GetType().Name);
            AnsiConsole.MarkupLine($"[red]PromptMeUp failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>Registers the model, service, and view layers used by the lightweight console host.</summary>
    private static void ConfigureServices(IServiceCollection services, AppPaths paths)
    {
        services.AddSingleton(paths);
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: false);
        });

        services.AddSingleton<ICommandLineParser, CommandLineParser>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IDatabaseService, SqliteDatabaseService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IEnvironmentSecretService, EnvironmentSecretService>();
        services.AddSingleton<ISensitiveDataRedactor, SensitiveDataRedactor>();
        services.AddSingleton<IPromptCatalogService, YamlPromptCatalogService>();
        services.AddSingleton<IAiCostCalculator, AiCostCalculator>();
        services.AddSingleton<IActivityAuditService, ActivityAuditService>();
        services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();
        services.AddSingleton<ICommandRiskAssessmentService, CommandRiskAssessmentService>();
        services.AddSingleton<ICommandExecutionService, CommandExecutionService>();
        services.AddSingleton<IPortablePathService, PortablePathService>();
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
        services.AddSingleton<ISetupView, SetupView>();
        services.AddSingleton<IStatusView, StatusView>();
        services.AddSingleton<ICostsView, CostsView>();
        services.AddSingleton<IChatView, ChatView>();
        services.AddSingleton<IHelpView, HelpView>();
        services.AddSingleton<IMainMenuView, MainMenuView>();
        services.AddSingleton<ICommandAuthorizationView, CommandAuthorizationView>();
        services.AddSingleton<IThirdPartyView, ThirdPartyView>();
        services.AddSingleton<IPortablePathView, PortablePathView>();
        services.AddSingleton<INerdFontView, NerdFontView>();

        services.AddSingleton<IPromptMeUpApplication, PromptMeUpApplication>();
    }
}

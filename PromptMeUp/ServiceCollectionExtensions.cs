// SPDX-License-Identifier: MIT

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

/// <summary>Registers the PromptMeUp runtime with the console host service collection.</summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>Adds the model, service, view, and workflow layers used by the lightweight console host.</summary>
    public static IServiceCollection AddPromptMeUpRuntime(
        this IServiceCollection services,
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
        services.AddSingleton<IProjectBannerSchedule>(provider => new ProjectBannerSchedule(
            Path.Combine(paths.DataDirectory, "project-banner-date.txt"),
            TimeProvider.System,
            provider.GetRequiredService<ILogger<ProjectBannerSchedule>>()));
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
        services.AddSingleton<SkillsAndMemoryStore>();
        services.AddSingleton<MemoryReflectionService>();
        services.AddSingleton<ReminderService>();
        services.AddSingleton<SkillCatalogService>();
        services.AddSingleton<SettingsFeatureOverviewService>();
        services.AddSingleton<SkillActionService>();
        services.AddSingleton<SkillsAndMemoryView>();
        services.AddSingleton<SkillsAndMemoryWorkflow>();
        services.AddSingleton<ICommandRiskAssessmentService, CommandRiskAssessmentService>();
        services.AddSingleton<ICommandExecutionService, CommandExecutionService>();
        services.AddSingleton<IScriptLanguageCatalog, ScriptLanguageCatalog>();
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
        services.AddSingleton<ISetupView, FullscreenSetupView>();
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
        services.AddSingleton<IFirstRunView, FirstRunView>();
        services.AddSingleton<IHomeView, HomeView>();
        services.AddSingleton<IDesktopLauncherService, DesktopLauncherService>();
        services.AddSingleton<CommandGuideService>();
        services.AddSingleton<CommandGuideWorkflow>();
        services.AddSingleton<DiagnosticBundleService>();
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
        services.AddSingleton<ApplicationActivityRecorder>();
        services.AddSingleton<SetupWorkflow>();
        services.AddSingleton<FirstRunWorkflow>();
        services.AddSingleton<MemoryManagerWorkflow>();
        services.AddSingleton<MemoryCommandWorkflow>();
        services.AddSingleton<InstallationWorkflow>();
        services.AddSingleton<LennaWorkflow>();
        services.AddSingleton<HelpWorkflow>();
        services.AddSingleton<AboutWorkflow>();
        services.AddSingleton<AiCommandHandler>();
        services.AddSingleton<MemoryCommandHandler>();
        services.AddSingleton<InformationCommandHandler>();
        services.AddSingleton<SetupCommandHandler>();
        services.AddSingleton<IApplicationCommandRouter, ApplicationCommandRouter>();
        services.AddSingleton<IPromptMeUpApplication, PromptMeUpApplication>();
        return services;
    }
}

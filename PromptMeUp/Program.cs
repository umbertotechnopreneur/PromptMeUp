// SPDX-License-Identifier: MIT

using System.Text;
using Microsoft.Extensions.DependencyInjection;
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
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SessionId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        using var logSession = Serilog.Context.LogContext.PushProperty("SessionId", Guid.NewGuid().ToString("N")[..12]);
        Log.Information("Application starting. Version={Version}, Runtime={Runtime}, ArgumentCount={ArgumentCount}, InputRedirected={InputRedirected}, OutputRedirected={OutputRedirected}",
            typeof(Program).Assembly.GetName().Version?.ToString(),
            System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
            args.Length, Console.IsInputRedirected, Console.IsOutputRedirected);

        using var shutdown = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;
        IConsoleShellView? shell = null;
        ILocalizationService? text = null;
        var exitCode = 1;

        try
        {
            var services = new ServiceCollection();
            services.AddPromptMeUpRuntime(paths, shutdown.Token);
            await using var provider = services.BuildServiceProvider();
            shell = provider.GetRequiredService<IConsoleShellView>();
            text = provider.GetRequiredService<ILocalizationService>();
            exitCode = await provider.GetRequiredService<IPromptMeUpApplication>().RunAsync(args, shutdown.Token);
            return exitCode;
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            exitCode = 130;
            return exitCode;
        }
        catch (InteractiveFlowCanceledException)
        {
            exitCode = 0;
            return exitCode;
        }
        catch (Exception exception)
        {
            var cause = exception.GetBaseException();
            Log.Fatal("PromptMeUp terminated unexpectedly. ExceptionType={ExceptionType}, CauseType={CauseType}, StackTrace={StackTrace}",
                exception.GetType().FullName, cause.GetType().FullName, new System.Diagnostics.StackTrace(cause, fNeedFileInfo: false).ToString());
            if (text is null)
            {
                text = new LocalizationService();
                text.SetLanguage(SupportedLanguages.ResolveSystemLanguage());
            }
            AnsiConsole.MarkupLine($"[{TerminalTheme.Error}]{Markup.Escape(text.Text("Startup.Failed", cause.GetType().Name, paths.LogsDirectory))}[/]");
            if (args.Length == 0 && !Console.IsInputRedirected && !Console.IsOutputRedirected && !shutdown.IsCancellationRequested)
            {
                AnsiConsole.MarkupLine($"[{TerminalTheme.Primary}]{Markup.Escape(text.Text("Startup.PressEnter"))}[/]");
                try { await Console.In.ReadLineAsync(shutdown.Token); }
                catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
                catch (IOException) { }
            }
            return exitCode;
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
                Log.Information("Application stopped. ExitCode={ExitCode}", exitCode);
                await Log.CloseAndFlushAsync();
            }
        }
    }

}

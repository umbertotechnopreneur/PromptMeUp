// SPDX-License-Identifier: MIT

using System.Reflection;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IConsoleShellView
{
    ConsoleRenderOptions Options { get; }

    void Configure(ConsoleRenderOptions options);

    void RenderHeader(string command, AppSettings? settings, bool hasApiKey);

    void RenderRuntimeStatus(ShellRuntimeStatus status);

    Task<T> RunWithStatusAsync<T>(string message, Func<Task<T>> action);

    void RenderFooter(string command);

    void RenderError(string message);

    void RenderNotice(string message);
}

public sealed class ConsoleShellView : IConsoleShellView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly AppPaths _paths;

    /// <summary>Creates the shared premium console chrome used by every top-level command.</summary>
    public ConsoleShellView(IAnsiConsole console, ILocalizationService text, AppPaths paths)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public ConsoleRenderOptions Options { get; private set; } = new(false, false);

    /// <summary>Applies terminal compatibility preferences for the current invocation.</summary>
    public void Configure(ConsoleRenderOptions options) => Options = options;

    /// <summary>Draws the product banner followed by a compact live-status strip.</summary>
    public void RenderHeader(string command, AppSettings? settings, bool hasApiKey)
    {
        if (!Console.IsOutputRedirected)
        {
            _console.Clear(home: false);
        }
        var icon = Options.NoEmoji || Console.IsOutputRedirected ? "HM" : "󰚩  HM";
        var title = new Markup(
            $"[bold mediumpurple2]{Markup.Escape(icon)} // HELP ME[/]\n" +
            "[bold white]PromptMeUp[/]  [grey]Ask. Understand. Approve. Act.[/]\n\n" +
            "[link=https://umbertogiacobbi.biz]umbertogiacobbi.biz[/]  [grey]•[/]  " +
            "[link=https://github.com/umbertotechnopreneur/PromptMeUp]github.com/umbertotechnopreneur/PromptMeUp[/]");
        _console.Write(new Panel(title)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.MediumPurple2),
            Header = new PanelHeader(" BOOTSTRAP // READY ", Justify.Right),
            Padding = new Padding(2, 1)
        });

        var language = settings?.Language ?? _text.Language;
        var model = settings?.Model ?? "—";
        var keyState = settings?.AiEnabled == false
            ? _text.Text("Status.Disabled")
            : hasApiKey
                ? _text.Text("Status.Ready")
                : _text.Text("Status.Missing");
        _console.MarkupLine(
            $"[on grey15]  [grey70]{Markup.Escape(_text.Text("Footer.Command"))}[/] [white]{Markup.Escape(command)}[/]" +
            $"   [grey70]{Markup.Escape(_text.Text("Status.Language"))}[/] [white]{Markup.Escape(language)}[/]" +
            $"   [grey70]{Markup.Escape(_text.Text("Status.Model"))}[/] [white]{Markup.Escape(model)}[/]" +
            $"   [grey70]{Markup.Escape(_text.Text("Status.ApiKey"))}[/] [{(hasApiKey ? "green" : "yellow")}]{Markup.Escape(keyState)}[/]  [/]");
        RenderRuntimeStatus(ShellRuntimeStatus.FromSettings(settings));
        _console.WriteLine();
    }

    /// <summary>Draws the fixed provider, model, thinking, turn-cost, and running-cost status contract.</summary>
    public void RenderRuntimeStatus(ShellRuntimeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        var promptCost = FormatCost(status.PromptCostUsd);
        var responseCost = FormatCost(status.ResponseCostUsd);
        var runningCost = FormatCost(status.RunningCostUsd);
        var context = status.ContextWindowTokens > 0
            ? $"{(status.ContextIsEstimated ? "~" : string.Empty)}{status.ContextInputTokens:N0}/{status.ContextWindowTokens:N0} ({status.ContextInputTokens * 100d / status.ContextWindowTokens:0.0}%)"
            : "n/a";
        _console.MarkupLine(
            $"[on grey11]  [grey70]PROVIDER[/] [white]{Markup.Escape(status.Provider)}[/]" +
            $"   [grey70]MODEL[/] [white]{Markup.Escape(status.Model)}[/]" +
            $"   [grey70]THINKING[/] [white]{Markup.Escape(status.ThinkingLevel)}[/]" +
            $"   [grey70]PROMPT[/] [deepskyblue1]{promptCost}[/]" +
            $"   [grey70]RESPONSE[/] [deepskyblue1]{responseCost}[/]" +
            $"   [grey70]SESSION[/] [green]{runningCost}[/]" +
            $"   [grey70]CONTEXT[/] [yellow]{Markup.Escape(context)}[/]" +
            $"   [grey70]CACHE R/W[/] [white]{status.CachedInputTokens:N0}/{status.CacheWriteTokens:N0}[/]  [/]");
    }

    /// <summary>Runs one operation inside a premium spinner when the terminal supports animation.</summary>
    public async Task<T> RunWithStatusAsync<T>(string message, Func<Task<T>> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(action);
        if (Options.NoAnimation || Console.IsOutputRedirected)
        {
            return await action().ConfigureAwait(false);
        }

        return await _console.Status()
            .Spinner(Spinner.Known.Dots12)
            .SpinnerStyle(new Style(Color.MediumPurple2))
            .StartAsync(Markup.Escape(message), _ => action())
            .ConfigureAwait(false);
    }

    /// <summary>Draws the closing status strip with version and local data location.</summary>
    public void RenderFooter(string command)
    {
        _console.WriteLine();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
        _console.Write(new Rule
        {
            Style = Style.Parse("grey35")
        });
        _console.MarkupLine(
            $"[grey]{Markup.Escape(_text.Text("Footer.Command"))}:[/] {Markup.Escape(command)}   " +
            $"[grey]{Markup.Escape(_text.Text("Footer.Version"))}:[/] {Markup.Escape(version)}   " +
            $"[grey]{Markup.Escape(_text.Text("Footer.Data"))}:[/] {Markup.Escape(_paths.DataDirectory)}");
    }

    /// <summary>Shows a sanitized error panel without exposing exception internals.</summary>
    public void RenderError(string message) => _console.Write(new Panel(Markup.Escape(message))
    {
        Header = new PanelHeader(" ERROR "),
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Red)
    });

    /// <summary>Shows a short informational panel.</summary>
    public void RenderNotice(string message) => _console.Write(new Panel(Markup.Escape(message))
    {
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.DeepSkyBlue1)
    });

    /// <summary>Formats small per-request USD amounts without hiding sub-cent costs.</summary>
    private static string FormatCost(decimal? value) => value.HasValue
        ? $"${value.Value:0.00000000}"
        : "n/a";
}

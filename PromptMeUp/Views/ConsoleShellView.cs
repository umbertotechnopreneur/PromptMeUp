// SPDX-License-Identifier: MIT

using System.Reflection;
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

    void RenderSuccess(string message);

    void RenderWarning(string message);

    void RenderMuted(string message);

    void RenderSectionTitle(string message);

    string ReadText(string prompt);

    void RenderVersion(string applicationVersion, string runtimeVersion, string runtimeIdentifier);

    void WriteLine();
}

public sealed class ConsoleShellView : IConsoleShellView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;

    /// <summary>Creates the shared premium console chrome used by every top-level command.</summary>
    public ConsoleShellView(IAnsiConsole console, ILocalizationService text)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public ConsoleRenderOptions Options { get; private set; } = new(false, false);

    /// <summary>Applies terminal compatibility preferences for the current invocation.</summary>
    public void Configure(ConsoleRenderOptions options) => Options = options;

    /// <summary>Draws a compact product and invocation header without idle usage metrics.</summary>
    public void RenderHeader(string command, AppSettings? settings, bool hasApiKey)
    {
        if (!Console.IsOutputRedirected)
        {
            _console.Clear(home: true);
        }

        var invocation = command.Equals("main", StringComparison.OrdinalIgnoreCase)
            ? "hm"
            : $"hm {command}";
        _console.Write(new Rule(
            $"[bold mediumpurple2]HM[/] [grey]/[/] [white]{Markup.Escape(invocation)}[/]")
        {
            Justification = Justify.Left,
            Style = Style.Parse("grey35")
        });

        RenderHeaderContext(command, settings, hasApiKey);
    }

    /// <summary>Draws responsive AI identity, cost, and context lines for an active request.</summary>
    public void RenderRuntimeStatus(ShellRuntimeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        var promptCost = FormatCost(status.PromptCostUsd);
        var responseCost = FormatCost(status.ResponseCostUsd);
        var runningCost = FormatCost(status.RunningCostUsd);
        var context = status.ContextWindowTokens > 0
            ? $"{(status.ContextIsEstimated ? "~" : string.Empty)}{status.ContextInputTokens:N0}/{status.ContextWindowTokens:N0} ({status.ContextInputTokens * 100d / status.ContextWindowTokens:0.0}%)"
            : "n/a";
        var identity =
            $"[grey]AI[/] [white]{Markup.Escape(status.Provider)}[/] [grey]·[/] " +
            $"[white]{Markup.Escape(status.Model)}[/] [grey]· {Markup.Escape(status.ThinkingLevel)}[/]";
        var costs =
            $"[grey]PROMPT[/] [deepskyblue1]{promptCost}[/]  " +
            $"[grey]RESPONSE[/] [deepskyblue1]{responseCost}[/]  " +
            $"[grey]SESSION[/] [green]{runningCost}[/]";

        if (status.PromptCostUsd.HasValue || status.ResponseCostUsd.HasValue || status.RunningCostUsd > 0)
        {
            if (_console.Profile.Width >= 112)
            {
                _console.MarkupLine($"{identity}   {costs}");
            }
            else
            {
                _console.MarkupLine(identity);
                _console.MarkupLine(costs);
            }
        }
        else
        {
            _console.MarkupLine(identity);
        }

        if (status.ContextWindowTokens > 0 || status.CachedInputTokens > 0 || status.CacheWriteTokens > 0)
        {
            var cache = status.CachedInputTokens > 0 || status.CacheWriteTokens > 0
                ? $"  [grey]CACHE R/W[/] [white]{status.CachedInputTokens:N0}/{status.CacheWriteTokens:N0}[/]"
                : string.Empty;
            _console.MarkupLine($"[grey]CONTEXT[/] [deepskyblue1]{Markup.Escape(context)}[/]{cache}");
        }
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

    /// <summary>Draws a compact closing line with invocation and version.</summary>
    public void RenderFooter(string command)
    {
        _console.WriteLine();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.1";
        var invocation = command.Equals("main", StringComparison.OrdinalIgnoreCase)
            ? "hm"
            : $"hm {command}";
        _console.MarkupLine($"[grey]{Markup.Escape(invocation)} · v{Markup.Escape(version)}[/]");
    }

    /// <summary>Shows a sanitized frameless error without exposing exception internals.</summary>
    public void RenderError(string message) =>
        TerminalTheme.WriteBlock(_console, "ERROR", message, "red");

    /// <summary>Shows a short frameless informational message.</summary>
    public void RenderNotice(string message) =>
        TerminalTheme.WriteBlock(_console, "INFO", message);

    /// <summary>Shows one successful operation message using the shared terminal palette.</summary>
    public void RenderSuccess(string message) =>
        _console.MarkupLine($"[green]{Markup.Escape(message)}[/]");

    /// <summary>Shows one recoverable warning using the shared terminal palette.</summary>
    public void RenderWarning(string message) =>
        _console.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");

    /// <summary>Shows low-emphasis explanatory text without leaking Spectre into the application layer.</summary>
    public void RenderMuted(string message) =>
        _console.MarkupLine($"[grey]{Markup.Escape(message)}[/]");

    /// <summary>Shows a compact section heading for a focused command workflow.</summary>
    public void RenderSectionTitle(string message) =>
        _console.MarkupLine($"[bold deepskyblue1]{Markup.Escape(message)}[/]");

    /// <summary>Reads one required text value using a localized passive-view prompt.</summary>
    public string ReadText(string prompt) =>
        _console.Prompt(new TextPrompt<string>(Markup.Escape(prompt)));

    /// <summary>Renders product, runtime, and platform versions without exposing Spectre to the orchestrator.</summary>
    public void RenderVersion(string applicationVersion, string runtimeVersion, string runtimeIdentifier)
    {
        _console.MarkupLine($"[bold]PromptMeUp[/] {Markup.Escape(applicationVersion)}");
        _console.MarkupLine($"[grey].NET {Markup.Escape(runtimeVersion)} · {Markup.Escape(runtimeIdentifier)}[/]");
    }

    /// <summary>Writes one layout separator line through the passive console boundary.</summary>
    public void WriteLine() => _console.WriteLine();

    /// <summary>Shows invocation context and keyboard navigation without rendering idle AI costs.</summary>
    private void RenderHeaderContext(string command, AppSettings? settings, bool hasApiKey)
    {
        var context = settings is null
            ? string.Empty
            : $"[grey]{Markup.Escape(settings.Language)}[/]";
        if (settings is not null && IsAiInvocation(command))
        {
            if (settings.AiEnabled)
            {
                var keyState = hasApiKey ? _text.Text("Status.Ready") : _text.Text("Status.Missing");
                var keyColor = hasApiKey ? "green" : "yellow";
                context +=
                    $" [grey]·[/] [white]{Markup.Escape(settings.Model)}[/]" +
                    $" [grey]· {Markup.Escape(_text.Text($"Reasoning.{settings.ReasoningEffort}"))} ·[/] " +
                    $"[{keyColor}]{Markup.Escape(keyState)}[/]";
            }
            else
            {
                context += $" [grey]· {Markup.Escape(_text.Text("Status.Disabled"))}[/]";
            }
        }

        var shortcuts = Console.IsInputRedirected
                        || Console.IsOutputRedirected
                        || !IsInteractiveInvocation(command)
            ? string.Empty
            : $"[mediumpurple2]{Markup.Escape(_text.Text("Navigation.Shortcuts"))}[/]";
        if (context.Length > 0 && shortcuts.Length > 0)
        {
            _console.MarkupLine($"{context}  [grey]·[/]  {shortcuts}");
        }
        else if (context.Length > 0)
        {
            _console.MarkupLine(context);
        }
        else if (shortcuts.Length > 0)
        {
            _console.MarkupLine(shortcuts);
        }
    }

    /// <summary>Identifies invocations that benefit from showing the selected AI model before work begins.</summary>
    private static bool IsAiInvocation(string command) => command is "main" or "query" or "chat" or "test-ai";

    /// <summary>Identifies commands that actively read navigation or confirmation keys.</summary>
    private static bool IsInteractiveInvocation(string command) =>
        command is "main" or "setup" or "chat" or "where" or "path" or "install-font";

    /// <summary>Formats small per-request USD amounts without hiding sub-cent costs.</summary>
    private static string FormatCost(decimal? value) => value.HasValue
        ? $"${value.Value:0.00000000}"
        : "n/a";
}

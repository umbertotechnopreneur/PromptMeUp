// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

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

    /// <summary>Draws a compact product and invocation header while preserving prior terminal output.</summary>
    public void RenderHeader(string command, AppSettings? settings, bool hasApiKey)
    {
        var invocation = command.Equals("main", StringComparison.OrdinalIgnoreCase)
            ? "hm"
            : $"hm {command}";
        var icon = TerminalTheme.Icon(Options, "✦", "*");
        _console.WriteLine();
        var identity = new Rows(
            new Markup($"[bold {TerminalTheme.Accent}]{Markup.Escape(icon)} PromptMeUp[/]"),
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Tagline"))}[/]"));
        var action = new Rows(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Footer.Command").ToUpperInvariant())}[/]"),
            new Markup($"[bold {TerminalTheme.Primary}]{Markup.Escape(invocation)}[/]"));
        if (_console.Profile.Width >= 72)
        {
            var banner = new Grid();
            banner.AddColumn();
            banner.AddColumn(new GridColumn().RightAligned());
            banner.AddRow(identity, action);
            _console.Write(TerminalTheme.Panel(banner, "PROMPTMEUP"));
        }
        else
        {
            _console.Write(TerminalTheme.Panel(new Rows(identity, action), "PROMPTMEUP"));
        }

        RenderHeaderContext(command, settings, hasApiKey);
        _console.WriteLine();
    }

    /// <summary>Draws one responsive turn snapshot after a request or on explicit status demand.</summary>
    public void RenderRuntimeStatus(ShellRuntimeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        var turnCost = status.PromptCostUsd.HasValue || status.ResponseCostUsd.HasValue
            ? FormatCost((status.PromptCostUsd ?? 0m) + (status.ResponseCostUsd ?? 0m))
            : _text.Text("Costs.Unavailable");
        var context = status.ContextWindowTokens > 0
            ? $"{(status.ContextIsEstimated ? "~" : string.Empty)}{FormatTokens(status.ContextTotalTokens)} / {FormatTokens(status.ContextWindowTokens)} · {status.ContextTotalTokens * 100d / status.ContextWindowTokens:0.0}%"
            : _text.Text("Costs.Unavailable");
        var cache = status.CachedInputTokens > 0 || status.CacheWriteTokens > 0
            ? $"{FormatTokens(status.CachedInputTokens)} / {FormatTokens(status.CacheWriteTokens)}"
            : _text.Text("Costs.Unavailable");
        var icon = TerminalTheme.Icon(Options, "📊", "=");
        RenderMetricPanel(
            $"{icon} {_text.Text("Shell.Session")}",
            [
                TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "🧠", "AI")} {_text.Text("Shell.Model")}", status.Model),
                TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "◌", "~")} {_text.Text("Shell.Context")}", context, TerminalTheme.Info),
                TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "↘", "in")} {_text.Text("Shell.Input")}", FormatTokens(status.InputTokens), TerminalTheme.Info),
                TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "↗", "out")} {_text.Text("Shell.Output")}", FormatTokens(status.OutputTokens), TerminalTheme.Info),
                TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "◈", "$")} {_text.Text("Shell.TurnCost")}", turnCost, TerminalTheme.Info),
                TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "✓", "+")} {_text.Text("Shell.SessionCost")}", FormatCost(status.RunningCostUsd), TerminalTheme.Success),
                TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "▣", "#")} {_text.Text("Shell.Cache")}", cache)
            ]);
        _console.WriteLine();
    }

    /// <summary>Runs one operation with an honest indeterminate Spectre progress display when animation is supported.</summary>
    public async Task<T> RunWithStatusAsync<T>(string message, Func<Task<T>> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(action);
        if (Options.NoAnimation || Console.IsOutputRedirected)
        {
            return await action().ConfigureAwait(false);
        }

        var spinner = new SpinnerColumn(Spinner.Known.Dots12)
        {
            Style = Style.Parse(TerminalTheme.Accent),
            CompletedText = TerminalTheme.Icon(Options, "✓", "OK"),
            CompletedStyle = Style.Parse(TerminalTheme.Success)
        };
        var progressBar = new ProgressBarColumn
        {
            CompletedStyle = Style.Parse(TerminalTheme.Success),
            IndeterminateStyle = Style.Parse(TerminalTheme.Info),
            RemainingStyle = Style.Parse(TerminalTheme.Divider)
        };
        return await _console.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(spinner, new TaskDescriptionColumn(), progressBar)
            .StartAsync(async context =>
            {
                var task = context.AddTask(Markup.Escape(message), autoStart: true);
                task.IsIndeterminate = true;
                var result = await action().ConfigureAwait(false);
                task.IsIndeterminate = false;
                task.Value = task.MaxValue;
                return result;
            })
            .ConfigureAwait(false);
    }

    /// <summary>Leaves a deliberate blank boundary before control returns to the host terminal.</summary>
    public void RenderFooter(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        _console.WriteLine();
    }

    /// <summary>Shows a sanitized frameless error without exposing exception internals.</summary>
    public void RenderError(string message) =>
        TerminalTheme.WriteBlock(_console, "ERROR", message, "red");

    /// <summary>Shows a short frameless informational message.</summary>
    public void RenderNotice(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        // An interrupted Spectre prompt leaves its cursor after the prompt text.
        _console.WriteLine();
        TerminalTheme.WriteBlock(_console, "INFO", message);
    }

    /// <summary>Shows one successful operation message using the shared terminal palette.</summary>
    public void RenderSuccess(string message) =>
        _console.MarkupLine($"[green]{Markup.Escape(message)}[/]");

    /// <summary>Shows one recoverable warning using the shared terminal palette.</summary>
    public void RenderWarning(string message) =>
        _console.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");

    /// <summary>Shows low-emphasis explanatory text without leaking Spectre into the application layer.</summary>
    public void RenderMuted(string message) =>
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(message)}[/]");

    /// <summary>Shows a compact section heading for a focused command workflow.</summary>
    public void RenderSectionTitle(string message) =>
        _console.MarkupLine($"[bold deepskyblue1]{Markup.Escape(message)}[/]");

    /// <summary>Reads one required text value using a localized passive-view prompt.</summary>
    public string ReadText(string prompt) =>
        _console.Prompt(new TextPrompt<string>(Markup.Escape(prompt)));

    /// <summary>Renders product, runtime, source, and safety details as a compact About box.</summary>
    public void RenderVersion(string applicationVersion, string runtimeVersion, string runtimeIdentifier)
    {
        const string repositoryUrl = "https://github.com/umbertotechnopreneur/PromptMeUp";
        const string websiteUrl = "https://umbertogiacobbi.biz";
        var icon = TerminalTheme.Icon(Options, "✨", "*");
        var details = new Grid();
        details.AddColumn();
        details.AddColumn();
        details.AddRow(
            TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "◆", "*")} {_text.Text("Shell.Application")}", $"v{applicationVersion}", TerminalTheme.Accent),
            TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "⚙", "~")} {_text.Text("Shell.Runtime")}", $".NET {runtimeVersion}", TerminalTheme.Info));
        details.AddRow(
            TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "🖥", "OS")} {_text.Text("Shell.Platform")}", runtimeIdentifier),
            TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "⚖", "=")} {_text.Text("About.License")}", "MIT", TerminalTheme.Success));
        var links = new Rows(
            new Markup(
                $"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("About.Repository"))}[/]\n" +
                $"[link={repositoryUrl}]{Markup.Escape(repositoryUrl)}[/]"),
            new Markup(
                $"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("About.Website"))}[/]\n" +
                $"[link={websiteUrl}]{Markup.Escape(websiteUrl)}[/]"),
            new Markup($"[{TerminalTheme.Info}]{Markup.Escape(_text.Text("About.Note"))}[/]"));
        _console.Write(TerminalTheme.Panel(new Rows(details, new Text(string.Empty), links), $"{icon} {_text.Text("About.Title")}"));
    }

    /// <summary>Writes one layout separator line through the passive console boundary.</summary>
    public void WriteLine() => _console.WriteLine();

    /// <summary>Shows a small settings dashboard and keyboard navigation under the invocation header.</summary>
    private void RenderHeaderContext(string command, AppSettings? settings, bool hasApiKey)
    {
        if (settings is not null && IsAiInvocation(command))
        {
            var state = settings.AiEnabled
                ? hasApiKey ? _text.Text("Status.Ready") : _text.Text("Status.Missing")
                : _text.Text("Status.Disabled");
            var stateColor = settings.AiEnabled && hasApiKey
                ? TerminalTheme.Success
                : settings.AiEnabled ? "yellow" : TerminalTheme.Muted;
            var stateIcon = settings.AiEnabled && hasApiKey
                ? TerminalTheme.Icon(Options, "●", "+")
                : TerminalTheme.Icon(Options, "!", "!");
            var dashboardIcon = TerminalTheme.Icon(Options, "🪞", "=");
            RenderMetricPanel(
                $"{dashboardIcon} {_text.Text("Shell.Session")}",
                [
                    TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "🌐", "@")} {_text.Text("Status.Language")}", settings.Language.ToUpperInvariant(), TerminalTheme.Accent),
                    TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "🧠", "AI")} {_text.Text("Status.Model")}", settings.Model),
                    TerminalTheme.Metric($"{TerminalTheme.Icon(Options, "⚙", "~")} {_text.Text("Shell.Thinking")}", _text.Text($"Reasoning.{settings.ReasoningEffort}"), TerminalTheme.Info),
                    TerminalTheme.Metric($"{stateIcon} AI", state, stateColor)
                ]);
        }

        if (!Console.IsInputRedirected && !Console.IsOutputRedirected && IsInteractiveInvocation(command))
        {
            _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Navigation.Shortcuts"))}[/]");
        }
    }

    /// <summary>Identifies invocations that benefit from showing the selected AI model before work begins.</summary>
    private static bool IsAiInvocation(string command) => command is "main" or "query" or "chat" or "test-ai";

    /// <summary>Identifies commands that actively read navigation or confirmation keys.</summary>
    private static bool IsInteractiveInvocation(string command) =>
        command is "main" or "setup" or "chat" or "where" or "path" or "install-font";

    /// <summary>Renders a responsive grid of metrics inside one compact dashboard panel.</summary>
    private void RenderMetricPanel(string header, IReadOnlyList<IRenderable> metrics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        ArgumentNullException.ThrowIfNull(metrics);
        var columns = _console.Profile.Width >= 112 ? 4 : _console.Profile.Width >= 72 ? 2 : 1;
        var grid = new Grid();
        for (var column = 0; column < columns; column++)
        {
            grid.AddColumn();
        }

        for (var offset = 0; offset < metrics.Count; offset += columns)
        {
            var row = new IRenderable[columns];
            for (var column = 0; column < columns; column++)
            {
                row[column] = offset + column < metrics.Count
                    ? metrics[offset + column]
                    : new Text(string.Empty);
            }
            grid.AddRow(row);
        }

        _console.Write(TerminalTheme.Panel(grid, header));
    }

    /// <summary>Formats small per-request USD amounts without hiding sub-cent costs.</summary>
    private static string FormatCost(decimal value) => $"${value:0.00000000}";

    /// <summary>Formats token counts compactly so context cards remain readable at terminal width.</summary>
    private static string FormatTokens(long value) => value switch
    {
        >= 1_000_000 => $"{value / 1_000_000d:0.0}M",
        >= 10_000 => $"{value / 1_000d:0.0}K",
        _ => value.ToString("N0")
    };
}

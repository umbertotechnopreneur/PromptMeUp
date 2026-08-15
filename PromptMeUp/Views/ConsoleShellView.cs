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
        var icon = TerminalTheme.IconPrefix(Options, "✦", "*");
        TerminalTheme.WriteRule(_console, $"{icon}PromptMeUp", TerminalTheme.Accent);
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Tagline"))}[/]");
        _console.MarkupLine(
            $"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Footer.Command"))}:[/] [bold {TerminalTheme.Primary}]{Markup.Escape(invocation)}[/]");

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
        var icon = TerminalTheme.IconPrefix(Options, "📊", "=");
        RenderSessionSnapshot(
            $"{icon}{_text.Text("Shell.Session")}",
            [
                TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "🧠", "AI")}{_text.Text("Shell.Model")}", status.Model),
                TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "◌", "~")}{_text.Text("Shell.Context")}", context, TerminalTheme.Info),
                TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "↘", "in")}{_text.Text("Shell.Input")}", FormatTokens(status.InputTokens), TerminalTheme.Info),
                TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "↗", "out")}{_text.Text("Shell.Output")}", FormatTokens(status.OutputTokens), TerminalTheme.Info),
                TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "◈", "$")}{_text.Text("Shell.TurnCost")}", turnCost, TerminalTheme.Info),
                TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "✓", "+")}{_text.Text("Shell.SessionCost")}", FormatCost(status.RunningCostUsd), TerminalTheme.Success),
                TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "▣", "#")}{_text.Text("Shell.Cache")}", cache)
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

        return await _console.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(new StackedProgressColumn())
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
        TerminalTheme.WriteSection(
            _console,
            TerminalTheme.IconPrefix(Options, "❌", "x") + _text.Text("Common.Error"),
            message,
            "red");

    /// <summary>Shows a short frameless informational message.</summary>
    public void RenderNotice(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _console.WriteLine();
        _console.MarkupLine(
            $"[bold {TerminalTheme.Info}]{TerminalTheme.IconPrefix(Options, "ℹ", "i")}INFO[/]  [{TerminalTheme.Primary}]{Markup.Escape(message)}[/]");
        _console.WriteLine();
    }

    /// <summary>Shows one successful operation message using the shared terminal palette.</summary>
    public void RenderSuccess(string message) =>
        _console.MarkupLine($"[{TerminalTheme.Success}]{Markup.Escape(message)}[/]");

    /// <summary>Shows one recoverable warning using the shared terminal palette.</summary>
    public void RenderWarning(string message) =>
        _console.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");

    /// <summary>Shows low-emphasis explanatory text without leaking Spectre into the application layer.</summary>
    public void RenderMuted(string message) =>
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(message)}[/]");

    /// <summary>Shows a compact section heading for a focused command workflow.</summary>
    public void RenderSectionTitle(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        TerminalTheme.WriteRule(
            _console,
            TerminalTheme.IconPrefix(Options, "↻", "~") + message,
            TerminalTheme.Info);
    }

    /// <summary>Reads one required text value using a localized passive-view prompt.</summary>
    public string ReadText(string prompt) =>
        _console.Prompt(new TextPrompt<string>(Markup.Escape(prompt)));

    /// <summary>Renders product, runtime, source, and safety details as a compact frameless About section.</summary>
    public void RenderVersion(string applicationVersion, string runtimeVersion, string runtimeIdentifier)
    {
        const string repositoryUrl = "https://github.com/umbertotechnopreneur/PromptMeUp";
        const string websiteUrl = "https://umbertogiacobbi.biz";
        var icon = TerminalTheme.IconPrefix(Options, "✨", "*");
        var details = TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "◆", "*")}{_text.Text("Shell.Application")}", $"v{applicationVersion}", TerminalTheme.Accent),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "⚙", "~")}{_text.Text("Shell.Runtime")}", $".NET {runtimeVersion}", TerminalTheme.Info),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "🖥", "OS")}{_text.Text("Shell.Platform")}", runtimeIdentifier),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "⚖", "=")}{_text.Text("About.License")}", "MIT", TerminalTheme.Success)
        ], preferredPairs: 2, width: _console.Profile.Width);
        var links = new Grid();
        links.AddColumn(new GridColumn().RightAligned().NoWrap());
        links.AddColumn(new GridColumn().LeftAligned());
        links.AddRow(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("About.Repository"))}:[/]"),
            new Markup($"[link={repositoryUrl}]{Markup.Escape(repositoryUrl)}[/]"));
        links.AddRow(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("About.Website"))}:[/]"),
            new Markup($"[link={websiteUrl}]{Markup.Escape(websiteUrl)}[/]"));
        TerminalTheme.WriteRule(_console, $"{icon}{_text.Text("About.Title")}", TerminalTheme.Accent);
        _console.Write(details);
        _console.WriteLine();
        _console.Write(links);
        _console.WriteLine();
        _console.MarkupLine($"[{TerminalTheme.Info}]{Markup.Escape(_text.Text("About.Note"))}[/]");
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
            var dashboardIcon = TerminalTheme.IconPrefix(Options, "🪞", "=");
            RenderSessionSnapshot(
                $"{dashboardIcon}{_text.Text("Shell.Session")}",
                [
                    TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(Options, "🌐", "@") + _text.Text("Status.Language"), settings.Language.ToUpperInvariant(), TerminalTheme.Accent),
                    TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "🧠", "AI")}{_text.Text("Status.Model")}", settings.Model),
                    TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "⚙", "~")}{_text.Text($"Shell.Thinking")}", _text.Text($"Reasoning.{settings.ReasoningEffort}"), TerminalTheme.Info),
                    TerminalTheme.CompactMetric($"{stateIcon}\u00A0AI", state, stateColor)
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

    /// <summary>Renders no more than two compact metric rows beneath a subtle frameless divider.</summary>
    private void RenderSessionSnapshot(string header, IReadOnlyList<CompactTerminalMetric> metrics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        ArgumentNullException.ThrowIfNull(metrics);
        var grid = TerminalTheme.PairGrid(
            metrics,
            preferredPairs: 4,
            width: _console.Profile.Width,
            preservePairCount: true);

        TerminalTheme.WriteRule(_console, header, TerminalTheme.Accent);
        _console.Write(grid);
    }

    /// <summary>Formats small per-request USD amounts without hiding sub-cent costs.</summary>
    private static string FormatCost(decimal value) => $"${value:0.00000000}";

    /// <summary>Formats token counts compactly so context summaries remain readable at terminal width.</summary>
    private static string FormatTokens(long value) => value switch
    {
        >= 1_000_000 => $"{value / 1_000_000d:0.0}M",
        >= 10_000 => $"{value / 1_000d:0.0}K",
        _ => value.ToString("N0")
    };

    private sealed class StackedProgressColumn : ProgressColumn
    {
        private readonly SpinnerColumn _spinner = new(Spinner.Known.Dots12)
        {
            Style = Style.Parse(TerminalTheme.Accent)
        };
        private readonly TaskDescriptionColumn _description = new();
        private readonly ProgressBarColumn _progressBar = new()
        {
            CompletedStyle = Style.Parse(TerminalTheme.Success),
            IndeterminateStyle = Style.Parse(TerminalTheme.Info),
            RemainingStyle = Style.Parse(TerminalTheme.Divider)
        };

        /// <summary>Stacks the active progress bar beneath its spinner and status description.</summary>
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            var layout = new Grid();
            layout.AddColumn(new GridColumn().NoWrap());
            layout.AddColumn();
            layout.AddRow(
                _spinner.Render(options, task, deltaTime),
                _description.Render(options, task, deltaTime));
            layout.AddRow(
                new Text(string.Empty),
                _progressBar.Render(options, task, deltaTime));
            return layout;
        }

        /// <summary>Keeps the stacked progress surface aligned with the shared 80%-width visual rhythm.</summary>
        public override int? GetColumnWidth(RenderOptions options) =>
            Math.Max(20, (int)Math.Floor(options.ConsoleSize.Width * 0.8d));
    }
}

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

    void RenderHeader(string command, AppSettings? settings, bool hasApiKey, string currentDirectory);

    void RenderRuntimeStatus(ShellRuntimeStatus status);

    Task<T> RunWithStatusAsync<T>(string message, Func<Task<T>> action);

    void RenderFooter();

    void RenderProjectBanner();

    void RenderError(string message);

    void RenderNotice(string message);

    void RenderActiveSkills(IReadOnlyList<SkillDefinition> skills);

    void RenderSuccess(string message);

    void RenderWarning(string message);

    void RenderMuted(string message);

    void RenderSectionTitle(string message);

    string ReadText(string prompt);

    void RenderVersion(BuildInformation buildInformation, string runtimeVersion, string runtimeIdentifier);

    void WriteLine();
}

public sealed class ConsoleShellView : IConsoleShellView
{
    private const string RepositoryUrl = "https://github.com/umbertotechnopreneur/PromptMeUp";
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;
    private readonly IProjectBannerSchedule _projectBannerSchedule;
    private bool _projectBannerRendered;

    /// <summary>Creates the shared premium console chrome used by every top-level command.</summary>
    public ConsoleShellView(IAnsiConsole console, ILocalizationService text, IProjectBannerSchedule projectBannerSchedule)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _projectBannerSchedule = projectBannerSchedule ?? throw new ArgumentNullException(nameof(projectBannerSchedule));
    }

    public ConsoleRenderOptions Options { get; private set; } = new(false, false);

    /// <summary>Applies terminal compatibility preferences for the current invocation.</summary>
    public void Configure(ConsoleRenderOptions options) => Options = options;

    /// <summary>Draws a compact product and invocation header while preserving prior terminal output.</summary>
    public void RenderHeader(string command, AppSettings? settings, bool hasApiKey, string currentDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);
        RenderOpeningBanner();
        RenderCurrentDirectory(currentDirectory);
        RenderHeaderContext(command, settings, hasApiKey);
    }

    /// <summary>Renders the opening identity and product promise as an unmistakable terminal banner.</summary>
    private void RenderOpeningBanner()
    {
        var icon = TerminalTheme.IconPrefix(Options, "✦", "*");
        TerminalTheme.WriteRule(_console, $"{icon}P R O M P T M E U P", TerminalTheme.Accent);
        _console.MarkupLine($"  [bold {TerminalTheme.Info}]{Markup.Escape(_text.Text("Shell.OpeningKicker"))}[/]");
        _console.MarkupLine($"  [{TerminalTheme.Primary}]{Markup.Escape(_text.Text("Tagline"))}[/]");
        _console.WriteLine();
    }

    /// <summary>Aligns the working directory with the tagline.</summary>
    private void RenderCurrentDirectory(string currentDirectory)
    {
        var grid = TerminalTheme.PairGrid(
            [TerminalTheme.CompactMetric(
                TerminalTheme.IconPrefix(Options, "📂", ">") + _text.Text("Shell.CurrentDirectory"),
                currentDirectory)],
            preferredPairs: 1,
            width: Math.Max(1, _console.Profile.Width - 2));
        _console.Write(new Padder(grid, new Padding(2, 0, 0, 0)));
    }

    /// <summary>Draws one responsive turn snapshot after a request or on explicit status demand.</summary>
    public void RenderRuntimeStatus(ShellRuntimeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        var turnCost = status.HasTurnCost
            ? status.TurnCostUsd.HasValue ? FormatCost(status.TurnCostUsd.Value) : _text.Text("Costs.Unavailable")
            : status.PromptCostUsd.HasValue || status.ResponseCostUsd.HasValue
                ? FormatCost((status.PromptCostUsd ?? 0m) + (status.ResponseCostUsd ?? 0m))
                : _text.Text("Costs.Unavailable");
        var cache = status.CachedInputTokens > 0 || status.CacheWriteTokens > 0
            ? $"{FormatTokens(status.CachedInputTokens)} / {FormatTokens(status.CacheWriteTokens)}"
            : _text.Text("Costs.Unavailable");
        var icon = TerminalTheme.IconPrefix(Options, "📊", "=");
        var metrics = new List<CompactTerminalMetric>
        {
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "🧠", "AI")}{_text.Text("Shell.Model")}", status.Model),
            TerminalTheme.CompactMetric(_text.Text("Shell.ActiveContext"), FormatContextUsage(status.ActiveContextTokens, status.ContextWindowTokens), TerminalTheme.Info),
            TerminalTheme.CompactMetric(_text.Text("Shell.MemoryUsage"), status.ActiveContextTokens.HasValue
                ? _text.Text("Shell.MemoryUsageValue", status.MemoryCount, FormatTokens(status.MemoryTokens))
                : _text.Text("Costs.Unavailable")),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "↘", "in")}{_text.Text("Shell.LastInput")}", FormatTokens(status.InputTokens), TerminalTheme.Info),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "↗", "out")}{_text.Text("Shell.LastOutput")}", FormatTokens(status.OutputTokens), TerminalTheme.Info),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "◈", "$")}{_text.Text("Shell.TurnCost")}", turnCost, TerminalTheme.Info),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "✓", "+")}{_text.Text("Shell.SessionCost")}", status.SessionCostKnown ? FormatCost(status.RunningCostUsd) : _text.Text("Costs.Unavailable"), TerminalTheme.Success),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "▣", "#")}{_text.Text("Shell.Cache")}", cache)
        };
        if (status.HasSessionUsage)
        {
            metrics.Add(TerminalTheme.CompactMetric(_text.Text("Shell.SessionInput"), FormatTokens(status.SessionInputTokens), TerminalTheme.Info));
            metrics.Add(TerminalTheme.CompactMetric(_text.Text("Shell.SessionOutput"), FormatTokens(status.SessionOutputTokens), TerminalTheme.Info));
        }

        var pairs = _console.Profile.Width >= 72 ? 2 : 1;
        var labelWidth = metrics.Where((_, index) => index % pairs == 0)
            .Select(metric => new Segment(metric.Label + ":").CellCount())
            .Append(new Segment(_text.Text("Shell.ContextBudget") + ":").CellCount()).Max();
        RenderSessionSnapshot(
            $"{icon}{_text.Text("Shell.Session")}",
            metrics,
            preferredPairs: 2,
            firstLabelWidth: labelWidth);
        _console.WriteLine();
        RenderContextBudget(status, labelWidth);
        if (status.HasContextBreakdown)
        {
            RenderContextLegend(status);
        }
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

    /// <summary>Displays the shared project banner on exit unless help has already shown it.</summary>
    public void RenderFooter()
    {
        if (!Options.SuppressFooter)
        {
            RenderProjectBanner();
        }
    }

    /// <summary>Renders the localized thanks, project links, and copyright at most once per local day.</summary>
    public void RenderProjectBanner()
    {
        if (_projectBannerRendered || !_projectBannerSchedule.TryMarkRenderedToday())
        {
            return;
        }
        var content = new Grid();
        content.AddColumn();
        content.AddRow(new Markup($"[bold {TerminalTheme.Primary}]{TerminalTheme.IconPrefix(Options, "👋", "*")}{Markup.Escape(_text.Text("Footer.Thanks"))}[/]"));
        content.AddRow(new Text(" "));
        content.AddRow(new Markup($"[{TerminalTheme.Primary}]{Markup.Escape(_text.Text("Footer.Support"))}[/]"));
        content.AddRow(new Text(" "));
        content.AddRow(new Markup($"[{TerminalTheme.Info} link={RepositoryUrl}]{RepositoryUrl}[/]"));
        content.AddRow(new Text(" "));
        content.AddRow(new Markup($"[{TerminalTheme.Muted}]Copyright (c) [link=https://umbertogiacobbi.biz]umbertogiacobbi.biz[/][/]"));
        TerminalTheme.WriteRule(_console, "hm · help me", TerminalTheme.Accent);
        _console.WriteLine();
        _console.Write(content);
        _console.WriteLine();
        _projectBannerRendered = true;
    }

    /// <summary>Shows a sanitized frameless error without exposing exception internals.</summary>
    public void RenderError(string message) =>
        TerminalTheme.WriteSection(
            _console,
            TerminalTheme.IconPrefix(Options, "❌", "x") + _text.Text("Common.Error"),
            message,
            TerminalTheme.Error);

    /// <summary>Shows a short frameless informational message.</summary>
    public void RenderNotice(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _console.WriteLine();
        _console.MarkupLine(
            $"[bold {TerminalTheme.Info}]{TerminalTheme.IconPrefix(Options, "ℹ", "i")}[/][{TerminalTheme.Primary}]{Markup.Escape(message)}[/]");
        _console.WriteLine();
    }

    /// <summary>Identifies each selected skill with its package icon and a readable name color.</summary>
    public void RenderActiveSkills(IReadOnlyList<SkillDefinition> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);
        if (skills.Count == 0)
        {
            return;
        }
        var names = skills.Select(skill =>
        {
            var color = ThemeCatalogService.ContrastRatio(skill.Color, TerminalTheme.Current.Colors.Background) >= 4.5d
                ? skill.Color : TerminalTheme.Primary;
            var label = TerminalTheme.IconPrefix(Options, skill.Icon, "*") + skill.Name;
            return $"[bold {color}]{Markup.Escape(label)}[/]";
        });
        _console.WriteLine();
        _console.MarkupLine(
            $"[bold {TerminalTheme.Info}]{TerminalTheme.IconPrefix(Options, "ℹ", "i")}[/][{TerminalTheme.Primary}]{_text.Text("Lab.Active", string.Join(", ", names))}[/]");
        _console.WriteLine();
    }

    /// <summary>Shows one successful operation message using the shared terminal palette.</summary>
    public void RenderSuccess(string message) =>
        _console.MarkupLine($"[{TerminalTheme.Success}]{Markup.Escape(message)}[/]");

    /// <summary>Shows one recoverable warning using the shared terminal palette.</summary>
    public void RenderWarning(string message) =>
        _console.MarkupLine($"[{TerminalTheme.Warning}]{Markup.Escape(message)}[/]");

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
    public void RenderVersion(BuildInformation buildInformation, string runtimeVersion, string runtimeIdentifier)
    {
        const string websiteUrl = "https://umbertogiacobbi.biz";
        const string motto = "Yet another CLI AI assistant :-)";
        var icon = TerminalTheme.IconPrefix(Options, "✨", "*");
        var details = TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "◆", "*")}{_text.Text("Shell.Application")}", $"v{buildInformation.Version}", TerminalTheme.Accent),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "⚙", "~")}{_text.Text("Shell.Runtime")}", $".NET {runtimeVersion}", TerminalTheme.Info),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "🖥", "OS")}{_text.Text("Shell.Platform")}", runtimeIdentifier),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "⚖", "=")}{_text.Text("About.License")}", "MIT", TerminalTheme.Success),
            TerminalTheme.CompactMetric(_text.Text("About.BuildDate"), buildInformation.BuiltAtLocal.ToString("O"), TerminalTheme.Info),
            TerminalTheme.CompactMetric(_text.Text("About.GitCommit"), buildInformation.GitCommit, TerminalTheme.Accent)
        ], preferredPairs: 2, width: _console.Profile.Width);
        var links = new Grid();
        links.AddColumn(new GridColumn().RightAligned().NoWrap());
        links.AddColumn(new GridColumn().LeftAligned());
        links.AddRow(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("About.Repository"))}:[/]"),
            new Markup($"[link={RepositoryUrl}]{Markup.Escape(RepositoryUrl)}[/]"));
        links.AddRow(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("About.Website"))}:[/]"),
            new Markup($"[link={websiteUrl}]{Markup.Escape(websiteUrl)}[/]"));
        TerminalTheme.WriteRule(_console, $"{icon}{_text.Text("About.Title")}", TerminalTheme.Accent);
        _console.MarkupLine($"[{TerminalTheme.Success}]{Markup.Escape(motto)}[/]");
        _console.WriteLine();
        _console.Write(details);
        _console.WriteLine();
        _console.Write(links);
        _console.WriteLine();
        _console.MarkupLine($"[{TerminalTheme.Info}]{Markup.Escape(_text.Text("About.Note"))}[/]");
    }

    /// <summary>Writes one layout separator line through the passive console boundary.</summary>
    public void WriteLine() => _console.WriteLine();

    /// <summary>Aligns startup settings beneath the working directory without a separate heading.</summary>
    private void RenderHeaderContext(string command, AppSettings? settings, bool hasApiKey)
    {
        if (settings is not null && IsAiInvocation(command))
        {
            var state = settings.AiEnabled
                ? hasApiKey ? _text.Text("Status.Ready") : _text.Text("Status.Missing")
                : _text.Text("Status.Disabled");
            var stateColor = settings.AiEnabled && hasApiKey
                ? TerminalTheme.Success
                : settings.AiEnabled ? TerminalTheme.Warning : TerminalTheme.Muted;
            var stateIcon = settings.AiEnabled && hasApiKey
                ? TerminalTheme.Icon(Options, "●", "+")
                : TerminalTheme.Icon(Options, "!", "!");
            var grid = TerminalTheme.PairGrid(
                [
                    TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(Options, "🌐", "@") + _text.Text("Status.Language"), settings.Language.ToUpperInvariant(), TerminalTheme.Accent),
                    TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "🧠", "AI")}{_text.Text("Status.Model")}", settings.Model),
                    TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(Options, "⚙️", "~")}{_text.Text($"Shell.Thinking")}", _text.Text($"Reasoning.{settings.ReasoningEffort}"), TerminalTheme.Info),
                    TerminalTheme.CompactMetric($"{stateIcon}\u00A0AI", state, stateColor)
                ],
                preferredPairs: 3,
                width: Math.Max(1, _console.Profile.Width - 2));
            _console.Write(new Padder(grid, new Padding(2, 0, 0, 0)));
        }
    }

    /// <summary>Identifies invocations that benefit from showing the selected AI model before work begins.</summary>
    private static bool IsAiInvocation(string command) => command is "main" or "query" or "direct" or "chat" or "test-ai";

    /// <summary>Renders compact metric rows that adapt to the available terminal width.</summary>
    private void RenderSessionSnapshot(string header, IReadOnlyList<CompactTerminalMetric> metrics, int preferredPairs = 4, int? firstLabelWidth = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        ArgumentNullException.ThrowIfNull(metrics);
        var grid = TerminalTheme.PairGrid(
            metrics,
            preferredPairs: preferredPairs,
            width: _console.Profile.Width,
            firstLabelWidth: firstLabelWidth);

        TerminalTheme.WriteRule(_console, header, TerminalTheme.Accent);
        _console.Write(grid);
    }

    /// <summary>Gives the operating budget its own label, Spectre progress bar, and complete localized values.</summary>
    private void RenderContextBudget(ShellRuntimeStatus status, int firstLabelWidth)
    {
        const int minimumBarWidth = 8;
        const int maximumBarWidth = 36;
        var width = Math.Max(1, _console.Profile.Width);
        var labelText = _text.Text("Shell.ContextBudget") + ":";
        labelText = new string(' ', Math.Max(0, Math.Min(firstLabelWidth, width) - new Segment(labelText).CellCount())) + labelText;
        var valueText = FormatContextUsage(status.ActiveContextTokens, status.ContextBudgetTokens);
        var label = new Text(labelText, Style.Parse(TerminalTheme.Muted));
        var value = new Text(valueText, Style.Parse($"bold {TerminalTheme.Info}"));
        if (width < 4)
        {
            // Spectre bars require at least four terminal cells; preserve the text during extreme resizing.
            _console.Write(new Rows(label, value));
            return;
        }
        var labelWidth = new Segment(labelText).CellCount();
        var valueWidth = new Segment(valueText).CellCount();
        var barWidth = width - labelWidth - valueWidth - 4;
        if (barWidth >= minimumBarWidth)
        {
            _console.Write(BudgetColumns(label, BudgetBar(status, Math.Min(maximumBarWidth, barWidth)), value));
            return;
        }

        barWidth = width - valueWidth - 2;
        IRenderable detail = barWidth >= minimumBarWidth
            ? BudgetColumns(BudgetBar(status, Math.Min(maximumBarWidth, barWidth)), value)
            : new Rows(BudgetBar(status, Math.Min(maximumBarWidth, width)), value);
        _console.Write(new Rows(label, detail));
    }

    /// <summary>Renders categorized context against the full capacity, preserving an unavailable-data fallback.</summary>
    private IRenderable BudgetBar(ShellRuntimeStatus status, int width)
    {
        if (status.HasContextBreakdown && status.ContextBudgetTokens > 0)
        {
            var cells = AllocateContextBarCells(status, width);
            var useSymbols = UseContextBarSymbols();
            char[] glyphs = useSymbols ? ['S', 'U', 'A', '.'] : ['━', '━', '━', '─'];
            var colors = new[] { TerminalTheme.Warning, TerminalTheme.Info, TerminalTheme.Success, TerminalTheme.Muted };
            var bar = new Paragraph();
            for (var index = 0; index < cells.Length; index++)
            {
                bar.Append(new string(glyphs[index], cells[index]), Style.Parse(colors[index]));
            }
            return bar;
        }

        var percentage = status.ActiveContextTokens.HasValue && status.ContextBudgetTokens > 0
            ? Math.Clamp(status.ActiveContextTokens.Value * 100d / status.ContextBudgetTokens, 0d, 100d)
            : 0d;
        var task = new ProgressTask(0, _text.Text("Shell.ContextBudget"), 100d, autoStart: false, timeProvider: TimeProvider.System)
        {
            Value = percentage
        };
        var column = new ProgressBarColumn
        {
            Width = width,
            CompletedStyle = Style.Parse(TerminalTheme.Info),
            FinishedStyle = Style.Parse(TerminalTheme.Info),
            RemainingStyle = Style.Parse(TerminalTheme.Divider)
        };
        var options = new RenderOptions(_console.Profile.Capabilities, new Size(width, 1));
        return column.Render(options, task, TimeSpan.Zero);
    }

    /// <summary>Allocates terminal cells by share, including free space, with at most one cell of rounding per category.</summary>
    internal static int[] AllocateContextBarCells(ShellRuntimeStatus status, int width)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(status.ContextBudgetTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(status.SystemInstructionTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(status.UserMessageTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(status.AssistantMessageTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(status.GuideTokens);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(status.GuideTokens, status.SystemInstructionTokens);
        var used = (decimal)status.SystemInstructionTokens + status.UserMessageTokens + status.AssistantMessageTokens;
        var capacity = Math.Max(status.ContextBudgetTokens, used);
        decimal[] tokens = [status.SystemInstructionTokens, status.UserMessageTokens, status.AssistantMessageTokens, capacity - used];
        var exact = tokens.Select(value => value * width / capacity).ToArray();
        var cells = exact.Select(value => (int)decimal.Floor(value)).ToArray();

        // Give leftover cells to the largest fractional shares; never enlarge every tiny segment independently.
        var remainderOrder = Enumerable.Range(0, cells.Length)
            .OrderByDescending(index => exact[index] - cells[index])
            .ThenBy(index => index)
            .Take(width - cells.Sum());
        foreach (var index in remainderOrder)
        {
            cells[index]++;
        }
        return cells;
    }

    /// <summary>Explains every bar category numerically and identifies the guide as already included in system tokens.</summary>
    private void RenderContextLegend(ShellRuntimeStatus status)
    {
        var useSymbols = UseContextBarSymbols();
        var free = status.ContextBudgetTokens > 0 && status.ActiveContextTokens.HasValue
            ? FormatContextTokens(Math.Max(0, status.ContextBudgetTokens - status.ActiveContextTokens.Value))
            : _text.Text("Costs.Unavailable");
        _console.Write(TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(ContextLegendLabel("Shell.ContextSystem", "S", useSymbols), FormatContextTokens(status.SystemInstructionTokens), TerminalTheme.Warning),
            TerminalTheme.CompactMetric(ContextLegendLabel("Shell.ContextUser", "U", useSymbols), FormatContextTokens(status.UserMessageTokens), TerminalTheme.Info),
            TerminalTheme.CompactMetric(ContextLegendLabel("Shell.ContextAssistant", "A", useSymbols), FormatContextTokens(status.AssistantMessageTokens), TerminalTheme.Success),
            TerminalTheme.CompactMetric(ContextLegendLabel("Shell.ContextFree", ".", useSymbols), free, TerminalTheme.Muted),
            TerminalTheme.CompactMetric(_text.Text("Shell.ContextGuideIncluded"), FormatContextTokens(status.GuideTokens), TerminalTheme.Warning)
        ], preferredPairs: 2, width: _console.Profile.Width));
    }

    /// <summary>Uses explicit role letters when the terminal cannot distinguish colored Unicode segments.</summary>
    private bool UseContextBarSymbols() =>
        !_console.Profile.Capabilities.Ansi || !_console.Profile.Supports(ColorSystem.Legacy) || !_console.Profile.Capabilities.Unicode;

    /// <summary>Connects a translated category to its colorless bar symbol when needed.</summary>
    private string ContextLegendLabel(string key, string symbol, bool useSymbols) =>
        useSymbols ? $"[{symbol}] {_text.Text(key)}" : _text.Text(key);

    /// <summary>Formats full estimated context counts in the interface language without compact-value rounding.</summary>
    private string FormatContextTokens(long value) =>
        _text.Text("Shell.ContextTokenEstimate", value.ToString("N0", _text.Culture));

    /// <summary>Aligns budget components on an open row with two spaces between columns.</summary>
    private static Grid BudgetColumns(params IRenderable[] components)
    {
        var grid = new Grid();
        for (var index = 0; index < components.Length; index++)
        {
            grid.AddColumn(new GridColumn
            {
                NoWrap = true,
                Padding = new Padding(0, 0, index + 1 < components.Length ? 2 : 0, 0)
            });
        }
        grid.AddRow(components);
        return grid;
    }

    /// <summary>Formats small per-request USD amounts without hiding sub-cent costs.</summary>
    private static string FormatCost(decimal value) => $"${value:0.00000000}";

    /// <summary>Distinguishes an estimated active request from unavailable context measurements.</summary>
    private string FormatContextUsage(long? usedTokens, long capacityTokens)
    {
        if (!usedTokens.HasValue)
        {
            return capacityTokens > 0
                ? $"{_text.Text("Costs.Unavailable")} / {capacityTokens.ToString("N0", _text.Culture)}"
                : _text.Text("Costs.Unavailable");
        }

        return capacityTokens > 0
            ? $"~{usedTokens.Value.ToString("N0", _text.Culture)} / {capacityTokens.ToString("N0", _text.Culture)} · {(usedTokens.Value * 100d / capacityTokens).ToString("0.0", _text.Culture)}%"
            : $"~{usedTokens.Value.ToString("N0", _text.Culture)} / {_text.Text("Costs.Unavailable")}";
    }

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

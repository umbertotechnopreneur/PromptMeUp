// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public interface IStatusView
{
    void Render(AppStatus status);
}

public sealed class StatusView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : IStatusView
{
    /// <summary>Renders configuration, secrets, prompts, storage, and synchronization status as a readable dashboard.</summary>
    public void Render(AppStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "🪞", "=") + text.Text("Status.Title"),
            TerminalTheme.Accent);
        var configuration = TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "◈", "#") + text.Text("Status.Setup"), status.Settings.SetupCompleted ? text.Text("Status.Completed") : text.Text("Status.Required"), status.Settings.SetupCompleted ? TerminalTheme.Success : "yellow"),
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "🌐", "@") + text.Text("Status.Language"), status.Settings.Language.ToUpperInvariant(), TerminalTheme.Accent),
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "🧠", "AI") + text.Text("Status.Model"), status.Settings.Model),
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "🔑", "K") + text.Text("Status.ApiKey"), status.HasApiKey ? text.Text("Status.Ready") : text.Text("Status.Missing"), status.HasApiKey ? TerminalTheme.Success : "yellow"),
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "🔐", "K") + text.Text("Status.AdminKey"), status.HasAdminKey ? text.Text("Status.Ready") : text.Text("Status.Missing"), status.HasAdminKey ? TerminalTheme.Success : "yellow"),
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "↻", "~") + text.Text("Status.Pricing"), status.LastPricingSync?.ToLocalTime().ToString("g", text.Culture) ?? text.Text("Costs.Unavailable"), TerminalTheme.Info)
        ], preferredPairs: 3, width: console.Profile.Width);
        TerminalTheme.WriteRule(console, $"{TerminalTheme.IconPrefix(shell.Options, "⚙", "~")}{text.Text("Status.Configuration")}", TerminalTheme.Accent);
        console.Write(configuration);
        console.WriteLine();

        console.Write(TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(text.Text("Artifact.ScriptLimit"), $"{status.ArtifactLimits.MaxScriptBytes / ArtifactLimits.Mebibyte} MiB"),
            TerminalTheme.CompactMetric(text.Text("Artifact.PlanLimit"), $"{status.ArtifactLimits.MaxPlanBytes / ArtifactLimits.Mebibyte} MiB"),
            TerminalTheme.CompactMetric(text.Text("Artifact.OutputBudget"), status.ArtifactLimits.MaxOutputTokens.ToString("N0", text.Culture))
        ], preferredPairs: 3, width: console.Profile.Width));
        console.WriteLine();

        var localData = TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "▣", "#") + text.Text("Status.Database"), status.DatabasePath),
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "▤", "#") + text.Text("Status.Logs"), status.LogsDirectory),
            TerminalTheme.CompactMetric(TerminalTheme.IconPrefix(shell.Options, "✦", "*") + text.Text("Status.Prompts"), $"{status.PromptCount} · {status.PromptDirectory}")
        ], preferredPairs: 1, width: console.Profile.Width);
        TerminalTheme.WriteRule(console, $"{TerminalTheme.IconPrefix(shell.Options, "💾", "#")}{text.Text("Status.LocalData")}", TerminalTheme.Accent);
        console.Write(localData);
        console.WriteLine();
    }

}

public interface ICostsView
{
    void Render(CostOverview overview);
}

public sealed class CostsView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : ICostsView
{
    /// <summary>Renders local usage estimates and cached official prices with semantic pricing chips.</summary>
    public void Render(CostOverview overview)
    {
        ArgumentNullException.ThrowIfNull(overview);
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "💳", "$") + text.Text("Costs.Title"),
            TerminalTheme.Accent);
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Costs.Subtitle"))}[/]");
        var metrics = TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(shell.Options, "💵", "$")}{text.Text("Costs.TodayEstimate")}", FormatUsd(overview.EstimatedCostTodayUsd), TerminalTheme.Success),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(shell.Options, "📅", "#")}{text.Text("Costs.MonthEstimate")}", FormatUsd(overview.EstimatedCostCurrentMonthUsd), TerminalTheme.Info),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(shell.Options, "🏢", "#")}{text.Text("Costs.ApiCost")}", overview.ActualOrganizationCostCurrentMonthUsd.HasValue ? FormatUsd(overview.ActualOrganizationCostCurrentMonthUsd.Value) : text.Text("Costs.Unavailable")),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(shell.Options, "↗", "+")}{text.Text("Costs.Requests")}", overview.RequestsToday.ToString("N0", text.Culture)),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(shell.Options, "◌", "~")}{text.Text("Costs.Tokens")}", overview.TotalTokensToday.ToString("N0", text.Culture)),
            TerminalTheme.CompactMetric($"{TerminalTheme.IconPrefix(shell.Options, "↻", "~")}{text.Text("Costs.LastSync")}", overview.LastPricingSync?.ToLocalTime().ToString("g", text.Culture) ?? text.Text("Costs.Unavailable"))
        ], preferredPairs: 3, width: console.Profile.Width);
        TerminalTheme.WriteRule(console, $"{TerminalTheme.IconPrefix(shell.Options, "📈", "=")}{text.Text("Costs.Overview")}", TerminalTheme.Accent);
        console.Write(metrics);
        console.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse(TerminalTheme.Divider));
        table.Title = new TableTitle($"[bold {TerminalTheme.Accent}]{Markup.Escape(text.Text("Costs.Models"))}[/]");
        table.AddColumn(new TableColumn(text.Text("Costs.Model")).NoWrap());
        table.AddColumn(new TableColumn(text.Text("Costs.Tier")).Centered().NoWrap());
        table.AddColumn(new TableColumn(text.Text("Costs.Input")).RightAligned());
        table.AddColumn(new TableColumn(text.Text("Costs.Cached")).RightAligned());
        table.AddColumn(new TableColumn(text.Text("Costs.Output")).RightAligned());
        foreach (var price in overview.Prices
                     .OrderBy(price => Classify(price))
                     .ThenBy(price => price.InputUsdPerMillionTokens + price.OutputUsdPerMillionTokens)
                     .ThenBy(price => price.Model, StringComparer.OrdinalIgnoreCase))
        {
            table.AddRow(
                new Markup($"[bold {TerminalTheme.Primary}]{Markup.Escape(price.Model)}[/]"),
                CostChip(Classify(price)),
                new Markup($"[{TerminalTheme.Primary}]{Markup.Escape(FormatUsd(price.InputUsdPerMillionTokens))}[/]"),
                new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(price.CachedInputUsdPerMillionTokens.HasValue ? FormatUsd(price.CachedInputUsdPerMillionTokens.Value) : "—")}[/]"),
                new Markup($"[{TerminalTheme.Primary}]{Markup.Escape(FormatUsd(price.OutputUsdPerMillionTokens))}[/]"));
        }
        console.Write(table);
    }

    /// <summary>Assigns a semantic band from the official input-plus-output price per million tokens.</summary>
    private static CostBand Classify(AiModelPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        var total = price.InputUsdPerMillionTokens + price.OutputUsdPerMillionTokens;
        return total switch
        {
            <= 2m => CostBand.Cheap,
            <= 20m => CostBand.Affordable,
            <= 90m => CostBand.Premium,
            _ => CostBand.Extreme
        };
    }

    /// <summary>Renders a high-contrast semantic chip for a model pricing band.</summary>
    private IRenderable CostChip(CostBand band)
    {
        var (icon, fallback, label, color) = band switch
        {
            CostBand.Cheap => ("🌱", "$", text.Text("Costs.Cheap"), TerminalTheme.Success),
            CostBand.Affordable => ("✓", "+", text.Text("Costs.Affordable"), TerminalTheme.Info),
            CostBand.Premium => ("◆", "*", text.Text("Costs.Premium"), TerminalTheme.Accent),
            _ => ("⚠", "!", text.Text("Costs.Extreme"), "red")
        };
        return new Markup(
            $"[bold {color}]{Markup.Escape(TerminalTheme.IconPrefix(shell.Options, icon, fallback))}{Markup.Escape(label)}[/]");
    }

    /// <summary>Formats USD amounts using invariant decimal notation.</summary>
    private static string FormatUsd(decimal value) => $"${value:0.########}";

    private enum CostBand
    {
        Cheap,
        Affordable,
        Premium,
        Extreme
    }
}

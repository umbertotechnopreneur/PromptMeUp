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
        TerminalTheme.WriteHeading(console, text.Text("Status.Title"));
        var configuration = CreateGrid(
        [
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "⚙", "~")} {text.Text("Status.Setup")}", status.Settings.SetupCompleted ? text.Text("Status.Completed") : text.Text("Status.Required"), status.Settings.SetupCompleted ? TerminalTheme.Success : "yellow"),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "🌐", "@")} {text.Text("Status.Language")}", status.Settings.Language.ToUpperInvariant(), TerminalTheme.Accent),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "🧠", "AI")} {text.Text("Status.Model")}", status.Settings.Model),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "🔑", "K")} {text.Text("Status.ApiKey")}", status.HasApiKey ? text.Text("Status.Ready") : text.Text("Status.Missing"), status.HasApiKey ? TerminalTheme.Success : "yellow"),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "🔐", "K")} {text.Text("Status.AdminKey")}", status.HasAdminKey ? text.Text("Status.Ready") : text.Text("Status.Missing"), status.HasAdminKey ? TerminalTheme.Success : "yellow"),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "↻", "~")} {text.Text("Status.Pricing")}", status.LastPricingSync?.ToLocalTime().ToString("g", text.Culture) ?? text.Text("Costs.Unavailable"), TerminalTheme.Info)
        ], console.Profile.Width);
        console.Write(TerminalTheme.Panel(configuration, $"{TerminalTheme.Icon(shell.Options, "🪞", "=")} {text.Text("Status.Configuration")}"));
        console.WriteLine();

        var localData = CreateGrid(
        [
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "▣", "#")} {text.Text("Status.Database")}", status.DatabasePath),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "▤", "#")} {text.Text("Status.Logs")}", status.LogsDirectory),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "✦", "*")} {text.Text("Status.Prompts")}", $"{status.PromptCount} · {status.PromptDirectory}")
        ], console.Profile.Width);
        console.Write(TerminalTheme.Panel(localData, $"{TerminalTheme.Icon(shell.Options, "💾", "#")} {text.Text("Status.LocalData")}"));
    }

    /// <summary>Builds an adaptive metrics grid that stays readable at narrow terminal widths.</summary>
    private static Grid CreateGrid(IReadOnlyList<IRenderable> metrics, int width)
    {
        var columns = width >= 112 ? 3 : width >= 72 ? 2 : 1;
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
                row[column] = offset + column < metrics.Count ? metrics[offset + column] : new Text(string.Empty);
            }
            grid.AddRow(row);
        }

        return grid;
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
        TerminalTheme.WriteHeading(console, text.Text("Costs.Title"), text.Text("Costs.Subtitle"));
        var metrics = CreateGrid(
        [
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "💵", "$")} {text.Text("Costs.TodayEstimate")}", FormatUsd(overview.EstimatedCostTodayUsd), TerminalTheme.Success),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "📅", "#")} {text.Text("Costs.MonthEstimate")}", FormatUsd(overview.EstimatedCostCurrentMonthUsd), TerminalTheme.Info),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "🏢", "#")} {text.Text("Costs.ApiCost")}", overview.ActualOrganizationCostCurrentMonthUsd.HasValue ? FormatUsd(overview.ActualOrganizationCostCurrentMonthUsd.Value) : text.Text("Costs.Unavailable")),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "↗", "+")} {text.Text("Costs.Requests")}", overview.RequestsToday.ToString("N0", text.Culture)),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "◌", "~")} {text.Text("Costs.Tokens")}", overview.TotalTokensToday.ToString("N0", text.Culture)),
            TerminalTheme.Metric($"{TerminalTheme.Icon(shell.Options, "↻", "~")} {text.Text("Costs.LastSync")}", overview.LastPricingSync?.ToLocalTime().ToString("g", text.Culture) ?? text.Text("Costs.Unavailable"))
        ], console.Profile.Width);
        console.Write(TerminalTheme.Panel(metrics, $"{TerminalTheme.Icon(shell.Options, "📈", "=")} {text.Text("Costs.Overview")}"));
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

    /// <summary>Builds an adaptive metrics grid that keeps cost summaries legible on narrow terminals.</summary>
    private static Grid CreateGrid(IReadOnlyList<IRenderable> metrics, int width)
    {
        var columns = width >= 112 ? 3 : width >= 72 ? 2 : 1;
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
                row[column] = offset + column < metrics.Count ? metrics[offset + column] : new Text(string.Empty);
            }
            grid.AddRow(row);
        }

        return grid;
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
        var (icon, fallback, label, foreground, background) = band switch
        {
            CostBand.Cheap => ("🌱", "$", text.Text("Costs.Cheap"), "black", TerminalTheme.Success),
            CostBand.Affordable => ("✓", "+", text.Text("Costs.Affordable"), "black", TerminalTheme.Info),
            CostBand.Premium => ("◆", "*", text.Text("Costs.Premium"), "black", TerminalTheme.Accent),
            _ => ("⚠", "!", text.Text("Costs.Extreme"), TerminalTheme.Primary, "red")
        };
        return new Markup(
            $"[{foreground} on {background}] {Markup.Escape(TerminalTheme.Icon(shell.Options, icon, fallback))} {Markup.Escape(label)} [/]");
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

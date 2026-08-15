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

public sealed class StatusView(IAnsiConsole console, ILocalizationService text) : IStatusView
{
    /// <summary>Renders configuration, secrets, prompts, storage, and synchronization status.</summary>
    public void Render(AppStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        TerminalTheme.WriteHeading(console, text.Text("Status.Title"));
        var table = new Table().Border(TableBorder.None).HideHeaders();
        table.AddColumn("Field");
        table.AddColumn("Value");
        AddRow(table, text.Text("Status.Setup"), status.Settings.SetupCompleted ? text.Text("Status.Completed") : text.Text("Status.Required"));
        AddRow(table, text.Text("Status.Language"), status.Settings.Language);
        AddRow(table, text.Text("Status.Model"), status.Settings.Model);
        AddRow(table, text.Text("Status.ApiKey"), status.HasApiKey ? text.Text("Status.Ready") : text.Text("Status.Missing"));
        AddRow(table, text.Text("Status.AdminKey"), status.HasAdminKey ? text.Text("Status.Ready") : text.Text("Status.Missing"));
        AddRow(table, text.Text("Status.Pricing"), status.LastPricingSync?.ToLocalTime().ToString("g", text.Culture) ?? text.Text("Costs.Unavailable"));
        AddRow(table, text.Text("Status.Database"), status.DatabasePath);
        AddRow(table, text.Text("Status.Logs"), status.LogsDirectory);
        AddRow(table, text.Text("Status.Prompts"), $"{status.PromptCount} · {status.PromptDirectory}");
        console.Write(table);
    }

    /// <summary>Adds one escaped status row.</summary>
    private static void AddRow(Table table, string name, string value) =>
        table.AddRow(new Markup($"[grey]{Markup.Escape(name)}[/]"), new Text(value));
}

public interface ICostsView
{
    void Render(CostOverview overview);
}

public sealed class CostsView(IAnsiConsole console, ILocalizationService text) : ICostsView
{
    /// <summary>Renders local usage estimates, optional organization cost, and cached official prices.</summary>
    public void Render(CostOverview overview)
    {
        ArgumentNullException.ThrowIfNull(overview);
        TerminalTheme.WriteHeading(console, text.Text("Costs.Title"), text.Text("Costs.Subtitle"));
        var metrics = new Grid();
        metrics.AddColumn();
        metrics.AddColumn();
        metrics.AddColumn();
        metrics.AddRow(
            Metric(text.Text("Costs.TodayEstimate"), FormatUsd(overview.EstimatedCostTodayUsd)),
            Metric(text.Text("Costs.MonthEstimate"), FormatUsd(overview.EstimatedCostCurrentMonthUsd)),
            Metric(text.Text("Costs.ApiCost"), overview.ActualOrganizationCostCurrentMonthUsd.HasValue ? FormatUsd(overview.ActualOrganizationCostCurrentMonthUsd.Value) : text.Text("Costs.Unavailable")));
        metrics.AddRow(
            Metric(text.Text("Costs.Requests"), overview.RequestsToday.ToString("N0", text.Culture)),
            Metric(text.Text("Costs.Tokens"), overview.TotalTokensToday.ToString("N0", text.Culture)),
            Metric(text.Text("Costs.LastSync"), overview.LastPricingSync?.ToLocalTime().ToString("g", text.Culture) ?? text.Text("Costs.Unavailable")));
        console.Write(metrics);
        console.WriteLine();

        var table = new Table().Border(TableBorder.None);
        table.Title = new TableTitle(Markup.Escape(text.Text("Costs.Models")));
        table.AddColumn(text.Text("Costs.Model"));
        table.AddColumn(new TableColumn(text.Text("Costs.Input")).RightAligned());
        table.AddColumn(new TableColumn(text.Text("Costs.Cached")).RightAligned());
        table.AddColumn(new TableColumn(text.Text("Costs.Output")).RightAligned());
        foreach (var price in overview.Prices)
        {
            table.AddRow(
                Markup.Escape(price.Model),
                FormatUsd(price.InputUsdPerMillionTokens),
                price.CachedInputUsdPerMillionTokens.HasValue ? FormatUsd(price.CachedInputUsdPerMillionTokens.Value) : "—",
                FormatUsd(price.OutputUsdPerMillionTokens));
        }
        console.Write(table);
    }

    /// <summary>Creates one label/value metric block.</summary>
    private static IRenderable Metric(string label, string value) => new Markup(
        $"[grey]{Markup.Escape(label)}[/]\n[bold white]{Markup.Escape(value)}[/]");

    /// <summary>Formats USD amounts using invariant decimal notation.</summary>
    private static string FormatUsd(decimal value) => $"${value:0.########}";
}

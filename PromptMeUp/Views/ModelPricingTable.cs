// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Shares the cached model price comparison between costs and AI settings.</summary>
internal static class ModelPricingTable
{
    /// <summary>Creates a responsive price table and retains supported models whose cached prices are unavailable.</summary>
    internal static IRenderable Create(
        ILocalizationService text,
        ConsoleRenderOptions options,
        IReadOnlyList<AiModelPrice> prices,
        string? selectedModel = null,
        bool selectableModelsOnly = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(prices);
        var entries = selectableModelsOnly
            ? AiModelCatalog.Models.Select(model => new PriceEntry(model.Id, prices.FirstOrDefault(price =>
                string.Equals(price.Model, model.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(price.ServiceTier, "standard", StringComparison.OrdinalIgnoreCase)
                && string.Equals(price.ContextWindow, "short", StringComparison.OrdinalIgnoreCase))))
            : prices.Select(price => new PriceEntry(price.Model, price));
        return new PricingContent(text, options, entries
            .OrderBy(entry => Classify(entry.Price))
            .ThenBy(entry => entry.Price is { } price ? price.InputUsdPerMillionTokens + price.OutputUsdPerMillionTokens : decimal.MaxValue)
            .ThenBy(entry => entry.Model, StringComparer.OrdinalIgnoreCase)
            .ToArray(), selectedModel);
    }

    /// <summary>Assigns a semantic band from the input-plus-output price per million tokens.</summary>
    private static CostBand Classify(AiModelPrice? price)
    {
        if (price is null)
        {
            return CostBand.Unavailable;
        }

        var total = price.InputUsdPerMillionTokens + price.OutputUsdPerMillionTokens;
        return total switch
        {
            <= 2m => CostBand.Cheap,
            <= 20m => CostBand.Affordable,
            <= 90m => CostBand.Premium,
            _ => CostBand.Extreme
        };
    }

    /// <summary>Renders the same supplied price rows within the actual available terminal width.</summary>
    private sealed class PricingContent(
        ILocalizationService text,
        ConsoleRenderOptions renderOptions,
        IReadOnlyList<PriceEntry> entries,
        string? selectedModel) : IRenderable
    {
        /// <summary>Allows the comparison to shrink to the width allocated by its parent view.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) =>
            new(Math.Min(1, Math.Max(0, maxWidth)), Math.Max(0, maxWidth));

        /// <summary>Switches between full columns, compact columns, and stacked metrics as space decreases.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            if (maxWidth <= 0)
            {
                return [];
            }

            var title = new Markup($"[bold {TerminalTheme.Accent}]{Markup.Escape(text.Text("Costs.Models"))}[/]");
            var body = entries.Count == 0
                ? (IRenderable)new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Costs.Unavailable"))}[/]")
                : maxWidth >= 48 ? CreateTable(maxWidth >= 72) : CreateStackedPrices(maxWidth);
            return ((IRenderable)new Rows(title, body)).Render(options, maxWidth);
        }

        /// <summary>Builds an open table without fixed or nonwrapping columns.</summary>
        private IRenderable CreateTable(bool separateTier)
        {
            var table = new Table().Border(TableBorder.None);
            table.AddColumn(Header("Costs.Model"));
            if (separateTier)
            {
                table.AddColumn(Header("Costs.Tier").Centered());
            }
            table.AddColumn(Header("Costs.Input").RightAligned());
            table.AddColumn(Header("Costs.Cached").RightAligned());
            table.AddColumn(Header("Costs.Output").RightAligned());

            foreach (var entry in entries)
            {
                var values = PriceValues(entry.Price);
                var model = ModelLabel(entry.Model);
                var band = CostLabel(Classify(entry.Price));
                IRenderable[] cells = separateTier
                    ? [model, band, values[0], values[1], values[2]]
                    : [new Rows(model, band), values[0], values[1], values[2]];
                table.AddRow(cells);
            }

            return table;
        }

        /// <summary>Stacks each model with labeled values when a multi-model table would be too narrow.</summary>
        private IRenderable CreateStackedPrices(int width)
        {
            var rows = new List<IRenderable>();
            foreach (var entry in entries)
            {
                rows.Add(ModelLabel(entry.Model));
                rows.Add(CostLabel(Classify(entry.Price)));
                var values = PriceValues(entry.Price);
                string[] labels = ["Costs.Input", "Costs.Cached", "Costs.Output"];
                if (width < 24)
                {
                    for (var index = 0; index < labels.Length; index++)
                    {
                        rows.Add(new Rows(Header(labels[index]).Header, values[index]));
                    }
                }
                else
                {
                    var grid = new Grid();
                    grid.AddColumn(new GridColumn().RightAligned());
                    grid.AddColumn(new GridColumn().LeftAligned());
                    for (var index = 0; index < labels.Length; index++)
                    {
                        grid.AddRow(Header(labels[index]).Header, values[index]);
                    }
                    rows.Add(grid);
                }
                rows.Add(new Text(" "));
            }

            return new Rows(rows);
        }

        /// <summary>Creates one localized high-contrast column heading.</summary>
        private TableColumn Header(string key) =>
            new(new Markup($"[bold {TerminalTheme.Primary}]{Markup.Escape(text.Text(key))}[/]"));

        /// <summary>Marks the selected model while keeping model identifiers readable in every theme.</summary>
        private IRenderable ModelLabel(string model)
        {
            var selected = string.Equals(model, selectedModel, StringComparison.OrdinalIgnoreCase);
            var marker = selected ? "> " : string.Empty;
            var color = selected ? TerminalTheme.Accent : TerminalTheme.Primary;
            return new Markup($"[bold {color}]{marker}{Markup.Escape(model)}[/]");
        }

        /// <summary>Renders the existing semantic cost bands and explicitly identifies missing prices.</summary>
        private IRenderable CostLabel(CostBand band)
        {
            if (band == CostBand.Unavailable)
            {
                return new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Costs.Unavailable"))}[/]");
            }

            var (icon, fallback, key, color) = band switch
            {
                CostBand.Cheap => ("🌱", "$", "Costs.Cheap", TerminalTheme.Success),
                CostBand.Affordable => ("✓", "+", "Costs.Affordable", TerminalTheme.Info),
                CostBand.Premium => ("◆", "*", "Costs.Premium", TerminalTheme.Accent),
                _ => ("⚠", "!", "Costs.Extreme", TerminalTheme.Error)
            };
            return new Markup($"[bold {color}]{Markup.Escape(TerminalTheme.IconPrefix(renderOptions, icon, fallback))}{Markup.Escape(text.Text(key))}[/]");
        }

        /// <summary>Formats the three USD prices per million tokens without inventing missing rates.</summary>
        private static IRenderable[] PriceValues(AiModelPrice? price) =>
        [
            PriceValue(price?.InputUsdPerMillionTokens, TerminalTheme.Primary),
            PriceValue(price?.CachedInputUsdPerMillionTokens, TerminalTheme.Muted),
            PriceValue(price?.OutputUsdPerMillionTokens, TerminalTheme.Primary)
        ];

        /// <summary>Uses an invariant dollar amount or an unavailable-rate marker.</summary>
        private static IRenderable PriceValue(decimal? value, string color) =>
            new Markup($"[{color}]{Markup.Escape(value.HasValue ? "$" + value.Value.ToString("0.########", CultureInfo.InvariantCulture) : "—")}[/]");
    }

    private sealed record PriceEntry(string Model, AiModelPrice? Price);

    private enum CostBand
    {
        Cheap,
        Affordable,
        Premium,
        Extreme,
        Unavailable
    }
}

// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Renders a compact, theme-aware prompt status strip with optional separators.</summary>
internal sealed class TerminalPromptBar(
    ILocalizationService text,
    ShellRuntimeStatus? status,
    bool showBorders = true,
    bool showStatus = true,
    bool showBreakdown = true)
{
    /// <summary>Returns the status strip and its optional top separator as single terminal rows.</summary>
    internal IReadOnlyList<string> Header(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        if (!showStatus)
        {
            return showBorders ? [Rule(width)] : [];
        }
        var rows = new List<string>();
        if (showBorders)
        {
            rows.Add(Rule(width));
        }
        if (showBreakdown && TryAlignedRows(width, out var alignedStatus, out var alignedBreakdown))
        {
            rows.Add(alignedStatus);
            rows.Add(alignedBreakdown);
        }
        else
        {
            rows.Add(Status(width));
            if (showBreakdown && Breakdown(width) is { } breakdown)
            {
                rows.Add(breakdown);
            }
        }
        return rows;
    }

    /// <summary>Returns an optional bottom separator without enclosing the conversation in a card.</summary>
    internal IReadOnlyList<string> Footer(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        return showBorders ? [Rule(width)] : [];
    }

    /// <summary>Combines the model with current context and cost only when the terminal can fit them.</summary>
    private string Status(int width)
    {
        var model = Clip(status?.Model ?? "PromptMeUp", Math.Max(1, width - 2));
        var modelMarkup = $"[bold {TerminalTheme.Accent}]{Markup.Escape(model)}[/]";
        if (status is not { ActiveContextTokens: { } used, ContextBudgetTokens: > 0 })
        {
            return modelMarkup;
        }

        var percentage = Math.Clamp(used * 100d / status.ContextBudgetTokens, 0d, 100d);
        var context = $"~{FormatTokens(used)}/{FormatTokens(status.ContextBudgetTokens)} · "
            + $"{percentage.ToString("0", CultureInfo.InvariantCulture)}%";
        var separator = "  │  ";
        if (new Segment(model + separator + context).CellCount() > width)
        {
            return modelMarkup;
        }

        var result = modelMarkup + $"[{TerminalTheme.Divider}]{separator}[/]"
            + $"[bold {TerminalTheme.Info}]{Markup.Escape(context)}[/]";
        var plainStatus = model + separator + context;
        var cost = status.SessionCostKnown
            ? $"{text.Text("Shell.SessionCost")}: ~${status.RunningCostUsd.ToString("0.0000", CultureInfo.InvariantCulture)}"
            : string.Empty;
        if (cost.Length > 0 && new Segment(plainStatus + separator + cost).CellCount() <= width)
        {
            result += $"[{TerminalTheme.Divider}]{separator}[/]"
                + $"[{TerminalTheme.Success}]{Markup.Escape(cost)}[/]";
            plainStatus += separator + cost;
        }
        var (meter, meterMarkup) = ContextMeter(percentage);
        if (new Segment(plainStatus + separator + meter).CellCount() <= width)
        {
            result += $"[{TerminalTheme.Divider}]{separator}[/]"
                + meterMarkup;
        }
        return result;
    }

    /// <summary>Aligns the status and context legend at shared column boundaries when full labels fit.</summary>
    private bool TryAlignedRows(int width, out string statusRow, out string breakdownRow)
    {
        statusRow = string.Empty;
        breakdownRow = string.Empty;
        if (status is not { HasContextBreakdown: true, ActiveContextTokens: { } used, ContextBudgetTokens: > 0,
                SessionCostKnown: true })
        {
            return false;
        }

        var percentage = Math.Clamp(used * 100d / status.ContextBudgetTokens, 0d, 100d);
        var model = Clip(status.Model, Math.Max(1, width - 2));
        var context = $"~{FormatTokens(used)}/{FormatTokens(status.ContextBudgetTokens)} · "
            + $"{percentage.ToString("0", CultureInfo.InvariantCulture)}%";
        var cost = $"{text.Text("Shell.SessionCost")}: ~${status.RunningCostUsd.ToString("0.0000", CultureInfo.InvariantCulture)}";
        var (meter, meterMarkup) = ContextMeter(percentage);
        (string Plain, string Markup)[] upper =
        [
            (model, $"[bold {TerminalTheme.Accent}]{Markup.Escape(model)}[/]"),
            (context, $"[bold {TerminalTheme.Info}]{Markup.Escape(context)}[/]"),
            (cost, $"[{TerminalTheme.Success}]{Markup.Escape(cost)}[/]"),
            (meter, meterMarkup)
        ];
        var lower = ContextMetrics(status).Select(metric =>
        {
            var plain = $"{metric.Label} ~{FormatTokens(metric.Value)}";
            var markup = $"[{metric.Color}]{Markup.Escape(metric.Label)}[/] "
                + $"[{TerminalTheme.Primary}]~{FormatTokens(metric.Value)}[/]";
            return (Plain: plain, Markup: markup);
        }).ToArray();

        for (var count = lower.Length; count >= 5; count--)
        {
            var widths = Enumerable.Range(0, count)
                .Select(index => Math.Max(new Segment(lower[index].Plain).CellCount(),
                    index < upper.Length ? new Segment(upper[index].Plain).CellCount() : 0))
                .ToArray();
            if (widths.Sum() + (count - 1) * 5 > width)
            {
                continue;
            }

            var separator = $"[{TerminalTheme.Divider}]  │  [/]";
            statusRow = string.Join(separator, upper.Select((cell, index) => PadCell(cell, widths[index])));
            breakdownRow = string.Join(separator, lower.Take(count).Select((cell, index) => PadCell(cell, widths[index])));
            return true;
        }
        return false;
    }

    /// <summary>Returns a thirteen-cell theme-colored context meter and its unstyled width equivalent.</summary>
    private static (string Plain, string Markup) ContextMeter(double percentage)
    {
        const int meterWidth = 13;
        var filled = Math.Clamp((int)Math.Round(percentage * meterWidth / 100d), 0, meterWidth);
        return (new string('━', meterWidth),
            $"[{TerminalTheme.Accent}]{new string('━', filled)}[/]"
            + $"[{TerminalTheme.Divider}]{new string('─', meterWidth - filled)}[/]");
    }

    /// <summary>Pads a colored value to its shared terminal-cell column width.</summary>
    private static string PadCell((string Plain, string Markup) cell, int width) =>
        cell.Markup + new string(' ', width - new Segment(cell.Plain).CellCount());

    /// <summary>Shows the measured context categories, with guide tokens marked as part of system tokens.</summary>
    private string? Breakdown(int width)
    {
        if (status is not { HasContextBreakdown: true, ActiveContextTokens: { }, ContextBudgetTokens: > 0 })
        {
            return null;
        }

        var metrics = ContextMetrics(status);
        foreach (var compact in new[] { false, true })
        {
            foreach (var count in new[] { 6, 5, 4, 3, 2 })
            {
                var selected = metrics.Take(count).ToArray();
                var plain = string.Join("  │  ", selected.Select(metric =>
                    $"{(compact ? metric.ShortLabel : metric.Label)} ~{FormatTokens(metric.Value)}"));
                if (new Segment(plain).CellCount() > width)
                {
                    continue;
                }
                return string.Join($"[{TerminalTheme.Divider}]  │  [/]", selected.Select(metric =>
                    $"[{metric.Color}]{Markup.Escape(compact ? metric.ShortLabel : metric.Label)}[/] "
                    + $"[{TerminalTheme.Primary}]~{FormatTokens(metric.Value)}[/]"));
            }
        }
        return null;
    }

    /// <summary>Provides the same ordered context categories to aligned and compact layouts.</summary>
    private (string Label, string ShortLabel, long Value, string Color)[] ContextMetrics(ShellRuntimeStatus current)
    {
        var free = Math.Max(0, current.ContextBudgetTokens - current.ActiveContextTokens!.Value);
        return
        [
            (text.Text("Shell.ContextSystem"), "S", current.SystemInstructionTokens, TerminalTheme.Warning),
            (text.Text("Shell.ContextUser"), "U", current.UserMessageTokens, TerminalTheme.Info),
            (text.Text("Shell.ContextTool"), "T", current.ToolOutputTokens, TerminalTheme.Accent),
            (text.Text("Shell.ContextAssistant"), "A", current.AssistantMessageTokens, TerminalTheme.Success),
            (text.Text("Shell.ContextFree"), "·", free, TerminalTheme.Muted),
            (text.Text("Shell.ContextGuideIncluded"), "G⊂S", current.GuideTokens, TerminalTheme.Warning)
        ];
    }

    /// <summary>Draws a thin full-width theme separator around the active input area.</summary>
    private static string Rule(int width) => ThemeSeparator.Markup(width);

    /// <summary>Shortens token amounts for a one-line status strip.</summary>
    private static string FormatTokens(long value) => value switch
    {
        >= 1_000_000 => (value / 1_000_000d).ToString("0.0", CultureInfo.InvariantCulture) + "M",
        >= 10_000 => (value / 1_000d).ToString("0.0", CultureInfo.InvariantCulture) + "K",
        _ => value.ToString("N0", CultureInfo.InvariantCulture)
    };

    /// <summary>Clips labels on Unicode text-element boundaries before terminal rendering.</summary>
    private static string Clip(string value, int width)
    {
        var elements = StringInfo.GetTextElementEnumerator(value);
        var result = new List<string>();
        var cells = 0;
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            var size = new Segment(element).CellCount();
            if (cells + size > width)
            {
                return string.Concat(result) + (width > 1 ? "…" : string.Empty);
            }
            result.Add(element);
            cells += size;
        }
        return string.Concat(result);
    }
}

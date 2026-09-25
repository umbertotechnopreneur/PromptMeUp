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
        rows.Add(Status(width));
        if (showBreakdown && Breakdown(width) is { } breakdown)
        {
            rows.Add(breakdown);
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
        var context = $"{FormatTokens(used)}/{FormatTokens(status.ContextBudgetTokens)} · "
            + $"{percentage.ToString("0", CultureInfo.InvariantCulture)}%";
        var separator = "  │  ";
        if (new Segment(model + separator + context).CellCount() > width)
        {
            return modelMarkup;
        }

        var result = modelMarkup + $"[{TerminalTheme.Divider}]{separator}[/]"
            + $"[bold {TerminalTheme.Info}]{Markup.Escape(context)}[/]";
        var plainStatus = model + separator + context;
        const int meterWidth = 8;
        var filled = Math.Clamp((int)Math.Round(percentage * meterWidth / 100d), 0, meterWidth);
        var meter = new string('━', meterWidth);
        if (new Segment(plainStatus + separator + meter).CellCount() <= width)
        {
            result += $"[{TerminalTheme.Divider}]{separator}[/]"
                + $"[{TerminalTheme.Accent}]{new string('━', filled)}[/]"
                + $"[{TerminalTheme.Divider}]{new string('─', meterWidth - filled)}[/]";
            plainStatus += separator + meter;
        }
        var cost = status.SessionCostKnown
            ? $"{text.Text("Shell.SessionCost")}: ${status.RunningCostUsd.ToString("0.0000", CultureInfo.InvariantCulture)}"
            : string.Empty;
        if (cost.Length > 0 && new Segment(plainStatus + separator + cost).CellCount() <= width)
        {
            result += $"[{TerminalTheme.Divider}]{separator}[/]"
                + $"[{TerminalTheme.Success}]{Markup.Escape(cost)}[/]";
        }
        return result;
    }

    /// <summary>Shows the measured context categories, with guide tokens marked as part of system tokens.</summary>
    private string? Breakdown(int width)
    {
        if (status is not { HasContextBreakdown: true, ActiveContextTokens: { }, ContextBudgetTokens: > 0 })
        {
            return null;
        }

        var free = Math.Max(0, status.ContextBudgetTokens - status.ActiveContextTokens.Value);
        (string Label, string ShortLabel, long Value, string Color)[] metrics =
        [
            (text.Text("Shell.ContextSystem"), "S", status.SystemInstructionTokens, TerminalTheme.Warning),
            (text.Text("Shell.ContextUser"), "U", status.UserMessageTokens, TerminalTheme.Info),
            (text.Text("Shell.ContextTool"), "T", status.ToolOutputTokens, TerminalTheme.Accent),
            (text.Text("Shell.ContextAssistant"), "A", status.AssistantMessageTokens, TerminalTheme.Success),
            (text.Text("Shell.ContextFree"), "·", free, TerminalTheme.Muted),
            (text.Text("Shell.ContextGuideIncluded"), "G⊂S", status.GuideTokens, TerminalTheme.Warning)
        ];
        foreach (var compact in new[] { false, true })
        {
            foreach (var count in new[] { 6, 5, 4, 3, 2 })
            {
                var selected = metrics.Take(count).ToArray();
                var plain = string.Join("  │  ", selected.Select(metric =>
                    $"{(compact ? metric.ShortLabel : metric.Label)} {FormatTokens(metric.Value)}"));
                if (new Segment(plain).CellCount() > width)
                {
                    continue;
                }
                return string.Join($"[{TerminalTheme.Divider}]  │  [/]", selected.Select(metric =>
                    $"[{metric.Color}]{Markup.Escape(compact ? metric.ShortLabel : metric.Label)}[/] "
                    + $"[{TerminalTheme.Primary}]{FormatTokens(metric.Value)}[/]"));
            }
        }
        return null;
    }

    /// <summary>Draws a thin full-width theme separator around the active input area.</summary>
    private static string Rule(int width) => $"[{TerminalTheme.Accent}]{new string('─', width)}[/]";

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

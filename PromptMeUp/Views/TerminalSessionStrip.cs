// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Fits indivisible session metrics and an accurate context legend into available terminal rows.</summary>
internal sealed class TerminalSessionStrip(ILocalizationService text, ShellRuntimeStatus? status,
    TerminalSession? session = null)
{
    private const string Gap = "  │  ";
    internal const int MeterWidth = 13;

    /// <summary>Drops secondary metrics before wrapping essential context categories as complete blocks.</summary>
    internal IReadOnlyList<string> Rows(int width, bool showBreakdown = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        var primary = new List<Block>
        {
            Value(TerminalText.Clip(status?.Model ?? "PromptMeUp", Math.Max(1, width / 3)), TerminalTheme.Accent)
        };
        if (status is { ActiveContextTokens: { } used, ContextBudgetTokens: > 0 })
        {
            var percentage = Math.Clamp(used * 100d / status.ContextBudgetTokens, 0d, 100d);
            primary.Add(Value($"~{Tokens(used)}/{Tokens(status.ContextBudgetTokens)} · {percentage:0}%", TerminalTheme.Info));
        }

        // Priority order is intentional: state and mode survive before cost, elapsed time, and the meter.
        var optional = new List<Block>();
        if (session is not null)
        {
            optional.Add(Value(TerminalActivityRow.Symbol(session.State, !session.Options.NoEmoji) + " "
                + text.Text("Terminal.State." + session.State), TerminalActivityRow.Color(session.State)));
            var modeKey = status?.ConversationMode is { } mode ? "Terminal.Mode." + mode : session.ModeKey;
            optional.Add(Value(text.Text(modeKey), TerminalTheme.AccentSecondary));
        }
        if (status is { SessionCostKnown: true })
        {
            optional.Add(Value(text.Text("Shell.SessionCost") + ": ~$"
                + status.RunningCostUsd.ToString("0.0000", CultureInfo.InvariantCulture), TerminalTheme.Success));
        }
        if (session is not null) optional.Add(Value(TerminalActivityRow.Duration(session.Elapsed), TerminalTheme.Muted));
        if (status is { HasContextBreakdown: true, ContextBudgetTokens: > 0 }) optional.Add(Meter(status));
        foreach (var block in optional)
        {
            if (Width(primary) + TerminalText.Width(Gap) + block.Width <= width) primary.Add(block);
        }

        var rows = Pack(primary, width);
        if (showBreakdown && status is { HasContextBreakdown: true, ActiveContextTokens: { }, ContextBudgetTokens: > 0 })
        {
            var metrics = ContextBlocks(status, compact: width < 100);
            // System, user, tool and assistant counts are never dropped to make space for optional metrics.
            rows.AddRange(Pack(Width(metrics) <= width ? metrics : metrics.Take(4).ToArray(), width));
            if (Width(metrics) > width && width >= 80) rows.AddRange(Pack(metrics.Skip(4).ToArray(), width));
        }
        return rows;
    }

    /// <summary>Builds the same numeric role breakdown used in the full context summary.</summary>
    private Block[] ContextBlocks(ShellRuntimeStatus current, bool compact)
    {
        (string Key, string Short, long Count, string Color)[] metrics =
        [
            ("Shell.ContextSystem", "S", current.SystemInstructionTokens, TerminalTheme.Warning),
            ("Shell.ContextUser", "U", current.UserMessageTokens, TerminalTheme.Info),
            ("Shell.ContextTool", "T", current.ToolOutputTokens, TerminalTheme.Accent),
            ("Shell.ContextAssistant", "A", current.AssistantMessageTokens, TerminalTheme.Success),
            ("Shell.ContextFree", "·", Math.Max(0, current.ContextBudgetTokens - current.ActiveContextTokens!.Value), TerminalTheme.Muted),
            ("Shell.ContextGuideIncluded", "G⊂S", current.GuideTokens, TerminalTheme.Warning)
        ];
        return metrics.Select(metric =>
        {
            var label = compact ? metric.Short : text.Text(metric.Key);
            var value = "~" + Tokens(metric.Count);
            return new Block(label + " " + value,
                $"[{metric.Color}]{Markup.Escape(label)}[/] [{TerminalTheme.Primary}]{value}[/]");
        }).ToArray();
    }

    /// <summary>Uses the shared proportional allocation so the meter and context legend agree.</summary>
    private static Block Meter(ShellRuntimeStatus current)
    {
        var cells = ConsoleShellView.AllocateContextBarCells(current, MeterWidth);
        string[] colors = [TerminalTheme.Warning, TerminalTheme.Info, TerminalTheme.Accent, TerminalTheme.Success, TerminalTheme.Divider];
        var markup = string.Concat(cells.Select((count, index) =>
            $"[{colors[index]}]{new string(index == 4 ? '─' : '━', count)}[/]"));
        return new Block(new string('─', MeterWidth), markup);
    }

    /// <summary>Wraps between complete blocks and clips only a block wider than the whole terminal.</summary>
    private static List<string> Pack(IReadOnlyList<Block> blocks, int width)
    {
        var rows = new List<string>();
        var current = new List<Block>();
        foreach (var source in blocks)
        {
            var block = source.Width > width ? Value(TerminalText.Clip(source.Plain, width), TerminalTheme.Primary) : source;
            if (current.Count > 0 && Width(current) + TerminalText.Width(Gap) + block.Width > width)
            {
                rows.Add(Join(current));
                current.Clear();
            }
            current.Add(block);
        }
        if (current.Count > 0) rows.Add(Join(current));
        return rows;
    }

    /// <summary>Measures a row including its separators in terminal cells.</summary>
    private static int Width(IReadOnlyList<Block> blocks) =>
        blocks.Sum(block => block.Width) + Math.Max(0, blocks.Count - 1) * TerminalText.Width(Gap);

    /// <summary>Joins already escaped blocks with the active theme's divider color.</summary>
    private static string Join(IEnumerable<Block> blocks) =>
        string.Join($"[{TerminalTheme.Divider}]{Gap}[/]", blocks.Select(block => block.Markup));

    /// <summary>Creates a literal themed metric without accepting provider markup.</summary>
    private static Block Value(string value, string color) => new(value, $"[{color}]{Markup.Escape(value)}[/]");

    /// <summary>Uses compact, culture-independent counts that remain readable in narrow terminals.</summary>
    private static string Tokens(long value) => value switch
    {
        >= 1_000_000 => (value / 1_000_000d).ToString("0.0", CultureInfo.InvariantCulture) + "M",
        >= 10_000 => (value / 1_000d).ToString("0.0", CultureInfo.InvariantCulture) + "K",
        _ => value.ToString("N0", CultureInfo.InvariantCulture)
    };

    /// <summary>Keeps the measurement and styled representation of one indivisible metric together.</summary>
    private sealed record Block(string Plain, string Markup)
    {
        internal int Width => TerminalText.Width(Plain);
    }
}

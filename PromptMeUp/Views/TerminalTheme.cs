// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

internal static class TerminalTheme
{
    private static readonly Color[] RuleGradientStops =
    [
        new(39, 220, 232), new(100, 142, 255), new(172, 114, 244), new(245, 101, 172)
    ];

    internal static TerminalThemeDefinition Current { get; private set; } = TerminalThemeDefinition.Default;

    internal static string Background => Current.Colors.Background;
    internal static string Accent => Current.Colors.Accent;
    internal static string Info => Current.Colors.Info;
    internal static string Primary => Current.Colors.Primary;
    internal const string FieldValue = "#F5F5F5";
    internal const string White = "#FFFFFF";
    internal static string Muted => Current.Colors.Muted;
    internal static string Divider => Current.Colors.Divider;
    internal static string Success => Current.Colors.Success;
    internal static string Warning => Current.Colors.Warning;
    internal static string Error => Current.Colors.Error;
    internal static string SelectionBackground => Current.Colors.SelectionBackground;
    internal static string SelectionForeground => Current.Colors.SelectionForeground;

    /// <summary>Applies a validated catalog theme to subsequent terminal rendering.</summary>
    internal static void Apply(TerminalThemeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(definition.Colors);
        Current = definition;
    }

    /// <summary>Returns an explicit emoji presentation for ambiguous icons or an ASCII fallback for constrained terminals.</summary>
    internal static string Icon(ConsoleRenderOptions options, string icon, string fallback)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(icon);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);
        return options.NoEmoji ? fallback : icon switch
        {
            // Text-default symbols can occupy an emoji cell and visually consume the following space.
            "ℹ" or "⚙" or "🛡" or "⚠" or "⏱" or "✂" => icon + "\uFE0F",
            _ => icon
        };
    }

    /// <summary>Starts a label with an icon and one trailing space, leaving leading indentation to the layout.</summary>
    internal static string IconPrefix(ConsoleRenderOptions options, string icon, string fallback) =>
        options.NoEmoji ? $"{Icon(options, icon, fallback)}\u00A0" : $"{Icon(options, icon, fallback)} ";

    /// <summary>Creates one compact label-value metric for a dense, frameless session summary.</summary>
    internal static CompactTerminalMetric CompactMetric(string label, string value, string? valueColor = null) =>
        new(label, value, valueColor ?? Primary);

    /// <summary>Builds a frameless responsive grid of right-aligned labels and left-aligned values.</summary>
    internal static Grid PairGrid(
        IReadOnlyList<CompactTerminalMetric> metrics,
        int preferredPairs,
        int width,
        bool preservePairCount = false,
        int? firstLabelWidth = null)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(preferredPairs);
        var responsivePairs = width >= 112 ? 3 : width >= 72 ? 2 : 1;
        var pairs = preservePairCount ? preferredPairs : Math.Min(preferredPairs, responsivePairs);
        var grid = new Grid();
        for (var pair = 0; pair < pairs; pair++)
        {
            grid.AddColumn(new GridColumn { Width = pair == 0 ? firstLabelWidth : null }.RightAligned().NoWrap());
            grid.AddColumn(new GridColumn().LeftAligned());
        }

        for (var offset = 0; offset < metrics.Count; offset += pairs)
        {
            var row = new IRenderable[pairs * 2];
            for (var pair = 0; pair < pairs; pair++)
            {
                if (offset + pair < metrics.Count)
                {
                    var metric = metrics[offset + pair];
                    row[pair * 2] = new Markup($"[{Muted}]{Markup.Escape(metric.Label)}:[/]");
                    row[(pair * 2) + 1] = new Markup($"[bold {metric.ValueColor}]{Markup.Escape(metric.Value)}[/]");
                }
                else
                {
                    row[pair * 2] = new Text(string.Empty);
                    row[(pair * 2) + 1] = new Text(string.Empty);
                }
            }

            grid.AddRow(row);
        }

        return grid;
    }

    /// <summary>Writes an accessible 80%-width divider with a concise section label.</summary>
    internal static void WriteRule(IAnsiConsole console, string title, string? color = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var targetWidth = Math.Max(1, (int)Math.Floor(console.Profile.Width * 0.8d));
        var dividerWidth = Math.Max(1, targetWidth - title.Length - 1);
        console.WriteLine();
        console.MarkupLine(
            $"[bold {color ?? Info}]{Markup.Escape(title)}[/] [{Divider}]{new string('─', dividerWidth)}[/]");
    }

    /// <summary>Writes an accessible 80%-width heading followed by a restrained horizontal color gradient.</summary>
    internal static void WriteGradientRule(IAnsiConsole console, string title, string? color = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var targetWidth = Math.Max(1, (int)Math.Floor(console.Profile.Width * 0.8d));
        var dividerWidth = Math.Max(1, targetWidth - title.Length - 1);
        console.Write(new Text(title, Style.Parse("bold " + (color ?? Info))));
        console.Write(new Text(" "));
        var character = console.Profile.Capabilities.Unicode ? "─" : "-";
        for (var index = 0; index < dividerWidth; index++)
        {
            console.Write(new Text(character, new Style(RuleGradient(index, dividerWidth))));
        }

        console.WriteLine();
    }

    /// <summary>Interpolates the shared cyan-to-pink accent across one horizontal rule.</summary>
    private static Color RuleGradient(int index, int length)
    {
        var position = length <= 1 ? 0d : index / (double)(length - 1) * (RuleGradientStops.Length - 1);
        var stop = Math.Min((int)position, RuleGradientStops.Length - 2);
        var fraction = position - stop;
        var start = RuleGradientStops[stop];
        var end = RuleGradientStops[stop + 1];
        return new Color(
            (byte)Math.Round(start.R * (1d - fraction) + end.R * fraction),
            (byte)Math.Round(start.G * (1d - fraction) + end.G * fraction),
            (byte)Math.Round(start.B * (1d - fraction) + end.B * fraction));
    }

    /// <summary>Writes an unboxed section with a continuous divider and escaped multiline content.</summary>
    internal static void WriteSection(IAnsiConsole console, string title, string content, string? color = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(content);
        WriteRule(console, title, color);
        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            console.MarkupLine($"  [{Primary}]{Markup.Escape(line)}[/]");
        }

        console.WriteLine();
    }

    /// <summary>Writes escaped multiline content under a colored label without surrounding it with a card.</summary>
    internal static void WriteBlock(IAnsiConsole console, string label, string content, string? color = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(content);
        console.MarkupLine($"[bold {color ?? Info}]{Markup.Escape(label)}[/]");
        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            console.MarkupLine($"  [{Primary}]{Markup.Escape(line)}[/]");
        }

        console.WriteLine();
    }
}

internal sealed record CompactTerminalMetric(string Label, string Value, string ValueColor);

// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

internal static class TerminalTheme
{
    internal const string Accent = "mediumpurple2";
    internal const string Info = "deepskyblue1";
    internal const string Primary = "white";
    internal const string Muted = "grey78";
    internal const string Divider = "grey58";
    internal const string Success = "springgreen2";

    /// <summary>Returns a visual icon when supported or an ASCII fallback for constrained terminals.</summary>
    internal static string Icon(ConsoleRenderOptions options, string icon, string fallback)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(icon);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);
        return options.NoEmoji ? fallback : icon;
    }

    /// <summary>Returns an icon followed by a non-breaking space so labels never touch their visual cue.</summary>
    internal static string IconPrefix(ConsoleRenderOptions options, string icon, string fallback) =>
        $"{Icon(options, icon, fallback)}\u00A0";

    /// <summary>Creates one compact label-value metric for a dense, frameless session summary.</summary>
    internal static CompactTerminalMetric CompactMetric(string label, string value, string valueColor = Primary) =>
        new(label, value, valueColor);

    /// <summary>Builds a frameless responsive grid of right-aligned labels and left-aligned values.</summary>
    internal static Grid PairGrid(
        IReadOnlyList<CompactTerminalMetric> metrics,
        int preferredPairs,
        int width,
        bool preservePairCount = false)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(preferredPairs);
        var responsivePairs = width >= 112 ? 3 : width >= 72 ? 2 : 1;
        var pairs = preservePairCount ? preferredPairs : Math.Min(preferredPairs, responsivePairs);
        var grid = new Grid();
        for (var pair = 0; pair < pairs; pair++)
        {
            grid.AddColumn(new GridColumn().RightAligned().NoWrap());
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
    internal static void WriteRule(IAnsiConsole console, string title, string color = Info)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var targetWidth = Math.Max(1, (int)Math.Floor(console.Profile.Width * 0.8d));
        var dividerWidth = Math.Max(1, targetWidth - title.Length - 1);
        console.WriteLine();
        console.MarkupLine(
            $"[bold {color}]{Markup.Escape(title)}[/] [{Divider}]{new string('─', dividerWidth)}[/]");
    }

    /// <summary>Writes an unboxed section with a continuous divider and escaped multiline content.</summary>
    internal static void WriteSection(IAnsiConsole console, string title, string content, string color = Info)
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
    internal static void WriteBlock(IAnsiConsole console, string label, string content, string color = Info)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(content);
        console.MarkupLine($"[bold {color}]{Markup.Escape(label)}[/]");
        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            console.MarkupLine($"  [{Primary}]{Markup.Escape(line)}[/]");
        }

        console.WriteLine();
    }
}

internal sealed record CompactTerminalMetric(string Label, string Value, string ValueColor);

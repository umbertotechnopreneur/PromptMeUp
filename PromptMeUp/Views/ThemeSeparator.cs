// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Draws a responsive theme separator using the same three color stops on every surface.</summary>
internal sealed class ThemeSeparator(string? title = null, string? titleColor = null, int? width = null) : IRenderable
{
    /// <summary>Reserves only the available width for the single separator row.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        new(0, Math.Max(0, Math.Min(width ?? maxWidth, maxWidth)));

    /// <summary>Clips a title if needed, then fills the remaining cells with a theme-colored line.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var available = Math.Max(0, Math.Min(width ?? maxWidth, maxWidth));
        if (available == 0)
        {
            yield break;
        }

        if (!string.IsNullOrEmpty(title))
        {
            var heading = new Segment(title + " ", Style.Parse("bold " + (titleColor ?? TerminalTheme.Info)));
            if (heading.CellCount() >= available)
            {
                foreach (var segment in Segment.Truncate([heading], available))
                {
                    yield return segment;
                }
                yield break;
            }

            yield return heading;
            available -= heading.CellCount();
        }

        var character = options.Capabilities.Unicode ? "─" : "-";
        for (var index = 0; index < available; index++)
        {
            yield return new Segment(character, new Style(GradientColor(index, available)));
        }
    }

    /// <summary>Provides the same gradient as escaped markup for line-based prompt bars.</summary>
    internal static string Markup(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        var result = new StringBuilder(width * 15);
        for (var index = 0; index < width; index++)
        {
            var color = GradientColor(index, width);
            result.Append($"[#{color.R:X2}{color.G:X2}{color.B:X2}]─[/]");
        }
        return result.ToString();
    }

    /// <summary>Interpolates the theme's main accent, information blue, and single secondary accent.</summary>
    private static Color GradientColor(int index, int length)
    {
        var stops = new[]
        {
            ParseColor(TerminalTheme.Accent), ParseColor(TerminalTheme.Info),
            ParseColor(TerminalTheme.AccentSecondary)
        };
        var position = length <= 1 ? 0d : index / (double)(length - 1) * 2d;
        var stop = Math.Min((int)position, 1);
        var fraction = position - stop;
        var first = stops[stop];
        var second = stops[stop + 1];
        return new Color(
            (byte)Math.Round(first.R * (1d - fraction) + second.R * fraction),
            (byte)Math.Round(first.G * (1d - fraction) + second.G * fraction),
            (byte)Math.Round(first.B * (1d - fraction) + second.B * fraction));
    }

    /// <summary>Converts a validated theme hex value to a Spectre color.</summary>
    private static Color ParseColor(string value) => new(
        byte.Parse(value.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        byte.Parse(value.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        byte.Parse(value.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
}

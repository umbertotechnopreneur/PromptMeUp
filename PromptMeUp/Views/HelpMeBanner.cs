// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Renders the HELP ME artwork with white initials and a cyan-to-pink truecolor gradient.</summary>
internal sealed class HelpMeBanner : IRenderable
{
    private const int BannerWidth = 54;
    private static readonly string[][] Glyphs =
    [
        ["██╗  ██╗", "██║  ██║", "███████║", "██╔══██║", "██║  ██║", "╚═╝  ╚═╝"],
        ["███████╗", "██╔════╝", "█████╗  ", "██╔══╝  ", "███████╗", "╚══════╝"],
        ["██╗     ", "██║     ", "██║     ", "██║     ", "███████╗", "╚══════╝"],
        ["██████╗ ", "██╔══██╗", "██████╔╝", "██╔═══╝ ", "██║     ", "╚═╝     "],
        ["   ", "   ", "   ", "   ", "   ", "   "],
        ["███╗   ███╗", "████╗ ████║", "██╔████╔██║", "██║╚██╔╝██║", "██║ ╚═╝ ██║", "╚═╝     ╚═╝"],
        ["███████╗", "██╔════╝", "█████╗  ", "██╔══╝  ", "███████╗", "╚══════╝"]
    ];
    private static readonly Color[] GradientStops =
    [
        new(39, 220, 232), new(100, 142, 255), new(172, 114, 244), new(245, 101, 172)
    ];
    private static readonly Style InitialStyle = new(Color.White, decoration: Decoration.Bold);

    /// <summary>Reserves the full artwork when possible and a plain-text wordmark in compact terminals.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        var width = Math.Max(0, maxWidth);
        return new Measurement(Math.Min(7, width), Math.Min(options.Capabilities.Unicode ? BannerWidth : 7, width));
    }

    /// <summary>Preserves complete letter shapes and lets Spectre adapt colors to terminal capabilities.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (!options.Capabilities.Unicode || maxWidth < BannerWidth)
        {
            const string compact = "HELP ME";
            for (var index = 0; index < Math.Min(compact.Length, maxWidth); index++)
            {
                yield return new Segment(compact[index].ToString(), index is 0 or 5
                    ? InitialStyle : new Style(Gradient(index * 8, 0)));
            }
            yield break;
        }

        for (var row = 0; row < 6; row++)
        {
            if (row > 0)
            {
                yield return Segment.LineBreak;
            }
            var column = 0;
            for (var glyph = 0; glyph < Glyphs.Length; glyph++)
            {
                foreach (var character in Glyphs[glyph][row])
                {
                    yield return new Segment(character.ToString(), glyph is 0 or 5
                        ? InitialStyle : new Style(Gradient(column, row)));
                    column++;
                }
            }
        }
    }

    /// <summary>Interpolates the original artwork's diagonal gradient independently of interface theme colors.</summary>
    private static Color Gradient(int column, int row)
    {
        var position = Math.Clamp((column - 8) / 46d * 0.9d + row / 5d * 0.1d, 0d, 1d) * 3d;
        var index = Math.Min((int)position, GradientStops.Length - 2);
        var fraction = position - index;
        var start = GradientStops[index];
        var end = GradientStops[index + 1];
        return new Color(
            (byte)Math.Round(start.R * (1d - fraction) + end.R * fraction),
            (byte)Math.Round(start.G * (1d - fraction) + end.G * fraction),
            (byte)Math.Round(start.B * (1d - fraction) + end.B * fraction));
    }
}

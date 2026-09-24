// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Renders the HELP ME artwork with high-contrast initials and horizontal bands from the active theme.</summary>
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

    /// <summary>Reserves the full artwork when possible and a plain-text wordmark in compact terminals.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        var width = Math.Max(0, maxWidth);
        return new Measurement(Math.Min(7, width), Math.Min(options.Capabilities.Unicode ? BannerWidth : 7, width));
    }

    /// <summary>Preserves complete letter shapes and lets Spectre adapt colors to terminal capabilities.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var initialStyle = Style.Parse("bold " + TerminalTheme.Primary);
        var bands = new[]
        {
            Style.Parse(TerminalTheme.Accent),
            Style.Parse(TerminalTheme.Info),
            Style.Parse(TerminalTheme.Success)
        };
        if (!options.Capabilities.Unicode || maxWidth < BannerWidth)
        {
            const string compact = "HELP ME";
            for (var index = 0; index < Math.Min(compact.Length, maxWidth); index++)
            {
                yield return new Segment(compact[index].ToString(), index is 0 or 5
                    ? initialStyle : bands[Math.Min(index / 3, bands.Length - 1)]);
            }
            yield break;
        }

        for (var row = 0; row < 6; row++)
        {
            if (row > 0)
            {
                yield return Segment.LineBreak;
            }
            for (var glyph = 0; glyph < Glyphs.Length; glyph++)
            {
                foreach (var character in Glyphs[glyph][row])
                {
                    yield return new Segment(character.ToString(), glyph is 0 or 5
                        ? initialStyle : bands[row / 2]);
                }
            }
        }
    }
}

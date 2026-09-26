// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Draws the shared product wordmark with active theme colors and an optional compact presentation.</summary>
internal sealed class WelcomeBanner(bool compact = false) : IRenderable
{
    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        ['P'] = ["#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    "],
        ['R'] = ["#### ", "#   #", "#   #", "#### ", "# #  ", "#  # ", "#   #"],
        ['O'] = [" ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "],
        ['M'] = ["#   #", "## ##", "# # #", "# # #", "#   #", "#   #", "#   #"],
        ['T'] = ["#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  "],
        ['E'] = ["#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####"],
        ['U'] = ["#   #", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "]
    };

    /// <summary>Adds the shared wordmark to a scrolling surface with one blank row on either side.</summary>
    internal static void Render(IAnsiConsole console)
    {
        console.WriteLine();
        console.Write(new WelcomeBanner());
        console.WriteLine();
    }

    /// <summary>Allows the containing view to center either the complete artwork or the compact product name.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        CreateContent(options, maxWidth).Measure(options, maxWidth);

    /// <summary>Rebuilds the color bands at render time so previews and theme changes remain consistent.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        CreateContent(options, maxWidth).Render(options, maxWidth);

    /// <summary>Keeps the product name readable when the full wordmark cannot fit.</summary>
    private IRenderable CreateContent(RenderOptions options, int maxWidth)
    {
        const string word = "PROMPTMEUP";
        if (compact || maxWidth < word.Length * 6 - 1)
        {
            return new Text("PromptMeUp", Style.Parse("bold " + TerminalTheme.Accent));
        }
        string[] colors = [TerminalTheme.Primary, TerminalTheme.Primary, TerminalTheme.Accent,
            TerminalTheme.Accent, TerminalTheme.Info, TerminalTheme.Info, TerminalTheme.AccentSecondary];
        var rows = new List<IRenderable>();
        for (var row = 0; row < 7; row++)
        {
            var line = string.Join(' ', word.Select(letter => Glyphs[letter][row]));
            if (options.Capabilities.Unicode)
            {
                line = line.Replace('#', '█');
            }
            rows.Add(new Text(line, Style.Parse("bold " + colors[row])));
        }
        return new Rows(rows);
    }
}

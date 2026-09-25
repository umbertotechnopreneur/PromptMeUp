// SPDX-License-Identifier: MIT

using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Draws the shared pixel-style ASCII wordmark used only by onboarding and the home menu.</summary>
internal static class WelcomeBanner
{
    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        ['P'] = ["#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    "],
        ['R'] = ["#### ", "#   #", "#   #", "#### ", "# #  ", "#  # ", "#   #"],
        ['O'] = [" ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "],
        ['M'] = ["#   #", "## ##", "# # #", "# # #", "#   #", "#   #", "#   #"],
        ['T'] = ["#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  "],
        ['E'] = ["#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####"],
        ['U'] = ["#   #", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "],
        ['H'] = ["#   #", "#   #", "#   #", "#####", "#   #", "#   #", "#   #"]
    };

    /// <summary>Uses bright cyan bands and a compact mark when the terminal cannot fit the full word.</summary>
    internal static void Render(IAnsiConsole console)
    {
        var word = console.Profile.Width >= 64 ? "PROMPTMEUP" : "HM";
        string[] colors = ["#E0FFFF", "#C5FAFF", "#A5F3FC", "#67E8F9", "#22D3EE", "#38BDF8", "#7DD3FC"];
        console.WriteLine();
        for (var row = 0; row < 7; row++)
        {
            var line = string.Join(' ', word.Select(letter => Glyphs[letter][row])).TrimEnd();
            if (console.Profile.Capabilities.Unicode)
            {
                line = line.Replace('#', '█');
            }
            console.MarkupLine($"[bold {colors[row]}]{Markup.Escape(line)}[/]");
        }
        if (word == "HM")
        {
            console.MarkupLine("[bold #A5F3FC]PromptMeUp[/]");
        }
        console.WriteLine();
    }
}

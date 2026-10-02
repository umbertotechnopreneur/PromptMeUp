// SPDX-License-Identifier: MIT

using PromptMeUp.Views;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Tests;

public sealed class TerminalLinkGridTests
{
    /// <summary>Preserves complete destinations without hyperlinks while adapting the label-above-address layout.</summary>
    [Theory]
    [InlineData(80, 1)]
    [InlineData(120, 2)]
    [InlineData(220, 4)]
    public void Links_AdaptColumnsWithoutLosingCopyableAddresses(int width, int columns)
    {
        TerminalLink[] links =
        [
            new("Privacy", "https://umbertogiacobbi.biz/privacy/"),
            new("Terms", "https://umbertogiacobbi.biz/terms/"),
            new("GitHub", "https://github.com/umbertotechnopreneur/PromptMeUp"),
            new("Made with love by Umberto", "https://umbertogiacobbi.biz")
        ];
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(output)
        });
        console.Profile.Width = width;
        console.Profile.Capabilities.Links = false;

        console.Write(new TerminalLinkGrid(links));

        var visibleOutput = System.Text.RegularExpressions.Regex.Replace(
            output.ToString(), @"\x1B(?:\[[0-?]*[ -/]*[@-~]|\].*?(?:\x1B\\|\x07))", string.Empty);
        var lines = visibleOutput.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        Assert.Equal(4 / columns * 3 - 1, lines.Length);
        Assert.All(lines, line => Assert.InRange(new Segment(line).CellCount(), 0, width));
        foreach (var link in links)
        {
            var labelRow = Array.FindIndex(lines, line => line.Contains(link.Label, StringComparison.Ordinal));
            Assert.True(labelRow >= 0);
            Assert.Contains(link.Url, lines[labelRow + 1], StringComparison.Ordinal);
        }
        Assert.DoesNotContain("…", visibleOutput, StringComparison.Ordinal);
    }
}

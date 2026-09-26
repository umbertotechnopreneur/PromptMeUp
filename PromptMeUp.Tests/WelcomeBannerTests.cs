// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class WelcomeBannerTests
{
    /// <summary>Centering preserves the artwork's fixed columns instead of shifting its shorter rows.</summary>
    [Theory]
    [InlineData(80)]
    [InlineData(120)]
    [InlineData(160)]
    public void ProductBanner_CentersTheArtworkAsOneBlock(int width)
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(output)
        });
        console.Profile.Width = width;
        console.Profile.Capabilities.Unicode = false;

        console.Write(new ProductBanner(new LocalizationService()));

        var artwork = output.ToString().ReplaceLineEndings("\n").Split('\n').Where(line => line.Contains('#')).ToArray();
        Assert.Equal(7, artwork.Length);
        Assert.Single(artwork.Select(line => line.IndexOf('#')).Distinct());
    }
}

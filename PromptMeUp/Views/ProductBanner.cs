// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Combines the shared terminal artwork and localized slogan using the active theme.</summary>
internal sealed class ProductBanner(ILocalizationService text) : IRenderable
{
    /// <summary>Measures the banner and slogan within their containing view.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        CreateContent().Measure(options, maxWidth);

    /// <summary>Renders the current language and palette without caching theme-specific colors.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        CreateContent().Render(options, maxWidth);

    /// <summary>Centers the wordmark and short slogan while allowing narrow terminals to wrap the text.</summary>
    private IRenderable CreateContent() => new Rows(
        Align.Center(new HelpMeBanner()),
        new Text(" "),
        Align.Center(new Text("PromptMeUp", Style.Parse("bold " + TerminalTheme.Primary))),
        Align.Center(new Text(text.Text("About.Slogan"), Style.Parse(TerminalTheme.Info))));
}

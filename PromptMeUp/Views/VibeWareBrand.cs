// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Places the supplied VibeWare artwork beside product content with a readable manifesto link.</summary>
/// <param name="console">The terminal whose color capabilities determine whether artwork is useful.</param>
/// <param name="content">Product information that remains readable without image support.</param>
internal sealed class VibeWareBrand(IAnsiConsole console, IRenderable content) : IRenderable
{
    internal const string ManifestoUrl = "https://umbertogiacobbi.biz/vibeware/manifesto";
    private const int ImageSize = 20;
    private static readonly Lazy<byte[]> Pixels = new(LoadPixels);

    /// <summary>Measures the adaptive content using the same layout as rendering.</summary>
    /// <param name="options">Terminal rendering capabilities.</param>
    /// <param name="maxWidth">Available terminal columns.</param>
    public Measurement Measure(RenderOptions options, int maxWidth) => CreateContent(options, maxWidth).Measure(options, maxWidth);

    /// <summary>Renders a small static logo on wide color terminals and text on other terminals.</summary>
    /// <param name="options">Terminal rendering capabilities.</param>
    /// <param name="maxWidth">Available terminal columns.</param>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => CreateContent(options, maxWidth).Render(options, maxWidth);

    /// <summary>Keeps artwork at the top right without reserving space on narrow or colorless terminals.</summary>
    /// <param name="options">Terminal rendering capabilities.</param>
    /// <param name="width">Available terminal columns.</param>
    private IRenderable CreateContent(RenderOptions options, int width)
    {
        var name = new Markup($"[bold {TerminalTheme.Info} link={ManifestoUrl}]VibeWare[/]");
        var link = new Markup($"[{TerminalTheme.Muted} link={ManifestoUrl}]{ManifestoUrl}[/]");
        var requiredWidth = Math.Max(88, content.Measure(options, width).Min + ImageSize + 2);
        if (width < requiredWidth || !console.Profile.Capabilities.Ansi || !console.Profile.Capabilities.Unicode
            || !console.Profile.Supports(ColorSystem.Legacy))
            return new Rows(content, Text.Empty, name, link);

        var artwork = new Canvas(ImageSize, ImageSize) { Scale = false };
        var pixels = Pixels.Value;
        for (var y = 0; y < ImageSize; y++)
        {
            for (var x = 0; x < ImageSize; x++)
            {
                var offset = (y * ImageSize + x) * 3;
                artwork.SetPixel(x, y, new Color(pixels[offset], pixels[offset + 1], pixels[offset + 2]));
            }
        }
        var header = new Grid().Expand()
            .AddColumn(new GridColumn { Padding = new Padding(0, 0, 2, 0) })
            .AddColumn(new GridColumn { Width = ImageSize, Padding = new Padding(0) });
        header.AddRow(content, new Rows(artwork, Align.Center(name)));
        return new Rows(header, Text.Empty, Align.Right(link));
    }

    /// <summary>Reads the small embedded RGB artwork without image libraries or external files.</summary>
    /// <exception cref="InvalidOperationException">The shipped artwork is missing or malformed.</exception>
    private static byte[] LoadPixels()
    {
        using var stream = typeof(VibeWareBrand).Assembly.GetManifestResourceStream("PromptMeUp.Assets.vibeware-symbol.rgb")
            ?? throw new InvalidOperationException("The VibeWare artwork is missing.");
        var pixels = new byte[ImageSize * ImageSize * 3];
        if (stream.Length != pixels.Length) throw new InvalidOperationException("The VibeWare artwork has an invalid size.");
        stream.ReadExactly(pixels);
        return pixels;
    }
}

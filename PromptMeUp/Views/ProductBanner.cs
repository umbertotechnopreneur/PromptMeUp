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


using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Combines the shared terminal artwork and localized slogan using the active theme.</summary>
internal sealed class ProductBanner(ILocalizationService text, bool compact = false) : IRenderable
{
    /// <summary>Measures the banner and slogan within their containing view.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        CreateContent().Measure(options, maxWidth);

    /// <summary>Renders the current language and palette without caching theme-specific colors.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        CreateContent().Render(options, maxWidth);

    /// <summary>Centers the wordmark and short slogan while allowing narrow terminals to wrap the text.</summary>
    private IRenderable CreateContent() => new Rows(
        Align.Center(new WelcomeBanner(compact)),
        new Text(" "),
        Align.Center(new Text(text.Text("About.Slogan"), Style.Parse(TerminalTheme.Info))));
}

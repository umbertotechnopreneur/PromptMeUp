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

/// <summary>Highlights localized shortcut keys and their icons without wrapping owned terminal rows.</summary>
internal sealed class TerminalShortcutHint(string hint, ConsoleRenderOptions preferences,
    IReadOnlyList<string> icons, IReadOnlyList<string> colors, int width) : IRenderable
{
    /// <summary>Reserves one terminal row for the keyboard guide.</summary>
    /// <param name="options">The terminal rendering capabilities.</param>
    /// <param name="maxWidth">The available terminal cells.</param>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        new(Math.Min(1, Math.Max(0, maxWidth)), Math.Max(0, maxWidth));

    /// <summary>Styles keys separately from action descriptions and clips complete display cells.</summary>
    /// <param name="options">The terminal rendering capabilities.</param>
    /// <param name="maxWidth">The available terminal cells.</param>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0) return [];
        var segments = new List<Segment>();
        var actions = hint.Split('·', StringSplitOptions.TrimEntries);
        for (var index = 0; index < actions.Length; index++)
        {
            if (index > 0) segments.Add(new Segment(" · ", Style.Parse(TerminalTheme.Divider)));
            var color = index < colors.Count ? colors[index] : TerminalTheme.Info;
            if (!preferences.NoEmoji && options.Unicode && index < icons.Count)
                segments.Add(new Segment(icons[index] + " ", Style.Parse(color)));
            var keyEnd = actions[index].IndexOf(' ');
            var key = keyEnd < 0 ? actions[index] : actions[index][..keyEnd];
            var description = keyEnd < 0 ? string.Empty : actions[index][keyEnd..];
            segments.Add(new Segment(key, Style.Parse("bold " + color)));
            segments.Add(new Segment(description, Style.Parse(TerminalTheme.Primary)));
        }
        return Segment.Truncate(segments, Math.Min(maxWidth, width));
    }
}

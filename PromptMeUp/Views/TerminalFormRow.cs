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

/// <summary>Aligns labels and values consistently across editable fields, metadata, and palette samples.</summary>
internal sealed class TerminalFormRow(IRenderable label, Func<int, IRenderable> value) : IRenderable
{
    /// <summary>Uses the containing region's width so every value begins in the same column.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) => new(Math.Min(1, Math.Max(0, maxWidth)), Math.Max(0, maxWidth));

    /// <summary>Right-aligns labels and keeps wrapped value lines within the value column.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var width = Math.Max(0, maxWidth);
        if (width == 0)
        {
            yield break;
        }
        var gutter = Math.Min(2, Math.Max(0, width - 2));
        var labelWidth = Math.Min(32, (width - gutter) * 2 / 5);
        var valueWidth = width - labelWidth - gutter;
        var labels = Segment.SplitLines(label.Render(options, labelWidth)).ToArray();
        var values = Segment.SplitLines(value(valueWidth).Render(options, valueWidth)).ToArray();
        for (var row = 0; row < Math.Max(labels.Length, values.Length); row++)
        {
            if (row > 0)
            {
                yield return Segment.LineBreak;
            }
            var labelLine = row < labels.Length ? Segment.Truncate(labels[row], labelWidth).ToArray() : [];
            yield return new Segment(new string(' ', Math.Max(0, labelWidth - Segment.CellCount(labelLine))));
            foreach (var segment in labelLine)
            {
                yield return segment;
            }
            yield return new Segment(new string(' ', gutter));
            if (row < values.Length)
            {
                foreach (var segment in Segment.Truncate(values[row], valueWidth))
                {
                    yield return segment;
                }
            }
        }
    }
}

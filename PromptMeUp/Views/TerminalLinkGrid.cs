// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Pairs a literal link label with its complete, copyable address.</summary>
internal sealed record TerminalLink(string Label, string Url);

/// <summary>Centers labeled links in four, two, or one columns without abbreviating their destinations.</summary>
internal sealed class TerminalLinkGrid(IReadOnlyList<TerminalLink> links) : IRenderable
{
    /// <summary>Lets the link group occupy the available width.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) => new(0, Math.Max(0, maxWidth));

    /// <summary>Chooses equal-width columns that fit complete labels and addresses, wrapping only on very narrow terminals.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0 || links.Count == 0)
        {
            yield break;
        }
        var cellWidth = links.Max(link => Math.Max(new Segment(link.Label).CellCount(), new Segment(link.Url).CellCount())) + 2;
        var columns = links.Count >= 4 && maxWidth >= cellWidth * 4 ? 4
            : links.Count >= 2 && maxWidth >= cellWidth * 2 ? 2 : 1;
        var grid = new Grid().Expand();
        for (var index = 0; index < columns; index++)
        {
            grid.AddColumn(new GridColumn { Width = Math.Max(1, maxWidth / columns - 2), Padding = new Padding(1, 0) }.Centered());
        }
        for (var offset = 0; offset < links.Count; offset += columns)
        {
            var cells = Enumerable.Range(0, columns).Select(index => offset + index < links.Count
                ? CreateLink(links[offset + index]) : (IRenderable)new Text(string.Empty)).ToArray();
            grid.AddRow(cells);
            if (offset + columns < links.Count)
            {
                grid.AddEmptyRow();
            }
        }
        foreach (var segment in ((IRenderable)grid).Render(options, maxWidth))
        {
            yield return segment;
        }
    }

    /// <summary>Renders a label above its destination and attaches the same hyperlink to both lines.</summary>
    private static IRenderable CreateLink(TerminalLink link) => new Rows(
        new Markup($"[bold {TerminalTheme.Info} link={Markup.Escape(link.Url)}]{Markup.Escape(link.Label)}[/]"),
        new Markup($"[underline {TerminalTheme.Muted} link={Markup.Escape(link.Url)}]{Markup.Escape(link.Url)}[/]"));
}

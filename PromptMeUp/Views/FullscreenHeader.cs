// SPDX-License-Identifier: MIT

using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Gives fullscreen views one open product header with optional project metadata.</summary>
internal static class FullscreenHeader
{
    internal const int Height = 3;
    private const string RepositoryUrl = "https://github.com/umbertotechnopreneur/PromptMeUp";
    private static readonly string Version = typeof(FullscreenHeader).Assembly.GetName().Version?.ToString(3)
        ?? throw new InvalidOperationException("The application version is missing.");

    /// <summary>Places a product row above a single full-width divider and a blank line.</summary>
    internal static IRenderable Create(string title, ConsoleRenderOptions options, bool showRepository = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(options);
        return new Rows(
            new Padder(new HeaderLine(title, options, showRepository), new Padding(2, 0, 2, 0)),
            new ThemeSeparator(),
            new Text(" "));
    }

    /// <summary>Keeps release metadata at the right edge while allowing the view title to shrink.</summary>
    private sealed class HeaderLine(string title, ConsoleRenderOptions renderOptions, bool showRepository) : IRenderable
    {
        /// <summary>Uses the full available line for the two ends of the header.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) =>
            new(Math.Min(1, Math.Max(0, maxWidth)), Math.Max(0, maxWidth));

        /// <summary>Emits a single row without splitting graphemes or letting a long title wrap.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var width = Math.Max(0, maxWidth);
            var project = showRepository ? "GitHub" : null;
            var metadata = project is null ? $"v{Version}" : $"v{Version}  ·  {project}";
            var metadataWidth = new Segment(metadata).CellCount();
            if (width < metadataWidth + 5)
            {
                yield return new Segment(Fit($"hm  v{Version}", width, options.Capabilities.Unicode),
                    Style.Parse(TerminalTheme.Primary));
                yield break;
            }

            var brand = width >= 70 ? "hm / PromptMeUp" : "hm";
            var icon = renderOptions.NoEmoji ? string.Empty : TerminalTheme.IconPrefix(renderOptions, "💻", "hm");
            var left = Fit($"{icon}{brand}  {title}", width - metadataWidth - 2, options.Capabilities.Unicode);
            if (left.StartsWith(icon + "hm", StringComparison.Ordinal))
            {
                yield return new Segment(icon, Style.Parse(TerminalTheme.Accent));
                yield return new Segment("hm", Style.Parse(TerminalTheme.Primary));
                yield return new Segment(left[(icon.Length + 2)..], Style.Parse(TerminalTheme.Primary));
            }
            else
            {
                yield return new Segment(left, Style.Parse(TerminalTheme.Primary));
            }
            yield return new Segment(new string(' ', width - metadataWidth - new Segment(left).CellCount()));
            yield return new Segment($"v{Version}", Style.Parse(TerminalTheme.Muted));
            if (project is not null)
            {
                yield return new Segment("  ·  ", Style.Parse(TerminalTheme.Divider));
                foreach (var segment in ((IRenderable)new Markup($"[underline {TerminalTheme.Info} link={RepositoryUrl}]{project}[/]"))
                    .Render(options, new Segment(project).CellCount()))
                {
                    yield return segment;
                }
            }
        }

        /// <summary>Clips header text by terminal-cell width and preserves complete Unicode text elements.</summary>
        private static string Fit(string value, int width, bool unicode)
        {
            if (width <= 0)
            {
                return string.Empty;
            }
            if (new Segment(value).CellCount() <= width)
            {
                return value;
            }
            var marker = unicode ? "…" : ".";
            var budget = width - new Segment(marker).CellCount();
            var elements = StringInfo.GetTextElementEnumerator(value);
            var cells = 0;
            var end = 0;
            while (elements.MoveNext())
            {
                var element = elements.GetTextElement();
                var elementWidth = new Segment(element).CellCount();
                if (cells + elementWidth > budget)
                {
                    break;
                }
                cells += elementWidth;
                end = elements.ElementIndex + element.Length;
            }
            return value[..end] + marker;
        }
    }
}

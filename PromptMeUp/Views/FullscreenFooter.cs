// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Gives fullscreen views consistent notice, action, and shortcut rows without enclosing frames.</summary>
internal static class FullscreenFooter
{
    internal const int NoticeRows = 2;
    internal const int ActionsRows = 2;
    internal const int HintRows = 2;

    /// <summary>Reserves the shared footer rows while allowing a notice to use additional space.</summary>
    internal static int Height(int noticeRows = NoticeRows)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(noticeRows, NoticeRows);
        return checked(noticeRows + ActionsRows + HintRows);
    }

    /// <summary>Aligns notices and shortcuts with the action row beneath one full-width separator.</summary>
    internal static IRenderable Create(
        IRenderable notice,
        IRenderable actions,
        IRenderable hints,
        int noticeRows = NoticeRows)
    {
        ArgumentNullException.ThrowIfNull(notice);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(hints);
        _ = Height(noticeRows);
        return new Layout("fullscreen-footer").SplitRows(
            new Layout("notice", new Rows(new Text(" "), Inset(notice))).Size(noticeRows),
            new Layout("actions", new Rows(
                new ThemeSeparator(), Inset(actions))).Size(ActionsRows),
            new Layout("hints", new Rows(new Text(" "), Inset(hints))).Size(HintRows));
    }

    /// <summary>Emphasizes shortcut actions after each colon while keeping labels and dividers subdued.</summary>
    internal static IRenderable Shortcuts(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ShortcutLine(value);
    }

    /// <summary>Chooses the localized compact shortcut row when the full text exceeds the content width.</summary>
    internal static IRenderable Shortcuts(string value, string compactValue, int availableWidth)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(compactValue);
        return new ShortcutLine(new Segment(value).CellCount() > availableWidth ? compactValue : value);
    }

    /// <summary>Matches the horizontal margins of shared fullscreen headings and form fields.</summary>
    private static Padder Inset(IRenderable content) => new(content, new Padding(2, 0, 2, 0));

    /// <summary>Keeps literal shortcut descriptions on one terminal row with distinct label and action styles.</summary>
    private sealed class ShortcutLine(string value) : IRenderable
    {
        /// <summary>Measures the complete shortcut row within the available terminal width.</summary>
        public Measurement Measure(RenderOptions options, int maxWidth) =>
            new(0, Math.Min(new Segment(value).CellCount(), Math.Max(0, maxWidth)));

        /// <summary>Preserves literal labels, punctuation, and actions while clipping complete terminal cells.</summary>
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var label = Style.Parse(TerminalTheme.Muted);
            var divider = Style.Parse(TerminalTheme.Divider);
            var action = Style.Parse($"bold {TerminalTheme.Info}");
            var segments = new List<Segment>();
            var start = 0;
            while (start < value.Length)
            {
                var separator = value.IndexOf('|', start);
                var end = separator < 0 ? value.Length : separator;
                var colon = value.IndexOf(':', start, end - start);
                if (colon < 0)
                {
                    segments.Add(new Segment(value[start..end], label));
                }
                else
                {
                    segments.Add(new Segment(value[start..colon], label));
                    segments.Add(new Segment(":", divider));
                    segments.Add(new Segment(value[(colon + 1)..end], action));
                }
                if (separator >= 0)
                {
                    segments.Add(new Segment("|", divider));
                }
                start = end + 1;
            }
            return maxWidth > 0 ? Segment.Truncate(segments, maxWidth) : [];
        }
    }
}

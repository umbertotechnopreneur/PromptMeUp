// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Shares the open header, sidebar, content, and footer geometry of terminal workspaces.</summary>
internal static class FullscreenWorkspace
{
    /// <summary>Reserves the same responsive sidebar width for help, settings, and interactive menus.</summary>
    internal static int SidebarWidth(int terminalWidth) => Math.Clamp(terminalWidth / 4, 20, 30);

    /// <summary>Builds the common fullscreen shell around passive content and action renderables.</summary>
    internal static IRenderable Create(string title, ConsoleRenderOptions options, int terminalWidth,
        IRenderable content, IRenderable? sidebar, IRenderable footer, int noticeRows, bool showRepository = false)
    {
        var root = new Layout("workspace").SplitRows(
            new Layout("header", FullscreenHeader.Create(title, options, showRepository)).Size(FullscreenHeader.Height),
            new Layout("body"),
            new Layout("footer", footer).Size(FullscreenFooter.Height(noticeRows)));
        if (sidebar is null)
        {
            root["body"].Update(content);
        }
        else
        {
            root["body"].SplitColumns(
                new Layout("sections", sidebar).Size(SidebarWidth(terminalWidth)),
                new Layout("content", content));
        }
        return new FormSurface(root);
    }
}

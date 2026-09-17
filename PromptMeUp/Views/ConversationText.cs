// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Shares a readable, indented text column between static and animated conversation output.</summary>
internal static class ConversationText
{
    private const int MaximumWidth = 108;

    /// <summary>Lets Spectre wrap styled text before writing it without clearing or redrawing terminal history.</summary>
    internal static void Write(IAnsiConsole console, IRenderable content, bool animate = false,
        int chunkSize = 1, CancellationToken cancellationToken = default)
    {
        var width = Math.Max(1, Math.Min(MaximumWidth, console.Profile.Width - 1));
        var indent = Math.Min(2, width - 1);
        var options = new RenderOptions(console.Profile.Capabilities, new Size(console.Profile.Width, console.Profile.Height));
        var segments = content.Render(options, width - indent);
        foreach (var line in Segment.SplitLines(segments))
        {
            cancellationToken.ThrowIfCancellationRequested();
            console.Write(new Text(new string(' ', indent)));
            foreach (var segment in line)
            {
                if (!animate)
                {
                    console.Write(new Text(segment.Text, segment.Style));
                    continue;
                }
                WriteAnimated(console, segment, Math.Max(1, chunkSize), cancellationToken);
            }
            console.WriteLine();
        }
    }

    /// <summary>Types already wrapped styled segments in complete Unicode graphemes using a bounded animation budget.</summary>
    private static void WriteAnimated(IAnsiConsole console, Segment segment, int chunkSize, CancellationToken cancellationToken)
    {
        var elements = StringInfo.GetTextElementEnumerator(segment.Text);
        var chunk = new StringBuilder();
        var count = 0;
        while (elements.MoveNext())
        {
            cancellationToken.ThrowIfCancellationRequested();
            chunk.Append(elements.GetTextElement());
            if (++count < chunkSize)
            {
                continue;
            }
            console.Write(new Text(chunk.ToString(), segment.Style));
            chunk.Clear();
            count = 0;
            Thread.Sleep(4);
        }
        if (chunk.Length > 0)
        {
            console.Write(new Text(chunk.ToString(), segment.Style));
        }
    }
}

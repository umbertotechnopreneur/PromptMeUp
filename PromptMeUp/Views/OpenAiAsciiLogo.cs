// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Builds a theme-aware OpenAI ASCII mark for terminal layouts.</summary>
internal static class OpenAiAsciiLogo
{
    internal const int DetailedColumnWidth = 54;
    internal const int CompactColumnWidth = 24;

    private static readonly string[] DetailedLines =
    [
        "                .d88888b.",
        "             .8P\"       \"9bd888b.",
        "            .8P       .d8P\"     \"988.",
        "          .8888      .d8P\"   ,     98.",
        "        .8P\" 88     8\"      .d98b.   88",
        "       .8P   88     8 .d8P\"    \"98b. 88",
        "      88     88     8P\"   `\"8b.     \"98.",
        "      88.    88     8        8\"8b.     88",
        "      88     \"98.8       8   88     88",
        "       `8b.      \"98.,   d8   88     88",
        "         88 \"98b.    .d8P\"  8    88   d8\"",
        "         88    \"98bP\"     .8    88 .d8\"",
        "         \"8b      `    .d8P\"   8888\"",
        "          \"88b.,    .d8P\"      d8\"",
        "             \"9888P98b.      .d8\"",
        "                    \"988888P\""
    ];

    private static readonly string[] CompactLines =
    [
        "      .-==-.      ",
        "    .' /\\ '.     ",
        "   / / /  \\ \\    ",
        "  | | | /\\ | |   ",
        "   \\ \\ \\/ / /    ",
        "    '.\\_\\/_.''    ",
        "      '-..-'      "
    ];

    /// <summary>Creates either the detailed mark or its narrow-terminal variant.</summary>
    internal static IRenderable Create(bool detailed)
    {
        var lines = detailed ? DetailedLines : CompactLines;
        var rows = new List<IRenderable>(lines.Length + 2);
        for (var index = 0; index < lines.Length; index++)
        {
            var color = index < lines.Length / 2 ? TerminalTheme.Accent : TerminalTheme.Info;
            rows.Add(new Text(lines[index], Style.Parse(color)));
        }

        var width = lines.Max(line => line.Length);
        rows.Add(new Text(string.Empty));
        rows.Add(new Text("OpenAI".PadLeft((width + "OpenAI".Length) / 2),
            Style.Parse("bold " + TerminalTheme.Primary)));
        return new Rows(rows);
    }
}

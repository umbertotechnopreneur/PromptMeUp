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

namespace PromptMeUp.Views;

/// <summary>Shares the theme and spacing of data tables while leaving cell content and alignment to each view.</summary>
internal static class TerminalTable
{
    /// <summary>Creates an open table, with optional rounded borders and row separators for denser comparisons.</summary>
    internal static Table Create(bool showBorder = false, bool separateRows = false)
    {
        return new Table
        {
            Border = showBorder ? TableBorder.Rounded : TableBorder.None,
            BorderStyle = Style.Parse(TerminalTheme.Divider),
            ShowRowSeparators = separateRows
        };
    }

    /// <summary>Creates an escaped high-contrast column heading that can be aligned or sized by its caller.</summary>
    internal static TableColumn Column(string label, int cellPadding = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cellPadding);
        return new TableColumn(new Text(label, Style.Parse($"bold {TerminalTheme.Accent}")))
        {
            Padding = new Padding(cellPadding, 0)
        };
    }

    /// <summary>Creates an open table with consistently styled localized column headings.</summary>
    internal static Table Create(params string[] headings)
    {
        var table = Create(showBorder: false);
        foreach (var heading in headings)
        {
            table.AddColumn(Column(heading));
        }
        return table;
    }
}

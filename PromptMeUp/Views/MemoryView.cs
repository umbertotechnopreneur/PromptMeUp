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


using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IMemoryView
{
    void Render(IReadOnlyList<PersistentMemory> memories);
}

public sealed class MemoryView : IMemoryView
{
    private readonly IAnsiConsole _console;
    private readonly ILocalizationService _text;

    /// <summary>Creates a passive view for explicit saved memories.</summary>
    public MemoryView(IAnsiConsole console, ILocalizationService text)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Displays saved notes with their removable identifiers while preserving scrollback.</summary>
    public void Render(IReadOnlyList<PersistentMemory> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);
        TerminalTheme.WriteRule(_console, _text.Text("Memory.Title"), TerminalTheme.Accent);
        if (memories.Count == 0)
        {
            _console.Write(new Text(_text.Text("Memory.None"), Style.Parse(TerminalTheme.Primary)));
            _console.WriteLine();
            return;
        }

        var table = TerminalTable.Create("ID", _text.Text("Memory.Note"));
        foreach (var memory in memories)
        {
            table.AddRow(
                new Text(memory.Id, Style.Parse(TerminalTheme.Info)),
                new Text(memory.Text, Style.Parse(TerminalTheme.Primary)));
        }

        _console.Write(table);
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Memory.Syntax"))}[/]");
        _console.WriteLine();
    }
}

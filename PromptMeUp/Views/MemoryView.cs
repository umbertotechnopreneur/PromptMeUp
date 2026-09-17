// SPDX-License-Identifier: MIT

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

    /// <summary>Displays saved notes with their removable identifiers and scope while preserving scrollback.</summary>
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

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(Style.Parse(TerminalTheme.Accent))
            .AddColumn(new TableColumn(new Text("ID", Style.Parse(TerminalTheme.Muted))))
            .AddColumn(new TableColumn(new Text(_text.Text("Memory.Scope"), Style.Parse(TerminalTheme.Muted))))
            .AddColumn(new TableColumn(new Text(_text.Text("Memory.Note"), Style.Parse(TerminalTheme.Muted))));
        foreach (var memory in memories)
        {
            table.AddRow(
                new Text(memory.Id, Style.Parse(TerminalTheme.Info)),
                new Text(_text.Text(memory.IsGlobal ? "Memory.Global" : "Memory.Project"), Style.Parse(TerminalTheme.Primary)),
                new Text(memory.Text, Style.Parse(TerminalTheme.Primary)));
        }

        _console.Write(table);
        _console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(_text.Text("Memory.Syntax"))}[/]");
        _console.WriteLine();
    }
}

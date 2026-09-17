// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Displays open experimental workflows using the shared palette without owning I/O services.</summary>
public sealed class ExperimentalView(IAnsiConsole console, ILocalizationService text)
{
    /// <summary>Shows right-aligned labels and left-aligned values with intentional whitespace.</summary>
    public void Render(string title, IEnumerable<(string Label, string Value)> fields)
    {
        console.WriteLine();
        TerminalTheme.WriteRule(console, title, TerminalTheme.Accent);
        var grid = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn(new GridColumn());
        foreach (var (label, value) in fields)
        {
            grid.AddRow(new Text(label, Style.Parse(TerminalTheme.Muted)), new Text(Safe(value), Style.Parse(TerminalTheme.Primary)));
            grid.AddEmptyRow();
        }
        console.Write(grid);
    }

    /// <summary>Returns an explicitly selected action after one unboxed separator.</summary>
    public int Choose(string title, params string[] choices)
    {
        TerminalTheme.WriteRule(console, title, TerminalTheme.Accent);
        return console.Prompt(new SelectionPrompt<int>().PageSize(10)
            .HighlightStyle(Style.Parse($"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}")).UseConverter(index => Markup.Escape(Safe(choices[index])))
            .AddChoices(Enumerable.Range(0, choices.Length)));
    }

    /// <summary>Reads one bounded text field without interpreting it as markup.</summary>
    public string Read(string label, string initial = "") => console.Prompt(new TextPrompt<string>(Markup.Escape(label) + ": ")
        .DefaultValue(initial).AllowEmpty()).Trim();

    /// <summary>Defaults every state-changing confirmation to cancellation.</summary>
    public bool Confirm(string message) => Choose(message, text.Text("Lab.Cancel"), text.Text("Lab.Confirm")) == 1;

    /// <summary>Removes terminal controls from untrusted package and proposal text.</summary>
    private static string Safe(string value) => string.Concat(value.Where(character => !char.IsControl(character) || character is '\n' or '\t'));
}

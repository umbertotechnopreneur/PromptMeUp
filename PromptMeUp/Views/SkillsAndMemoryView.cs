// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Displays skills and memory workflows using the shared palette without owning I/O services.</summary>
public sealed class SkillsAndMemoryView(IAnsiConsole console, ILocalizationService text, IConsoleShellView shell)
{
    private readonly FullscreenMenuView _skillsMenu = new(console, text, shell.Options);

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
        var menu = choices.Select((label, index) => new TerminalMenuChoice<int>(index, Safe(label))).ToArray();
        return TerminalChoiceMenu.Select(console, menu, pageSize: 10);
    }

    /// <summary>Returns a typed choice so workflow decisions do not rely on a selection index.</summary>
    internal T Choose<T>(string title, IReadOnlyList<(T Value, string Label)> choices)
    {
        ArgumentNullException.ThrowIfNull(choices);
        if (choices.Count == 0)
        {
            throw new ArgumentException("A menu needs at least one choice.", nameof(choices));
        }
        return choices[Choose(title, choices.Select(choice => choice.Label).ToArray())].Value;
    }

    /// <summary>Uses the shared fullscreen workspace for grouped skill commands and preserves the scrolling prompt fallback.</summary>
    internal SkillMenuSelection? ChooseSkills(string title, IReadOnlyList<SkillMenuGroup> groups,
        Func<SkillMenuSelection, IReadOnlyList<SkillMenuGroup>?>? handleInline = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count == 0)
        {
            throw new ArgumentException("The skills menu needs at least one group.", nameof(groups));
        }
        if (!FullscreenViewport.CanUse(console))
        {
            var choices = new[] { (Value: (SkillMenuItem?)null, Label: text.Text("Lab.Back")) }
                .Concat(groups.SelectMany(group => group.Items.Select(item =>
                    (Value: (SkillMenuItem?)item, Label: group.Label + " — " + item.Label))))
                .ToArray();
            var item = Choose(title, choices);
            return item is null ? null : new SkillMenuSelection(item,
                item.InputLabel is null ? null : Read(item.InputLabel, item.InitialInput));
        }

        var currentGroups = groups;
        var selected = _skillsMenu.Select(title, ToFullscreenGroups(currentGroups), text.Text("Lab.Back"), value =>
        {
            var updated = handleInline?.Invoke(new SkillMenuSelection(
                currentGroups[value.GroupIndex].Items[value.ItemIndex], value.Input));
            if (updated is not null)
            {
                currentGroups = updated;
            }
            return updated is null ? null : ToFullscreenGroups(updated);
        });
        return selected is { } value
            ? new SkillMenuSelection(currentGroups[value.GroupIndex].Items[value.ItemIndex], value.Input)
            : null;
    }

    /// <summary>Maps skill groups to the shared fullscreen menu without the redundant right-hand metadata column.</summary>
    private IReadOnlyList<FullscreenMenuGroup> ToFullscreenGroups(IReadOnlyList<SkillMenuGroup> groups) =>
        groups.Select(group => new FullscreenMenuGroup(group.Icon, group.Label, group.Description,
            group.Items.Select(item => new FullscreenMenuItem(item.Icon, item.Label, item.Description,
                PackageDetails(item.Skill), item.CanExecute, item.InputLabel, item.InitialInput,
                item.MultilineInput)).ToArray(), ShowItemDescriptions: false))
            .ToArray();

    /// <summary>Builds the complete package review shown beside each installed-skill action.</summary>
    private string? PackageDetails(SkillDefinition? skill)
    {
        if (skill is null)
        {
            return null;
        }
        var lines = new List<string>
        {
            text.Text("Lab.Source") + ": " + skill.Directory,
            text.Text("Lab.Package") + ": " + skill.Description,
            string.Empty,
            skill.Instructions
        };
        foreach (var script in skill.Scripts)
        {
            lines.Add(string.Empty);
            lines.Add(script.Key + ".ps1");
            lines.Add(script.Value);
        }
        return string.Join('\n', lines);
    }

    /// <summary>Reads one bounded text field without interpreting it as markup.</summary>
    public string Read(string label, string initial = "")
    {
        var prompt = new TextPrompt<string>(Markup.Escape(label) + ": ").AllowEmpty()
            .Validate(value => value.Length <= 4096 ? ValidationResult.Success() : ValidationResult.Error(text.Text("Lab.Invalid")));
        if (initial.Length > 0)
        {
            prompt.DefaultValue(initial);
        }
        return console.Prompt(prompt).Trim();
    }

    /// <summary>Defaults every state-changing confirmation to cancellation.</summary>
    public bool Confirm(string message) => Choose(message, text.Text("Lab.Cancel"), text.Text("Lab.Confirm")) == 1;

    /// <summary>Removes terminal controls from untrusted package and proposal text.</summary>
    private static string Safe(string value) => string.Concat(value.Where(character => !char.IsControl(character) || character is '\n' or '\t'));
}

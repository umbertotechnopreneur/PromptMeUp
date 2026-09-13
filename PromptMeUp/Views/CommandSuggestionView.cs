// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface ICommandSuggestionView
{
    CommandSuggestionDecision Select(
        IReadOnlyList<SuggestedCommand> suggestions,
        bool offerChatContinuation);
}

public sealed class CommandSuggestionView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : ICommandSuggestionView
{
    /// <summary>Shows safe next-step choices and returns a selection without authorizing or executing a command.</summary>
    public CommandSuggestionDecision Select(
        IReadOnlyList<SuggestedCommand> suggestions,
        bool offerChatContinuation)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        var hasSuggestions = suggestions.Count > 0;
        if (Console.IsInputRedirected || Console.IsOutputRedirected || (!hasSuggestions && !offerChatContinuation))
        {
            return new CommandSuggestionDecision(CommandSuggestionAction.DoNotExecute, null);
        }

        var entries = new List<MenuEntry>
        {
            new(CommandSuggestionAction.DoNotExecute, null)
        };
        if (offerChatContinuation)
        {
            entries.Add(new MenuEntry(CommandSuggestionAction.StartChat, null));
        }
        entries.AddRange(suggestions.Select(command => new MenuEntry(CommandSuggestionAction.SelectCommand, command)));

        var icon = TerminalTheme.IconPrefix(shell.Options, "🧭", ">");
        var titleKey = hasSuggestions ? "CommandMenu.Title" : "CommandMenu.ContinueTitle";
        var hintKey = hasSuggestions ? "CommandMenu.Hint" : "CommandMenu.ContinueHint";
        TerminalTheme.WriteRule(console, $"{icon}{text.Text(titleKey)}", TerminalTheme.Accent);
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text(hintKey))}[/]");
        console.WriteLine();
        // Spectre requires at least three rows even when the menu contains fewer choices.
        var selected = console.Prompt(
            new SelectionPrompt<MenuEntry>()
                .Title($"[bold {TerminalTheme.Primary}]{Markup.Escape(text.Text("CommandMenu.Choose"))}[/]")
                .PageSize(Math.Clamp(entries.Count, 3, 12))
                .HighlightStyle(Style.Parse(TerminalTheme.Accent))
                .UseConverter(entry => Label(entry, hasSuggestions))
                .AddChoices(entries));
        return new CommandSuggestionDecision(selected.Action, selected.Command);
    }

    /// <summary>Formats a menu entry with hierarchy while keeping suggested command text visibly exact.</summary>
    private string Label(MenuEntry entry, bool hasSuggestions) => entry.Action switch
    {
        CommandSuggestionAction.DoNotExecute =>
            $"[bold {TerminalTheme.Warning}]{Markup.Escape(TerminalTheme.IconPrefix(shell.Options, "🛑", "x"))}{Markup.Escape(text.Text(hasSuggestions ? "CommandMenu.None" : "CommandMenu.Finish"))}[/]",
        CommandSuggestionAction.StartChat =>
            $"[bold {TerminalTheme.Accent}]{Markup.Escape(TerminalTheme.IconPrefix(shell.Options, "💬", ">"))}{Markup.Escape(text.Text("CommandMenu.StartChat"))}[/]",
        CommandSuggestionAction.SelectCommand when entry.Command is not null =>
            $"[bold {TerminalTheme.Info}]{Markup.Escape(TerminalTheme.IconPrefix(shell.Options, "⌘", ">"))}{Markup.Escape(entry.Command.Label)}[/] " +
            $"[{TerminalTheme.Muted}]{Markup.Escape(entry.Command.Command)}[/]",
        _ => throw new InvalidOperationException("Unsupported command suggestion menu entry.")
    };

    private sealed record MenuEntry(CommandSuggestionAction Action, SuggestedCommand? Command);
}

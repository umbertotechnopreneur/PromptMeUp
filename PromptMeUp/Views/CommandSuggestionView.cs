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
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
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

        var icon = TerminalTheme.Icon(shell.Options, "🧭", ">");
        console.Write(TerminalTheme.Panel(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("CommandMenu.Hint"))}[/]"),
            $"{icon} {text.Text("CommandMenu.Title")}"));
        var selected = console.Prompt(
            new SelectionPrompt<MenuEntry>()
                .Title($"[bold {TerminalTheme.Primary}]{Markup.Escape(text.Text("CommandMenu.Choose"))}[/]")
                .PageSize(Math.Min(12, entries.Count))
                .HighlightStyle(new Style(Color.MediumPurple2))
                .UseConverter(Label)
                .AddChoices(entries));
        return new CommandSuggestionDecision(selected.Action, selected.Command);
    }

    /// <summary>Formats a menu entry with hierarchy while keeping suggested command text visibly exact.</summary>
    private string Label(MenuEntry entry) => entry.Action switch
    {
        CommandSuggestionAction.DoNotExecute =>
            $"[bold yellow]{Markup.Escape(TerminalTheme.Icon(shell.Options, "🛑", "x"))} {Markup.Escape(text.Text("CommandMenu.None"))}[/]",
        CommandSuggestionAction.StartChat =>
            $"[bold {TerminalTheme.Accent}]{Markup.Escape(TerminalTheme.Icon(shell.Options, "💬", ">"))} {Markup.Escape(text.Text("CommandMenu.StartChat"))}[/]",
        CommandSuggestionAction.SelectCommand when entry.Command is not null =>
            $"[bold {TerminalTheme.Info}]{Markup.Escape(TerminalTheme.Icon(shell.Options, "⌘", ">"))} {Markup.Escape(entry.Command.Label)}[/] " +
            $"[{TerminalTheme.Muted}]{Markup.Escape(entry.Command.Command)}[/]",
        _ => throw new InvalidOperationException("Unsupported command suggestion menu entry.")
    };

    private sealed record MenuEntry(CommandSuggestionAction Action, SuggestedCommand? Command);
}

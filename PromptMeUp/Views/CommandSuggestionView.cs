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
        var choices = new Grid()
            .AddColumn(new GridColumn().RightAligned().NoWrap())
            .AddColumn(new GridColumn().LeftAligned());
        for (var index = 0; index < entries.Count; index++)
        {
            choices.AddRow(
                $"[bold {TerminalTheme.Accent}]{index}[/]",
                Label(entries[index], hasSuggestions));
        }
        console.Write(choices);
        console.WriteLine();
        var selected = entries[ReadSelection(entries.Count)];
        return new CommandSuggestionDecision(selected.Action, selected.Command);
    }

    /// <summary>Accepts a single digit immediately, or a complete validated number followed by Enter for larger menus.</summary>
    private int ReadSelection(int entryCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entryCount);
        var maximum = entryCount - 1;
        var hintKey = entryCount <= 10 ? "CommandMenu.DigitHint" : "CommandMenu.NumberHint";
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text(hintKey, maximum))}[/]");
        var prompt = $"[bold {TerminalTheme.Accent}]› {Markup.Escape(text.Text("CommandMenu.Choose"))}[/] ";
        var error = $"[{TerminalTheme.Error}]{Markup.Escape(text.Text("CommandMenu.InvalidNumber", maximum))}[/]";
        if (entryCount > 10)
        {
            return console.Prompt(new TextPrompt<int>(prompt)
                .ValidationErrorMessage(error)
                .Validate(value => value >= 0 && value < entryCount
                    ? ValidationResult.Success()
                    : ValidationResult.Error()));
        }

        console.Markup(prompt);
        while (true)
        {
            var key = EscapeAwareConsoleInput.EnsureNotEscape(console.Input.ReadKey(intercept: true))
                ?? throw new InteractiveFlowCanceledException();
            if ((key.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) == 0
                && key.KeyChar is >= '0' and <= '9'
                && key.KeyChar - '0' < entryCount)
            {
                console.MarkupLine($"[bold {TerminalTheme.Primary}]{key.KeyChar}[/]");
                return key.KeyChar - '0';
            }

            console.WriteLine();
            console.MarkupLine(error);
            console.Markup(prompt);
        }
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

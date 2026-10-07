// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface ICommandSuggestionView
{
    CommandSuggestionDecision Select(IReadOnlyList<SuggestedCommand> suggestions, bool offerChatContinuation);
}

public sealed class CommandSuggestionView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell,
    ICommandClipboard clipboard) : ICommandSuggestionView
{
    private bool _commandHintShownOnPreviousMenu;

    /// <summary>Offers shared inline next-step choices while keeping command selection separate from authorization.</summary>
    /// <param name="suggestions">The commands available for preview and authorization.</param>
    /// <param name="offerChatContinuation">Whether to offer continuing the conversation.</param>
    /// <exception cref="ArgumentNullException">Thrown when suggestions is null.</exception>
    public CommandSuggestionDecision Select(IReadOnlyList<SuggestedCommand> suggestions, bool offerChatContinuation)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        var hasSuggestions = suggestions.Count > 0;
        if (Console.IsInputRedirected || Console.IsOutputRedirected || (!hasSuggestions && !offerChatContinuation))
            return new CommandSuggestionDecision(CommandSuggestionAction.DoNotExecute, null);

        var choices = CreateChoices(suggestions, offerChatContinuation);

        // Give the safety reminder a one-in-five chance, with a gap after each appearance.
        var showHint = !hasSuggestions || (!_commandHintShownOnPreviousMenu && Random.Shared.Next(5) == 0);
        _commandHintShownOnPreviousMenu = hasSuggestions && showHint;
        if (showHint)
        {
            var prefix = hasSuggestions ? "    " + TerminalTheme.IconPrefix(shell.Options, "ℹ️", "i") : string.Empty;
            var hint = prefix + text.Text(hasSuggestions ? "CommandMenu.Hint" : "CommandMenu.ContinueHint");
            console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(hint)}[/]");
        }
        while (true)
        {
            var selection = TerminalConversationPrompt.Select(console, text, choices,
                text.Text(hasSuggestions ? "CommandMenu.Title" : "CommandMenu.ContinueTitle"), numbered: true);
            if (selection.Action != CommandSuggestionAction.CopyAndExit) return selection;

            // A grouped copy choice has no command until the user picks one in its submenu.
            var command = selection.SuggestedCommand ?? SelectCommandToCopy(suggestions);
            if (command is null) continue;
            CommandCopyView.Copy(console, text, clipboard, command.Command, exitAfterCopy: true);
            return new CommandSuggestionDecision(CommandSuggestionAction.DoNotExecute, null);
        }
    }

    /// <summary>Keeps direct copying for one command and groups copying before the final stop choice for multiple commands.</summary>
    /// <param name="suggestions">Commands available for selection or copying.</param>
    /// <param name="offerChatContinuation">Whether to include the chat continuation choice.</param>
    private IReadOnlyList<TerminalMenuChoice<CommandSuggestionDecision>> CreateChoices(
        IReadOnlyList<SuggestedCommand> suggestions, bool offerChatContinuation)
    {
        var groupedCopy = suggestions.Count > 1;
        var stop = new TerminalMenuChoice<CommandSuggestionDecision>(new(CommandSuggestionAction.DoNotExecute, null),
            TerminalTheme.IconPrefix(shell.Options, "🛑", "x") + text.Text(suggestions.Count > 0 ? "CommandMenu.None" : "CommandMenu.Finish"),
            Tone: TerminalMenuTone.Caution);
        var choices = new List<TerminalMenuChoice<CommandSuggestionDecision>>();
        if (!groupedCopy) choices.Add(stop);
        if (offerChatContinuation)
        {
            choices.Add(new(new(CommandSuggestionAction.StartChat, null),
                TerminalTheme.IconPrefix(shell.Options, "💬", ">") + text.Text("CommandMenu.StartChat")));
        }
        foreach (var command in suggestions)
        {
            choices.Add(new(new(CommandSuggestionAction.SelectCommand, command),
                TerminalTheme.IconPrefix(shell.Options, "⌘", ">") + command.Label, command.Command));
            if (!groupedCopy)
            {
                choices.Add(new(new(CommandSuggestionAction.CopyAndExit, command),
                    TerminalTheme.IconPrefix(shell.Options, "📋", "+") + text.Text("Command.CopyAndExit"), command.Command));
            }
        }
        if (groupedCopy)
        {
            choices.Add(new(new(CommandSuggestionAction.CopyAndExit, null),
                TerminalTheme.IconPrefix(shell.Options, "📋", "+") + text.Text("Command.CopyMenu")));
            choices.Add(stop);
        }
        return choices;
    }

    /// <summary>Chooses one exact command to copy, with Back and Escape returning to the parent menu.</summary>
    /// <param name="suggestions">Commands offered in the parent menu.</param>
    private SuggestedCommand? SelectCommandToCopy(IReadOnlyList<SuggestedCommand> suggestions)
    {
        var choices = suggestions.Select(command => new TerminalMenuChoice<SuggestedCommand?>(command,
            TerminalTheme.IconPrefix(shell.Options, "📋", "+") + command.Label, command.Command)).ToList();
        choices.Add(new(null, TerminalTheme.IconPrefix(shell.Options, "↩️", "<") + text.Text("Command.CopyMenuBack")));
        try
        {
            return TerminalConversationPrompt.Select(console, text, choices,
                text.Text("Command.CopyMenuTitle"), numbered: true, echoSelection: false);
        }
        catch (InteractiveFlowCanceledException)
        {
            return null;
        }
    }
}

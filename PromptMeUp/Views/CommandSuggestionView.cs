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
    IConsoleShellView shell) : ICommandSuggestionView
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

        var choices = new List<TerminalMenuChoice<CommandSuggestionDecision>>
        {
            new(new(CommandSuggestionAction.DoNotExecute, null),
                TerminalTheme.IconPrefix(shell.Options, "🛑", "x") + text.Text(hasSuggestions ? "CommandMenu.None" : "CommandMenu.Finish"),
                Tone: TerminalMenuTone.Caution)
        };
        if (offerChatContinuation)
        {
            choices.Add(new(new(CommandSuggestionAction.StartChat, null),
                TerminalTheme.IconPrefix(shell.Options, "💬", ">") + text.Text("CommandMenu.StartChat")));
        }
        choices.AddRange(suggestions.Select(command => new TerminalMenuChoice<CommandSuggestionDecision>(
            new(CommandSuggestionAction.SelectCommand, command),
            TerminalTheme.IconPrefix(shell.Options, "⌘", ">") + command.Label, command.Command)));

        // Give the safety reminder a one-in-five chance, with a gap after each appearance.
        var showHint = !hasSuggestions || (!_commandHintShownOnPreviousMenu && Random.Shared.Next(5) == 0);
        _commandHintShownOnPreviousMenu = hasSuggestions && showHint;
        if (showHint)
        {
            var prefix = hasSuggestions ? "    " + TerminalTheme.IconPrefix(shell.Options, "ℹ️", "i") : string.Empty;
            var hint = prefix + text.Text(hasSuggestions ? "CommandMenu.Hint" : "CommandMenu.ContinueHint");
            console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(hint)}[/]");
        }
        return TerminalConversationPrompt.Select(console, text, choices,
            text.Text(hasSuggestions ? "CommandMenu.Title" : "CommandMenu.ContinueTitle"), numbered: true);
    }
}

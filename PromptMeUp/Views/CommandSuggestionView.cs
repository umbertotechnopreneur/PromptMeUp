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
    /// <summary>Offers shared inline next-step choices while keeping command selection separate from authorization.</summary>
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

        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text(hasSuggestions ? "CommandMenu.Hint" : "CommandMenu.ContinueHint"))}[/]");
        return TerminalConversationPrompt.Select(console, text, choices,
            text.Text(hasSuggestions ? "CommandMenu.Title" : "CommandMenu.ContinueTitle"), numbered: true);
    }
}

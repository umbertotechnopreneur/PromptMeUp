// SPDX-License-Identifier: MIT

using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Identifies the semantic emphasis of one terminal menu choice.</summary>
internal enum TerminalMenuTone { Primary, Positive, Muted, Caution }

/// <summary>Provides a typed value and safe, consistently styled menu text.</summary>
internal sealed record TerminalMenuChoice<T>(T Value, string Label, string? Detail = null,
    TerminalMenuTone Tone = TerminalMenuTone.Primary);

/// <summary>Shares menu appearance and keyboard behavior across scrolling setup flows.</summary>
internal static class TerminalChoiceMenu
{
    /// <summary>Shows one selected value without interpreting choice text as terminal markup.</summary>
    internal static async Task<T> SelectAsync<T>(IAnsiConsole console,
        IReadOnlyList<TerminalMenuChoice<T>> choices, CancellationToken ct, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        Validate(choices);
        var prompt = new SelectionPrompt<TerminalMenuChoice<T>>()
            .HighlightStyle(FocusStyle())
            .UseConverter(Format)
            .AddChoices(choices);
        if (!string.IsNullOrWhiteSpace(title))
        {
            prompt.Title(Title(title));
        }
        var selected = await prompt.ShowAsync(console, ct).ConfigureAwait(false);
        return selected.Value;
    }

    /// <summary>Shows optional checkboxes and returns only explicitly checked values.</summary>
    internal static async Task<IReadOnlyList<T>> SelectManyAsync<T>(IAnsiConsole console,
        IReadOnlyList<TerminalMenuChoice<T>> choices, CancellationToken ct, string title,
        string instructions, bool allowEmpty = true)
    {
        ArgumentNullException.ThrowIfNull(console);
        Validate(choices);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);
        var prompt = new MultiSelectionPrompt<TerminalMenuChoice<T>>()
            .Title(Title(title))
            .InstructionsText(Markup.Escape(instructions))
            .HighlightStyle(FocusStyle())
            .UseConverter(Format)
            .AddChoices(choices);
        if (allowEmpty)
        {
            prompt.NotRequired();
        }
        var selected = await prompt.ShowAsync(console, ct).ConfigureAwait(false);
        return selected.Select(choice => choice.Value).ToArray();
    }

    /// <summary>Checks that a menu can display at least one non-empty choice.</summary>
    private static void Validate<T>(IReadOnlyList<TerminalMenuChoice<T>> choices)
    {
        ArgumentNullException.ThrowIfNull(choices);
        if (choices.Count == 0 || choices.Any(choice => choice is null || string.IsNullOrWhiteSpace(choice.Label)))
        {
            throw new ArgumentException("A terminal menu needs non-empty choices.", nameof(choices));
        }
    }

    /// <summary>Uses the same high-contrast focus colors in single and multiple selection menus.</summary>
    private static Style FocusStyle() =>
        Style.Parse($"{TerminalTheme.SelectionForeground} on {TerminalTheme.SelectionBackground}");

    /// <summary>Formats a localized heading without allowing markup in its text.</summary>
    private static string Title(string title) =>
        $"[bold {TerminalTheme.Accent}]{Markup.Escape(title)}[/]";

    /// <summary>Styles the choice and optional detail with existing semantic theme colors.</summary>
    private static string Format<T>(TerminalMenuChoice<T> choice)
    {
        var color = choice.Tone switch
        {
            TerminalMenuTone.Positive => TerminalTheme.Success,
            TerminalMenuTone.Muted => TerminalTheme.Muted,
            TerminalMenuTone.Caution => TerminalTheme.Warning,
            _ => TerminalTheme.Primary
        };
        var label = $"[{color}]{Markup.Escape(choice.Label)}[/]";
        return string.IsNullOrWhiteSpace(choice.Detail)
            ? label
            : label + $" [{TerminalTheme.Muted}]· {Markup.Escape(choice.Detail)}[/]";
    }
}

// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Identifies the semantic emphasis of one terminal menu choice.</summary>
internal enum TerminalMenuTone { Primary, Positive, Muted, Caution }

/// <summary>Provides a typed value and safe, consistently styled menu text.</summary>
internal sealed record TerminalMenuChoice<T>(T Value, string Label, string? Detail = null,
    TerminalMenuTone Tone = TerminalMenuTone.Primary);

/// <summary>Shares menu appearance and keyboard behavior across scrolling setup flows.</summary>
internal static class TerminalChoiceMenu
{
    /// <summary>Renders numbered destinations with consistent spacing and optional descriptions.</summary>
    internal static IRenderable Numbered(IReadOnlyList<TerminalMenuChoice<int>> choices, bool showDetails)
    {
        Validate(choices);
        var grid = new Grid().AddColumn(new GridColumn().RightAligned().NoWrap()).AddColumn();
        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            var numberColor = choice.Tone == TerminalMenuTone.Caution ? TerminalTheme.Warning : TerminalTheme.Accent;
            var label = $"[bold {Color(choice.Tone)}]{Markup.Escape(choice.Label)}[/]";
            if (showDetails && !string.IsNullOrWhiteSpace(choice.Detail))
            {
                label += $"\n[{TerminalTheme.Muted}]{Markup.Escape(choice.Detail)}[/]";
            }
            grid.AddRow(new Markup($"[bold {numberColor}]{choice.Value}[/]"), new Markup(label));
            if (index < choices.Count - 1)
            {
                grid.AddEmptyRow();
            }
        }
        return grid;
    }

    /// <summary>Returns the complete choice when a flow needs to show the selected label afterward.</summary>
    internal static Task<TerminalMenuChoice<T>> SelectChoiceAsync<T>(IAnsiConsole console,
        IReadOnlyList<TerminalMenuChoice<T>> choices, CancellationToken ct, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        return CreateSingle(choices, title).ShowAsync(console, ct);
    }

    /// <summary>Shows the same single-choice menu in a synchronous console workflow.</summary>
    internal static T Select<T>(IAnsiConsole console, IReadOnlyList<TerminalMenuChoice<T>> choices,
        string? title = null, int? pageSize = null, string? moreChoicesText = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        return console.Prompt(CreateSingle(choices, title, pageSize, moreChoicesText)).Value;
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
            .UseConverter(choice => Format(choice))
            .AddChoices(choices);
        if (allowEmpty)
        {
            prompt.NotRequired();
        }
        var selected = await prompt.ShowAsync(console, ct).ConfigureAwait(false);
        return selected.Select(choice => choice.Value).ToArray();
    }

    /// <summary>Builds one single-choice prompt with shared spacing and focus styling.</summary>
    private static SelectionPrompt<TerminalMenuChoice<T>> CreateSingle<T>(
        IReadOnlyList<TerminalMenuChoice<T>> choices, string? title,
        int? pageSize = null, string? moreChoicesText = null)
    {
        Validate(choices);
        var prompt = new SelectionPrompt<TerminalMenuChoice<T>>()
            .HighlightStyle(FocusStyle())
            .UseConverter(choice => Format(choice))
            .AddChoices(choices);
        if (!string.IsNullOrWhiteSpace(title))
        {
            prompt.Title(Title(title));
        }
        if (pageSize is { } size)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(size, 3);
            prompt.PageSize(size);
        }
        if (!string.IsNullOrWhiteSpace(moreChoicesText))
        {
            prompt.MoreChoicesText(Markup.Escape(moreChoicesText));
        }
        return prompt;
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
        var color = Color(choice.Tone);
        var label = $"[{color}]{Markup.Escape(choice.Label)}[/]";
        return string.IsNullOrWhiteSpace(choice.Detail)
            ? label
            : label + $" [{TerminalTheme.Muted}]· {Markup.Escape(choice.Detail)}[/]";
    }

    /// <summary>Maps semantic menu emphasis to the existing theme palette.</summary>
    private static string Color(TerminalMenuTone tone) => tone switch
    {
        TerminalMenuTone.Positive => TerminalTheme.Success,
        TerminalMenuTone.Muted => TerminalTheme.Muted,
        TerminalMenuTone.Caution => TerminalTheme.Warning,
        _ => TerminalTheme.Primary
    };
}

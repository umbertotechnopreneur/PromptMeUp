// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IMemoryForgetView
{
    string? SelectForDeletion(IReadOnlyList<PersistentMemory> matches);
}

/// <summary>Offers numbered saved-note choices and a separate explicit deletion confirmation.</summary>
public sealed class MemoryForgetView(IAnsiConsole console, ILocalizationService text) : IMemoryForgetView
{
    private const int PageSize = 8;

    /// <summary>Returns an existing note identifier only after selection and confirmation, without changing storage.</summary>
    public string? SelectForDeletion(IReadOnlyList<PersistentMemory> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);
        if (matches.Count == 0)
        {
            return null;
        }
        var page = 0;
        var pageCount = (matches.Count + PageSize - 1) / PageSize;
        while (true)
        {
            TerminalTheme.WriteRule(console, text.Text("MemoryCli.Choose"), TerminalTheme.Warning);
            console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("MemoryCli.Page", page + 1, pageCount))}[/]");
            var visible = matches.Skip(page * PageSize).Take(PageSize).ToArray();
            var choices = CreateGrid();
            choices.AddRow(Number(0), Markup.Escape(text.Text("Form.Cancel")));
            for (var index = 0; index < visible.Length; index++)
            {
                var memory = visible[index];
                var scope = text.Text(memory.IsGlobal ? "Memory.Global" : "Memory.Project");
                choices.AddRow(Number(index + 1),
                    $"[{TerminalTheme.Primary}]{Markup.Escape(memory.Text)}[/]\n" +
                    $"[{TerminalTheme.Muted}]{Markup.Escape(scope)} · {memory.Id}[/]");
            }
            if (pageCount > 1)
            {
                choices.AddRow(Number(9), Markup.Escape(text.Text("MemoryCli.Next")));
            }
            console.Write(choices);
            var choice = ReadChoice(visible.Length, pageCount > 1);
            if (choice == 0)
            {
                return null;
            }
            if (choice == 9)
            {
                page = (page + 1) % pageCount;
                continue;
            }
            var selected = visible[choice - 1];
            TerminalTheme.WriteSection(console, text.Text("MemoryManager.DeleteConfirm"), selected.Text, TerminalTheme.Warning);
            console.MarkupLine($"[{TerminalTheme.Muted}]{selected.Id}[/]");
            var confirmation = CreateGrid();
            confirmation.AddRow(Number(0), Markup.Escape(text.Text("Form.Cancel")));
            confirmation.AddRow(Number(1), $"[{TerminalTheme.Error}]{Markup.Escape(text.Text("MemoryManager.Delete"))}[/]");
            console.Write(confirmation);
            return ReadChoice(1, false) == 1 ? selected.Id : null;
        }
    }

    /// <summary>Reads one digit without Enter, reserving nine for paging and zero for cancellation.</summary>
    private int ReadChoice(int maximum, bool canPage)
    {
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("MemoryCli.DigitHint"))}[/]");
        while (true)
        {
            var key = EscapeAwareConsoleInput.EnsureNotEscape(console.Input.ReadKey(intercept: true))
                ?? throw new InteractiveFlowCanceledException();
            if ((key.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) == 0
                && key.KeyChar is >= '0' and <= '9')
            {
                var number = key.KeyChar - '0';
                if (number <= maximum || (canPage && number == 9))
                {
                    console.MarkupLine(Number(number));
                    return number;
                }
            }
            console.MarkupLine($"[{TerminalTheme.Error}]{Markup.Escape(text.Text("MemoryCli.DigitHint"))}[/]");
        }
    }

    /// <summary>Creates an open layout with aligned number markers and wrapping note descriptions.</summary>
    private static Grid CreateGrid() => new Grid()
        .AddColumn(new GridColumn().RightAligned().NoWrap())
        .AddColumn(new GridColumn().LeftAligned());

    /// <summary>Uses the shared warm palette and normal weight for the requested number-and-chevron marker.</summary>
    private static string Number(int number) => $"[{TerminalTheme.Warning}]{number}>[/]";
}

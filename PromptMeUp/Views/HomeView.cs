// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Identifies the user's requested destination without coupling the menu to workflow execution.</summary>
public enum HomeAction { Exit, Chat, Script, Diagnose, Memories, Skills, Settings, Help }

/// <summary>Provides the numbered home menu and bounded prompts for destinations that need input.</summary>
public interface IHomeView
{
    Task<HomeAction> ChooseAsync(CancellationToken ct);
    Task<string> ReadRequestAsync(HomeAction action, int maximumCharacters, CancellationToken ct);
    void RenderNonInteractive();
}

/// <summary>Replaces the no-argument text wall with a compact keyboard menu and a shared ASCII banner.</summary>
public sealed class HomeView(IAnsiConsole console, ILocalizationService text, IConsoleShellView shell) : IHomeView
{
    private const string WebsiteUrl = "https://umbertogiacobbi.biz";
    private const string RepositoryUrl = "https://github.com/umbertotechnopreneur/PromptMeUp";

    /// <summary>Waits for a single numbered key without requiring Enter or clearing terminal history.</summary>
    public async Task<HomeAction> ChooseAsync(CancellationToken ct)
    {
        WelcomeBanner.Render(console);
        console.MarkupLine($"[{TerminalTheme.Muted}]Copyright (c) 2026 [link={WebsiteUrl}]Umberto Giacobbi[/] · [link={RepositoryUrl}]GitHub[/][/] ");
        console.WriteLine();
        console.MarkupLine($"[{TerminalTheme.Accent}]{Markup.Escape(TerminalTheme.IconPrefix(shell.Options, "⭐", "*"))}[/][{TerminalTheme.Info} link={RepositoryUrl}]{Markup.Escape(text.Text("Home.StarPrompt"))}[/]");
        TerminalTheme.WriteRule(console,
            TerminalTheme.IconPrefix(shell.Options, "✨", "*") + text.Text("Home.Question"), TerminalTheme.Accent);
        var rows = new[]
        {
            ("💬", "Chat"), ("📝", "Script"), ("🔎", "Diagnose"), ("🧠", "Memories"),
            ("🧩", "Skills"), ("⚙️", "Settings"), ("❓", "Help")
        };
        var showDescriptions = console.Profile.Height >= 32;
        var choices = rows.Select((row, index) => new TerminalMenuChoice<int>(index + 1,
            TerminalTheme.IconPrefix(shell.Options, row.Item1, ">") + text.Text("Home." + row.Item2),
            text.Text("Home." + row.Item2 + "Hint"))).ToArray();
        console.Write(TerminalChoiceMenu.Numbered(choices, showDescriptions));
        console.WriteLine();
        console.Write(TerminalChoiceMenu.Numbered(
            [new TerminalMenuChoice<int>(0, text.Text("Home.Exit"), Tone: TerminalMenuTone.Caution)],
            showDetails: false));
        console.Write(new ThemeSeparator());
        console.MarkupLine($"[{TerminalTheme.Info}]{Markup.Escape(text.Text("Home.Hint"))}[/]");
        while (true)
        {
            var key = await console.Input.ReadKeyAsync(intercept: true, ct).ConfigureAwait(false);
            if (key is null || key.Value.Key == ConsoleKey.Escape || key.Value.KeyChar == '0')
            {
                return HomeAction.Exit;
            }
            if ((key.Value.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) == 0
                && key.Value.KeyChar is >= '1' and <= '7')
            {
                console.WriteLine();
                return (HomeAction)(key.Value.KeyChar - '0');
            }
        }
    }

    /// <summary>Collects a request for a script or an error, with an empty response returning to the menu.</summary>
    public Task<string> ReadRequestAsync(HomeAction action, int maximumCharacters, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var title = action switch
        {
            HomeAction.Script => "Home.ScriptRequest",
            HomeAction.Diagnose => "Home.ErrorRequest",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Home.InputHelp"))}[/]");
        console.MarkupLine($"[bold {TerminalTheme.Accent}]{Markup.Escape(text.Text(title))}[/]");
        return Task.FromResult(new MultilineChatPrompt(console, text).Read(">", maximumCharacters));
    }

    /// <summary>Gives redirected invocations a short useful response without waiting for keyboard input.</summary>
    public void RenderNonInteractive() => console.MarkupLine(Markup.Escape(text.Text("Home.Redirected")));
}

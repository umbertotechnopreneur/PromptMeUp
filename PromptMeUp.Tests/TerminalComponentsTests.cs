// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class TerminalComponentsTests
{
    /// <summary>Protects all four context categories and complete row widths across supported languages and terminal sizes.</summary>
    [Theory]
    [InlineData(32)]
    [InlineData(40)]
    [InlineData(60)]
    [InlineData(80)]
    [InlineData(120)]
    [InlineData(180)]
    public void SessionStrip_PreservesContextCountsWithoutOverflow(int width)
    {
        var status = ShellRuntimeStatus.FromSettings(AppSettings.Default) with
        {
            SystemInstructionTokens = 101,
            UserMessageTokens = 202,
            ToolOutputTokens = 303,
            AssistantMessageTokens = 404,
            ActiveContextTokens = 1010,
            ContextBudgetTokens = 16000,
            HasContextBreakdown = true
        };
        foreach (var language in new[] { "en", "it", "fr", "de", "es", "vi" })
        {
            var text = new LocalizationService();
            text.SetLanguage(language);
            var rows = new TerminalSessionStrip(text, status).Rows(width).Select(Markup.Remove).ToArray();
            Assert.All(rows, row => Assert.InRange(TerminalText.Width(row), 0, width));
            var joined = string.Join('\n', rows);
            foreach (var count in new[] { 101, 202, 303, 404 }) Assert.Contains("~" + count, joined, StringComparison.Ordinal);
        }
    }

    /// <summary>Ensures obsolete local turns are evicted without renumbering retained entries or losing recent details.</summary>
    [Fact]
    public void Transcript_BoundsRetentionAndNavigatesRecentDetails()
    {
        var history = new TerminalTranscript();
        for (var index = 0; index < TerminalTranscript.MaximumTurns + 3; index++)
        {
            history.Add(TerminalTurnKind.Tool, "result", new Text("value"), 5, hasDetails: index == 130);
        }
        Assert.Equal(TerminalTranscript.MaximumTurns, history.Turns.Count);
        Assert.Equal(4, history.Turns[0].Number);
        Assert.True(history.SelectLatestDetails());
        Assert.Equal(127, history.SelectedIndex);
        history.Move(-1);
        Assert.Equal(126, history.SelectedIndex);
        history.Clear();
        history.Move(-1);
        Assert.Empty(history.Turns);
        Assert.Null(history.SelectedIndex);
    }

    /// <summary>Rejects oversized entries so their callers can print complete output rather than hiding unretained details.</summary>
    [Fact]
    public void Transcript_RejectsOversizedEntryWithoutDiscardingHistory()
    {
        var history = new TerminalTranscript();
        Assert.True(history.Add(TerminalTurnKind.User, "question", new Text("kept"), 4));
        Assert.False(history.Add(TerminalTurnKind.Tool, "oversized", new Text("result"), TerminalTranscript.MaximumCharacters + 1));
        Assert.Single(history.Turns);
    }

    /// <summary>Prevents context and cost from a previous workflow leaking into another mode's prompt.</summary>
    [Fact]
    public void Session_ChangesModeWithoutReusingUnrelatedMetrics()
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(new StringWriter()) });
        var session = TerminalSession.For(console);
        session.LastStatus = ShellRuntimeStatus.FromSettings(AppSettings.Default);
        session.History.Add(TerminalTurnKind.User, "question", new Text("question"), 8);
        session.SetMode(ConversationDisplayMode.Plan);
        Assert.Null(session.LastStatus);
        Assert.False(session.HasPromptDock);
        Assert.Single(session.History.Turns);
        session.SetMode(ConversationDisplayMode.Chat, hasPromptDock: true);
        Assert.True(session.HasPromptDock);
        session.Reset(new ConsoleRenderOptions(true, true));
        Assert.Empty(session.History.Turns);
    }

    /// <summary>Preserves full output in redirected terminals where no details hotkey can be used.</summary>
    [Fact]
    public void Disclosure_NoninteractiveConsole_KeepsCompleteLiteralOutput()
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(output),
            Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
        });
        var text = new LocalizationService();
        TerminalDisclosure.Write(console, text, TerminalTurnKind.Tool, "Result", new Text("first\n[red]literal[/]\nlast"), 30);
        Assert.Contains("first", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("[red]literal[/]", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("last", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Uses the same Markdown subset for original answers and read-only details, without raw emphasis markers.</summary>
    [Fact]
    public void MarkdownContent_PreservesFormattedTextAndLiteralCode()
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Ansi = AnsiSupport.No, Out = new AnsiConsoleOutput(output) });
        console.Write(PoorMarkdownRenderer.Content("## Heading\n**Important**\n```sh\necho '[red]'\n```"));
        Assert.Contains("Heading", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Important", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("echo '[red]'", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("**", output.ToString(), StringComparison.Ordinal);
    }
}

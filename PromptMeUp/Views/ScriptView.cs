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
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IScriptView
{
    void Render(ScriptPresentation presentation);
    ScriptAction Choose(ScriptPresentation presentation);
    bool ConfirmSave(string path);
}

public sealed class ScriptView(IAnsiConsole console, ILocalizationService text) : IScriptView
{
    /// <summary>Shows the generated artifact and an exact replacement diff without accessing the filesystem.</summary>
    public void Render(ScriptPresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        var language = text.Text("Script.Language." + presentation.Language.Language);
        TerminalSession.For(console).SetMode(ConversationDisplayMode.Script);
        TerminalTurnHeader.Write(console, text, TerminalTurnKind.Script, language);
        console.Write(new Text(presentation.Artifact.Explanation, Style.Parse(TerminalTheme.Primary)));
        console.WriteLine();
        console.Write(new Text(
            text.Text(presentation.OutputWasSpecified ? "Script.OutputDestination" : "Script.SuggestedDestination", presentation.OutputPath),
            Style.Parse(TerminalTheme.Info)));
        console.WriteLine();
        if (!presentation.Runtime.IsAvailable)
        {
            console.Write(new Text(text.Text("Script.RuntimeUnavailable", language), Style.Parse(TerminalTheme.Warning)));
            console.WriteLine();
        }
        TerminalTheme.WriteRule(console, text.Text("Script.Source"), TerminalTheme.Info);
        console.Write(new Text(presentation.Artifact.Source, Style.Parse(TerminalTheme.Primary)));
        console.WriteLine();
        console.WriteLine();
        TerminalSession.For(console).History.Add(TerminalTurnKind.Script, language,
            new Rows(new Text(TerminalText.Safe(presentation.Artifact.Explanation)),
                new Text(TerminalText.Safe(presentation.OutputPath), Style.Parse(TerminalTheme.Info)),
                new Text(TerminalText.Safe(presentation.Artifact.Source), Style.Parse(TerminalTheme.Primary))),
            presentation.Artifact.Explanation.Length + presentation.OutputPath.Length + presentation.Artifact.Source.Length);
        if (presentation.Original is not null && presentation.Original != presentation.Artifact.Source)
        {
            var diff = TerminalTable.Create("−", "+");
            var before = presentation.Original.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var after = presentation.Artifact.Source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            for (var index = 0; index < Math.Max(before.Length, after.Length); index++)
            {
                var oldLine = index < before.Length ? before[index] : string.Empty;
                var newLine = index < after.Length ? after[index] : string.Empty;
                if (oldLine != newLine)
                {
                    diff.AddRow(new Text($"{index + 1}: {oldLine}"), new Text($"{index + 1}: {newLine}"));
                }
            }
            console.Write(diff);
        }
    }

    /// <summary>Offers only actions supported by the selected local runtime while keeping execution and saving separate.</summary>
    public ScriptAction Choose(ScriptPresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        using var state = new TerminalStateScope(console, text, TerminalActivityState.NeedsInput);
        var actions = new List<ScriptAction> { ScriptAction.Save };
        if (presentation.Runtime.IsAvailable)
        {
            actions.Add(ScriptAction.Execute);
        }
        actions.Add(ScriptAction.DoNothing);
        if (presentation.Runtime.IsAvailable && presentation.Language.SupportsValidation)
        {
            actions.Add(ScriptAction.Validate);
        }
        actions.Add(ScriptAction.Revise);
        TerminalPromptDock.Align(console, reservedRows: actions.Count + 3);
        var choices = actions.Select(action => new TerminalMenuChoice<ScriptAction>(
            action, text.Text("Script." + action), Tone: action switch
            {
                ScriptAction.Save => TerminalMenuTone.Positive,
                ScriptAction.DoNothing => TerminalMenuTone.Muted,
                ScriptAction.Execute => TerminalMenuTone.Caution,
                _ => TerminalMenuTone.Primary
            })).ToArray();
        return TerminalConversationPrompt.Select(console, text, choices, text.Text("Script.Action"));
    }

    /// <summary>Confirms the concrete destination after the full source has been displayed.</summary>
    public bool ConfirmSave(string path)
    {
        TerminalPromptDock.Align(console, reservedRows: 2);
        return TerminalConversationPrompt.Confirm(console, text, text.Text("Script.Confirm", path));
    }
}

// SPDX-License-Identifier: MIT

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
        TerminalTheme.WriteRule(console, text.Text("Script.Help", language), TerminalTheme.Accent);
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
        if (presentation.Original is not null && presentation.Original != presentation.Artifact.Source)
        {
            var diff = new Table().Border(TableBorder.Simple).AddColumn("-").AddColumn("+");
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
        return console.Prompt(new SelectionPrompt<ScriptAction>()
            .Title(text.Text("Script.Action"))
            .UseConverter(action => text.Text("Script." + action))
            .AddChoices(actions.ToArray()));
    }

    /// <summary>Confirms the concrete destination after the full source has been displayed.</summary>
    public bool ConfirmSave(string path)
    {
        TerminalPromptDock.Align(console, reservedRows: 2);
        return console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Script.Confirm", path))) { DefaultValue = false });
    }
}

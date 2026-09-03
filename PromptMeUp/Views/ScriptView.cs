// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IScriptView
{
    void Render(ScriptArtifact artifact, string? original);
    ScriptAction Choose();
    bool ConfirmSave(string path);
}

public sealed class ScriptView(IAnsiConsole console, ILocalizationService text) : IScriptView
{
    /// <summary>Shows the generated artifact and an exact replacement diff without accessing the filesystem.</summary>
    public void Render(ScriptArtifact artifact, string? original)
    {
        TerminalTheme.WriteRule(console, text.Text("Script.Help"), TerminalTheme.Accent);
        console.Write(new Panel(new Text(artifact.Explanation)).BorderColor(Color.Cyan1));
        console.Write(new Panel(new Text(artifact.Source)).Header(text.Text("Script.Source")).BorderColor(Color.Cyan1));
        if (original is not null && original != artifact.Source)
        {
            var diff = new Table().Border(TableBorder.Rounded).AddColumn("-").AddColumn("+");
            var before = original.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var after = artifact.Source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
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

    /// <summary>Offers cancellation first and keeps saving, validation, and revision distinct.</summary>
    public ScriptAction Choose() => console.Prompt(new SelectionPrompt<ScriptAction>()
        .Title(text.Text("Script.Action"))
        .UseConverter(action => text.Text("Script." + action))
        .AddChoices(ScriptAction.Cancel, ScriptAction.Validate, ScriptAction.Save, ScriptAction.Revise));

    /// <summary>Confirms the concrete destination after the full source has been displayed.</summary>
    public bool ConfirmSave(string path) => console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Script.Confirm", path))) { DefaultValue = false });
}

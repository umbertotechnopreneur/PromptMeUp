// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IFilePreviewView
{
    void Render(FilePreview preview);
    bool ConfirmReview();
}

public sealed class FilePreviewView(IAnsiConsole console, ILocalizationService text) : IFilePreviewView
{
    /// <summary>Renders concrete file mappings, byte counts, and collision warnings from an immutable snapshot.</summary>
    public void Render(FilePreview preview)
    {
        TerminalTheme.WriteRule(console, text.Text("Preview.Help"), TerminalTheme.Accent);
        console.Write(new Panel(new Text(text.Text("Preview.Snapshot"))).BorderColor(Color.Cyan1));
        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn(text.Text("Preview.Source")).AddColumn(text.Text("Preview.Target"))
            .AddColumn(text.Text("Preview.Bytes")).AddColumn(text.Text("Plan.Status"));
        foreach (var effect in preview.Effects)
        {
            table.AddRow(new Text(effect.Source),
                new Text(effect.Destination ?? text.Text("Preview.Delete")),
                new Text(effect.Bytes.ToString("N0", text.Culture)),
                new Text(text.Text(effect.Collision ? "Preview.Collision" : "Preview.Ready"),
                    Style.Parse(effect.Collision ? "yellow" : TerminalTheme.Primary)));
        }
        console.Write(table);
        console.Write(new Text(text.Text("Preview.Total", preview.Effects.Count, preview.Effects.Sum(effect => effect.Bytes))));
        console.WriteLine();
    }

    /// <summary>Offers individual command review after the complete effect snapshot, defaulting to no action.</summary>
    public bool ConfirmReview() => console.Prompt(new ConfirmationPrompt(text.Text("Preview.Review")) { DefaultValue = false });
}

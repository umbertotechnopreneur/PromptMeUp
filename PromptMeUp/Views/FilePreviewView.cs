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
        console.Write(new Text(text.Text("Preview.Snapshot"), Style.Parse(TerminalTheme.Info)));
        console.WriteLine();
        console.WriteLine();
        var table = TerminalTable.Create(text.Text("Preview.Source"), text.Text("Preview.Target"),
            text.Text("Preview.Bytes"), text.Text("Plan.Status"));
        table.Columns[2].RightAligned();
        foreach (var effect in preview.Effects)
        {
            table.AddRow(new Text(effect.Source),
                new Text(effect.Destination ?? text.Text("Preview.Delete")),
                new Text(effect.Bytes.ToString("N0", text.Culture)),
                new Text(text.Text(effect.Collision ? "Preview.Collision" : "Preview.Ready"),
                    Style.Parse(effect.Collision ? TerminalTheme.Warning : TerminalTheme.Primary)));
        }
        console.Write(table);
        console.Write(new Text(text.Text("Preview.Total", preview.Effects.Count, preview.Effects.Sum(effect => effect.Bytes))));
        console.WriteLine();
    }

    /// <summary>Offers individual command review after the complete effect snapshot, defaulting to no action.</summary>
    public bool ConfirmReview() => console.Prompt(new ConfirmationPrompt(text.Text("Preview.Review")) { DefaultValue = false });
}

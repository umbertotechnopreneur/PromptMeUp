// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface INerdFontView
{
    bool PreviewAndConfirm(bool dryRun, bool preauthorized);

    void RenderResult(FontInstallResult result);
}

public sealed class NerdFontView(IAnsiConsole console, ILocalizationService text) : INerdFontView
{
    /// <summary>Shows the exact opt-in font command and asks for authorization unless --yes was supplied.</summary>
    public bool PreviewAndConfirm(bool dryRun, bool preauthorized)
    {
        var operation = "oh-my-posh font install JetBrainsMono --headless";
        console.Write(new Panel(new Text(dryRun ? $"DRY RUN · {operation}" : operation))
        {
            Header = new PanelHeader($" {text.Text("Font.Title")} "),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.MediumPurple2)
        });
        return preauthorized || console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Font.Confirm")))
        {
            DefaultValue = false
        });
    }

    /// <summary>Renders the font helper result and terminal-profile hint.</summary>
    public void RenderResult(FontInstallResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        console.MarkupLine($"[green]{Markup.Escape(result.DryRun ? text.Text("Font.Preview") : text.Text("Font.Ready", result.FontName))}[/]");
        console.MarkupLine($"[grey]{Markup.Escape(result.Message)}[/]");
        if (!result.DryRun)
        {
            console.MarkupLine($"[yellow]{Markup.Escape(text.Text("Font.TerminalHint"))}[/]");
        }
    }
}

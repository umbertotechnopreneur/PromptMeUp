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

public sealed class NerdFontView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : INerdFontView
{
    /// <summary>Shows the exact opt-in font command and asks for authorization unless --yes was supplied.</summary>
    public bool PreviewAndConfirm(bool dryRun, bool preauthorized)
    {
        var operation = "oh-my-posh font install JetBrainsMono --headless";
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "✎", "#") + text.Text("Font.Title"),
            TerminalTheme.Accent);
        TerminalTheme.WriteBlock(
            console,
            dryRun ? text.Text("Font.DryRun") : text.Text("Command.Preview"),
            operation,
            TerminalTheme.Accent);
        return preauthorized || console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Font.Confirm")))
        {
            DefaultValue = false
        });
    }

    /// <summary>Renders the font helper result and terminal-profile hint.</summary>
    public void RenderResult(FontInstallResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var message = result.DryRun
            ? text.Text("Font.Preview")
            : result.Changed ? text.Text("Font.Ready", result.FontName) : text.Text("Font.Unsupported");
        var color = result.DryRun ? TerminalTheme.Info : result.Changed ? TerminalTheme.Success : "yellow";
        var icon = result.DryRun ? "🧪" : result.Changed ? "✅" : "⚠";
        var fallbackIcon = result.DryRun ? "~" : result.Changed ? "+" : "!";
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, icon, fallbackIcon) + text.Text("Font.Title"),
            color);
        console.MarkupLine($"[bold {color}]{Markup.Escape(message)}[/]");
        if (result.Changed && !string.IsNullOrWhiteSpace(result.Message))
        {
            console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(result.Message)}[/]");
        }

        if (result.Changed)
        {
            console.MarkupLine($"[yellow]{Markup.Escape(text.Text("Font.TerminalHint"))}[/]");
        }
    }
}

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

public interface IThemeView
{
    string? Collect(string current);
}

/// <summary>Previews catalog palettes without owning settings or filesystem access.</summary>
public sealed class ThemeView(IAnsiConsole console, ILocalizationService text,
    IConsoleShellView shell, IThemeCatalogService themes) : IThemeView
{
    /// <summary>Collects a theme selection and restores the active palette until the application saves it.</summary>
    public string? Collect(string current)
    {
        var original = TerminalTheme.Current;
        var selected = current;
        try
        {
            if (FullscreenForm.CanUse(console))
            {
                var field = new FormField("theme", "Theme.Select", () => selected, value =>
                {
                    selected = value;
                    TerminalTheme.Apply(themes.Resolve(value));
                })
                {
                    Choices = () => themes.Themes.Select(theme => new FormChoice(theme.Id, Name(theme))).ToArray(),
                    HelpKey = "Theme.Preview"
                };
                var saved = new FullscreenForm(console, text, shell.Options).Run("Theme.Title",
                    [new FormPage("Theme.Title", [field])]);
                return saved ? selected : null;
            }

            shell.RenderNotice(text.Text("Form.Unavailable"));
            var choices = themes.Themes.OrderBy(theme => theme.Id == current ? 0 : 1)
                .Select(theme => new TerminalMenuChoice<TerminalThemeDefinition>(theme, Name(theme))).ToArray();
            var choice = TerminalChoiceMenu.Select(console, choices, text.Text("Theme.Select"));
            TerminalTheme.Apply(choice);
            TerminalTheme.WriteSection(console, text.Text("Theme.Title"), text.Text("Theme.Preview"));
            shell.RenderSuccess(Name(choice));
            return console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Setup.Confirm")))
            { DefaultValue = true }) ? choice.Id : null;
        }
        finally
        {
            TerminalTheme.Apply(original);
        }
    }

    /// <summary>Localizes bundled palette names while preserving custom catalog display names.</summary>
    private string Name(TerminalThemeDefinition theme) => theme.Id switch
    {
        "cyan" => text.Text("Theme.Cyan"),
        "green" => text.Text("Theme.Green"),
        "amber" => text.Text("Theme.Amber"),
        "ocean" => text.Text("Theme.Ocean"),
        "cobalt" => text.Text("Theme.Cobalt"),
        "violet" => text.Text("Theme.Violet"),
        "rose" => text.Text("Theme.Rose"),
        "coral" => text.Text("Theme.Coral"),
        "forest" => text.Text("Theme.Forest"),
        "mint" => text.Text("Theme.Mint"),
        "midnight" => text.Text("Theme.Midnight"),
        "coffee" => text.Text("Theme.Coffee"),
        "graphite" => text.Text("Theme.Graphite"),
        _ => theme.Name
    };
}

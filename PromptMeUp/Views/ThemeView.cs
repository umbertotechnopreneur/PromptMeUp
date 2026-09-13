// SPDX-License-Identifier: MIT

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
                var saved = new FullscreenForm(console, text).Run("Theme.Title",
                    [new FormPage("Theme.Title", [field])],
                    () => [new FormSummary(text.Text("Theme.Select"), Name(themes.Resolve(selected)))]);
                return saved ? selected : null;
            }

            shell.RenderNotice(text.Text("Form.Unavailable"));
            var choice = console.Prompt(new SelectionPrompt<TerminalThemeDefinition>()
                .Title(Markup.Escape(text.Text("Theme.Select")))
                .UseConverter(theme => Markup.Escape(Name(theme)))
                .HighlightStyle(Style.Parse(TerminalTheme.Accent))
                .AddChoices(themes.Themes.OrderBy(theme => theme.Id == current ? 0 : 1)));
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
        _ => theme.Name
    };
}

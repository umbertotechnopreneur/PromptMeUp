// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IRecipeView
{
    void RenderList(IReadOnlyList<CommandRecipe> recipes);
    void Render(CommandRecipe recipe);
    bool ConfirmSave(string destination);
    bool ConfirmPrerequisites();
    IReadOnlyDictionary<string, string> ReadParameters(CommandRecipe recipe);
}

public sealed class RecipeView(IAnsiConsole console, ILocalizationService text) : IRecipeView
{
    /// <summary>Shows local recipe names and descriptions in a searchable-by-eye table.</summary>
    public void RenderList(IReadOnlyList<CommandRecipe> recipes)
    {
        TerminalTheme.WriteRule(console, text.Text("Recipe.Help"), TerminalTheme.Accent);
        var table = new Table().Border(TableBorder.Rounded).AddColumn(text.Text("Recipe.Name")).AddColumn(text.Text("Recipe.Description"));
        foreach (var recipe in recipes)
        {
            table.AddRow(new Text(recipe.Name), new Text(recipe.Description));
        }
        console.Write(table);
        if (recipes.Count == 0)
        {
            console.Write(new Text(text.Text("Recipe.Empty")));
            console.WriteLine();
        }
    }

    /// <summary>Shows the exact reusable commands, checks, prerequisites, and original working directory.</summary>
    public void Render(CommandRecipe recipe)
    {
        TerminalTheme.WriteRule(console, recipe.Name, TerminalTheme.Accent);
        console.Write(new Panel(new Text(recipe.Description + "\n" + (recipe.Directory ?? text.Text("Recipe.CurrentDirectory")))).BorderColor(Color.Cyan1));
        foreach (var prerequisite in recipe.Prerequisites)
        {
            console.Write(new Text("• " + prerequisite));
            console.WriteLine();
        }
        foreach (var step in recipe.Steps)
        {
            console.Write(new Panel(new Text(step.Command + "\n\n" + step.Verification + "\n\n" + step.Expected))
                .Header(Markup.Escape(step.Label)).BorderColor(Color.Cyan1));
        }
    }

    /// <summary>Confirms a concrete save/import/export destination after the whole recipe was displayed.</summary>
    public bool ConfirmSave(string destination) => console.Prompt(
        new ConfirmationPrompt(Markup.Escape(text.Text("Recipe.ConfirmSave", destination))) { DefaultValue = false });

    /// <summary>Requires an explicit acknowledgement of prerequisites before creating a fresh guided run.</summary>
    public bool ConfirmPrerequisites() => console.Prompt(new ConfirmationPrompt(text.Text("Recipe.Prerequisites")) { DefaultValue = false });

    /// <summary>Collects bounded invocation parameters interactively, never through command-line values.</summary>
    public IReadOnlyDictionary<string, string> ReadParameters(CommandRecipe recipe)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in recipe.Parameters)
        {
            values.Add(parameter.Name, console.Prompt(new TextPrompt<string>(Markup.Escape(parameter.Name + ": " + parameter.Description))
                .Validate(value => value.Length is > 0 and <= 1024 ? ValidationResult.Success() : ValidationResult.Error(text.Text("Recipe.Parameters")))));
        }
        return values;
    }
}

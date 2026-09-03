// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed class RecipeWorkflow(
    RecipeStore recipes,
    PlanStore planStore,
    PlanWorkflow plans,
    IRecipeView view,
    IActivityAuditService audit,
    IConsoleShellView shell,
    ILocalizationService text)
{
    /// <summary>Manages reviewed local definitions and starts a newly authorized plan for every reuse.</summary>
    public async Task<int> RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        if (options.RecipeAction == "list")
        {
            view.RenderList(await recipes.ListAsync(cancellationToken).ConfigureAwait(false));
            return 0;
        }
        CommandRecipe recipe;
        if (options.RecipeAction == "import")
        {
            recipe = await recipes.ReadFileAsync(options.InputFile!, cancellationToken).ConfigureAwait(false);
        }
        else if (options.RecipeAction == "save")
        {
            using var lease = planStore.Acquire(options.SourcePlan!);
            var completed = await planStore.LoadAsync(options.SourcePlan!, cancellationToken).ConfigureAwait(false);
            recipe = recipes.FromCompletedPlan(options.RecipeName!, completed);
        }
        else
        {
            recipe = await recipes.LoadAsync(options.RecipeName!, cancellationToken).ConfigureAwait(false);
        }
        view.Render(recipe);
        switch (options.RecipeAction)
        {
            case "show":
                return 0;
            case "save" or "import":
                if (view.ConfirmSave(recipe.Name))
                {
                    await recipes.SaveAsync(recipe, cancellationToken).ConfigureAwait(false);
                    await audit.RecordAsync("recipe_saved", "completed", null, new { recipe.Name }, cancellationToken).ConfigureAwait(false);
                    shell.RenderSuccess(text.Text("Recipe.Saved", recipe.Name));
                }
                return 0;
            case "export":
                var destination = Path.GetFullPath(options.OutputFile!);
                if (view.ConfirmSave(destination))
                {
                    await recipes.ExportAsync(recipe, destination, cancellationToken).ConfigureAwait(false);
                    shell.RenderSuccess(text.Text("Recipe.Saved", destination));
                }
                return 0;
            case "run":
                if (!view.ConfirmPrerequisites())
                {
                    return 0;
                }
                var values = view.ReadParameters(recipe);
                var plan = recipes.Bind(recipe, values, Environment.CurrentDirectory);
                using (planStore.Acquire(plan.Id))
                {
                    await planStore.SaveAsync(plan, cancellationToken).ConfigureAwait(false);
                }
                return await plans.RunAsync(options with { Command = AppCommand.Plan, ResumeId = plan.Id }, settings, cancellationToken).ConfigureAwait(false);
            default:
                throw new InvalidOperationException(text.Text("Recipe.Usage"));
        }
    }
}

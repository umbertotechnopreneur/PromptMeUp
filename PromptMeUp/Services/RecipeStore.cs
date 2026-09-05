// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public sealed class RecipeStore(AppPaths paths, PlanStore plans, ISensitiveDataRedactor redactor, ILocalizationService text, ArtifactLimits? limits = null)
{
    private readonly ArtifactLimits _limits = limits ?? ArtifactLimits.Default;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Checks portable recipe and parameter identifiers without accepting path components.</summary>
    public static bool IsValidName(string? name) => name is { Length: > 0 and <= 40 }
        && char.IsAsciiLetter(name[0]) && name.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    /// <summary>Lists bounded validated local recipes without a provider request.</summary>
    public async Task<IReadOnlyList<CommandRecipe>> ListAsync(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(paths.DataDirectory, "recipes");
        if (!System.IO.Directory.Exists(directory))
        {
            return [];
        }
        var files = System.IO.Directory.EnumerateFiles(directory, "*.json").Take(201).ToArray();
        if (files.Length > 200)
        {
            throw new InvalidOperationException(text.Text("Recipe.Limit"));
        }
        var recipes = new List<CommandRecipe>();
        foreach (var file in files.Order(StringComparer.Ordinal))
        {
            recipes.Add(await LoadAsync(Path.GetFileNameWithoutExtension(file), cancellationToken).ConfigureAwait(false));
        }
        return recipes;
    }

    /// <summary>Loads a recipe by bounded local name and checks that its embedded identity matches.</summary>
    public async Task<CommandRecipe> LoadAsync(string name, CancellationToken cancellationToken)
    {
        var recipe = await ReadFileAsync(Resolve(name), cancellationToken).ConfigureAwait(false);
        if (!string.Equals(recipe.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(text.Text("Recipe.Invalid"));
        }
        return recipe;
    }

    /// <summary>Reads and validates a bounded JSON recipe without treating saved statuses as authority.</summary>
    public async Task<CommandRecipe> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await BoundedArtifactFile.ReadAsync(path, _limits.MaxPlanBytes, text, cancellationToken).ConfigureAwait(false);
            var recipe = JsonSerializer.Deserialize<CommandRecipe>(bytes, JsonOptions);
            Validate(recipe!);
            return recipe! with { Steps = Pending(recipe!.Steps) };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            throw new InvalidOperationException(text.Text("Recipe.ReadError"));
        }
    }

    /// <summary>Saves a new recipe atomically and never replaces an existing recipe name.</summary>
    public async Task SaveAsync(CommandRecipe recipe, CancellationToken cancellationToken)
    {
        Validate(recipe);
        var path = Resolve(recipe.Name);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (System.IO.Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.json").Take(200).Count() >= 200)
        {
            throw new InvalidOperationException(text.Text("Recipe.Limit"));
        }
        await WriteNewAsync(path, recipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Exports a reviewed definition without adding invocation-specific parameter values.</summary>
    public Task ExportAsync(CommandRecipe recipe, string path, CancellationToken cancellationToken)
    {
        Validate(recipe);
        if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(text.Text("Recipe.SaveError"));
        }
        return WriteNewAsync(Path.GetFullPath(path), recipe, cancellationToken);
    }

    /// <summary>Converts only a fully completed plan into a reusable definition with fresh pending steps.</summary>
    public CommandRecipe FromCompletedPlan(string name, ExecutionPlan plan)
    {
        plans.Validate(plan);
        if (plan.Steps.Any(step => step.Status != PlanStepStatus.Completed))
        {
            throw new InvalidOperationException(text.Text("Recipe.Incomplete"));
        }
        var recipe = new CommandRecipe(1, name, plan.Goal, plan.Directory, [], [], Pending(plan.Steps));
        Validate(recipe);
        return recipe;
    }

    /// <summary>Binds input as quoted PowerShell data rather than substituting values into command source.</summary>
    public ExecutionPlan Bind(CommandRecipe recipe, IReadOnlyDictionary<string, string> values, string currentDirectory)
    {
        Validate(recipe);
        if (values.Count != recipe.Parameters.Count || recipe.Parameters.Any(parameter => !values.ContainsKey(parameter.Name)))
        {
            throw new InvalidOperationException(text.Text("Recipe.Parameters"));
        }
        var bindings = new List<string>();
        foreach (var parameter in recipe.Parameters)
        {
            var value = values[parameter.Name];
            if (!SafeText(value, 1024))
            {
                throw new InvalidOperationException(text.Text("Recipe.Parameters"));
            }
            bindings.Add(ScriptArtifactService.Quote(parameter.Name) + " = " + ScriptArtifactService.Quote(value));
        }
        var prefix = bindings.Count == 0 ? string.Empty : "$hmParameters = @{ " + string.Join("; ", bindings) + " }; ";
        var steps = recipe.Steps.Select(step => step with
        {
            Command = prefix + step.Command,
            Verification = prefix + step.Verification,
            Status = PlanStepStatus.Pending
        }).ToList();
        var plan = new ExecutionPlan(1, Guid.NewGuid().ToString("N"), recipe.Description,
            recipe.Directory ?? Path.GetFullPath(currentDirectory), steps);
        plans.Validate(plan);
        return plan;
    }

    /// <summary>Validates schema, portable identity, parameter descriptions, prerequisites, and credential-free source.</summary>
    public void Validate(CommandRecipe recipe)
    {
        if (recipe is null || recipe.Version != 1 || !IsValidName(recipe.Name) || !SafeText(recipe.Description, _limits.MaxPlanBytes)
            || recipe.Prerequisites is null || recipe.Prerequisites.Count > 12 || recipe.Prerequisites.Any(item => !SafeText(item, 1000))
            || recipe.Parameters is null || recipe.Parameters.Count > 12
            || recipe.Parameters.Any(parameter => parameter is null || !IsValidName(parameter.Name) || !SafeText(parameter.Description, 500))
            || recipe.Parameters.Select(parameter => parameter.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != recipe.Parameters.Count
            || recipe.Steps is null)
        {
            throw new InvalidOperationException(text.Text("Recipe.Invalid"));
        }
        plans.Validate(new ExecutionPlan(1, Guid.NewGuid().ToString("N"), recipe.Description,
            recipe.Directory ?? Environment.CurrentDirectory, Pending(recipe.Steps)));
        BoundedArtifactFile.CheckSize(JsonSerializer.SerializeToUtf8Bytes(recipe, JsonOptions).Length, _limits.MaxPlanBytes, text);
    }

    /// <summary>Clones steps as data-only pending actions, discarding any saved approval or progress implication.</summary>
    private static List<PlanStep> Pending(IEnumerable<PlanStep> steps) =>
        steps.Select(step => step is null ? null! : step with { Status = PlanStepStatus.Pending }).ToList();

    /// <summary>Writes complete JSON before publishing a new file, preserving any existing destination.</summary>
    private async Task WriteNewAsync(string path, CommandRecipe recipe, CancellationToken cancellationToken)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(recipe with { Steps = Pending(recipe.Steps) }, JsonOptions), cancellationToken).ConfigureAwait(false);
            File.Move(temporary, path, overwrite: false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(text.Text("Recipe.SaveError"));
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    /// <summary>Resolves a case-normalized recipe name within local application data.</summary>
    private string Resolve(string name) => IsValidName(name)
        ? Path.Combine(paths.DataDirectory, "recipes", name.ToLowerInvariant() + ".json")
        : throw new InvalidOperationException(text.Text("Recipe.Invalid"));

    /// <summary>Rejects recognizable credentials and terminal control characters from saved definitions and bound values.</summary>
    private bool SafeText(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum
        && redactor.Redact(value) == value && !value.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t'));
}

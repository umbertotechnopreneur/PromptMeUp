// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record RecipeParameter(string Name, string Description);

public sealed record CommandRecipe(
    int Version,
    string Name,
    string Description,
    string? Directory,
    List<string> Prerequisites,
    List<RecipeParameter> Parameters,
    List<PlanStep> Steps);

// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

public static class OpenAiKeyPolicy
{
    /// <summary>Checks whether a value has the local shape expected for an OpenAI secret without authenticating it.</summary>
    public static bool IsPlausible(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret)
            || secret.Length < 20
            || !secret.StartsWith("sk-", StringComparison.Ordinal)
            || !string.Equals(secret, secret.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        return secret.All(character => !char.IsWhiteSpace(character) && !char.IsControl(character));
    }
}

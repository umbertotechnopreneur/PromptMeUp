// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

public static class OpenAiKeyPolicy
{
    // Windows Credential Manager accepts at most 2,560 bytes; the vault uses UTF-16.
    public const int MaximumLength = 1_280;

    /// <summary>Checks whether a value has the local shape expected for an OpenAI secret without authenticating it.</summary>
    public static bool IsPlausible(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret)
            || secret.Length < 20
            || secret.Length > MaximumLength
            || !secret.StartsWith("sk-", StringComparison.Ordinal)
            || !string.Equals(secret, secret.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        return secret.All(character => !char.IsWhiteSpace(character) && !char.IsControl(character));
    }
}

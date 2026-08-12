// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IEnvironmentSecretService
{
    string? Load(string variableName);

    bool IsConfigured(string variableName);

    bool LooksLikeOpenAiKey(string? secret);

    SecretStoreResult StoreForCurrentUser(string variableName, string secret);
}

public sealed class EnvironmentSecretService(ILogger<EnvironmentSecretService> logger) : IEnvironmentSecretService
{
    /// <summary>Reads a secret from process, user, then machine scope without logging its value.</summary>
    public string? Load(string variableName)
    {
        var name = ValidateVariableName(variableName);
        var processValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        if (!string.IsNullOrWhiteSpace(processValue) || !OperatingSystem.IsWindows())
        {
            return processValue;
        }

        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
               ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
    }

    /// <summary>Reports whether a variable contains a plausible OpenAI secret.</summary>
    public bool IsConfigured(string variableName) => LooksLikeOpenAiKey(Load(variableName));

    /// <summary>Performs a local shape check; this does not authenticate with OpenAI.</summary>
    public bool LooksLikeOpenAiKey(string? secret)
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

    /// <summary>Stores a validated secret in current process and Windows user scope only.</summary>
    public SecretStoreResult StoreForCurrentUser(string variableName, string secret)
    {
        var name = ValidateVariableName(variableName);
        if (!LooksLikeOpenAiKey(secret))
        {
            throw new ArgumentException("The OpenAI key has an invalid shape.", nameof(secret));
        }

        Environment.SetEnvironmentVariable(name, secret, EnvironmentVariableTarget.Process);
        if (!OperatingSystem.IsWindows())
        {
            logger.LogInformation("Environment secret loaded for this process. Variable={Variable}, Scope=Process", name);
            return new SecretStoreResult(
                false,
                $"Export {name} in your shell or secret manager before future hm sessions.");
        }

        // User scope persists on Windows; process scope above makes the new key available immediately.
        Environment.SetEnvironmentVariable(name, secret, EnvironmentVariableTarget.User);
        logger.LogInformation("Environment secret stored. Variable={Variable}, Scopes=User+Process", name);
        return new SecretStoreResult(true, $"{name} is available to this process and future Windows user sessions.");
    }

    /// <summary>Validates the allowlisted OpenAI environment variable name.</summary>
    private static string ValidateVariableName(string variableName)
    {
        var name = variableName?.Trim() ?? string.Empty;
        if (name is not ("OPENAI_API_KEY" or "OPENAI_ADMIN_KEY"))
        {
            throw new ArgumentException("Only OPENAI_API_KEY and OPENAI_ADMIN_KEY are supported.", nameof(variableName));
        }

        return name;
    }
}

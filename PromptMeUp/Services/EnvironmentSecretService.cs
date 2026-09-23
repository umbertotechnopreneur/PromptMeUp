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

    /// <summary>Temporarily supplies a candidate key to the current async flow without persisting it.</summary>
    IDisposable UseTemporary(string variableName, string secret);
}

public sealed class EnvironmentSecretService(ILogger<EnvironmentSecretService> logger) : IEnvironmentSecretService
{
    private readonly AsyncLocal<(string Name, string Secret)?> _temporary = new();

    /// <summary>Reads the validation candidate, Windows vault, or an explicitly supplied environment secret.</summary>
    public string? Load(string variableName)
    {
        var name = ValidateVariableName(variableName);
        if (_temporary.Value is { } temporary && temporary.Name == name)
        {
            return temporary.Secret;
        }
        if (OperatingSystem.IsWindows() && WindowsCredentialStore.Read(name) is { } stored)
        {
            return stored;
        }
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
    public bool LooksLikeOpenAiKey(string? secret) => OpenAiKeyPolicy.IsPlausible(secret);

    /// <summary>Stores a secret in the Windows user vault or explicitly limits non-Windows storage to this process.</summary>
    public SecretStoreResult StoreForCurrentUser(string variableName, string secret)
    {
        var name = ValidateVariableName(variableName);
        if (!LooksLikeOpenAiKey(secret))
        {
            throw new ArgumentException("The OpenAI key has an invalid shape.", nameof(secret));
        }

        if (!OperatingSystem.IsWindows())
        {
            Environment.SetEnvironmentVariable(name, secret, EnvironmentVariableTarget.Process);
            logger.LogInformation("Environment secret loaded for this process. Variable={Variable}, Scope=Process", name);
            return new SecretStoreResult(
                $"Export {name} in your shell or secret manager before future hm sessions.");
        }

        WindowsCredentialStore.Write(name, secret);
        logger.LogInformation("Credential stored in Windows Credential Manager. Variable={Variable}", name);
        return new SecretStoreResult($"{name} is stored in Windows Credential Manager for your Windows account.");
    }

    /// <summary>Scopes a candidate secret to validation and restores the prior value when validation finishes.</summary>
    public IDisposable UseTemporary(string variableName, string secret)
    {
        var name = ValidateVariableName(variableName);
        if (!LooksLikeOpenAiKey(secret))
        {
            throw new ArgumentException("The OpenAI key has an invalid shape.", nameof(secret));
        }
        var previous = _temporary.Value;
        _temporary.Value = (name, secret);
        return new TemporarySecret(_temporary, previous);
    }

    /// <summary>Restores the preceding async-local credential without writing environment variables.</summary>
    private sealed class TemporarySecret(AsyncLocal<(string Name, string Secret)?> slot,
        (string Name, string Secret)? previous) : IDisposable
    {
        private bool _disposed;

        /// <summary>Releases the candidate secret once, including failed or cancelled validation.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                slot.Value = previous;
                _disposed = true;
            }
        }
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

// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Reflection;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Reads the immutable build identity embedded in the application assembly.</summary>
public static class BuildInformationReader
{
    /// <summary>Loads compiler-generated metadata without consulting the runtime machine or clock.</summary>
    public static BuildInformation Read()
    {
        var assembly = typeof(BuildInformationReader).Assembly;
        var version = assembly.GetName().Version?.ToString(3)
            ?? throw new InvalidOperationException("The application version is missing.");
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();
        var machineName = GetRequiredValue(metadata, "BuildMachine");
        var timestamp = GetRequiredValue(metadata, "BuildDateUtc");
        if (!DateTimeOffset.TryParseExact(timestamp, "O", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var builtAtUtc) || builtAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("The application build timestamp must be an ISO 8601 UTC value.");
        }
        return new BuildInformation(version, machineName, builtAtUtc);
    }

    /// <summary>Rejects missing, empty, or duplicate metadata rather than substituting runtime information.</summary>
    private static string GetRequiredValue(AssemblyMetadataAttribute[] metadata, string key)
    {
        var matches = metadata.Where(attribute => attribute.Key == key).ToArray();
        if (matches.Length != 1 || string.IsNullOrWhiteSpace(matches[0].Value))
        {
            throw new InvalidOperationException($"The application build metadata '{key}' is missing or invalid.");
        }
        return matches[0].Value!;
    }
}

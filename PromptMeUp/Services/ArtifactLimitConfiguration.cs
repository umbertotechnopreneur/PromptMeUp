// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public static class ArtifactLimitConfiguration
{
    /// <summary>Loads non-secret MiB settings without changing persistent application preferences.</summary>
    public static ArtifactLimits Load(Func<string, string?> readVariable, ILocalizationService text) => new(
        Read("PROMPTMEUP_MAX_SCRIPT_MIB", 1, readVariable, text),
        Read("PROMPTMEUP_MAX_PLAN_MIB", 8, readVariable, text),
        ReadOutputTokens(readVariable, text));

    /// <summary>Loads a separate artifact generation budget without changing ordinary chat verbosity.</summary>
    private static int ReadOutputTokens(Func<string, string?> readVariable, ILocalizationService text)
    {
        const string name = "PROMPTMEUP_MAX_ARTIFACT_OUTPUT_TOKENS";
        var value = readVariable(name);
        if (value is null)
        {
            return ArtifactLimits.Default.MaxOutputTokens;
        }
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var tokens) || tokens is < 1 or > 65_536)
        {
            throw new InvalidOperationException(text.Text("Artifact.OutputConfiguration", name));
        }
        return tokens;
    }

    /// <summary>Rejects malformed configuration explicitly instead of silently replacing it with defaults.</summary>
    private static int Read(string name, int fallback, Func<string, string?> readVariable, ILocalizationService text)
    {
        var value = readVariable(name);
        if (value is null)
        {
            return fallback * ArtifactLimits.Mebibyte;
        }
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var mebibytes) || mebibytes is < 1 or > 64)
        {
            throw new InvalidOperationException(text.Text("Artifact.Configuration", name));
        }
        return mebibytes * ArtifactLimits.Mebibyte;
    }
}

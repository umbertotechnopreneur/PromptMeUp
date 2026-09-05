// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed class ArtifactLimits
{
    public const int Mebibyte = 1024 * 1024;
    public static ArtifactLimits Default { get; } = new();

    /// <summary>Defines byte limits independently of chat message and model context budgets.</summary>
    public ArtifactLimits(int maxScriptBytes = Mebibyte, int maxPlanBytes = 8 * Mebibyte, int maxOutputTokens = 16_384)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxScriptBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxScriptBytes, 64 * Mebibyte);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPlanBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxPlanBytes, 64 * Mebibyte);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOutputTokens);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxOutputTokens, 65_536);
        MaxScriptBytes = maxScriptBytes;
        MaxPlanBytes = maxPlanBytes;
        MaxOutputTokens = maxOutputTokens;
    }

    public int MaxScriptBytes { get; }
    public int MaxPlanBytes { get; }
    public int MaxOutputTokens { get; }

    /// <summary>Reserves JSON escaping and envelope space for a complete script and revision request.</summary>
    public int ScriptRequestBytes(int requestCharacters) => checked((MaxScriptBytes + requestCharacters) * 6 + 65_536);

    /// <summary>Allows a complete artifact inside the provider's additional JSON string envelope.</summary>
    public long ResponseBytes(string promptId) => promptId switch
    {
        "script-system" => (long)MaxScriptBytes * 12 + Mebibyte,
        "plan-system" => (long)MaxPlanBytes * 2 + Mebibyte,
        _ => 2 * Mebibyte
    };
}

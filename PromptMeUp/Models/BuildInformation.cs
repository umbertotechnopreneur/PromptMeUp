// SPDX-License-Identifier: MIT

using System.Globalization;

namespace PromptMeUp.Models;

/// <summary>Describes the immutable application version, compiler identity, and source commit.</summary>
public sealed record BuildInformation(string Version, string MachineName, DateTimeOffset BuiltAtUtc, string GitCommit)
{
    /// <summary>Formats the compilation instant as an ISO 8601 timestamp with its UTC offset.</summary>
    public string BuildTimestamp => BuiltAtUtc.ToString("O", CultureInfo.InvariantCulture);
}

// SPDX-License-Identifier: MIT

using System.Globalization;

namespace PromptMeUp.Models;

/// <summary>Describes the immutable application version, compiler identity, and source commit.</summary>
public sealed record BuildInformation(string Version, string MachineName, DateTimeOffset BuiltAtLocal, string GitCommit)
{
    /// <summary>Formats the compilation instant as an ISO 8601 timestamp with its local UTC offset.</summary>
    public string BuildTimestamp => BuiltAtLocal.ToString("O", CultureInfo.InvariantCulture);
}

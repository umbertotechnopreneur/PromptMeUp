// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Describes the application version and provenance captured during compilation.</summary>
public sealed record BuildInformation(string Version, string MachineName, DateTimeOffset BuiltAtLocal, string GitCommit);

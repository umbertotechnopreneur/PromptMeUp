// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Describes the application version and the machine and UTC instant captured during compilation.</summary>
public sealed record BuildInformation(string Version, string MachineName, DateTimeOffset BuiltAtUtc);

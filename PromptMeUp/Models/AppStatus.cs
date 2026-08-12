// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record AppStatus(
    AppSettings Settings,
    bool HasApiKey,
    bool HasAdminKey,
    DateTimeOffset? LastPricingSync,
    string DatabasePath,
    string LogsDirectory,
    string PromptDirectory,
    int PromptCount);

public sealed record FontInstallResult(bool Changed, bool DryRun, string FontName, string Message);

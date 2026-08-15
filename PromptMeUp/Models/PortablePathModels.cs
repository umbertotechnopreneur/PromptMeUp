// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public enum PortablePathAction
{
    Install,
    Remove,
    Status
}

public sealed record PortablePathPlan(
    PortablePathAction Action,
    string ExecutableDirectory,
    string PersistenceTarget,
    string Preview,
    bool IsPresent,
    bool RequiresChange);

public sealed record PortablePathResult(
    PortablePathAction Action,
    string ExecutableDirectory,
    string PersistenceTarget,
    bool Changed,
    bool IsPresent);

public sealed record SecretStoreResult(string Guidance);

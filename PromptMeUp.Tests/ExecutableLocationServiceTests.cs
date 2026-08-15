// SPDX-License-Identifier: MIT

using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ExecutableLocationServiceTests
{
    /// <summary>Verifies that location inspection is absolute, internally consistent, and non-mutating.</summary>
    [Fact]
    public void Resolve_ReturnsExecutableDirectoryAndChangeDirectoryCommand()
    {
        var result = new ExecutableLocationService().Resolve();

        Assert.True(Path.IsPathFullyQualified(result.ExecutablePath));
        Assert.Equal(Path.GetDirectoryName(result.ExecutablePath), result.DirectoryPath);
        Assert.Contains(result.DirectoryPath, result.ChangeDirectoryCommand, StringComparison.Ordinal);
        Assert.Contains(
            OperatingSystem.IsLinux() ? result.DirectoryPath : result.ExecutablePath,
            result.OpenFolderPreview,
            StringComparison.Ordinal);
    }
}

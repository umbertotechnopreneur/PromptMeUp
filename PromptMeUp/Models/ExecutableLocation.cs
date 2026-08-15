// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public enum ExecutableLocationAction
{
    DoNothing,
    ShowChangeDirectoryCommand,
    OpenContainingFolder
}

public sealed record ExecutableLocationInfo(
    string ExecutablePath,
    string DirectoryPath,
    string OpenFolderPreview,
    string ChangeDirectoryCommand);

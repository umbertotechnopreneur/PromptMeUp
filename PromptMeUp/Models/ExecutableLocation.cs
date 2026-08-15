// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public enum ExecutableLocationAction
{
    ShowChangeDirectoryCommand,
    OpenContainingFolder
}

public sealed record ExecutableLocationInfo(
    string ExecutablePath,
    string DirectoryPath,
    string OpenFolderPreview,
    string ChangeDirectoryCommand);

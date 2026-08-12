// SPDX-License-Identifier: MIT

namespace PromptMeUp.Infrastructure;

public sealed record AppPaths(
    string DataDirectory,
    string DatabasePath,
    string LogsDirectory,
    string LogFilePattern,
    string PromptDirectory)
{
    /// <summary>Resolves and creates the portable local data, log, database, and packaged-prompt paths.</summary>
    public static AppPaths Create()
    {
        var configuredDataDirectory = Environment.GetEnvironmentVariable("PROMPTMEUP_DATA_DIR");
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
            ? Path.Combine(localApplicationData, "PromptMeUp")
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredDataDirectory.Trim()));

        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new InvalidOperationException("A writable PromptMeUp data directory could not be resolved.");
        }

        var logsDirectory = Path.Combine(dataDirectory, "logs");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(logsDirectory);

        return new AppPaths(
            dataDirectory,
            Path.Combine(dataDirectory, "promptmeup.db"),
            logsDirectory,
            Path.Combine(logsDirectory, "promptmeup-.log"),
            Path.Combine(AppContext.BaseDirectory, "prompt"));
    }
}

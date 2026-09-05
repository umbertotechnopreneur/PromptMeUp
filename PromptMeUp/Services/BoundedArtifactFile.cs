// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static class BoundedArtifactFile
{
    /// <summary>Reads at most the configured byte budget even if a file grows after opening.</summary>
    internal static async Task<byte[]> ReadAsync(string path, int maximumBytes, ILocalizationService text, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous);
        CheckSize(stream.Length, maximumBytes, text);
        using var result = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            CheckSize(result.Length + read, maximumBytes, text);
            result.Write(buffer, 0, read);
        }
        return result.ToArray();
    }

    /// <summary>Uses one explicit byte contract for both reading and publishing artifacts.</summary>
    internal static void CheckSize(long bytes, int maximumBytes, ILocalizationService text)
    {
        if (bytes > maximumBytes)
        {
            throw new InvalidOperationException(text.Text("Artifact.TooLarge", maximumBytes));
        }
    }
}

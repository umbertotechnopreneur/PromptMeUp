// SPDX-License-Identifier: MIT

using System.Text;

namespace PromptMeUp.Services;

internal static class AtomicFileWriter
{
    /// <summary>Publishes a complete text file using the caller's overwrite policy and removes any unfinished temporary file.</summary>
    internal static async Task WriteAllTextAsync(
        string path,
        string contents,
        bool overwrite,
        CancellationToken cancellationToken,
        Encoding? encoding = null)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var write = encoding is null
                ? File.WriteAllTextAsync(temporary, contents, cancellationToken)
                : File.WriteAllTextAsync(temporary, contents, encoding, cancellationToken);
            await write.ConfigureAwait(false);
            File.Move(temporary, path, overwrite);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}

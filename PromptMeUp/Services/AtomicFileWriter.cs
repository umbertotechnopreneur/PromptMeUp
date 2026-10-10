// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


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

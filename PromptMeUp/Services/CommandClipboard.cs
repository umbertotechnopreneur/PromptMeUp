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


using System.Diagnostics;
using System.Text;

namespace PromptMeUp.Services;

/// <summary>Copies an exact previewed shell command without invoking a shell.</summary>
public interface ICommandClipboard
{
    /// <summary>Returns whether the host clipboard accepted the command.</summary>
    bool TryCopy(string command);
}

/// <summary>Writes commands to the Windows clipboard through the system clipboard utility.</summary>
public sealed class CommandClipboard : ICommandClipboard
{
    /// <summary>Copies the exact command through standard input, never through command-line interpolation.</summary>
    public bool TryCopy(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        if (!OperatingSystem.IsWindows()) return false;
        var utility = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "clip.exe");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(utility)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                StandardInputEncoding = new UnicodeEncoding(false, true)
            });
            if (process is null) return false;
            process.StandardInput.Write(command);
            process.StandardInput.Close();
            if (process.WaitForExit(3000)) return process.ExitCode == 0;
            process.Kill(entireProcessTree: true);
            return false;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            return false;
        }
    }
}

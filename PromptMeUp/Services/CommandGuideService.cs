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

namespace PromptMeUp.Services;

/// <summary>Opens only the bundled command reference in the operating system's PDF viewer.</summary>
public sealed class CommandGuideService
{
    public string DocumentPath => Path.Combine(AppContext.BaseDirectory, "docs", "promptmeup-quick-reference.pdf");

    /// <summary>Launches the installed document without interpreting a shell command or downloading content.</summary>
    public void Open()
    {
        if (!File.Exists(DocumentPath))
        {
            throw new FileNotFoundException("The bundled command guide is missing.", DocumentPath);
        }

        var start = new ProcessStartInfo();
        if (OperatingSystem.IsWindows())
        {
            start.FileName = DocumentPath;
            start.UseShellExecute = true;
            start.Verb = "open";
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            start.FileName = OperatingSystem.IsMacOS() ? "open" : "xdg-open";
            start.UseShellExecute = false;
            start.ArgumentList.Add(DocumentPath);
        }
        else
        {
            throw new PlatformNotSupportedException("No PDF launcher is configured for this platform.");
        }

        // Windows may reuse an existing PDF viewer and return no new process handle.
        using var process = Process.Start(start);
        if (!start.UseShellExecute && process is null)
        {
            throw new InvalidOperationException("The PDF viewer could not be started.");
        }
    }
}

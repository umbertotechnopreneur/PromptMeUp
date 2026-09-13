// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Selects a fullscreen workspace or the explicit sequential compatibility path.</summary>
public sealed class AdaptiveSetupView(IAnsiConsole console, ILocalizationService text,
    IConsoleShellView shell, SetupView sequential, FullscreenSetupView fullscreen) : ISetupView
{
    /// <summary>Collects full setup using the presentation supported by the current terminal.</summary>
    public SetupSubmission? Collect(SetupViewState state)
    {
        if (FullscreenForm.CanUse(console))
        {
            return fullscreen.Collect(state);
        }
        shell.RenderNotice(text.Text("Form.Unavailable"));
        return sequential.Collect(state);
    }

    /// <summary>Collects focused AI preferences without changing the command's persisted scope.</summary>
    public AppSettings? CollectAiSettings(AppSettings current)
    {
        if (FullscreenForm.CanUse(console))
        {
            return fullscreen.CollectAiSettings(current);
        }
        shell.RenderNotice(text.Text("Form.Unavailable"));
        return sequential.CollectAiSettings(current);
    }
}

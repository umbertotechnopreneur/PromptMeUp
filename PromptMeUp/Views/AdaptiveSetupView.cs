// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Opens the shared settings draft with fullscreen or section-menu presentation.</summary>
public sealed class AdaptiveSetupView(IAnsiConsole console, ILocalizationService text,
    IConsoleShellView shell, FullscreenSetupView fullscreen) : ISetupView
{
    /// <summary>Collects full setup using the presentation supported by the current terminal.</summary>
    public SetupSubmission? Collect(SetupViewState state)
    {
        if (!FullscreenForm.CanUse(console))
        {
            shell.RenderNotice(text.Text("Form.Unavailable"));
        }
        return fullscreen.Collect(state);
    }
}

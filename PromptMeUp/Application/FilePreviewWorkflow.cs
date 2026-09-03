// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed class FilePreviewWorkflow(
    FilePreviewService service,
    IFilePreviewView view,
    IAuthorizedCommandWorkflow commands,
    IActivityAuditService audit,
    IConsoleShellView shell,
    ILocalizationService text)
{
    /// <summary>Previews local effects and optionally offers each concrete operation through the existing authorization gate.</summary>
    public async Task<int> RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        var preview = service.Build(options);
        view.Render(preview);
        if (preview.Effects.Any(effect => effect.Collision))
        {
            shell.RenderWarning(text.Text("Preview.Collision"));
            return 1;
        }
        if (Console.IsInputRedirected || Console.IsOutputRedirected || !view.ConfirmReview())
        {
            return 0;
        }
        var session = Guid.NewGuid().ToString("N");
        await audit.StartSessionAsync(session, "file-preview", settings, new { preview.Operation, count = preview.Effects.Count }, cancellationToken).ConfigureAwait(false);
        var status = "paused";
        try
        {
            foreach (var effect in preview.Effects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var command = service.BuildCommand(preview, effect);
                var result = await commands.RunForResultAsync(session, command, settings, cancellationToken).ConfigureAwait(false);
                if (result is null)
                {
                    return 0;
                }
                if (result.TimedOut || result.ExitCode != 0)
                {
                    return 1;
                }
            }
            status = "completed";
            return 0;
        }
        finally
        {
            await audit.CloseSessionAsync(session, status, CancellationToken.None).ConfigureAwait(false);
        }
    }
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed class ArtifactAssistant(
    IOpenAiService openAi,
    IActivityAuditService audit,
    BoundedTextInput input,
    IConsoleShellView shell,
    ILocalizationService text,
    ArtifactLimits? limits = null)
{
    /// <summary>Creates one bounded artifact request with audited lifecycle and visible provider usage.</summary>
    public async Task<AiResponse> SendAsync(string promptId, string request, AppSettings settings, CancellationToken cancellationToken)
    {
        request = promptId == "script-system"
            ? input.SanitizeUtf8(request, (limits ?? ArtifactLimits.Default).ScriptRequestBytes(settings.MaxMessageCharacters))
            : input.Sanitize(request, settings.MaxMessageCharacters);
        var sessionId = Guid.NewGuid().ToString("N");
        var status = "failed";
        await audit.StartSessionAsync(sessionId, promptId, settings, new { invocation = promptId }, cancellationToken).ConfigureAwait(false);
        try
        {
            var response = await shell.RunWithStatusAsync(text.Text("Status.Thinking"),
                () => openAi.SendAsync(promptId, sessionId, [new ChatMessage("user", request)], settings, settings.Language, cancellationToken)).ConfigureAwait(false);
            shell.RenderRuntimeStatus(AiConversationWorkflow.CreateTurnSnapshot(response, settings, response.EstimatedCostUsd ?? 0));
            status = "completed";
            return response;
        }
        finally
        {
            await audit.CloseSessionAsync(sessionId, status, CancellationToken.None).ConfigureAwait(false);
        }
    }
}

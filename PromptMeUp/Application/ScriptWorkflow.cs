// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed class ScriptWorkflow(
    ArtifactAssistant assistant,
    ScriptArtifactService artifacts,
    BoundedTextInput input,
    IAuthorizedCommandWorkflow commands,
    IActivityAuditService audit,
    IScriptView view,
    IConsoleShellView shell,
    ILocalizationService text)
{
    /// <summary>Generates or revises a script and offers explicit save and non-executing validation actions.</summary>
    public async Task RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        var request = input.Sanitize(options.Query!, settings.MaxMessageCharacters, fromArgument: true);
        var original = options.InputFile is null ? null : await artifacts.ReadAsync(options.InputFile, cancellationToken).ConfigureAwait(false);
        while (true)
        {
            shell.RenderNotice(text.Text("Input.Sharing"));
            var response = await assistant.SendAsync("script-system", JsonSerializer.Serialize(new { request, original }), settings, cancellationToken).ConfigureAwait(false);
            var artifact = artifacts.Parse(response.Text);
            view.Render(artifact, original);
            while (true)
            {
                switch (view.Choose())
                {
                    case ScriptAction.Cancel:
                        return;
                    case ScriptAction.Save:
                        var path = Path.GetFullPath(options.OutputFile ?? shell.ReadText(text.Text("Script.Destination")));
                        if (view.ConfirmSave(path))
                        {
                            await artifacts.SaveAsync(path, artifact.Source, cancellationToken).ConfigureAwait(false);
                            shell.RenderSuccess(text.Text("Script.Saved", path));
                            return;
                        }
                        break;
                    case ScriptAction.Validate:
                        await ValidateAsync(artifact.Source, settings, cancellationToken).ConfigureAwait(false);
                        shell.RenderNotice(text.Text("Script.ValidationNote"));
                        break;
                    case ScriptAction.Revise:
                        original = artifact.Source;
                        request = input.Sanitize(shell.ReadText(text.Text("Script.Revision")), settings.MaxMessageCharacters);
                        goto NextRevision;
                    default:
                        throw new InvalidOperationException(text.Text("Cli.Invalid"));
                }
            }
        NextRevision:;
        }
    }

    /// <summary>Audits validation according to its authorized execution result while preserving failures during cleanup.</summary>
    private async Task ValidateAsync(string source, AppSettings settings, CancellationToken cancellationToken)
    {
        await using var session = await AuditSessionScope.StartAsync(
            audit, "script-validation", settings, new { }, AuditSessionOutcome.Failed, cancellationToken).ConfigureAwait(false);
        var result = await commands.RunForResultAsync(
            session.Id, ScriptArtifactService.BuildValidationCommand(source), settings, cancellationToken).ConfigureAwait(false);
        session.Outcome = result switch
        {
            null => AuditSessionOutcome.Cancelled,
            { TimedOut: false, ExitCode: 0 } => AuditSessionOutcome.Completed,
            _ => AuditSessionOutcome.Failed
        };
    }
}

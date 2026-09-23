// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Creates reviewed language-specific scripts and routes optional validation or execution through normal command authorization.</summary>
public sealed class ScriptWorkflow(
    ArtifactAssistant assistant,
    ScriptArtifactService artifacts,
    IScriptLanguageCatalog languages,
    BoundedTextInput input,
    IAuthorizedCommandWorkflow commands,
    IActivityAuditService audit,
    IScriptView view,
    IConsoleShellView shell,
    ILocalizationService text)
{
    /// <summary>Generates or revises a selected-language script and offers explicit save, temporary execution, validation, or no-op actions.</summary>
    public async Task RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        var language = settings.ScriptLanguage;
        var definition = languages.Get(language);
        var runtime = languages.GetAvailability(language);
        var executionMode = settings.DirectModeEnabled ? CommandExecutionMode.Direct : CommandExecutionMode.Confirm;
        var request = input.Sanitize(options.Query!, settings.MaxMessageCharacters, fromArgument: true);
        var original = options.InputFile is null ? null : await artifacts.ReadAsync(options.InputFile, cancellationToken).ConfigureAwait(false);
        while (true)
        {
            shell.RenderNotice(text.Text("Input.Sharing"));
            var response = await assistant.SendAsync(
                "script-system",
                JsonSerializer.Serialize(new
                {
                    request,
                    original,
                    scriptLanguage = new
                    {
                        id = definition.StorageValue,
                        name = definition.DisplayName,
                        extension = definition.FileExtension,
                        guidance = definition.PromptGuidance
                    }
                }),
                settings,
                cancellationToken).ConfigureAwait(false);
            var artifact = artifacts.Parse(response.Text);
            var outputPath = ResolveOutputPath(options, request, language);
            var presentation = new ScriptPresentation(
                artifact,
                original,
                definition,
                runtime,
                outputPath,
                options.OutputFile is not null);
            view.Render(presentation);
            var summaryRendered = assistant.RenderSummaryAfterVisibleTurn(response, settings);

            var revise = false;
            while (!revise)
            {
                switch (view.Choose(presentation))
                {
                    case ScriptAction.DoNothing:
                        RenderFinalSummary(response, settings, summaryRendered);
                        return;
                    case ScriptAction.Save:
                        if (view.ConfirmSave(outputPath))
                        {
                            await artifacts.SaveAsync(outputPath, artifact.Source, language, cancellationToken).ConfigureAwait(false);
                            shell.RenderSuccess(text.Text("Script.Saved", outputPath));
                            RenderFinalSummary(response, settings, summaryRendered);
                            return;
                        }
                        break;
                    case ScriptAction.Execute:
                        if (!runtime.IsAvailable)
                        {
                            shell.RenderWarning(text.Text("Script.RuntimeUnavailable", text.Text("Script.Language." + language)));
                            break;
                        }
                        var execution = await ExecuteAsync(artifact.Source, definition, settings, executionMode, cancellationToken).ConfigureAwait(false);
                        if (execution is not null)
                        {
                            RenderFinalSummary(response, settings, summaryRendered);
                            return;
                        }
                        break;
                    case ScriptAction.Validate:
                        if (!runtime.IsAvailable || !definition.SupportsValidation)
                        {
                            shell.RenderWarning(text.Text("Script.RuntimeUnavailable", text.Text("Script.Language." + language)));
                            break;
                        }
                        await ValidateAsync(artifact.Source, language, settings, executionMode, cancellationToken).ConfigureAwait(false);
                        shell.RenderNotice(text.Text("Script.ValidationNote"));
                        break;
                    case ScriptAction.Revise:
                        original = artifact.Source;
                        request = input.Sanitize(shell.ReadText(text.Text("Script.Revision")), settings.MaxMessageCharacters);
                        revise = true;
                        break;
                    default:
                        throw new InvalidOperationException(text.Text("Cli.Invalid"));
                }
            }
        }
    }

    /// <summary>Prefers the exact --output destination and otherwise returns a collision-free current-directory suggestion.</summary>
    private string ResolveOutputPath(CommandLineOptions options, string request, ScriptLanguage language) => options.OutputFile is { Length: > 0 }
        ? Path.GetFullPath(options.OutputFile)
        : artifacts.SuggestOutputPath(request, language);

    /// <summary>Delegates final output accounting to the common session-summary policy without duplicating an in-progress summary.</summary>
    private void RenderFinalSummary(AiResponse response, AppSettings settings, bool summaryRendered) =>
        assistant.RenderSummaryAtEnd(response, settings, summaryRendered);

    /// <summary>Audits syntax validation according to its authorized execution result while preserving failures during cleanup.</summary>
    private async Task ValidateAsync(
        string source,
        ScriptLanguage language,
        AppSettings settings,
        CommandExecutionMode executionMode,
        CancellationToken cancellationToken)
    {
        await using var session = await AuditSessionScope.StartAsync(
            audit, "script-validation", settings, new { language = ScriptLanguageCatalog.ToStorageValue(language) }, AuditSessionOutcome.Failed, cancellationToken).ConfigureAwait(false);
        var result = await commands.RunForResultAsync(
            session.Id, languages.BuildValidationCommand(language, source), settings, cancellationToken, executionMode).ConfigureAwait(false);
        session.Outcome = result switch
        {
            null => AuditSessionOutcome.Cancelled,
            { TimedOut: false, ExitCode: 0 } => AuditSessionOutcome.Completed,
            _ => AuditSessionOutcome.Failed
        };
    }

    /// <summary>Runs a reviewed temporary script through the ordinary risk, preview, confirmation, and output flow.</summary>
    private async Task<CommandExecutionResult?> ExecuteAsync(
        string source,
        ScriptLanguageDefinition definition,
        AppSettings settings,
        CommandExecutionMode executionMode,
        CancellationToken cancellationToken)
    {
        await using var session = await AuditSessionScope.StartAsync(
            audit,
            "script-execution",
            settings,
            new { language = definition.StorageValue, temporary = true },
            AuditSessionOutcome.Failed,
            cancellationToken).ConfigureAwait(false);
        var result = await commands.RunForResultAsync(
            session.Id,
            languages.BuildExecutionCommand(definition.Language, source),
            settings,
            cancellationToken,
            executionMode).ConfigureAwait(false);
        session.Outcome = result switch
        {
            null => AuditSessionOutcome.Cancelled,
            { TimedOut: false, ExitCode: 0 } => AuditSessionOutcome.Completed,
            _ => AuditSessionOutcome.Failed
        };
        return result;
    }
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public interface IAuthorizedCommandWorkflow
{
    Task<string?> RunAsync(
        string sessionId,
        string command,
        AppSettings settings,
        CancellationToken cancellationToken);
}

public sealed class AuthorizedCommandWorkflow : IAuthorizedCommandWorkflow
{
    private readonly ICommandRiskAssessmentService _riskAssessment;
    private readonly ICommandExecutionService _commandExecution;
    private readonly IActivityAuditService _audit;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly ICommandAuthorizationView _commandView;
    private readonly IConsoleShellView _shell;
    private readonly ILocalizationService _text;

    /// <summary>Creates the command workflow that preserves assessment, authorization, execution, and redaction boundaries.</summary>
    public AuthorizedCommandWorkflow(
        ICommandRiskAssessmentService riskAssessment,
        ICommandExecutionService commandExecution,
        IActivityAuditService audit,
        ISensitiveDataRedactor redactor,
        ICommandAuthorizationView commandView,
        IConsoleShellView shell,
        ILocalizationService text)
    {
        _riskAssessment = riskAssessment ?? throw new ArgumentNullException(nameof(riskAssessment));
        _commandExecution = commandExecution ?? throw new ArgumentNullException(nameof(commandExecution));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _commandView = commandView ?? throw new ArgumentNullException(nameof(commandView));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Assesses, previews, authorizes, executes, audits, and prepares bounded command output for the next AI turn.</summary>
    public async Task<string?> RunAsync(
        string sessionId,
        string command,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(settings);
        var assessment = await _riskAssessment.AssessAsync(
            command,
            settings.ReviewCommandsWithAi,
            settings,
            _text.Language,
            cancellationToken).ConfigureAwait(false);
        await _audit.AppendSessionEventAsync(
            sessionId,
            "command_preview",
            new { command, assessment },
            cancellationToken).ConfigureAwait(false);
        ApprovedCommand? approved;
        try
        {
            approved = _commandView.PreviewAndAuthorize(command, assessment);
        }
        catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _shell.RenderWarning(_text.Text("Command.Cancelled"));
            approved = null;
        }

        if (approved is null)
        {
            await _audit.RecordAsync(
                "command_authorization",
                "denied",
                sessionId,
                new { command, assessment.Score },
                cancellationToken).ConfigureAwait(false);
            return null;
        }

        await _audit.RecordAsync(
            "command_authorization",
            "approved",
            sessionId,
            new { command, assessment.Score },
            cancellationToken).ConfigureAwait(false);
        var result = await _shell.RunWithStatusAsync(
            _text.Text("Command.Running"),
            () => _commandExecution.ExecuteAsync(
                approved,
                TimeSpan.FromSeconds(settings.CommandTimeoutSeconds),
                cancellationToken)).ConfigureAwait(false);
        _commandView.RenderExecutionResult(result);
        var boundedOutput = Limit(_redactor.Redact(result.StandardOutput), settings.MaxCommandOutputCharacters);
        var boundedError = Limit(_redactor.Redact(result.StandardError), settings.MaxCommandOutputCharacters);
        var redactedCommand = _redactor.Redact(command);
        await _audit.AppendSessionEventAsync(
            sessionId,
            "command_output",
            new
            {
                result.Command,
                result.ExitCode,
                standardOutput = boundedOutput,
                standardError = boundedError,
                result.TimedOut,
                result.OutputTruncated,
                result.ElapsedMilliseconds
            },
            cancellationToken).ConfigureAwait(false);
        var followUp = $"""
            I explicitly authorized and ran this PowerShell command:
            {redactedCommand}

            Exit code: {result.ExitCode?.ToString() ?? "timeout"}
            Standard output:
            {boundedOutput}

            Standard error:
            {boundedError}

            Analyze this result and explain the next useful step. Do not imply that any additional command has run.
            """;
        return Limit(followUp, settings.MaxMessageCharacters);
    }

    /// <summary>Limits output retained and transmitted after an authorized command.</summary>
    private static string Limit(string value, int maximumCharacters)
    {
        if (value.Length <= maximumCharacters)
        {
            return value;
        }

        const string suffix = "\n[truncated by PromptMeUp]";
        return maximumCharacters <= suffix.Length
            ? value[..maximumCharacters]
            : value[..(maximumCharacters - suffix.Length)] + suffix;
    }
}

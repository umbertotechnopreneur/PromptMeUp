// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public interface IAuthorizedCommandWorkflow
{
    Task<CommandExecutionResult?> RunForResultAsync(
        string sessionId,
        string command,
        AppSettings settings,
        CancellationToken cancellationToken,
        CommandExecutionMode executionMode = CommandExecutionMode.Confirm);

    Task<string?> RunAsync(
        string sessionId,
        string command,
        AppSettings settings,
        CancellationToken cancellationToken,
        CommandExecutionMode executionMode = CommandExecutionMode.Confirm);
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
        CancellationToken cancellationToken,
        CommandExecutionMode executionMode = CommandExecutionMode.Confirm)
    {
        var result = await RunForResultAsync(sessionId, command, settings, cancellationToken, executionMode).ConfigureAwait(false);
        return result is null ? null : CreateFollowUp(result, settings);
    }

    /// <summary>Shares assessment, preview, audit, execution and output handling across both authorization modes.</summary>
    public async Task<CommandExecutionResult?> RunForResultAsync(
        string sessionId,
        string command,
        AppSettings settings,
        CancellationToken cancellationToken,
        CommandExecutionMode executionMode = CommandExecutionMode.Confirm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(settings);
        if (!Enum.IsDefined(executionMode))
        {
            throw new ArgumentOutOfRangeException(nameof(executionMode));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var assessment = await _riskAssessment.AssessAsync(
            command,
            executionMode == CommandExecutionMode.Direct || settings.ReviewCommandsWithAi,
            settings,
            _text.Language,
            cancellationToken).ConfigureAwait(false);
        await _audit.AppendSessionEventAsync(
            sessionId,
            "command_preview",
            new { command, assessment, executionMode },
            cancellationToken).ConfigureAwait(false);
        _commandView.RenderPreview(command, assessment);
        if (executionMode == CommandExecutionMode.Direct && !assessment.CanRunDirect)
        {
            _shell.RenderWarning(_text.Text(assessment.UsedAi ? "Direct.Blocked" : "Direct.ReviewRequired"));
            await _audit.RecordAsync("command_authorization", "blocked", sessionId,
                new { command, assessment.Score, assessment.Level, executionMode }, cancellationToken).ConfigureAwait(false);
            return null;
        }

        bool authorized;
        try
        {
            authorized = await _commandView.AuthorizeAsync(executionMode, cancellationToken).ConfigureAwait(false);
        }
        catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _shell.RenderWarning(_text.Text("Command.Cancelled"));
            authorized = false;
        }

        if (!authorized)
        {
            await _audit.RecordAsync(
                "command_authorization",
                "denied",
                sessionId,
                new { command, assessment.Score, executionMode },
                cancellationToken).ConfigureAwait(false);
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _audit.RecordAsync(
            "command_authorization",
            "approved",
            sessionId,
            new { command, assessment.Score, executionMode },
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var approved = ApprovedCommand.Create(command, assessment);
        var result = await _shell.RunWithStatusAsync(
            _text.Text("Command.Running"),
            () => _commandExecution.ExecuteAsync(
                approved,
                TimeSpan.FromSeconds(settings.CommandTimeoutSeconds),
                cancellationToken)).ConfigureAwait(false);
        _commandView.RenderExecutionResult(result);
        var boundedOutput = Limit(_redactor.Redact(result.StandardOutput), settings.MaxCommandOutputCharacters);
        var boundedError = Limit(_redactor.Redact(result.StandardError), settings.MaxCommandOutputCharacters);
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
        return result;
    }

    /// <summary>Builds bounded, redacted evidence for a later AI turn without conveying authorization.</summary>
    private string CreateFollowUp(CommandExecutionResult result, AppSettings settings)
    {
        var redactedCommand = _redactor.Redact(result.Command);
        var boundedOutput = Limit(_redactor.Redact(result.StandardOutput), settings.MaxCommandOutputCharacters);
        var boundedError = Limit(_redactor.Redact(result.StandardError), settings.MaxCommandOutputCharacters);
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

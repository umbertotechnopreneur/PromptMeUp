// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed partial class AiConversationWorkflow
{
    /// <summary>Recognizes a run directive without accepting similarly prefixed ordinary prompts.</summary>
    internal static bool TryParseRunCommand(string input, out string command)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Equals("/run", StringComparison.OrdinalIgnoreCase))
        {
            command = string.Empty;
            return true;
        }

        if (input.StartsWith("/run ", StringComparison.OrdinalIgnoreCase))
        {
            command = input[4..].Trim();
            return true;
        }

        command = string.Empty;
        return false;
    }

    /// <summary>Keeps skill and proposal commands local to the app, outside provider context.</summary>
    /// <param name="input">Direct user input to check for a local command.</param>
    /// <param name="settings">Current application preferences.</param>
    /// <param name="ct">Cancels the selected workflow.</param>
    /// <exception cref="OperationCanceledException">Application shutdown cancelled the workflow.</exception>
    private async Task<bool> HandleSkillsAndMemoryCommandAsync(string input, AppSettings settings, CancellationToken ct)
    {
        var parts = input.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts.FirstOrDefault()?.ToLowerInvariant() switch
        {
            "/skills" => AppCommand.Skills,
            "/learning" => AppCommand.Learning,
            "/proposals" => AppCommand.Proposals,
            "/dream" => AppCommand.Dream,
            "/heartbeat" => AppCommand.Heartbeat,
            _ => (AppCommand?)null
        };
        if (command is null)
        {
            return false;
        }
        var hasArguments = parts.Length != 1;
        var handlerAvailable = command == AppCommand.Proposals
            ? _memoryManagerWorkflow is not null
            : _skillsAndMemoryWorkflow is not null;
        if (hasArguments || !handlerAvailable)
        {
            _shell.RenderError(_text.Text("Lab.Invalid"));
            return true;
        }

        try
        {
            if (command == AppCommand.Proposals)
            {
                await _memoryManagerWorkflow!.RunAsync(ct, selectProposals: true).ConfigureAwait(false);
            }
            else
            {
                await _skillsAndMemoryWorkflow!.RunAsync(command.Value, settings, ct).ConfigureAwait(false);
            }
        }
        catch (InteractiveFlowCanceledException) when (!ct.IsCancellationRequested)
        {
            _shell.RenderNotice(_text.Text("Common.Cancelled"));
        }
        catch (InvalidOperationException exception)
        {
            _shell.RenderError(exception.Message);
        }
        return true;
    }

    /// <summary>Handles note commands locally so private administration never becomes a model prompt.</summary>
    private async Task<bool> HandleMemoryCommandAsync(string input, CancellationToken cancellationToken)
    {
        var parts = input.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }
        var command = parts[0].ToLowerInvariant();
        if (command is not ("/remember" or "/memories" or "/forget"))
        {
            return false;
        }
        try
        {
            switch (command)
            {
                case "/memories" when parts.Length == 1:
                    _memoryView.Render(await _persistentMemory.ListAsync(cancellationToken).ConfigureAwait(false));
                    break;
                case "/remember" when parts.Length == 2:
                    var note = parts[1].Trim();
                    var scope = note.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (string.Equals(scope.FirstOrDefault(), "global", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(scope.FirstOrDefault(), "project", StringComparison.OrdinalIgnoreCase))
                    {
                        note = scope.Length == 2 ? scope[1].Trim() : string.Empty;
                    }
                    var saved = await _persistentMemory.RememberAsync(note, true, cancellationToken).ConfigureAwait(false);
                    _shell.RenderSuccess(_text.Text("Memory.Saved", saved.Id));
                    break;
                case "/forget" when parts.Length == 2:
                    if (_skillsAndMemoryWorkflow is not null && !_skillsAndMemoryWorkflow.ConfirmMemoryForget())
                    {
                        _shell.RenderNotice(_text.Text("Common.Cancelled"));
                        break;
                    }
                    var removed = await _persistentMemory.ForgetAsync(parts[1].Trim(), cancellationToken).ConfigureAwait(false);
                    if (removed)
                    {
                        _shell.RenderSuccess(_text.Text("Memory.Forgotten"));
                    }
                    else
                    {
                        _shell.RenderWarning(_text.Text("Memory.NotFound"));
                    }
                    break;
                default:
                    _shell.RenderError(_text.Text("Memory.Syntax"));
                    break;
            }
        }
        catch (MemoryValidationException exception)
        {
            _shell.RenderError(exception.Message);
        }
        return true;
    }
}

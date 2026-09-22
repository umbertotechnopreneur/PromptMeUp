// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Coordinates local saved-memory browsing, editing, and confirmed deletion without provider calls.</summary>
public sealed class MemoryManagerWorkflow
{
    private readonly PersistentMemoryService _memories;
    private readonly IMemoryManagerView _view;
    private readonly ILocalizationService _text;
    private readonly SkillsAndMemoryWorkflow? _skillsAndMemory;

    /// <summary>Connects the passive memory manager to local persistence and localized operation feedback.</summary>
    public MemoryManagerWorkflow(
        PersistentMemoryService memories,
        IMemoryManagerView view,
        ILocalizationService text,
        SkillsAndMemoryWorkflow? skillsAndMemory = null)
    {
        _memories = memories ?? throw new ArgumentNullException(nameof(memories));
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _skillsAndMemory = skillsAndMemory;
    }

    /// <summary>Refreshes accessible notes after each operation and keeps recoverable input errors inside the manager.</summary>
    public async Task RunAsync(CancellationToken cancellationToken, bool selectProposals = false)
    {
        string? feedback = null;
        var feedbackIsError = false;
        var openProposals = selectProposals;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var memories = await _memories.ListAsync(cancellationToken).ConfigureAwait(false);
            var proposals = MemoryProposalWorkspace.Disabled;
            if (_skillsAndMemory is not null)
            {
                try
                {
                    proposals = await _skillsAndMemory.LoadProposalWorkspaceAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (IsProposalDataError(exception))
                {
                    feedback = _text.Text("Lab.Invalid");
                    feedbackIsError = true;
                }
            }
            MemoryManagerSelection selection;
            try
            {
                selection = _view.Choose(memories, proposals, feedback, feedbackIsError, openProposals);
                openProposals = false;
                feedback = null;
                feedbackIsError = false;
            }
            catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return;
            }
            if (selection.Action == MemoryManagerAction.Close)
            {
                return;
            }
            try
            {
                switch (selection.Action)
                {
                    case MemoryManagerAction.Create:
                        feedback = await SaveAsync(null, selection.Text, cancellationToken).ConfigureAwait(false);
                        break;
                    case MemoryManagerAction.Edit:
                        feedback = await SaveAsync(FindSelected(memories, selection.Id), selection.Text, cancellationToken).ConfigureAwait(false);
                        break;
                    case MemoryManagerAction.Delete:
                        var memory = FindSelected(memories, selection.Id);
                        if (await _memories.ForgetAsync(memory.Id, cancellationToken).ConfigureAwait(false))
                        {
                            feedback = _text.Text("Memory.Forgotten");
                        }
                        else
                        {
                            feedback = _text.Text("Memory.NotFound");
                            feedbackIsError = true;
                        }
                        break;
                    case MemoryManagerAction.ApproveProposal:
                    case MemoryManagerAction.RejectProposal:
                        if (_skillsAndMemory is null)
                        {
                            feedback = _text.Text("Lab.Disabled");
                            feedbackIsError = true;
                        }
                        else
                        {
                            await _skillsAndMemory.ApplyProposalReviewAsync(selection.Id
                                ?? throw new InvalidOperationException(_text.Text("Lab.Invalid")), selection.Text,
                                selection.Action == MemoryManagerAction.ApproveProposal, cancellationToken).ConfigureAwait(false);
                            feedback = _text.Text("Lab.Saved");
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(selection), "Unsupported memory action.");
                }
            }
            catch (MemoryValidationException exception)
            {
                feedback = exception.Message;
                feedbackIsError = true;
            }
            catch (InvalidOperationException exception) when (selection.Action is MemoryManagerAction.ApproveProposal or MemoryManagerAction.RejectProposal)
            {
                feedback = exception.Message;
                feedbackIsError = true;
            }
            catch (Exception exception) when (selection.Action is MemoryManagerAction.ApproveProposal or MemoryManagerAction.RejectProposal
                && IsProposalDataError(exception))
            {
                feedback = _text.Text("Lab.Invalid");
                feedbackIsError = true;
            }
            catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                feedback = _text.Text("Common.Cancelled");
            }
        }
    }

    /// <summary>Saves reviewed inline text through the same authoritative validation used by chat memory commands.</summary>
    private async Task<string> SaveAsync(PersistentMemory? memory, string? draft, CancellationToken cancellationToken)
    {
        var reviewed = draft ?? throw new MemoryValidationException(_text.Text("Memory.Empty"));
        var saved = memory is null
            ? await _memories.RememberAsync(reviewed, true, cancellationToken).ConfigureAwait(false)
            : await _memories.UpdateAsync(memory.Id, reviewed, true, cancellationToken).ConfigureAwait(false);
        return _text.Text("Memory.Saved", saved.Id);
    }

    /// <summary>Resolves only an exact identifier from the current accessible list.</summary>
    private PersistentMemory FindSelected(IReadOnlyList<PersistentMemory> memories, string? id) =>
        memories.SingleOrDefault(memory => string.Equals(memory.Id, id, StringComparison.Ordinal))
        ?? throw new MemoryValidationException(_text.Text("Memory.NotFound"));

    /// <summary>Identifies local proposal and settings failures that should remain inside the editor.</summary>
    private static bool IsProposalDataError(Exception exception) => exception is IOException
        or UnauthorizedAccessException
        or System.Text.Json.JsonException
        or YamlDotNet.Core.YamlException
        or ArgumentException;
}

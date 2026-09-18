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
    private readonly IConsoleShellView _shell;
    private readonly ILocalizationService _text;
    private readonly ExperimentalWorkflow? _experimental;
    private readonly ISettingsService? _settings;

    /// <summary>Connects the passive memory manager to local persistence and localized operation feedback.</summary>
    public MemoryManagerWorkflow(
        PersistentMemoryService memories,
        IMemoryManagerView view,
        IConsoleShellView shell,
        ILocalizationService text,
        ExperimentalWorkflow? experimental = null,
        ISettingsService? settings = null)
    {
        _memories = memories ?? throw new ArgumentNullException(nameof(memories));
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _experimental = experimental;
        _settings = settings;
    }

    /// <summary>Refreshes accessible notes after each operation and keeps recoverable input errors inside the manager.</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var memories = await _memories.ListAsync(cancellationToken).ConfigureAwait(false);
            MemoryManagerSelection selection;
            try
            {
                selection = _view.Choose(memories);
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
                        await SaveAsync(null, cancellationToken).ConfigureAwait(false);
                        break;
                    case MemoryManagerAction.Edit:
                        await SaveAsync(FindSelected(memories, selection.Id), cancellationToken).ConfigureAwait(false);
                        break;
                    case MemoryManagerAction.Delete:
                        var memory = FindSelected(memories, selection.Id);
                        _shell.RenderNotice(_text.Text("Lab.ForgetNotice"));
                        if (_view.ConfirmDelete(memory))
                        {
                            if (await _memories.ForgetAsync(memory.Id, cancellationToken).ConfigureAwait(false))
                            {
                                _shell.RenderSuccess(_text.Text("Memory.Forgotten"));
                            }
                            else
                            {
                                _shell.RenderError(_text.Text("Memory.NotFound"));
                            }
                        }
                        break;
                    case MemoryManagerAction.Proposals:
                        if (_experimental is not null && _settings is not null)
                        {
                            await _experimental.RunAsync(AppCommand.Proposals,
                                await _settings.LoadAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            _shell.RenderNotice(_text.Text("Lab.Disabled"));
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(selection), "Unsupported memory action.");
                }
            }
            catch (MemoryValidationException exception)
            {
                _shell.RenderError(exception.Message);
            }
            catch (InvalidOperationException exception) when (selection.Action == MemoryManagerAction.Proposals)
            {
                _shell.RenderError(exception.Message);
            }
            catch (InteractiveFlowCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _shell.RenderMuted(_text.Text("Common.Cancelled"));
            }
        }
    }

    /// <summary>Saves a reviewed draft through the same input validation used by chat memory commands.</summary>
    private async Task SaveAsync(PersistentMemory? memory, CancellationToken cancellationToken)
    {
        MemoryDraft? draft = null;
        string? error = null;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            draft = _view.Edit(memory, draft, error);
            if (draft is null)
            {
                return;
            }
            try
            {
                var saved = memory is null
                    ? await _memories.RememberAsync(draft.Text, true, cancellationToken).ConfigureAwait(false)
                    : await _memories.UpdateAsync(memory.Id, draft.Text, true, cancellationToken).ConfigureAwait(false);
                _shell.RenderSuccess(_text.Text("Memory.Saved", saved.Id));
                return;
            }
            catch (MemoryValidationException exception)
            {
                if (memory is not null && !(await _memories.ListAsync(cancellationToken).ConfigureAwait(false))
                    .Any(current => string.Equals(current.Id, memory.Id, StringComparison.Ordinal)))
                {
                    _shell.RenderError(_text.Text("Memory.NotFound"));
                    return;
                }
                error = exception.Message;
            }
        }
    }

    /// <summary>Resolves only an exact identifier from the current accessible list.</summary>
    private PersistentMemory FindSelected(IReadOnlyList<PersistentMemory> memories, string? id) =>
        memories.SingleOrDefault(memory => string.Equals(memory.Id, id, StringComparison.Ordinal))
        ?? throw new MemoryValidationException(_text.Text("Memory.NotFound"));
}

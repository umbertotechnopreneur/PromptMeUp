// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Applies explicit memory commands and resolves descriptive deletion requests without claiming unperformed writes.</summary>
public sealed class MemoryCommandWorkflow(
    PersistentMemoryService memories,
    IOpenAiService openAi,
    IActivityAuditService audit,
    ApplicationActivityRecorder activity,
    BoundedTextInput input,
    IEnvironmentSecretService secrets,
    IConsoleShellView shell,
    IMemoryView view,
    IMemoryForgetView forgetView,
    ILocalizationService text)
{
    /// <summary>Saves literal notes locally or deletes a single identified note after a complete lookup.</summary>
    public async Task<int> RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var request = input.Sanitize(options.Query ?? string.Empty, PersistentMemoryService.MaximumCharacters + 8, fromArgument: true).Trim();
            if (options.Command == AppCommand.Remember)
            {
                var parts = request.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
                var prefix = parts.FirstOrDefault();
                // Accept old command syntax without creating folder-specific memories.
                var explicitScope = string.Equals(prefix, "global", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "project", StringComparison.OrdinalIgnoreCase);
                var note = explicitScope ? parts.ElementAtOrDefault(1) ?? string.Empty : request;
                var saved = await memories.RememberAsync(note, true, cancellationToken).ConfigureAwait(false);
                shell.RenderSuccess(text.Text("Memory.Saved", saved.Id));
                view.Render([saved]);
                await activity.TryRecordAsync("remember", "completed", null, new { saved.Id, saved.IsGlobal }).ConfigureAwait(false);
                return 0;
            }
            if (options.Command != AppCommand.Forget)
            {
                throw new InvalidOperationException(text.Text("MemoryCli.Usage"));
            }

            var available = await memories.ListAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<PersistentMemory> matches;
            if (Guid.TryParseExact(request, "N", out var id))
            {
                matches = available.Where(memory => memory.Id == id.ToString("N")).ToArray();
            }
            else
            {
                matches = available.Where(memory => memory.Text.Equals(request, StringComparison.Ordinal)).ToArray();
                if (matches.Count == 0 && available.Count > 0)
                {
                    matches = await ResolveAsync(request, available, settings, cancellationToken).ConfigureAwait(false);
                }
            }
            if (matches.Count == 0)
            {
                shell.RenderWarning(text.Text("Memory.NotFound"));
                return 1;
            }
            shell.RenderNotice(text.Text("Lab.ForgetNotice"));
            var selectedId = forgetView.SelectForDeletion(matches);
            if (selectedId is null)
            {
                shell.RenderNotice(text.Text("Common.Cancelled"));
                return 0;
            }

            var selected = matches.Single(memory => memory.Id == selectedId);
            // Reject a note edited while the model was resolving the request.
            if (!await memories.ForgetAsync(selected.Id, cancellationToken, selected).ConfigureAwait(false))
            {
                shell.RenderWarning(text.Text("MemoryCli.Changed"));
                return 1;
            }
            shell.RenderSuccess(text.Text("MemoryCli.Forgotten", selected.Id));
            shell.RenderNotice(selected.Text);
            await activity.TryRecordAsync("forget", "completed", null, new { selected.Id, selected.IsGlobal }).ConfigureAwait(false);
            return 0;
        }
        catch (MemoryValidationException exception)
        {
            shell.RenderError(exception.Message);
            return 1;
        }
    }

    /// <summary>Searches every accessible note in bounded batches before offering matches for explicit selection.</summary>
    private async Task<IReadOnlyList<PersistentMemory>> ResolveAsync(
        string request,
        IReadOnlyList<PersistentMemory> available,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.SetupCompleted || !settings.AiEnabled)
        {
            throw new InvalidOperationException(text.Text("Error.SetupRequired"));
        }
        if (!secrets.IsConfigured(settings.ApiKeyVariable))
        {
            throw new InvalidOperationException(PromptMeUpApplication.AppendKeyRestartHint(
                text.Text("Error.ApiKeyMissing", settings.ApiKeyVariable), text, OperatingSystem.IsWindows()));
        }
        var matches = new List<PersistentMemory>();
        var totalCost = 0m;
        await using var session = await AuditSessionScope.StartAsync(
            audit, "memory-forget", settings, new { invocation = "forget" }, AuditSessionOutcome.Failed, cancellationToken).ConfigureAwait(false);
        foreach (var batch in available.Chunk(8))
        {
            var payload = JsonSerializer.Serialize(new
            {
                request,
                memories = batch.Select(memory => new { id = memory.Id, note = memory.Text })
            });
            var response = await shell.RunWithStatusAsync(text.Text("Status.Thinking"),
                () => openAi.SendAsync("memory-forget", session.Id, [new ChatMessage("user", payload)],
                    settings, settings.Language, cancellationToken)).ConfigureAwait(false);
            totalCost += response.EstimatedCostUsd ?? 0;
            shell.RenderRuntimeStatus(AiConversationWorkflow.CreateTurnSnapshot(response, settings, totalCost));
            try
            {
                matches.AddRange(ParseMatches(response.Text, batch));
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(text.Text("MemoryCli.InvalidResponse"));
            }
        }
        session.Outcome = AuditSessionOutcome.Completed;
        return matches;
    }

    /// <summary>Accepts only distinct identifiers present in the exact batch shown to the provider.</summary>
    internal static IReadOnlyList<PersistentMemory> ParseMatches(string response, IReadOnlyList<PersistentMemory> batch)
    {
        using var document = JsonDocument.Parse(response);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1
            || !root.TryGetProperty("matched_ids", out var ids) || ids.ValueKind != JsonValueKind.Array
            || ids.GetArrayLength() > batch.Count)
        {
            throw new JsonException("Invalid memory selection envelope.");
        }
        var matches = new List<PersistentMemory>();
        foreach (var id in ids.EnumerateArray())
        {
            if (id.ValueKind != JsonValueKind.String
                || batch.FirstOrDefault(memory => memory.Id == id.GetString()) is not { } matched
                || matches.Any(memory => memory.Id == matched.Id))
            {
                throw new JsonException("Unknown or repeated memory identifier.");
            }
            matches.Add(matched);
        }
        return matches;
    }
}

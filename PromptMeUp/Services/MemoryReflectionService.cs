// SPDX-License-Identifier: MIT

using System.Text.Encodings.Web;
using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Builds bounded evidence and validates advisory reflection without granting execution or memory authority.</summary>
public sealed class MemoryReflectionService(ISensitiveDataRedactor redactor, ILocalizationService text)
{
    private const int MaximumEvidence = 24;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Selects complete observations from independent sessions before filling a bounded Dream batch.</summary>
    public MemoryReflectionBatch DreamBatch(IReadOnlyList<LearningObservation> observations, int maximumCharacters)
    {
        ValidateObservations(observations);
        if (observations.Select(item => item.SessionId).Distinct(StringComparer.Ordinal).Count() < 2)
        {
            throw new InvalidOperationException(text.Text("Lab.NeedSessions"));
        }

        // Seed with the smallest full observation in two different sessions so one long turn cannot consume the batch.
        var seeds = observations.GroupBy(item => item.SessionId, StringComparer.Ordinal)
            .Select(group => group.MinBy(item => Serialize(true, [item], []).Length)!)
            .OrderBy(item => Serialize(true, [item], []).Length).Take(2).ToList();
        if (Serialize(true, seeds, []).Length > maximumCharacters)
        {
            throw new InvalidOperationException(text.Text("Lab.BatchTooSmall"));
        }
        foreach (var observation in observations.OrderByDescending(item => item.CreatedAt))
        {
            if (seeds.Count >= MaximumEvidence)
            {
                break;
            }
            if (!seeds.Any(item => item.Id == observation.Id)
                && Serialize(true, [.. seeds, observation], []).Length <= maximumCharacters)
            {
                seeds.Add(observation);
            }
        }
        var request = Serialize(true, seeds, []);
        ValidateSafe(request);
        return new(true, request, seeds.AsReadOnly(), [], observations.Count - seeds.Count);
    }

    /// <summary>Selects complete current-project and global memory snapshots within the serialized input ceiling.</summary>
    public MemoryReflectionBatch HeartbeatBatch(IReadOnlyList<PersistentMemory> memories, int maximumCharacters)
    {
        ValidateMemories(memories);
        var selected = new List<PersistentMemory>();
        foreach (var memory in memories.OrderByDescending(item => item.UpdatedAt))
        {
            if (selected.Count >= MaximumEvidence)
            {
                break;
            }
            if (Serialize(false, [], [.. selected, memory]).Length <= maximumCharacters)
            {
                selected.Add(memory);
            }
        }
        if (selected.Count == 0)
        {
            throw new InvalidOperationException(text.Text(memories.Count == 0 ? "Lab.None" : "Lab.BatchTooSmall"));
        }
        var request = Serialize(false, [], selected);
        ValidateSafe(request);
        return new(false, request, [], selected.AsReadOnly(), memories.Count - selected.Count);
    }

    /// <summary>Suggests only exact same-scope duplicates locally, preserving complete targets for later review.</summary>
    public IReadOnlyList<MemoryProposal> FindDuplicates(IReadOnlyList<PersistentMemory> memories)
    {
        ValidateMemories(memories);
        return memories.GroupBy(item => (item.IsGlobal, item.Text))
            .Where(group => group.Count() >= 2).Take(8)
            .Select(group => new MemoryProposal(Guid.NewGuid().ToString("N"), "memory", "merge", group.Key.Text,
                text.Text("Lab.DuplicateReason"), [], group.OrderBy(item => item.Id, StringComparer.Ordinal).Take(8).ToArray(), DateTimeOffset.UtcNow))
            .ToArray();
    }

    /// <summary>Rejects malformed output and binds every proposal to exact IDs from the approved evidence batch.</summary>
    public IReadOnlyList<MemoryProposal> Parse(string response, MemoryReflectionBatch batch)
    {
        if (string.IsNullOrWhiteSpace(response) || response.Length > 40_000)
        {
            throw Invalid();
        }
        ValidateSafe(response);
        try
        {
            using var document = JsonDocument.Parse(response, new JsonDocumentOptions { MaxDepth = 8 });
            RequireProperties(document.RootElement, "proposals");
            var array = document.RootElement.GetProperty("proposals");
            if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > 8)
            {
                throw Invalid();
            }
            var proposals = new List<MemoryProposal>();
            foreach (var item in array.EnumerateArray())
            {
                RequireProperties(item, "kind", "operation", "text", "rationale", "source_ids", "target_ids");
                var kind = ReadText(item, "kind", 20);
                var operation = ReadText(item, "operation", 20);
                var body = ReadText(item, "text", PersistentMemoryService.MaximumCharacters);
                var rationale = ReadText(item, "rationale", 1000);
                var sourceIds = ReadIds(item, "source_ids");
                var targetIds = ReadIds(item, "target_ids");
                if (kind is not ("memory" or "lesson" or "correction" or "preference")
                    || sourceIds.Any(id => !batch.Observations.Any(source => source.Id == id))
                    || targetIds.Any(id => !batch.Memories.Any(memory => memory.Id == id)))
                {
                    throw Invalid();
                }
                var targets = targetIds.Select(id => batch.Memories.Single(memory => memory.Id == id)).ToArray();
                if (batch.Dream)
                {
                    if (operation != "add" || targets.Length != 0
                        || batch.Observations.Where(source => sourceIds.Contains(source.Id, StringComparer.Ordinal))
                            .Select(source => source.SessionId).Distinct(StringComparer.Ordinal).Count() < 2)
                    {
                        throw Invalid();
                    }
                }
                else if (operation is not ("merge" or "archive" or "flag") || sourceIds.Count != 0
                    || targets.Length < (operation == "merge" ? 2 : 1)
                    || targets.Select(target => target.IsGlobal).Distinct().Count() > 1)
                {
                    throw Invalid();
                }
                proposals.Add(new(Guid.NewGuid().ToString("N"), kind, operation, body, rationale,
                    sourceIds, targets, DateTimeOffset.UtcNow));
            }
            return proposals;
        }
        catch (JsonException)
        {
            throw Invalid();
        }
    }

    /// <summary>Checks reviewed text without silently changing what the user approved.</summary>
    public string ValidateReviewedText(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length is 0 or > PersistentMemoryService.MaximumCharacters)
        {
            throw Invalid();
        }
        ValidateSafe(trimmed);
        return trimmed;
    }

    /// <summary>Serializes complete records, including escaping and envelope overhead, before measuring the input budget.</summary>
    private static string Serialize(bool dream, IReadOnlyList<LearningObservation> observations, IReadOnlyList<PersistentMemory> memories) =>
        JsonSerializer.Serialize(new
        {
            mode = dream ? "dream" : "heartbeat",
            observations = observations.Select(item => new { id = item.Id, session_id = item.SessionId, text = item.Text, created_at = item.CreatedAt }),
            memories = memories.Select(item => new { id = item.Id, scope = item.IsGlobal ? "global" : "project", text = item.Text, updated_at = item.UpdatedAt })
        }, Json);

    /// <summary>Validates captured evidence before allocating or sharing provider-bound batches.</summary>
    private void ValidateObservations(IReadOnlyList<LearningObservation> observations)
    {
        if (observations.Count > 200 || observations.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != observations.Count
            || observations.Any(item => !Guid.TryParseExact(item.Id, "N", out _)
                || string.IsNullOrWhiteSpace(item.SessionId) || string.IsNullOrWhiteSpace(item.Text) || item.Text.Length > 4000))
        {
            throw Invalid();
        }
        foreach (var item in observations)
        {
            ValidateSafe(item.Text);
        }
    }

    /// <summary>Rejects invalid memory snapshots instead of revising their displayed content in transit.</summary>
    private void ValidateMemories(IReadOnlyList<PersistentMemory> memories)
    {
        if (memories.Count > PersistentMemoryService.MaximumMemoriesPerScope * 2
            || memories.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != memories.Count
            || memories.Any(item => !Guid.TryParseExact(item.Id, "N", out _) || string.IsNullOrWhiteSpace(item.Text)
                || item.Text.Length > PersistentMemoryService.MaximumCharacters))
        {
            throw Invalid();
        }
        foreach (var item in memories)
        {
            ValidateSafe(item.Text);
        }
    }

    /// <summary>Requires the exact case-sensitive contract and rejects duplicate JSON properties.</summary>
    private void RequireProperties(JsonElement element, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Invalid();
        }
        var names = element.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Length != expected.Length || names.Distinct(StringComparer.Ordinal).Count() != names.Length
            || names.Any(name => !expected.Contains(name, StringComparer.Ordinal)))
        {
            throw Invalid();
        }
    }

    /// <summary>Reads a bounded plain-text field while rejecting credentials concealed by JSON escaping.</summary>
    private string ReadText(JsonElement element, string name, int maximum)
    {
        var property = element.GetProperty(name);
        if (property.ValueKind != JsonValueKind.String)
        {
            throw Invalid();
        }
        var value = property.GetString()!;
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum)
        {
            throw Invalid();
        }
        ValidateSafe(value);
        return value.Trim();
    }

    /// <summary>Reads at most eight distinct canonical IDs and rejects invalid elements before matching evidence.</summary>
    private IReadOnlyList<string> ReadIds(JsonElement element, string name)
    {
        var array = element.GetProperty(name);
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > 8)
        {
            throw Invalid();
        }
        var ids = new List<string>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !Guid.TryParseExact(item.GetString(), "N", out _)
                || ids.Contains(item.GetString()!, StringComparer.Ordinal))
            {
                throw Invalid();
            }
            ids.Add(item.GetString()!);
        }
        return ids;
    }

    /// <summary>Prevents provider-bound and persisted evidence from retaining recognizable credentials.</summary>
    private void ValidateSafe(string value)
    {
        if (value != redactor.Redact(value))
        {
            throw Invalid();
        }
    }

    /// <summary>Returns a localized validation error without echoing rejected provider data.</summary>
    private InvalidOperationException Invalid() => new(text.Text("Lab.Invalid"));
}

// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public sealed class PlanStore(AppPaths paths, ISensitiveDataRedactor redactor, ILocalizationService text, ArtifactLimits? limits = null)
{
    private readonly ArtifactLimits _limits = limits ?? ArtifactLimits.Default;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Parses a model plan as new pending steps, never importing model-supplied execution status.</summary>
    public ExecutionPlan Parse(string json, string goal, string directory)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var steps = document.RootElement.GetProperty("steps").EnumerateArray().Select(item => new PlanStep(
                item.GetProperty("label").GetString()!,
                item.GetProperty("command").GetString()!,
                item.GetProperty("verification").GetString()!,
                item.GetProperty("expected").GetString()!)).ToList();
            var plan = new ExecutionPlan(1, Guid.NewGuid().ToString("N"), goal, Path.GetFullPath(directory), steps);
            Validate(plan);
            return plan;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException(text.Text("Plan.Invalid"));
        }
    }

    /// <summary>Locks one plan for the complete guided session so concurrent invocations cannot replay it.</summary>
    public IDisposable Acquire(string id)
    {
        var path = Resolve(id) + ".lock";
        try
        {
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            throw new InvalidOperationException(text.Text("Plan.Busy"));
        }
    }

    /// <summary>Loads bounded state without trusting persisted statuses or commands as authorization.</summary>
    public async Task<ExecutionPlan> LoadAsync(string id, CancellationToken cancellationToken)
    {
        var path = Resolve(id);
        try
        {
            var bytes = await BoundedArtifactFile.ReadAsync(path, _limits.MaxPlanBytes, text, cancellationToken).ConfigureAwait(false);
            var plan = JsonSerializer.Deserialize<ExecutionPlan>(bytes, JsonOptions);
            Validate(plan!);
            if (plan!.Id != id)
            {
                throw new InvalidOperationException(text.Text("Plan.Invalid"));
            }
            return plan;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidOperationException(text.Text("Plan.LoadError"));
        }
    }

    /// <summary>Atomically records intent before execution and progress afterward, retaining valid JSON after interruption.</summary>
    public async Task SaveAsync(ExecutionPlan plan, CancellationToken cancellationToken)
    {
        Validate(plan);
        var path = Resolve(plan.Id);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(plan, JsonOptions), cancellationToken).ConfigureAwait(false);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    /// <summary>Checks supported schema, bounded content, valid progress states, and credential-free plan data.</summary>
    public void Validate(ExecutionPlan plan)
    {
        if (plan is null || plan.Version != 1 || !Guid.TryParseExact(plan.Id, "N", out _)
            || !ValidText(plan.Goal, _limits.MaxPlanBytes) || !ValidText(plan.Directory, 4096)
            || !Path.IsPathFullyQualified(plan.Directory) || plan.Steps is null || plan.Steps.Count is < 1 or > 8)
        {
            throw new InvalidOperationException(text.Text("Plan.Invalid"));
        }
        foreach (var step in plan.Steps)
        {
            if (step is null || !ValidText(step.Label, 160) || !ValidText(step.Command, _limits.MaxPlanBytes)
                || !ValidText(step.Verification, _limits.MaxPlanBytes) || !ValidText(step.Expected, _limits.MaxPlanBytes)
                || !Enum.IsDefined(step.Status))
            {
                throw new InvalidOperationException(text.Text("Plan.Invalid"));
            }
        }
        BoundedArtifactFile.CheckSize(JsonSerializer.SerializeToUtf8Bytes(plan, JsonOptions).Length, _limits.MaxPlanBytes, text);
    }

    /// <summary>Resolves only opaque generated identifiers inside the local plan directory.</summary>
    private string Resolve(string id) => Guid.TryParseExact(id, "N", out _)
        ? Path.Combine(paths.DataDirectory, "plans", id + ".json")
        : throw new InvalidOperationException(text.Text("Plan.Invalid"));

    /// <summary>Rejects credentials, control sequences, or unbounded strings in durable plan content.</summary>
    private bool ValidText(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum
        && redactor.Redact(value) == value
        && !value.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t'));
}

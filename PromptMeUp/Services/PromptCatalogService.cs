// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PromptMeUp.Services;

public interface IPromptCatalogService
{
    Task<PromptDefinition> GetAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PromptDefinition>> ListAsync(CancellationToken cancellationToken);
}

public sealed class YamlPromptCatalogService : IPromptCatalogService
{
    private readonly AppPaths _paths;
    private readonly ILogger<YamlPromptCatalogService> _logger;
    private readonly IDeserializer _deserializer;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyDictionary<string, PromptDefinition>? _cache;

    /// <summary>Creates the validated packaged YAML prompt catalog.</summary>
    public YamlPromptCatalogService(AppPaths paths, ILogger<YamlPromptCatalogService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <summary>Loads one validated YAML prompt by stable identifier.</summary>
    public async Task<PromptDefinition> GetAsync(string id, CancellationToken cancellationToken)
    {
        var prompts = await LoadAsync(cancellationToken).ConfigureAwait(false);
        return prompts.TryGetValue(id, out var prompt)
            ? prompt
            : throw new KeyNotFoundException($"Prompt resource '{id}' was not found.");
    }

    /// <summary>Lists all validated YAML prompt resources in identifier order.</summary>
    public async Task<IReadOnlyList<PromptDefinition>> ListAsync(CancellationToken cancellationToken) =>
        (await LoadAsync(cancellationToken).ConfigureAwait(false)).Values.OrderBy(prompt => prompt.Id, StringComparer.Ordinal).ToArray();

    /// <summary>Builds the immutable prompt cache once per process.</summary>
    private async Task<IReadOnlyDictionary<string, PromptDefinition>> LoadAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache is not null)
            {
                return _cache;
            }

            if (!Directory.Exists(_paths.PromptDirectory))
            {
                throw new DirectoryNotFoundException($"Prompt directory not found: {_paths.PromptDirectory}");
            }

            var loaded = new Dictionary<string, PromptDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.EnumerateFiles(_paths.PromptDirectory, "*.yaml", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var yaml = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                var document = _deserializer.Deserialize<PromptYaml>(yaml)
                    ?? throw new InvalidDataException($"Prompt file '{path}' is empty.");
                var definition = Validate(document, path);
                if (!loaded.TryAdd(definition.Id, definition))
                {
                    throw new InvalidDataException($"Duplicate prompt identifier '{definition.Id}'.");
                }
            }

            if (loaded.Count == 0)
            {
                throw new InvalidDataException("At least one YAML prompt resource is required.");
            }

            _cache = new Dictionary<string, PromptDefinition>(loaded, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation("YAML prompt catalog loaded. PromptCount={PromptCount}", _cache.Count);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Rejects incomplete prompt metadata and unsupported locale maps.</summary>
    private static PromptDefinition Validate(PromptYaml document, string path)
    {
        if (string.IsNullOrWhiteSpace(document.Id)
            || document.Id.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            || document.Version < 1
            || string.IsNullOrWhiteSpace(document.Description)
            || document.Texts is null
            || !document.Texts.TryGetValue("en", out var english)
            || string.IsNullOrWhiteSpace(english)
            || SupportedLanguages.Codes.Any(language =>
                !document.Texts.TryGetValue(language, out var localized) || string.IsNullOrWhiteSpace(localized)))
        {
            throw new InvalidDataException($"Prompt file '{path}' has invalid required metadata.");
        }

        foreach (var (language, text) in document.Texts)
        {
            if (!SupportedLanguages.IsSupported(language) || string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidDataException($"Prompt '{document.Id}' has invalid localized text '{language}'.");
            }
        }

        return new PromptDefinition(
            document.Id,
            document.Version,
            document.Description.Trim(),
            (document.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            new Dictionary<string, string>(document.Texts, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(document.Metadata ?? [], StringComparer.OrdinalIgnoreCase));
    }

    private sealed class PromptYaml
    {
        public string Id { get; init; } = string.Empty;

        public int Version { get; init; }

        public string Description { get; init; } = string.Empty;

        public List<string>? Tags { get; init; }

        public Dictionary<string, string>? Texts { get; init; }

        public Dictionary<string, string>? Metadata { get; init; }
    }
}

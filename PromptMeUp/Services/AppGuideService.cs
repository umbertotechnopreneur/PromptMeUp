// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IAppGuideService
{
    Task<AppGuideContext> LoadAsync(IReadOnlyList<string> topics, string language, CancellationToken cancellationToken);
}

public sealed class AppGuideService : IAppGuideService
{
    public const int MaxTopics = 2;
    public const long MaximumTokens = 3000;

    public static readonly IReadOnlyList<string> Topics = Array.AsReadOnly(new[]
    {
        "overview", "settings", "conversation", "memories", "commands", "workflows", "costs", "privacy"
    });

    private readonly IPromptCatalogService _catalog;
    private readonly ILogger<AppGuideService> _logger;

    /// <summary>Creates a guide reader restricted to the packaged prompt catalog.</summary>
    public AppGuideService(IPromptCatalogService catalog, ILogger<AppGuideService> logger)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Loads known chapters in the selected language and rejects invalid or oversized context.</summary>
    public async Task<AppGuideContext> LoadAsync(IReadOnlyList<string> topics, string language, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(topics);
        cancellationToken.ThrowIfCancellationRequested();
        if (!SupportedLanguages.IsSupported(language))
        {
            throw new ArgumentException("The app guide requires a supported language.", nameof(language));
        }

        var requested = topics.ToArray();
        if (requested.Length > MaxTopics
            || requested.Any(topic => !Topics.Contains(topic, StringComparer.Ordinal))
            || requested.Distinct(StringComparer.Ordinal).Count() != requested.Length)
        {
            throw new ArgumentException("The app guide requires at most two distinct known topics.", nameof(topics));
        }

        if (requested.Length == 0)
        {
            return AppGuideContext.Empty;
        }

        var normalizedLanguage = SupportedLanguages.Normalize(language);
        var text = new StringBuilder("<app-guide>\n");
        foreach (var topic in requested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = $"app-guide-{topic}";
            var chapter = await _catalog.GetAsync(id, cancellationToken).ConfigureAwait(false);
            if (chapter.Id != id || chapter.Version < 1
                || !chapter.Texts.TryGetValue(normalizedLanguage, out var localized)
                || string.IsNullOrWhiteSpace(localized))
            {
                throw new InvalidDataException($"App guide chapter '{id}' is incomplete.");
            }

            text.Append("<chapter id=\"").Append(id).Append("\" version=\"")
                .Append(chapter.Version.ToString(CultureInfo.InvariantCulture)).Append("\">\n")
                .Append(localized).Append("\n</chapter>\n");
        }

        text.Append("</app-guide>");
        var completed = text.ToString();
        var tokens = ContextTokenEstimator.Text(completed);
        if (tokens > MaximumTokens)
        {
            throw new InvalidDataException("The requested app guide exceeds its system-context allowance.");
        }

        _logger.LogInformation("App guide loaded. TopicCount={TopicCount}, Language={Language}, EstimatedTokens={EstimatedTokens}",
            requested.Length, normalizedLanguage, tokens);
        return new AppGuideContext(Array.AsReadOnly(requested), completed, tokens);
    }
}

// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class AppGuideServiceTests
{
    /// <summary>Checks that every packaged pair is localized and fits the guide allowance with its versioned wrapper.</summary>
    [Fact]
    public async Task LoadAsync_PackagedChapterPairs_FitEverySupportedLanguage()
    {
        var paths = new AppPaths(
            "unused-data",
            "unused-data/promptmeup.db",
            "unused-data/logs",
            "unused-data/logs/promptmeup-.log",
            Path.Combine(AppContext.BaseDirectory, "prompt"));
        var catalog = new YamlPromptCatalogService(paths, NullLogger<YamlPromptCatalogService>.Instance);
        var service = new AppGuideService(catalog, NullLogger<AppGuideService>.Instance);

        foreach (var language in SupportedLanguages.Codes)
        {
            for (var first = 0; first < AppGuideService.Topics.Count; first++)
            {
                for (var second = first + 1; second < AppGuideService.Topics.Count; second++)
                {
                    var requested = new[] { AppGuideService.Topics[first], AppGuideService.Topics[second] };
                    var context = await service.LoadAsync(requested, language, CancellationToken.None);

                    Assert.Equal(requested, context.Topics);
                    Assert.StartsWith("<app-guide>\n", context.Text, StringComparison.Ordinal);
                    Assert.EndsWith("</app-guide>", context.Text, StringComparison.Ordinal);
                    Assert.InRange(context.Tokens, 1, AppGuideService.MaximumTokens);
                    foreach (var topic in requested)
                    {
                        var chapter = await catalog.GetAsync($"app-guide-{topic}", CancellationToken.None);
                        Assert.Equal(SupportedLanguages.Codes.OrderBy(code => code), chapter.Texts.Keys.OrderBy(code => code));
                        Assert.Equal("system", chapter.Metadata["role"]);
                        Assert.Contains($"<chapter id=\"{chapter.Id}\" version=\"{chapter.Version}\">", context.Text, StringComparison.Ordinal);
                        Assert.Contains(chapter.Texts[language], context.Text, StringComparison.Ordinal);
                    }
                }
            }
        }
    }

    /// <summary>Rejects unknown names and path-like inputs before touching the catalog.</summary>
    [Theory]
    [InlineData("unknown")]
    [InlineData("../settings")]
    [InlineData("SETTINGS")]
    [InlineData("")]
    public async Task LoadAsync_InvalidTopic_DoesNotReadCatalog(string topic)
    {
        var catalog = new GuideCatalog();
        var service = new AppGuideService(catalog, NullLogger<AppGuideService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.LoadAsync([topic], "en", CancellationToken.None));

        Assert.Equal(0, catalog.ReadCount);
    }

    /// <summary>Rejects duplicate and excessive chapter requests before performing local retrieval.</summary>
    [Fact]
    public async Task LoadAsync_DuplicateOrExcessiveTopics_DoesNotReadCatalog()
    {
        var catalog = new GuideCatalog();
        var service = new AppGuideService(catalog, NullLogger<AppGuideService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.LoadAsync(["settings", "settings"], "en", CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.LoadAsync(["settings", "conversation", "costs"], "en", CancellationToken.None));

        Assert.Equal(0, catalog.ReadCount);
    }

    /// <summary>Rejects unsupported languages instead of silently returning English app instructions.</summary>
    [Fact]
    public async Task LoadAsync_UnsupportedLanguage_DoesNotReadCatalog()
    {
        var catalog = new GuideCatalog();
        var service = new AppGuideService(catalog, NullLogger<AppGuideService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.LoadAsync(["settings"], "ja", CancellationToken.None));

        Assert.Equal(0, catalog.ReadCount);
    }

    /// <summary>Returns no guide without reading any resource when no topics are requested.</summary>
    [Fact]
    public async Task LoadAsync_NoTopics_ReturnsEmptyContext()
    {
        var catalog = new GuideCatalog();
        var service = new AppGuideService(catalog, NullLogger<AppGuideService>.Instance);

        var context = await service.LoadAsync([], "en", CancellationToken.None);

        Assert.Same(AppGuideContext.Empty, context);
        Assert.Equal(0, catalog.ReadCount);
    }

    /// <summary>Honors cancellation before loading even a known packaged chapter.</summary>
    [Fact]
    public async Task LoadAsync_Canceled_DoesNotReadCatalog()
    {
        var catalog = new GuideCatalog();
        var service = new AppGuideService(catalog, NullLogger<AppGuideService>.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.LoadAsync(["settings"], "en", cancellation.Token));

        Assert.Equal(0, catalog.ReadCount);
    }

    /// <summary>Rejects a chapter that cannot be safely identified or localized as requested.</summary>
    [Fact]
    public async Task LoadAsync_MismatchedOrMissingChapter_RejectsContent()
    {
        var catalog = new GuideCatalog();
        var service = new AppGuideService(catalog, NullLogger<AppGuideService>.Instance);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync(["conversation"], "en", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync(["settings"], "it", CancellationToken.None));
    }

    /// <summary>Rejects oversized chapter contents instead of silently truncating trusted documentation.</summary>
    [Fact]
    public async Task LoadAsync_OversizedChapter_RejectsContent()
    {
        var catalog = new GuideCatalog
        {
            Chapter = new PromptDefinition("app-guide-settings", 1, "Settings", [],
                new Dictionary<string, string> { ["en"] = new('a', 12_000) }, new Dictionary<string, string>())
        };
        var service = new AppGuideService(catalog, NullLogger<AppGuideService>.Instance);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync(["settings"], "en", CancellationToken.None));
    }

    private sealed class GuideCatalog : IPromptCatalogService
    {
        public int ReadCount { get; private set; }

        public PromptDefinition Chapter { get; init; } = new("app-guide-settings", 1, "Settings", [],
            new Dictionary<string, string> { ["en"] = "Choose Save to apply preferences." }, new Dictionary<string, string>());

        /// <summary>Returns controlled guide data while recording whether validation allowed a lookup.</summary>
        public Task<PromptDefinition> GetAsync(string id, CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(Chapter);
        }

        /// <summary>Rejects catalog enumeration because guide retrieval must use an exact known identifier.</summary>
        public Task<IReadOnlyList<PromptDefinition>> ListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

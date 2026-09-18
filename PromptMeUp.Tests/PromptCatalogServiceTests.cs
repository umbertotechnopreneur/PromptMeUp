// SPDX-License-Identifier: MIT

using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Infrastructure;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class PromptCatalogServiceTests
{
    /// <summary>Verifies that every memory wrapper uses the user role and leaves room for bounded saved-note data.</summary>
    [Fact]
    public async Task GetAsync_MemoryContext_ProvidesBoundedLocalizedUserDataWrapper()
    {
        var paths = new AppPaths(
            "unused-data",
            "unused-data/promptmeup.db",
            "unused-data/logs",
            "unused-data/logs/promptmeup-.log",
            Path.Combine(AppContext.BaseDirectory, "prompt"));
        var catalog = new YamlPromptCatalogService(
            paths,
            NullLogger<YamlPromptCatalogService>.Instance);

        var prompt = await catalog.GetAsync("memory-context", CancellationToken.None);

        Assert.Equal("user", prompt.Metadata["role"]);
        Assert.Equal("800", prompt.Metadata["max-estimated-tokens"]);
        Assert.Equal(SupportedLanguages.Codes.OrderBy(language => language), prompt.Texts.Keys.OrderBy(language => language));
        foreach (var language in SupportedLanguages.Codes)
        {
            var sections = prompt.ResolveText(language).Split("{memories}", StringSplitOptions.None);
            Assert.Equal(2, sections.Length);
            var estimatedWrapperTokens = (long)Math.Ceiling(Encoding.UTF8.GetByteCount(string.Concat(sections)) / 4d) + 4;
            Assert.InRange(estimatedWrapperTokens, 1, 150);
        }
    }

    /// <summary>Verifies that both scoped assistant prompt contracts are packaged with every supported language.</summary>
    [Fact]
    public async Task GetAsync_AssistantPrompts_LoadsChatAndSingleQueryContracts()
    {
        var paths = new AppPaths(
            "unused-data",
            "unused-data/promptmeup.db",
            "unused-data/logs",
            "unused-data/logs/promptmeup-.log",
            Path.Combine(AppContext.BaseDirectory, "prompt"));
        var catalog = new YamlPromptCatalogService(
            paths,
            NullLogger<YamlPromptCatalogService>.Instance);

        var chat = await catalog.GetAsync("chat-system", CancellationToken.None);
        var query = await catalog.GetAsync("query-system", CancellationToken.None);

        Assert.Equal(13, chat.Version);
        Assert.Equal(11, query.Version);
        Assert.Equal("promptmeup-console-response-v2", chat.Metadata["response-format"]);
        Assert.Equal("promptmeup-console-response-v2", query.Metadata["response-format"]);
        Assert.Equal(SupportedLanguages.Codes.OrderBy(language => language), chat.Texts.Keys.OrderBy(language => language));
        Assert.Equal(SupportedLanguages.Codes.OrderBy(language => language), query.Texts.Keys.OrderBy(language => language));
        Assert.Contains("JSON object", query.ResolveText("en"), StringComparison.Ordinal);
        Assert.Contains("console", chat.ResolveText("en"), StringComparison.OrdinalIgnoreCase);
        foreach (var language in SupportedLanguages.Codes)
        {
            Assert.Contains("<user-configured-preamble>", chat.ResolveText(language), StringComparison.Ordinal);
            Assert.Contains("<user-configured-preamble>", query.ResolveText(language), StringComparison.Ordinal);
            Assert.Contains("<app-guide>", chat.ResolveText(language), StringComparison.Ordinal);
            Assert.Contains("<app-guide>", query.ResolveText(language), StringComparison.Ordinal);
            Assert.Contains("\"guide_topics\":[]", chat.ResolveText(language), StringComparison.Ordinal);
            Assert.Contains("\"guide_topics\":[]", query.ResolveText(language), StringComparison.Ordinal);
            foreach (var topic in AppGuideService.Topics)
            {
                Assert.Contains(topic + " (", chat.ResolveText(language), StringComparison.Ordinal);
                Assert.Contains(topic + " (", query.ResolveText(language), StringComparison.Ordinal);
            }
        }
    }
}

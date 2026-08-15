// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Infrastructure;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class PromptCatalogServiceTests
{
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

        Assert.Equal(3, chat.Version);
        Assert.Equal(2, query.Version);
        Assert.Equal(SupportedLanguages.Codes.OrderBy(language => language), chat.Texts.Keys.OrderBy(language => language));
        Assert.Equal(SupportedLanguages.Codes.OrderBy(language => language), query.Texts.Keys.OrderBy(language => language));
        Assert.Contains("JSON object", query.ResolveText("en"), StringComparison.Ordinal);
        Assert.Contains("console", chat.ResolveText("en"), StringComparison.OrdinalIgnoreCase);
    }
}

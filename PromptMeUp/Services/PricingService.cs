// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;
using PromptMeUp.Services.OpenAi;

namespace PromptMeUp.Services;

public interface IPricingService
{
    Task<PricingRefreshResult> RefreshDailyIfNeededAsync(AppSettings settings, bool force, CancellationToken cancellationToken);

    Task<CostOverview> GetOverviewAsync(CancellationToken cancellationToken);
}

public sealed class OpenAiPricingService : IPricingService
{
    public const string PricingMarkdownUrl = "https://developers.openai.com/api/docs/pricing.md";
    private const string Provider = "openai";
    private readonly HttpClient _http;
    private readonly IDatabaseService _database;
    private readonly IEnvironmentSecretService _secrets;
    private readonly ILogger<OpenAiPricingService> _logger;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    /// <summary>Creates the official pricing and optional organization-cost synchronization service.</summary>
    public OpenAiPricingService(
        HttpClient http,
        IDatabaseService database,
        IEnvironmentSecretService secrets,
        ILogger<OpenAiPricingService> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Refreshes standard prices and optional organization costs at most once per local day.</summary>
    public async Task<PricingRefreshResult> RefreshDailyIfNeededAsync(
        AppSettings settings,
        bool force,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var priceRows = 0;
            var organizationCostRows = 0;
            var pricesRefreshed = false;
            var organizationCostsAvailable = false;
            var latestPricing = await _database.GetLatestModelPriceSyncAsync(Provider, cancellationToken).ConfigureAwait(false);
            if (force || !WasSynchronizedToday(latestPricing))
            {
                priceRows = await RefreshPricesAsync(cancellationToken).ConfigureAwait(false);
                pricesRefreshed = true;
            }
            else
            {
                priceRows = (await _database.ListModelPricesAsync(Provider, cancellationToken).ConfigureAwait(false)).Count;
            }

            var adminKey = _secrets.Load(settings.AdminKeyVariable);
            if (_secrets.LooksLikeOpenAiKey(adminKey))
            {
                organizationCostsAvailable = true;
                var latestOrganizationCostSync = await _database.GetLastOrganizationCostSyncAsync(cancellationToken).ConfigureAwait(false);
                if (force || !WasSynchronizedToday(latestOrganizationCostSync))
                {
                    organizationCostRows = await RefreshOrganizationCostsAsync(adminKey!, cancellationToken).ConfigureAwait(false);
                }
            }

            return new PricingRefreshResult(
                priceRows,
                organizationCostRows,
                pricesRefreshed,
                organizationCostsAvailable,
                DateTimeOffset.UtcNow);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    /// <summary>Builds the current local usage, price, and organization-cost overview.</summary>
    public async Task<CostOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var prices = await _database.ListModelPricesAsync(Provider, cancellationToken).ConfigureAwait(false);
        var requestSummary = await _database.GetAiRequestSummaryAsync(cancellationToken).ConfigureAwait(false);
        return new CostOverview(
            await _database.GetLatestModelPriceSyncAsync(Provider, cancellationToken).ConfigureAwait(false),
            await _database.GetLastOrganizationCostSyncAsync(cancellationToken).ConfigureAwait(false),
            requestSummary.EstimatedCostTodayUsd,
            requestSummary.EstimatedCostCurrentMonthUsd,
            await _database.GetOrganizationCostCurrentMonthAsync(cancellationToken).ConfigureAwait(false),
            requestSummary.RequestsToday,
            requestSummary.InputTokensToday,
            requestSummary.OutputTokensToday,
            requestSummary.TotalTokensToday,
            prices);
    }

    /// <summary>Downloads and atomically replaces the cached official standard-price table.</summary>
    private async Task<int> RefreshPricesAsync(CancellationToken cancellationToken)
    {
        var markdown = await _http.GetStringAsync(PricingMarkdownUrl, cancellationToken).ConfigureAwait(false);
        var retrievedAt = DateTimeOffset.UtcNow;
        var prices = OpenAiPricingMarkdownParser.ParseStandardPricingData(markdown, retrievedAt, PricingMarkdownUrl);
        await _database.ReplaceModelPricesAsync(Provider, prices, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("OpenAI price table refreshed. PriceRows={PriceRows}", prices.Count);
        return prices.Count;
    }

    /// <summary>Downloads current-month billed cost buckets through the admin-only Costs API.</summary>
    private async Task<int> RefreshOrganizationCostsAsync(string adminKey, CancellationToken cancellationToken)
    {
        var localToday = DateOnly.FromDateTime(DateTime.Now);
        var localMonthStart = new DateOnly(localToday.Year, localToday.Month, 1);
        var from = StartOfLocalDayUtc(localMonthStart);
        var to = DateTimeOffset.UtcNow.AddSeconds(1);
        var retrievedAt = DateTimeOffset.UtcNow;
        var costs = new List<OrganizationCost>();
        string? page = null;

        do
        {
            var endpoint = BuildCostsEndpoint(from, to, page);
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminKey);
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new OpenAiRequestException(
                    OpenAiResponseParser.ReadApiError(json) ?? $"OpenAI Costs API returned HTTP {(int)response.StatusCode}.",
                    "organization_costs_failed",
                    (int)response.StatusCode);
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            foreach (var bucket in root.GetProperty("data").EnumerateArray())
            {
                var bucketStart = DateTimeOffset.FromUnixTimeSeconds(bucket.GetProperty("start_time").GetInt64());
                var bucketEnd = DateTimeOffset.FromUnixTimeSeconds(bucket.GetProperty("end_time").GetInt64());
                foreach (var result in bucket.GetProperty("results").EnumerateArray())
                {
                    var amount = result.GetProperty("amount");
                    var lineItem = ReadOptionalString(result, "line_item");
                    var projectId = ReadOptionalString(result, "project_id");
                    costs.Add(new OrganizationCost(
                        StableCostId(bucketStart, lineItem, projectId),
                        bucketStart,
                        bucketEnd,
                        amount.GetProperty("value").GetDecimal(),
                        amount.GetProperty("currency").GetString() ?? "usd",
                        lineItem,
                        projectId,
                        retrievedAt));
                }
            }

            page = root.TryGetProperty("has_more", out var hasMore) && hasMore.GetBoolean()
                ? ReadOptionalString(root, "next_page")
                : null;
        }
        while (!string.IsNullOrWhiteSpace(page));

        await _database.ReplaceOrganizationCostsAsync(from, to, costs, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("OpenAI organization costs refreshed. CostRows={CostRows}", costs.Count);
        return costs.Count;
    }

    /// <summary>Builds the documented Costs API query, including cursor pagination when present.</summary>
    private static Uri BuildCostsEndpoint(DateTimeOffset from, DateTimeOffset to, string? page)
    {
        var query = $"start_time={from.ToUnixTimeSeconds()}&end_time={to.ToUnixTimeSeconds()}&bucket_width=1d&limit=31&group_by=line_item";
        if (!string.IsNullOrWhiteSpace(page))
        {
            query += "&page=" + Uri.EscapeDataString(page);
        }

        return new Uri("https://api.openai.com/v1/organization/costs?" + query, UriKind.Absolute);
    }

    /// <summary>Returns whether a UTC synchronization timestamp belongs to the current local day.</summary>
    private static bool WasSynchronizedToday(DateTimeOffset? timestamp) =>
        timestamp.HasValue && DateOnly.FromDateTime(timestamp.Value.LocalDateTime) == DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Converts local midnight to UTC without assuming a fixed offset.</summary>
    private static DateTimeOffset StartOfLocalDayUtc(DateOnly date)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, TimeZoneInfo.Local));
    }

    /// <summary>Creates a deterministic local key for one API cost bucket/result combination.</summary>
    private static string StableCostId(DateTimeOffset bucketStart, string? lineItem, string? projectId)
    {
        var material = $"{bucketStart.ToUnixTimeSeconds()}|{lineItem}|{projectId}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }

    /// <summary>Reads a nullable JSON string without accepting non-string values.</summary>
    private static string? ReadOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

}

internal static class OpenAiPricingMarkdownParser
{
    /// <summary>Parses the official Standard pricing table into normalized short/long rows.</summary>
    internal static IReadOnlyList<AiModelPrice> ParseStandardPricingData(
        string markdown,
        DateTimeOffset retrievedAt,
        string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new InvalidDataException("OpenAI pricing markdown is empty.");
        }

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var headingIndex = Array.FindIndex(lines, line => string.Equals(line.Trim(), "### Standard pricing data", StringComparison.Ordinal));
        var tableIndex = headingIndex < 0
            ? -1
            : Array.FindIndex(lines, headingIndex + 1, line => line.TrimStart().StartsWith("| Model |", StringComparison.Ordinal));
        if (tableIndex < 0)
        {
            throw new InvalidDataException("OpenAI Standard pricing table was not found.");
        }

        var prices = new List<AiModelPrice>();
        for (var index = tableIndex + 1; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (!line.StartsWith('|'))
            {
                break;
            }

            var cells = ParseCells(line);
            if (cells.Count != 9)
            {
                break;
            }

            if (cells.All(cell => cell.Length == 0 || cell.All(character => character == '-')))
            {
                continue;
            }

            var model = NormalizeModelName(cells[0]);
            AddPrice(prices, model, "short", cells[1], cells[2], cells[3], cells[4], retrievedAt, sourceUrl);
            if (cells[5] != "-" && cells[8] != "-")
            {
                AddPrice(prices, model, "long", cells[5], cells[6], cells[7], cells[8], retrievedAt, sourceUrl);
            }
        }

        if (prices.Count == 0)
        {
            throw new InvalidDataException("OpenAI Standard pricing table contained no prices.");
        }

        return prices;
    }

    /// <summary>Adds one validated USD price row.</summary>
    private static void AddPrice(
        ICollection<AiModelPrice> prices,
        string model,
        string contextWindow,
        string input,
        string cachedInput,
        string cacheWrite,
        string output,
        DateTimeOffset retrievedAt,
        string sourceUrl) =>
        prices.Add(new AiModelPrice(
            "openai",
            model,
            "standard",
            contextWindow,
            "usd",
            ParsePrice(input, required: true)!.Value,
            ParsePrice(cachedInput, required: false),
            ParsePrice(cacheWrite, required: false),
            ParsePrice(output, required: true)!.Value,
            sourceUrl,
            retrievedAt));

    /// <summary>Splits a simple Markdown table row while preserving empty cells.</summary>
    private static IReadOnlyList<string> ParseCells(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed[1..];
        if (trimmed.EndsWith('|')) trimmed = trimmed[..^1];
        return trimmed.Split('|').Select(cell => cell.Trim()).ToArray();
    }

    /// <summary>Removes formatting and context-window qualifiers from model names.</summary>
    private static string NormalizeModelName(string value)
    {
        var normalized = value.Replace("`", string.Empty, StringComparison.Ordinal).Trim();
        var qualifier = normalized.IndexOf(" (", StringComparison.Ordinal);
        return qualifier > 0 ? normalized[..qualifier].Trim() : normalized;
    }

    /// <summary>Parses one dollar-denominated per-million-token amount.</summary>
    private static decimal? ParsePrice(string value, bool required)
    {
        var normalized = value.Trim();
        if (normalized == "-")
        {
            return required
                ? throw new InvalidDataException("OpenAI pricing table has a missing required price.")
                : null;
        }

        if (!normalized.StartsWith('$')
            || !decimal.TryParse(normalized[1..], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)
            || amount < 0)
        {
            throw new InvalidDataException($"Invalid OpenAI price '{value}'.");
        }

        return amount;
    }
}

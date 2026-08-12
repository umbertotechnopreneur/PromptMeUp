// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record AiModelPrice(
    string Provider,
    string Model,
    string ServiceTier,
    string ContextWindow,
    string Currency,
    decimal InputUsdPerMillionTokens,
    decimal? CachedInputUsdPerMillionTokens,
    decimal? CacheWriteUsdPerMillionTokens,
    decimal OutputUsdPerMillionTokens,
    string SourceUrl,
    DateTimeOffset RetrievedAt);

public sealed record OrganizationCost(
    string Id,
    DateTimeOffset BucketStart,
    DateTimeOffset BucketEnd,
    decimal Amount,
    string Currency,
    string? LineItem,
    string? ProjectId,
    DateTimeOffset RetrievedAt);

public sealed record PricingRefreshResult(
    int PriceRows,
    int OrganizationCostRows,
    bool PricesRefreshed,
    bool OrganizationCostsAvailable,
    DateTimeOffset CompletedAt);

public sealed record CostOverview(
    DateTimeOffset? LastPricingSync,
    DateTimeOffset? LastOrganizationCostSync,
    decimal EstimatedCostTodayUsd,
    decimal EstimatedCostCurrentMonthUsd,
    decimal? ActualOrganizationCostCurrentMonthUsd,
    int RequestsToday,
    long InputTokensToday,
    long OutputTokensToday,
    long TotalTokensToday,
    IReadOnlyList<AiModelPrice> Prices);

public sealed record AiRequestSummary(
    decimal EstimatedCostTodayUsd,
    decimal EstimatedCostCurrentMonthUsd,
    int RequestsToday,
    long InputTokensToday,
    long OutputTokensToday,
    long TotalTokensToday);

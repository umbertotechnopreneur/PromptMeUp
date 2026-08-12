// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record AiSessionRecord(
    string Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string Language,
    string Model,
    string Kind,
    string Status,
    string MetadataJson);

public sealed record AiSessionEventRecord(
    string Id,
    string SessionId,
    DateTimeOffset OccurredAt,
    string EventType,
    string PayloadJson);

public sealed record ActivityAuditRecord(
    string Id,
    DateTimeOffset OccurredAt,
    string? SessionId,
    string ActivityType,
    string Outcome,
    string PayloadJson);

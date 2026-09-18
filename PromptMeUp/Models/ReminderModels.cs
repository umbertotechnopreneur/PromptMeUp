// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Represents one locally retained, project-scoped reminder and its explicitly reviewed offset.</summary>
public sealed record Reminder(string Id, string Message, DateTimeOffset DueAt);

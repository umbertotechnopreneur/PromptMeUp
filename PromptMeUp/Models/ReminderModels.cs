// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Represents one locally retained global reminder and its explicitly reviewed offset.</summary>
public sealed record Reminder(string Id, string Message, DateTimeOffset DueAt);

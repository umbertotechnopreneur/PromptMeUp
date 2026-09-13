// SPDX-License-Identifier: MIT

using System.Text;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

internal static class ContextTokenEstimator
{
    /// <summary>Estimates text tokens locally from UTF-8 bytes without claiming provider tokenization accuracy.</summary>
    internal static long Text(string text) => string.IsNullOrEmpty(text)
        ? 0
        : Math.Max(1, (long)Math.Ceiling(Encoding.UTF8.GetByteCount(text) / 4d));

    /// <summary>Includes the same per-message overhead in memory budgeting and provider request estimates.</summary>
    internal static long Messages(IEnumerable<ChatMessage> messages) =>
        messages.Sum(message => Text(message.Content) + 4);
}

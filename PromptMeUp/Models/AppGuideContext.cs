// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Contains bounded, localized product documentation added to the system instructions.</summary>
public sealed record AppGuideContext(IReadOnlyList<string> Topics, string Text, long Tokens)
{
    public static AppGuideContext Empty { get; } = new([], string.Empty, 0);
}

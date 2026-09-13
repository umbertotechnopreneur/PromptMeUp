// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public static class ContextBudgetConfiguration
{
    /// <summary>Loads the bounded ordinary-chat input budget without persisting environment configuration.</summary>
    public static ConversationContextLimits Load(Func<string, string?> readVariable, ILocalizationService text)
    {
        ArgumentNullException.ThrowIfNull(readVariable);
        ArgumentNullException.ThrowIfNull(text);
        var value = readVariable("PROMPTMEUP_CONTEXT_TOKENS");
        if (value is null)
        {
            return ConversationContextLimits.Default;
        }
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var tokens)
            || tokens is < 4_000 or > 200_000)
        {
            throw new InvalidOperationException(text.Text("Context.InvalidBudget", 4_000, 200_000));
        }
        return new ConversationContextLimits(tokens);
    }
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ConversationMemoryTests
{
    /// <summary>Verifies that the oldest complete turn is pruned when the configured turn limit is exceeded.</summary>
    [Fact]
    public void Add_OverTurnLimit_PrunesOldestCompleteTurn()
    {
        var settings = AppSettings.Default with { MaxConversationTurns = 2 };
        var memory = new ConversationMemoryService().Create(settings);
        memory.Add("user", "first question");
        memory.Add("assistant", "first answer");
        memory.Add("user", "second question");
        memory.Add("assistant", "second answer");

        var update = memory.Add("user", "third question");

        Assert.Equal(2, update.PrunedMessages);
        Assert.Equal(2, update.Snapshot.TurnCount);
        Assert.Equal("second question", update.Snapshot.Messages[0].Content);
    }

    /// <summary>Verifies that one overlong message is rejected before it enters short-term memory.</summary>
    [Fact]
    public void Add_OverCharacterLimit_ThrowsConversationLimit()
    {
        var settings = AppSettings.Default with { MaxMessageCharacters = 500 };
        var memory = new ConversationMemoryService().Create(settings);

        Assert.Throws<ConversationLimitException>(() => memory.Add("user", new string('x', 501)));
        Assert.Empty(memory.Snapshot().Messages);
    }
}

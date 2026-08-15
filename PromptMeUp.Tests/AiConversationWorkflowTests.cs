// SPDX-License-Identifier: MIT

using PromptMeUp.Application;

namespace PromptMeUp.Tests;

public sealed class AiConversationWorkflowTests
{
    /// <summary>Verifies exact, argument-bearing, and similarly prefixed run input without executing a command.</summary>
    [Theory]
    [InlineData("/run", true, "")]
    [InlineData("/RUN Get-Location", true, "Get-Location")]
    [InlineData("/run   Get-ChildItem  ", true, "Get-ChildItem")]
    [InlineData("/runner", false, "")]
    public void TryParseRunCommand_InputShape_ReturnsExpectedResult(
        string input,
        bool expectedMatch,
        string expectedCommand)
    {
        var matched = AiConversationWorkflow.TryParseRunCommand(input, out var command);

        Assert.Equal(expectedMatch, matched);
        Assert.Equal(expectedCommand, command);
    }
}

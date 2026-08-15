// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class CommandLineParserTests
{
    /// <summary>Verifies that the two-letter command accepts a natural positional question.</summary>
    [Fact]
    public void Parse_PositionalQuestion_SelectsQueryCommand()
    {
        var result = new CommandLineParser().Parse(["come", "annullo", "un", "commit?"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Query, result.Options!.Command);
        Assert.Equal("come annullo un commit?", result.Options.Query);
    }

    /// <summary>Verifies that explicit query text and its unquoted trailing words form one question.</summary>
    [Fact]
    public void Parse_ExplicitQueryWithTrailingWords_PreservesCompleteQuestion()
    {
        var result = new CommandLineParser().Parse(["--query", "come", "annullo", "un", "commit?"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Query, result.Options!.Command);
        Assert.Equal("come annullo un commit?", result.Options.Query);
    }

    /// <summary>Verifies that inline query text and trailing positional words retain invocation order.</summary>
    [Fact]
    public void Parse_InlineQueryWithTrailingWords_PreservesCompleteQuestion()
    {
        var result = new CommandLineParser().Parse(["prima", "--query=seconda", "terza"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Query, result.Options!.Command);
        Assert.Equal("prima seconda terza", result.Options.Query);
    }

    /// <summary>Verifies that repeated explicit query switches fail instead of merging ambiguous values.</summary>
    [Fact]
    public void Parse_RepeatedExplicitQuery_ReturnsError()
    {
        var result = new CommandLineParser().Parse(["--query", "prima", "--query=seconda"]);

        Assert.False(result.Succeeded);
        Assert.Equal("--query can be specified only once.", result.Error);
    }

    /// <summary>Verifies that two top-level commands cannot be combined ambiguously.</summary>
    [Fact]
    public void Parse_ConflictingCommands_ReturnsError()
    {
        var result = new CommandLineParser().Parse(["--status", "--chat"]);

        Assert.False(result.Succeeded);
        Assert.Contains("cannot be combined", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Verifies language normalization and explicit portable PATH actions.</summary>
    [Fact]
    public void Parse_LanguageAndPath_ReturnsNormalizedOptions()
    {
        var result = new CommandLineParser().Parse(["--path=install", "--language", "VI", "--yes"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Path, result.Options!.Command);
        Assert.Equal("install", result.Options.PathAction);
        Assert.Equal("vi", result.Options.Language);
        Assert.True(result.Options.Yes);
    }
}

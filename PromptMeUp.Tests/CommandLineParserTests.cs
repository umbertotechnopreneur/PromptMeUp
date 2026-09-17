// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class CommandLineParserTests
{
    private readonly CommandLineParser _parser = new(new LocalizationService());

    /// <summary>Verifies the local memory screen accepts interface preferences without a note command-line value.</summary>
    [Fact]
    public void Parse_Memories_SelectsLocalManager()
    {
        var result = _parser.Parse(["--memories", "--language", "it", "--no-emoji"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Memories, result.Options!.Command);
        Assert.Equal("it", result.Options.Language);
        Assert.True(result.Options.NoEmoji);
        Assert.Null(result.Options.Query);
    }

    /// <summary>Rejects ambiguous commands and accidental note text passed to the memory screen switch.</summary>
    [Theory]
    [InlineData("--chat")]
    [InlineData("--setup")]
    [InlineData("a note")]
    public void Parse_MemoriesWithAnotherAction_RejectsInput(string argument)
    {
        var result = _parser.Parse(["--memories", argument]);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }

    /// <summary>Verifies the focused AI settings switch accepts rendering and language preferences.</summary>
    [Fact]
    public void Parse_AiSettings_SelectsFocusedConfiguration()
    {
        var result = _parser.Parse(["--ai-settings", "--no-animation", "--no-emoji", "--language", "it"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.AiSettings, result.Options!.Command);
        Assert.True(result.Options.NoAnimation);
        Assert.True(result.Options.NoEmoji);
        Assert.Equal("it", result.Options.Language);
    }

    /// <summary>Verifies AI settings cannot be mixed with another command or positional text.</summary>
    [Theory]
    [InlineData("--setup")]
    [InlineData("--status")]
    [InlineData("gpt-5.5")]
    public void Parse_AiSettingsWithConflictingArgument_ReturnsError(string argument)
    {
        var result = _parser.Parse(["--ai-settings", argument]);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }

    /// <summary>Verifies that the two-letter command accepts a natural positional question.</summary>
    [Fact]
    public void Parse_PositionalQuestion_SelectsQueryCommand()
    {
        var result = _parser.Parse(["come", "annullo", "un", "commit?"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Query, result.Options!.Command);
        Assert.Equal("come annullo un commit?", result.Options.Query);
    }

    /// <summary>Verifies that explicit query text and its unquoted trailing words form one question.</summary>
    [Fact]
    public void Parse_ExplicitQueryWithTrailingWords_PreservesCompleteQuestion()
    {
        var result = _parser.Parse(["--query", "come", "annullo", "un", "commit?"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Query, result.Options!.Command);
        Assert.Equal("come annullo un commit?", result.Options.Query);
    }

    /// <summary>Verifies that inline query text and trailing positional words retain invocation order.</summary>
    [Fact]
    public void Parse_InlineQueryWithTrailingWords_PreservesCompleteQuestion()
    {
        var result = _parser.Parse(["prima", "--query=seconda", "terza"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Query, result.Options!.Command);
        Assert.Equal("prima seconda terza", result.Options.Query);
    }

    /// <summary>Verifies that repeated explicit query switches fail instead of merging ambiguous values.</summary>
    [Fact]
    public void Parse_RepeatedExplicitQuery_ReturnsError()
    {
        var result = _parser.Parse(["--query", "prima", "--query=seconda"]);

        Assert.False(result.Succeeded);
        Assert.Equal("--query can be specified only once.", result.Error);
    }

    /// <summary>Verifies that two top-level commands cannot be combined ambiguously.</summary>
    [Fact]
    public void Parse_ConflictingCommands_ReturnsError()
    {
        var result = _parser.Parse(["--status", "--chat"]);

        Assert.False(result.Succeeded);
        Assert.Contains("cannot be combined", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Verifies language normalization and explicit portable PATH actions.</summary>
    [Fact]
    public void Parse_LanguageAndPath_ReturnsNormalizedOptions()
    {
        var result = _parser.Parse(["--path=install", "--language", "VI", "--yes"]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Path, result.Options!.Command);
        Assert.Equal("install", result.Options.PathAction);
        Assert.Equal("vi", result.Options.Language);
        Assert.True(result.Options.Yes);
    }

    /// <summary>Verifies that the requested single-dash where alias selects the executable-location command.</summary>
    [Theory]
    [InlineData("-where")]
    [InlineData("--where")]
    public void Parse_WhereAliases_SelectWhereCommand(string argument)
    {
        var result = _parser.Parse([argument]);

        Assert.True(result.Succeeded);
        Assert.Equal(AppCommand.Where, result.Options!.Command);
    }

    /// <summary>Verifies that parser errors follow an explicit supported interface language.</summary>
    [Fact]
    public void Parse_ErrorAfterLanguageOverride_IsLocalized()
    {
        var result = _parser.Parse(["--language", "it", "--sconosciuto"]);

        Assert.False(result.Succeeded);
        Assert.Equal("Argomento sconosciuto '--sconosciuto'. Usa --help per la sintassi dei comandi.", result.Error);
    }
}

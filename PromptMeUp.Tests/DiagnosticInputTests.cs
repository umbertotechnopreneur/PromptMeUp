// SPDX-License-Identifier: MIT

using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class DiagnosticInputTests
{
    /// <summary>Sanitizes evidence but rejects credential-shaped command-line input.</summary>
    [Fact]
    public async Task ReadAsync_RedactsBeforeSharing_ArgumentsAreRejected()
    {
        var service = new BoundedTextInput(new MarkerRedactor(), new LocalizationService());
        Assert.Equal("[removed]", await service.ReadAsync(new StringReader("marker"), 20, CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() => service.Sanitize("marker", 20, fromArgument: true));
    }

    private sealed class MarkerRedactor : ISensitiveDataRedactor
    {
        /// <summary>Substitutes a harmless marker to test the privacy boundary without credential fixtures.</summary>
        public string Redact(string value) => value.Replace("marker", "[removed]", StringComparison.Ordinal);
    }

    /// <summary>Preserves multiline evidence while enforcing a strict size limit.</summary>
    [Fact]
    public async Task ReadAsync_BoundsEvidenceWithoutSilentTruncation()
    {
        var service = new BoundedTextInput(new SensitiveDataRedactor(), new LocalizationService());
        Assert.Equal("a\nb", await service.ReadAsync(new StringReader("a\nb"), 3, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReadAsync(new StringReader("abcd"), 3, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReadAsync(new StringReader(" "), 3, CancellationToken.None));
    }

    /// <summary>Rejects conflicting sources and keeps diagnosis distinct from ordinary query parsing.</summary>
    [Theory]
    [InlineData("--diagnose", "build failed", true)]
    [InlineData("--diagnose --file", "build.log", true)]
    [InlineData("--status --file", "build.log", false)]
    [InlineData("--diagnose --query", "failure", false)]
    public void Parse_DiagnosticSources_AreStrict(string switches, string value, bool succeeds)
    {
        var args = switches.Split(' ').Append(value).ToArray();
        var parsed = new CommandLineParser(new LocalizationService()).Parse(args);
        Assert.Equal(succeeds, parsed.Succeeded);
        if (succeeds)
        {
            Assert.Equal(Models.AppCommand.Diagnose, parsed.Options!.Command);
        }
    }

    /// <summary>Rejects a log file combined with positional evidence.</summary>
    [Fact]
    public void Parse_FileAndText_RejectsAmbiguity()
    {
        Assert.False(new CommandLineParser(new LocalizationService()).Parse(["--diagnose", "--file", "log.txt", "text"]).Succeeded);
    }

    /// <summary>Propagates cancellation without performing provider or process work.</summary>
    [Fact]
    public async Task ReadAsync_CancelledToken_StopsReading()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new BoundedTextInput(new SensitiveDataRedactor(), new LocalizationService());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReadAsync(new StringReader("log"), 10, cancellation.Token));
    }
}

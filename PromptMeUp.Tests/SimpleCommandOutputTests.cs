// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using PromptMeUp.Models;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class SimpleCommandOutputTests
{
    /// <summary>Shows progress before a newline while retaining only the final rewritten row.</summary>
    [Fact]
    public void Progress_RewritesAndErasesCurrentLine()
    {
        var updates = new List<CommandOutputUpdate>();
        var output = new SimpleCommandOutput(false, updates.Add);
        output.Append("copy 10%");
        Assert.Equal("copy 10%", Flatten(Assert.Single(updates)));
        Assert.False(updates[0].CompleteLine);
        output.Append("\rcopy 100%\r\u001b[2Kdone\n");
        Assert.Equal("done", Flatten(updates[^1]));
        Assert.True(updates[^1].CompleteLine);
        Assert.Equal("done", output.Complete());
    }

    /// <summary>Handles controls split across reads without forwarding screen, title, or clipboard instructions.</summary>
    [Fact]
    public void Controls_SplitAtEveryCharacter_AreNormalized()
    {
        var updates = new List<CommandOutputUpdate>();
        var output = new SimpleCommandOutput(true, updates.Add);
        const string source = "\u001b[31;1mred\u001b[0m\u001b[2J\u001b[99A\u001b]52;c;clipboard\a\u001b]0;title\u001b\\ safe\n";
        foreach (var character in source) output.Append(character.ToString());
        Assert.Equal("red safe", output.Complete());
        var last = updates[^1];
        Assert.True(last.IsError);
        Assert.Equal(new CommandOutputSpan("red", 31, true), last.Spans[0]);
        Assert.All(updates.SelectMany(update => update.Spans), span => Assert.DoesNotContain('\u001b', span.Text));
    }

    /// <summary>Preserves newline, Unicode, tabs, and ordinary backspace edits.</summary>
    [Fact]
    public void Text_PreservesOrdinaryConsoleContent()
    {
        var output = new SimpleCommandOutput(false, null);
        output.Append("café Tiếng Việt 😀\r\nabc\bD\tend");
        Assert.Equal("café Tiếng Việt 😀\nabD     end", output.Complete());
    }

    /// <summary>Bounds long newline-free output while retaining the start and final diagnostics.</summary>
    [Fact]
    public void LargeOutput_RetainsBothEnds()
    {
        var updates = new List<CommandOutputUpdate>();
        var output = new SimpleCommandOutput(false, updates.Add);
        output.Append("FIRST\n" + new string('x', 90_000) + "\nLAST error detail");
        var captured = output.Complete();
        Assert.True(output.Truncated);
        Assert.InRange(captured.Length, 1, 32_768);
        Assert.StartsWith("FIRST\n", captured);
        Assert.EndsWith("LAST error detail", captured);
        Assert.Contains("characters omitted", captured);
        Assert.All(updates, update => Assert.True(Flatten(update).Length <= 32_768));
    }

    /// <summary>Discards unbounded escape payloads without retaining them or leaking a suffix to display.</summary>
    [Fact]
    public void OversizedControls_StayBounded()
    {
        var output = new SimpleCommandOutput(false, null);
        output.Append("first\u001b]52;" + new string('x', 100_000));
        output.Append("\a\u001b[" + new string('1', 10_000) + "mfinal");
        Assert.Equal("firstfinal", output.Complete());
    }

    /// <summary>A second reduction for the AI budget still includes the ending.</summary>
    [Fact]
    public void SmallerBudget_PreservesTailAndSurrogatePairs()
    {
        var excerpt = CommandOutputExcerpt.Limit("FIRST😀" + new string('x', 40_000) + "😀LAST", 80);
        Assert.True(excerpt.Length <= 80);
        Assert.StartsWith("FIRST😀", excerpt);
        Assert.EndsWith("😀LAST", excerpt);
        Assert.Contains("omitted", excerpt);
    }

    /// <summary>Never exposes a detached credential suffix after the rolling buffer loses its key.</summary>
    [Fact]
    public void TruncatedRawLine_DoesNotKeepDetachedCredentialSuffix()
    {
        var buffer = new CommandOutputExcerpt();
        buffer.Append("password=" + new string('s', 70_000) + "private-suffix\nfinal diagnostic");
        var captured = buffer.ToString();
        Assert.DoesNotContain("private-suffix", captured);
        Assert.EndsWith("final diagnostic", captured);
        Assert.DoesNotContain(new string('s', 20), new SensitiveDataRedactor().Redact(captured));
    }

    /// <summary>Fitting an omission marker cannot remove a retained credential key while keeping its value.</summary>
    [Fact]
    public void RawTailBudget_DropsPartialLineAfterMarkerReservation()
    {
        var buffer = new CommandOutputExcerpt();
        buffer.Append(new string('x', 50_000) + "\npassword=private-suffix\n" + new string('z', 21_800) + "\nfinal diagnostic");
        var captured = buffer.ToString();
        var redacted = new SensitiveDataRedactor().Redact(captured);
        Assert.InRange(captured.Length, 1, 32_768);
        Assert.DoesNotContain("private-suffix", redacted);
        Assert.EndsWith("final diagnostic", redacted);
    }

    /// <summary>Combines the visible spans in a test update.</summary>
    /// <param name="update">The normalized command line.</param>
    private static string Flatten(CommandOutputUpdate update) => string.Concat(update.Spans.Select(span => span.Text));
}

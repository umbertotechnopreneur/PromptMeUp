// SPDX-License-Identifier: MIT

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Application;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Tests;

public sealed class MemoryCommandTests
{
    /// <summary>Routes flags and the formerly misleading slash invocations to local memory workflows.</summary>
    [Theory]
    [InlineData("--remember", AppCommand.Remember)]
    [InlineData("/remember", AppCommand.Remember)]
    [InlineData("--forget", AppCommand.Forget)]
    [InlineData("/forget", AppCommand.Forget)]
    public void Parser_MemoryCommand_PreservesAllWords(string flag, AppCommand command)
    {
        var result = new CommandLineParser(new LocalizationService()).Parse([flag, "global", "Prefer", "short answers."]);
        Assert.True(result.Succeeded);
        Assert.Equal(command, result.Options!.Command);
        Assert.Equal("global Prefer short answers.", result.Options.Query);
    }

    /// <summary>Rejects missing memory text, duplicate operations, and mixed AI query invocations.</summary>
    [Theory]
    [InlineData("--remember")]
    [InlineData("--forget")]
    [InlineData("--remember", "note", "--forget", "note")]
    [InlineData("--remember", "note", "--remember", "note")]
    [InlineData("--remember", "note", "--query", "question")]
    public void Parser_InvalidMemoryCommand_Fails(params string[] args) =>
        Assert.False(new CommandLineParser(new LocalizationService()).Parse(args).Succeeded);

    /// <summary>Saving persists the note in the manager's store and exact-ID deletion needs no model.</summary>
    [Fact]
    public async Task Remember_ThenForgetById_PersistsAndDeletesWithoutAi()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = Store(fixture);
        var successes = new List<string>();
        var workflow = Workflow(fixture, service, successes, _ => throw new InvalidOperationException("Unexpected AI call."),
            matches => Assert.Single(matches).Id);
        var settings = AppSettings.Default with { AiEnabled = false };

        Assert.Equal(0, await workflow.RunAsync(Options("--remember", "global Prefer concise answers."), settings, default));
        var saved = Assert.Single(await Store(fixture).ListAsync(default));
        Assert.True(saved.IsGlobal);
        Assert.Equal("Prefer concise answers.", saved.Text);
        Assert.Contains(successes, message => message.Contains(saved.Id, StringComparison.Ordinal));
        Assert.Equal(0, await workflow.RunAsync(Options("--forget", saved.Id), settings, default));
        Assert.Empty(await service.ListAsync(default));
        Assert.Equal(2, successes.Count);
    }

    /// <summary>All lookup batches finish before the user chooses and confirms one of their combined matches.</summary>
    [Fact]
    public async Task Forget_Description_ShowsAllBatchesAndDeletesOnlyChosenNote()
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = Store(fixture);
        for (var index = 0; index < 17; index++)
        {
            await service.RememberAsync($"Synthetic preference {index}.", true, default);
        }
        var calls = 0;
        string? selectedId = null;
        var workflow = Workflow(fixture, service, [], messages =>
        {
            calls++;
            using var request = JsonDocument.Parse(Assert.Single(messages).Content);
            var notes = request.RootElement.GetProperty("memories");
            Assert.InRange(notes.GetArrayLength(), 1, 8);
            return JsonSerializer.Serialize(new { matched_ids = new[] { notes[0].GetProperty("id").GetString() } });
        }, matches =>
        {
            Assert.Equal(3, calls);
            Assert.Equal(3, matches.Count);
            selectedId = matches[2].Id;
            return selectedId;
        });

        Assert.Equal(0, await workflow.RunAsync(Options("--forget", "a synthetic preference"),
            AppSettings.Default with { SetupCompleted = true }, default));
        var remaining = await service.ListAsync(default);
        Assert.Equal(16, remaining.Count);
        Assert.DoesNotContain(remaining, note => note.Id == selectedId);
    }

    /// <summary>Cancellation and a concurrent edit cannot produce a deletion or a success message.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Forget_CancelledOrChanged_PreservesNote(bool changeNote)
    {
        using var fixture = new RegressionFixture();
        await fixture.Database.InitializeAsync(default);
        var service = Store(fixture);
        var saved = await service.RememberAsync("Original note.", true, default);
        var successes = new List<string>();
        var workflow = Workflow(fixture, service, successes, _ => throw new InvalidOperationException("Unexpected AI call."), matches =>
        {
            if (!changeNote)
            {
                return null;
            }
            service.UpdateAsync(saved.Id, "Changed note.", true, default).GetAwaiter().GetResult();
            return Assert.Single(matches).Id;
        });

        Assert.Equal(changeNote ? 1 : 0, await workflow.RunAsync(Options("--forget", saved.Id), AppSettings.Default, default));
        Assert.Equal(changeNote ? "Changed note." : saved.Text, Assert.Single(await service.ListAsync(default)).Text);
        Assert.Empty(successes);
    }

    /// <summary>Malformed, fabricated, duplicate, and extra-field model output cannot become deletion candidates.</summary>
    [Theory]
    [InlineData("not JSON")]
    [InlineData("[]")]
    [InlineData("{\"matched_ids\":null}")]
    [InlineData("{\"matched_ids\":[\"unknown\"]}")]
    [InlineData("{\"matched_ids\":[\"known\",\"known\"]}")]
    [InlineData("{\"matched_ids\":[],\"deleted\":true}")]
    public void ParseMatches_InvalidResponse_Rejects(string json) =>
        Assert.Throws<JsonException>(() => MemoryCommandWorkflow.ParseMatches(json,
            [new PersistentMemory("known", "A note.", true, DateTimeOffset.UtcNow)]));

    /// <summary>Builds parsed options exactly as a terminal invocation supplies them.</summary>
    private static CommandLineOptions Options(string flag, string value) =>
        new CommandLineParser(new LocalizationService()).Parse([flag, value]).Options!;

    /// <summary>Opens the same isolated store used by the manager and command workflows.</summary>
    private static PersistentMemoryService Store(RegressionFixture fixture) => new(
        fixture.Paths, new SensitiveDataRedactor(), new LocalizationService(), NullLogger<PersistentMemoryService>.Instance);

    /// <summary>Connects production persistence to synthetic AI responses and explicit user-selection callbacks.</summary>
    private static MemoryCommandWorkflow Workflow(RegressionFixture fixture, PersistentMemoryService service,
        List<string> successes, Func<IReadOnlyList<ChatMessage>, string> respond,
        Func<IReadOnlyList<PersistentMemory>, string?> select)
    {
        var text = new LocalizationService();
        var ai = TestProxy.Create<IOpenAiService>((method, args) => method.Name == "SendAsync"
            ? Task.FromResult(new AiResponse("synthetic", "synthetic", respond((IReadOnlyList<ChatMessage>)args[2]!),
                new(0, 0, 0, 0, 0, 0), new(0, 0, 0, 0, 0, 0, false), null, null, 200, 0, null))
            : throw new NotSupportedException(method.Name));
        var shell = TestProxy.Create<IConsoleShellView>((method, args) =>
        {
            if (method.Name == "RunWithStatusAsync")
            {
                return ((Func<Task<AiResponse>>)args[1]!)();
            }
            if (method.Name == "RenderSuccess")
            {
                successes.Add((string)args[0]!);
            }
            return null;
        });
        var view = TestProxy.Create<IMemoryView>((_, _) => null);
        var forgetView = TestProxy.Create<IMemoryForgetView>((_, args) => select((IReadOnlyList<PersistentMemory>)args[0]!));
        return new MemoryCommandWorkflow(service, ai, fixture.Audit,
            new ApplicationActivityRecorder(fixture.Audit, NullLogger<ApplicationActivityRecorder>.Instance),
            new BoundedTextInput(new SensitiveDataRedactor(), text), fixture.Secrets, shell, view, forgetView, text);
    }
}

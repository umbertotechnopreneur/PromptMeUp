// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class GlobalMemoryViewTests
{
    /// <summary>The unified workspace creates one global saved note directly from its central editor.</summary>
    [Fact]
    public void Choose_CreateNote_ReturnsReviewedText()
    {
        var harness = Create("en", Choose(0).Concat(Type("Prefer short answers.")));
        var view = new MemoryManagerView(harness.Console, harness.Text, harness.Shell);

        var selection = view.Choose([], MemoryProposalWorkspace.Disabled);

        Assert.Equal(MemoryManagerAction.Create, selection.Action);
        Assert.Equal("Prefer short answers.", selection.Text);
        Assert.Empty(harness.Keys);
    }

    /// <summary>The unified workspace edits an existing note without opening a second editor screen.</summary>
    [Fact]
    public void Choose_ExistingNote_ReturnsInlineEdit()
    {
        var harness = Create("en", Choose(2).Concat(Choose(1)).Concat(Type("Revised draft text.")));
        var view = new MemoryManagerView(harness.Console, harness.Text, harness.Shell);
        var memory = Note(false);

        var selection = view.Choose([memory], MemoryProposalWorkspace.Disabled);

        Assert.Equal(MemoryManagerAction.Edit, selection.Action);
        Assert.Equal(memory.Id, selection.Id);
        Assert.Equal("Revised draft text.", selection.Text);
        Assert.Empty(harness.Keys);
    }

    /// <summary>Saved-memory surfaces omit removed scope metadata in all supported languages.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public void SavedMemorySurfaces_AllLanguages_OmitScopeMetadata(string language)
    {
        var harness = Create(language, Choose(2).Concat(Choose(0)).Concat(Choose(4)));
        var view = new MemoryManagerView(harness.Console, harness.Text, harness.Shell);
        var notes = new[] { Note(false), Note(true) with { Id = new string('b', 32), Text = "Another saved note." } };

        var selection = view.Choose(notes, MemoryProposalWorkspace.Disabled);

        Assert.Equal(MemoryManagerAction.Close, selection.Action);
        new MemoryView(harness.Console, harness.Text).Render(notes);
        var deletion = Create(language, [new ConsoleKeyInfo('0', ConsoleKey.D0, false, false, false)]);
        Assert.Null(new MemoryForgetView(deletion.Console, deletion.Text).SelectForDeletion(notes));
        Assert.Contains(notes[0].Text, harness.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains(notes[0].Id, harness.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains(notes[1].Text, deletion.Output.ToString(), StringComparison.Ordinal);
        Assert.Empty(harness.Keys);
        Assert.Empty(deletion.Keys);
    }

    /// <summary>Provides deterministic legacy and current note data without creating or modifying storage.</summary>
    private static PersistentMemory Note(bool global) => new(new string('a', 32), "Prefer concise answers.", global,
        new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

    /// <summary>Creates passive views with in-memory input and rejects old scope labels.</summary>
    private static Harness Create(string language, IEnumerable<ConsoleKeyInfo> inputKeys)
    {
        var keys = new Queue<ConsoleKeyInfo>(inputKeys);
        var input = TestProxy.Create<IAnsiConsoleInput>((method, _) => method.Name switch
        {
            "ReadKey" => keys.Dequeue(),
            "ReadKeyAsync" => Task.FromResult<ConsoleKeyInfo?>(keys.Dequeue()),
            "IsKeyAvailable" => keys.Count > 0,
            _ => throw new NotSupportedException(method.Name)
        });
        var output = new StringWriter();
        var rendering = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(output)
        });
        rendering.Profile.Width = 160;
        rendering.Profile.Capabilities.Interactive = true;
        rendering.Profile.Capabilities.AlternateBuffer = false;
        var console = TestProxy.Create<IAnsiConsole>((method, args) => method.Name == "get_Input" ? input : method.Invoke(rendering, args));
        var localization = new LocalizationService();
        localization.SetLanguage(language);
        var text = TestProxy.Create<ILocalizationService>((method, args) =>
        {
            if (method.Name == "Text")
            {
                Assert.DoesNotContain((string)args![0]!, new[] { "Memory.Scope", "Memory.Global", "Memory.Project", "MemoryManager.ScopeHelp" });
            }
            return method.Invoke(localization, args);
        });
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(true, true));
        return new(console, text, shell, output, keys);
    }

    /// <summary>Selects one scrolling-menu action with fake navigation keys.</summary>
    private static IEnumerable<ConsoleKeyInfo> Choose(int index) =>
        Enumerable.Repeat(Key(ConsoleKey.DownArrow), index).Append(Key(ConsoleKey.Enter));

    /// <summary>Enters a note and confirms it without touching the real terminal.</summary>
    private static IEnumerable<ConsoleKeyInfo> Type(string value) => value.Select(character =>
        new ConsoleKeyInfo(character, (ConsoleKey)0, false, false, false)).Append(Key(ConsoleKey.Enter));

    /// <summary>Creates one ordinary navigation or confirmation event.</summary>
    private static ConsoleKeyInfo Key(ConsoleKey key) => new(key == ConsoleKey.Enter ? '\r' : '\0', key, false, false, false);

    private sealed record Harness(IAnsiConsole Console, ILocalizationService Text, ConsoleShellView Shell,
        StringWriter Output, Queue<ConsoleKeyInfo> Keys);
}

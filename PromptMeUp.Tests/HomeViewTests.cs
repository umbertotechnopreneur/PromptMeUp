// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class HomeViewTests
{
    /// <summary>Accepts each destination immediately, including digits typed with Shift on international keyboards.</summary>
    [Theory]
    [InlineData('1', HomeAction.Chat, false)]
    [InlineData('2', HomeAction.Script, false)]
    [InlineData('3', HomeAction.Diagnose, true)]
    [InlineData('4', HomeAction.Memories, false)]
    [InlineData('5', HomeAction.Skills, false)]
    [InlineData('6', HomeAction.Settings, false)]
    [InlineData('7', HomeAction.Help, false)]
    [InlineData('0', HomeAction.Exit, false)]
    public async Task NumberSelectsWithoutEnter(char digit, HomeAction expected, bool shift)
    {
        var keys = new Queue<ConsoleKeyInfo>([new(digit, (ConsoleKey)((int)ConsoleKey.D0 + digit - '0'), shift, false, false)]);
        var view = CreateView(keys);

        Assert.Equal(expected, await view.ChooseAsync(default));
        Assert.Empty(keys);
    }

    /// <summary>Ignores unrelated keys and modified shortcuts rather than entering an unintended workflow.</summary>
    [Fact]
    public async Task InvalidKeysDoNotSelectAndEscapeExits()
    {
        var keys = new Queue<ConsoleKeyInfo>([
            new('x', ConsoleKey.X, false, false, false),
            new('\r', ConsoleKey.Enter, false, false, false),
            new('2', ConsoleKey.D2, false, false, true),
            new('\u001b', ConsoleKey.Escape, false, false, false)]);
        var view = CreateView(keys);

        Assert.Equal(HomeAction.Exit, await view.ChooseAsync(default));
        Assert.Empty(keys);
    }

    /// <summary>Provides deterministic keyboard input without opening a terminal or allowing an implicit Enter.</summary>
    private static HomeView CreateView(Queue<ConsoleKeyInfo> keys)
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter())
        });
        console.Profile.Width = 80;
        var input = TestProxy.Create<IAnsiConsoleInput>((method, _) => method.Name == "ReadKeyAsync"
            ? Task.FromResult<ConsoleKeyInfo?>(keys.Dequeue())
            : throw new InvalidOperationException("Unexpected keyboard operation: " + method.Name));
        var wrapped = TestProxy.Create<IAnsiConsole>((method, args) => method.Name == "get_Input"
            ? input : method.Invoke(console, args));
        var text = new LocalizationService();
        var shell = TestProxy.Create<IConsoleShellView>((method, _) => method.Name == "get_Options"
            ? new ConsoleRenderOptions(NoAnimation: true, NoEmoji: true)
            : throw new InvalidOperationException("Unexpected shell operation: " + method.Name));
        return new HomeView(wrapped, text, shell);
    }
}

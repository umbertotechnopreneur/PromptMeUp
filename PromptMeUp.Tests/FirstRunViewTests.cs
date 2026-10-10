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
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class FirstRunViewTests
{
    /// <summary>Renders the real welcome and first selection, including the formerly invalid untitled divider.</summary>
    [Theory]
    [InlineData("it")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public async Task WelcomeAndLanguageSelectionRemainInteractive(string language)
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(output)
        });
        console.Profile.Width = 100;
        console.Profile.Height = 40;
        console.Profile.Capabilities.Interactive = true;
        var keys = new Queue<ConsoleKeyInfo>([new('\r', ConsoleKey.Enter, false, false, false)]);
        var input = TestProxy.Create<IAnsiConsoleInput>((method, _) => method.Name switch
        {
            "ReadKeyAsync" => Task.FromResult<ConsoleKeyInfo?>(keys.Dequeue()),
            "IsKeyAvailable" => keys.Count > 0,
            _ => throw new InvalidOperationException("Unexpected keyboard operation: " + method.Name)
        });
        var wrapped = TestProxy.Create<IAnsiConsole>((method, args) => method.Name == "get_Input"
            ? input : method.Invoke(console, args));
        var text = new LocalizationService();
        text.SetLanguage(language);
        var shell = TestProxy.Create<IConsoleShellView>((method, _) => method.Name == "get_Options"
            ? new ConsoleRenderOptions(NoAnimation: true, NoEmoji: false)
            : throw new InvalidOperationException("Unexpected shell operation: " + method.Name));
        var view = new FirstRunView(wrapped, text, new SensitiveDataRedactor(), shell);

        view.RenderWelcome();
        var answer = await view.ChooseLanguageAsync(language, default);

        Assert.Equal(FirstRunAction.Next, answer.Action);
        Assert.Equal(language, answer.Value);
        Assert.Empty(keys);
        Assert.Contains(text.Text("Oobe.Welcome"), output.ToString(), StringComparison.Ordinal);
    }
}

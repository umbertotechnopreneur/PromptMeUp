// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class PreferredNameViewTests
{
    /// <summary>The existing settings form normalizes edits and permits clearing a previously saved name.</summary>
    [Theory]
    [InlineData("", "  Rene\u0301  ", "René")]
    [InlineData("Previous name", "", "")]
    [InlineData("Previous name", "Morgan", "Morgan")]
    [InlineData("", "password=synthetic\rMorgan", "Morgan")]
    public void SettingsPersonalization_EditName_UsesExistingDraftForm(string current, string entered, string expected)
    {
        var input = new List<ConsoleKeyInfo> { Key(ConsoleKey.Enter) };
        input.AddRange(Type(entered));
        input.Add(Key(ConsoleKey.Enter));
        input.AddRange(Enumerable.Repeat(Key(ConsoleKey.DownArrow), 15));
        input.Add(Key(ConsoleKey.Enter));
        var (console, output, keys) = CreateConsole(input);
        var text = new LocalizationService();
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(true, true));
        var themes = TestProxy.Create<IThemeCatalogService>((method, _) => method.Name switch
        {
            "Resolve" => TerminalThemeDefinition.Default,
            "get_Themes" => new[] { TerminalThemeDefinition.Default },
            _ => throw new NotSupportedException(method.Name)
        });
        var about = TestProxy.Create<IAboutView>((method, _) => throw new NotSupportedException(method.Name));
        var original = AppSettings.Default with { AiEnabled = false, PreferredName = current };
        var view = new FullscreenSetupView(console, text, shell, new PromptInjectionProtectionService(),
            new SensitiveDataRedactor(), themes, about, new ScriptLanguageCatalog());

        var submission = view.Collect(new SetupViewState(original, false, false)
        {
            InitialSection = SettingsSection.Personalization
        });

        Assert.NotNull(submission);
        Assert.Equal(expected, submission.Settings.PreferredName);
        Assert.Equal(current, original.PreferredName);
        Assert.False(submission.TestConnection);
        Assert.Empty(keys);
        Assert.Contains(text.Text("Setup.PreferredName"), output.ToString(), StringComparison.Ordinal);
        Assert.Contains("sent to OpenAI", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Leave empty to disable", output.ToString(), StringComparison.Ordinal);
        if (entered.Contains('\r'))
        {
            Assert.Contains(text.Text("Setup.PreferredNameInvalid"), output.ToString(), StringComparison.Ordinal);
        }
    }

    /// <summary>The ordinary prompt-form default remains unchanged for unrelated settings fields.</summary>
    [Fact]
    public void PromptForm_DefaultBehavior_PreservesOtherFields()
    {
        var current = "Keep this value";
        var (console, _, keys) = CreateConsole([
            Key(ConsoleKey.Enter), Key(ConsoleKey.Enter),
            Key(ConsoleKey.DownArrow), Key(ConsoleKey.DownArrow), Key(ConsoleKey.Enter)]);
        var form = new SettingsPromptForm(console, new LocalizationService(), new ConsoleRenderOptions(true, true));
        var field = new FormField("example", "Setup.Custom", () => current, value => current = value);

        var saved = form.Run([new FormPage("Settings.Personalization", [field])], 0, () => null);

        Assert.True(saved);
        Assert.True(field.DefaultToCurrentValue);
        Assert.Equal("Keep this value", current);
        Assert.Empty(keys);
    }

    /// <summary>Every supported locale explains local storage, provider transmission, the limit, and the optional field.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public void PreferredNameCopy_AllLanguages_HaveCompleteHelp(string language)
    {
        var text = new LocalizationService();
        text.SetLanguage(language);

        Assert.NotEmpty(text.Text("Setup.PreferredName"));
        Assert.Contains("OpenAI", text.Text("Setup.PreferredNameHelp"), StringComparison.Ordinal);
        Assert.Contains("80", text.Text("Setup.PreferredNameHelp"), StringComparison.Ordinal);
        Assert.Contains("80", text.Text("Setup.PreferredNameInvalid"), StringComparison.Ordinal);
        Assert.NotEmpty(text.Text("Setup.ChangePreferredName"));
    }

    /// <summary>Creates in-memory interactive input while keeping all real terminal and environment state untouched.</summary>
    private static (IAnsiConsole Console, StringWriter Output, Queue<ConsoleKeyInfo> Keys) CreateConsole(IEnumerable<ConsoleKeyInfo> inputKeys)
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
        rendering.Profile.Width = 240;
        rendering.Profile.Capabilities.Interactive = true;
        rendering.Profile.Capabilities.AlternateBuffer = false;
        var console = TestProxy.Create<IAnsiConsole>((method, args) => method.Name == "get_Input"
            ? input : method.Invoke(rendering, args));
        return (console, output, keys);
    }

    /// <summary>Produces literal typed input with carriage returns mapped to normal confirmation keys.</summary>
    private static IEnumerable<ConsoleKeyInfo> Type(string value) => value.Select(character =>
        character == '\r' ? Key(ConsoleKey.Enter) : new ConsoleKeyInfo(character, (ConsoleKey)0, false, false, false));

    /// <summary>Produces one navigation key without reading the real console.</summary>
    private static ConsoleKeyInfo Key(ConsoleKey key) =>
        new(key == ConsoleKey.Enter ? '\r' : '\0', key, false, false, false);
}

// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class TerminalViewTests
{
    /// <summary>Verifies that icon labels have no leading space and keep a separator after emoji or ASCII prefixes.</summary>
    [Fact]
    public void IconPrefix_OmitsLeadingSpace()
    {
        Assert.Equal("🇮🇹 ", TerminalTheme.IconPrefix(new ConsoleRenderOptions(false, false), "🇮🇹", "@"));
        Assert.Equal("@\u00A0", TerminalTheme.IconPrefix(new ConsoleRenderOptions(false, true), "🇮🇹", "@"));
    }

    /// <summary>Verifies that every setup language choice starts with its spaced country flag.</summary>
    [Fact]
    public void SetupLanguageChoices_RenderCountryFlags()
    {
        var options = new ConsoleRenderOptions(NoAnimation: true, NoEmoji: false);

        foreach (var language in SupportedLanguages.All)
        {
            var rendered = FullscreenSetupView.LanguageLabel(language, options);
            Assert.StartsWith($"{language.Flag} {language.NativeName}", rendered, StringComparison.Ordinal);
        }
    }

    /// <summary>Verifies the chat intro stays short while help preserves the full localized command guide.</summary>
    [Fact]
    public void ChatIntro_OffersCompactShortcutsAndFullHelpWithoutCards()
    {
        var (console, output) = CreateConsole();
        var text = new LocalizationService();
        text.SetLanguage("it");
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(NoAnimation: true, NoEmoji: false));
        var view = new ChatView(console, text, new PoorMarkdownRenderer(console), shell);

        view.RenderIntro();

        var intro = output.ToString();
        Assert.Contains("/help", intro, StringComparison.Ordinal);
        Assert.DoesNotContain("/clear", intro, StringComparison.Ordinal);
        Assert.DoesNotContain("/remember", intro, StringComparison.Ordinal);

        view.RenderCommandGuide();

        var rendered = output.ToString();
        Assert.Contains("/run <comando>", rendered, StringComparison.Ordinal);
        Assert.Contains("/clear", rendered, StringComparison.Ordinal);
        Assert.Contains("/costs", rendered, StringComparison.Ordinal);
        Assert.Contains("/status", rendered, StringComparison.Ordinal);
        Assert.Contains("/context", rendered, StringComparison.Ordinal);
        Assert.Contains("/remember <testo>", rendered, StringComparison.Ordinal);
        Assert.Contains("/memories", rendered, StringComparison.Ordinal);
        Assert.Contains("/forget <id>", rendered, StringComparison.Ordinal);
        Assert.Contains("Memorie in chat:", rendered, StringComparison.Ordinal);
        Assert.Contains("/exit", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("╭", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("╮", rendered, StringComparison.Ordinal);
    }

    /// <summary>Verifies that teletype rendering preserves formatted content without exposing Markdown markers.</summary>
    [Fact]
    public void MarkdownAnimation_RendersContentWithoutRawMarkers()
    {
        var (console, output) = CreateConsole();
        var renderer = new PoorMarkdownRenderer(console);

        renderer.RenderAnimated("Testo **importante** con `codice`.", CancellationToken.None);

        var rendered = StripAnsi(output.ToString());
        Assert.Contains("Testo importante con codice.", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("**", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("`", rendered, StringComparison.Ordinal);
    }

    /// <summary>Checks that wrapping retains every word and Unicode grapheme with identical static and animated output.</summary>
    [Theory]
    [InlineData(32)]
    [InlineData(200)]
    public void MarkdownWrapping_PreservesContentAcrossTerminalWidths(int width)
    {
        var (console, output) = CreateConsole();
        console.Profile.Width = width;
        var renderer = new PoorMarkdownRenderer(console);
        var markdown = string.Join(" ", Enumerable.Repeat("Testo **importante** con `codice`.", 8)) + "\n\nCaffè 👩‍💻.";
        var expected = markdown.Replace("**", string.Empty).Replace("`", string.Empty);

        renderer.Render(markdown);
        var rendered = StripAnsi(output.ToString());
        Assert.Equal(Regex.Replace(expected, @"\s+", " ").Trim(), Regex.Replace(rendered, @"\s+", " ").Trim());
        Assert.Contains("👩‍💻", rendered, StringComparison.Ordinal);
        Assert.All(rendered.Split('\n'), line => Assert.True(line.TrimEnd('\r').Length <= Math.Min(108, width - 1)));

        output.GetStringBuilder().Clear();
        renderer.RenderAnimated(markdown, CancellationToken.None);
        Assert.Equal(rendered, StripAnsi(output.ToString()));
    }

    /// <summary>Verifies that status data uses frameless localized label-value rows.</summary>
    [Fact]
    public void StatusView_RendersFramelessLabelValueGrid()
    {
        var (console, output) = CreateConsole();
        var text = new LocalizationService();
        text.SetLanguage("it");
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(NoAnimation: true, NoEmoji: false));
        var status = new AppStatus(
            AppSettings.Default with { Language = "it" },
            HasApiKey: true,
            HasAdminKey: false,
            LastPricingSync: null,
            DatabasePath: "data.db",
            LogsDirectory: "logs",
            PromptDirectory: "prompt",
            PromptCount: 4);

        new StatusView(console, text, shell).Render(status);

        var rendered = output.ToString();
        Assert.Contains("Configurazione:", rendered, StringComparison.Ordinal);
        Assert.Contains("Chiave API:", rendered, StringComparison.Ordinal);
        Assert.Contains("data.db", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("╭", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("╮", rendered, StringComparison.Ordinal);
    }

    /// <summary>Verifies that the Nerd Font dry run is localized and suppresses internal English service copy.</summary>
    [Fact]
    public void NerdFontDryRun_RendersLocalizedCopyOnly()
    {
        var (console, output) = CreateConsole();
        var text = new LocalizationService();
        text.SetLanguage("it");
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(NoAnimation: true, NoEmoji: false));
        var view = new NerdFontView(console, text, shell);

        Assert.True(view.PreviewAndConfirm(dryRun: true, preauthorized: true));
        view.RenderResult(new FontInstallResult(
            Changed: false,
            DryRun: true,
            FontName: "JetBrainsMono Nerd Font",
            Message: "Would run: internal command"));

        var rendered = output.ToString();
        Assert.Contains("SIMULAZIONE", rendered, StringComparison.Ordinal);
        Assert.Contains("nessuna modifica al sistema", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Would run", rendered, StringComparison.Ordinal);
    }

    /// <summary>Verifies that command-result metadata uses the shared frameless label-value grid.</summary>
    [Fact]
    public void CommandResult_RendersFramelessAlignedMetadata()
    {
        var (console, output) = CreateConsole();
        var text = new LocalizationService();
        text.SetLanguage("it");
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(NoAnimation: true, NoEmoji: false));
        var view = new CommandAuthorizationView(console, text, new PoorMarkdownRenderer(console), shell, new CommandClipboard());

        view.RenderExecutionResult(new CommandExecutionResult(
            Command: "git branch --all",
            ExitCode: 1,
            StandardOutput: string.Empty,
            StandardError: "fatal: not a git repository",
            TimedOut: false,
            OutputTruncated: false,
            ElapsedMilliseconds: 750));

        var rendered = output.ToString();
        Assert.Contains("Codice uscita:", rendered, StringComparison.Ordinal);
        Assert.Contains("Durata:", rendered, StringComparison.Ordinal);
        Assert.Contains("Troncato:", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("╭", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("╮", rendered, StringComparison.Ordinal);
    }

    /// <summary>Verifies the copy choice transfers the exact previewed command and never authorizes execution.</summary>
    [Fact]
    public async Task CommandPreview_CopyChoice_DoesNotAuthorizeExecution()
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(new StringWriter())
        });
        var text = new LocalizationService();
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(NoAnimation: true, NoEmoji: true));
        string? copied = null;
        var clipboard = TestProxy.Create<ICommandClipboard>((method, args) =>
        {
            if (method.Name != "TryCopy") throw new NotSupportedException(method.Name);
            copied = (string?)args![0];
            return true;
        });
        var view = new CommandAuthorizationView(console, text, new PoorMarkdownRenderer(console), shell, clipboard,
            () => CommandAuthorizationView.CommandDecision.Copy);
        const string command = "git status --short && echo café";
        view.RenderPreview(command, new CommandRiskAssessment(5, CommandRiskLevel.Low, "Safe preview.", false, null));

        var authorized = await view.AuthorizeAsync(CommandExecutionMode.Confirm, CancellationToken.None);

        Assert.False(authorized);
        Assert.Equal(command, copied);
    }

    /// <summary>Verifies that the inline command menu consumes arrow and Enter keys without a live renderer.</summary>
    [Fact]
    public void ConversationMenu_AcceptsKeyboardChoice()
    {
        var keys = new Queue<ConsoleKeyInfo>(
        [
            new('\0', ConsoleKey.DownArrow, false, false, false),
            new('\r', ConsoleKey.Enter, false, false, false)
        ]);
        var input = TestProxy.Create<IAnsiConsoleInput>((method, _) => method.Name switch
        {
            "IsKeyAvailable" => keys.Count > 0,
            "ReadKey" => keys.Dequeue(),
            "ReadKeyAsync" => Task.FromResult<ConsoleKeyInfo?>(keys.Dequeue()),
            _ => throw new NotSupportedException(method.Name)
        });
        var output = new StringWriter();
        var rendering = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(output)
        });
        var console = TestProxy.Create<IAnsiConsole>((method, args) =>
            method.Name == "get_Input" ? input : method.Invoke(rendering, args));

        var selected = TerminalConversationPrompt.SelectInteractive(console, new LocalizationService(),
        [
            new TerminalMenuChoice<int>(0, "Stop"),
            new TerminalMenuChoice<int>(1, "Inspect git status", "git status")
        ], "Choose a command", numbered: true);

        Assert.Equal(1, selected);
        Assert.Empty(keys);
        Assert.Contains("Inspect git status", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Creates a deterministic colorless Spectre console backed by an in-memory writer.</summary>
    private static (IAnsiConsole Console, StringWriter Output) CreateConsole()
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(output)
        });
        return (console, output);
    }

    /// <summary>Removes ANSI control sequences so assertions verify semantic text independently of the host terminal.</summary>
    private static string StripAnsi(string value) =>
        Regex.Replace(value, @"\x1B\[[0-?]*[ -/]*[@-~]", string.Empty, RegexOptions.CultureInvariant);
}

// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class ShellBannerTests
{
    /// <summary>Verifies that help ends with the shared banner and process exit does not duplicate it.</summary>
    [Theory]
    [InlineData(48, true)]
    [InlineData(120, false)]
    public void HelpThenExit_RendersOneCompleteProjectBanner(int width, bool noEmoji)
    {
        var (console, output, text, shell) = CreateConsole(width, noEmoji);

        new HelpView(console, text, shell).Render();
        var help = output.ToString();
        shell.RenderFooter();

        Assert.Equal(help, output.ToString());
        var compact = Compact(help);
        Assert.Contains(Compact(text.Text("Footer.Thanks")), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Footer.Support")), compact, StringComparison.Ordinal);
        Assert.Contains("https://github.com/umbertotechnopreneur/PromptMeUp", compact, StringComparison.Ordinal);
        Assert.Contains("Copyright(c)umbertogiacobbi.biz", compact, StringComparison.Ordinal);
        Assert.True(help.LastIndexOf("--dry-run", StringComparison.Ordinal) < help.IndexOf("hm · help me", StringComparison.Ordinal));
    }

    /// <summary>Verifies that a caller-supplied path is shown in full even when it contains markup-like brackets.</summary>
    [Theory]
    [InlineData(48, true)]
    [InlineData(120, false)]
    public void Header_RendersCurrentDirectoryAsLiteralText(int width, bool noEmoji)
    {
        var (_, output, text, shell) = CreateConsole(width, noEmoji);
        const string directory = "/workspace/[draft]/project with spaces/subdirectory";

        shell.RenderHeader("query", AppSettings.Default with { Language = "it" }, true, directory);

        var compact = Compact(output.ToString());
        Assert.Contains(Compact(text.Text("Shell.CurrentDirectory")), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(directory), compact, StringComparison.Ordinal);
    }

    /// <summary>Creates a fixed-width in-memory console without touching the process environment.</summary>
    private static (IAnsiConsole Console, StringWriter Output, LocalizationService Text, ConsoleShellView Shell) CreateConsole(
        int width, bool noEmoji)
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(output)
        });
        console.Profile.Width = width;
        var text = new LocalizationService();
        text.SetLanguage("it");
        var shell = new ConsoleShellView(console, text);
        shell.Configure(new ConsoleRenderOptions(NoAnimation: true, NoEmoji: noEmoji));
        return (console, output, text, shell);
    }

    /// <summary>Removes layout whitespace and panel borders when checking wrapped semantic content.</summary>
    private static string Compact(string value) => Regex.Replace(value, @"[\s│]", string.Empty);
}

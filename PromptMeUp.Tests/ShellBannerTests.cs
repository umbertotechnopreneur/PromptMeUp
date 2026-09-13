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
        Assert.Contains("--ai-settings", compact, StringComparison.Ordinal);
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

    /// <summary>Verifies active context and operating budgets remain distinct from last-call and cumulative usage.</summary>
    [Theory]
    [InlineData(48, true)]
    [InlineData(120, false)]
    public void RuntimeStatus_RendersSeparateActiveContextAndUsage(int width, bool noEmoji)
    {
        var (_, output, text, shell) = CreateConsole(width, noEmoji);
        var status = ShellRuntimeStatus.FromSettings(AppSettings.Default) with
        {
            ActiveContextTokens = 8_400,
            ContextWindowTokens = 1_050_000,
            ContextBudgetTokens = 16_000,
            ContextTotalTokens = 987_654,
            InputTokens = 710,
            OutputTokens = 95,
            MemoryCount = 4,
            MemoryTokens = 620,
            SessionInputTokens = 2_300,
            SessionOutputTokens = 450,
            HasSessionUsage = true
        };

        shell.RenderRuntimeStatus(status);

        var compact = Compact(output.ToString());
        Assert.Contains(Compact(text.Text("Shell.ActiveContext")), compact, StringComparison.Ordinal);
        Assert.Contains("~8.400/1.050.000·0,8%", compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.ContextBudget")), compact, StringComparison.Ordinal);
        Assert.Contains("~8.400/16.000·52,5%", compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.MemoryUsageValue", 4, "620")), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.LastInput") + ":710"), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.LastOutput") + ":95"), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.SessionInput") + ":" + 2_300.ToString("N0")), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.SessionOutput") + ":450"), compact, StringComparison.Ordinal);
        Assert.DoesNotContain(Compact(text.Text("Shell.Context")), compact, StringComparison.Ordinal);
        Assert.DoesNotContain("987", compact, StringComparison.Ordinal);
    }

    /// <summary>Verifies absent active estimates retain the known capacities without inventing zero utilization.</summary>
    [Theory]
    [InlineData(48, true)]
    [InlineData(120, false)]
    public void RuntimeStatus_WithoutActiveEstimate_RendersUnavailableAndCapacity(int width, bool noEmoji)
    {
        var (_, output, text, shell) = CreateConsole(width, noEmoji);
        var status = ShellRuntimeStatus.FromSettings(AppSettings.Default) with
        {
            ContextWindowTokens = 1_050_000,
            ContextBudgetTokens = 16_000,
            ContextTotalTokens = 8_050,
            InputTokens = 710,
            OutputTokens = 95
        };

        shell.RenderRuntimeStatus(status);

        var compact = Compact(output.ToString());
        var unavailable = Compact(text.Text("Costs.Unavailable"));
        Assert.Contains(unavailable + "/1.050.000", compact, StringComparison.Ordinal);
        Assert.Contains(unavailable + "/16.000", compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.LastInput") + ":710"), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.LastOutput") + ":95"), compact, StringComparison.Ordinal);
        Assert.DoesNotContain("~0/", compact, StringComparison.Ordinal);
        Assert.DoesNotContain("%", compact, StringComparison.Ordinal);
        Assert.DoesNotContain(Compact(text.Text("Shell.SessionInput")), compact, StringComparison.Ordinal);
        Assert.DoesNotContain(Compact(text.Text("Shell.SessionOutput")), compact, StringComparison.Ordinal);
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

    /// <summary>Removes ANSI formatting, layout whitespace, and panel borders while preserving wrapped visible text.</summary>
    private static string Compact(string value) =>
        Regex.Replace(value, @"\x1B\[[0-?]*[ -/]*[@-~]|[\s│]", string.Empty, RegexOptions.CultureInvariant);
}

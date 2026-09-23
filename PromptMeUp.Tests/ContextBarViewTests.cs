// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class ContextBarViewTests
{
    /// <summary>Verifies that equal shares include free space and never count the system guide a second time.</summary>
    [Fact]
    public void ContextBar_EqualShares_IncludesFreeCapacityWithoutDuplicatingGuide()
    {
        var status = CreateStatus(400, 400, 400, 1_600) with { GuideTokens = 400 };

        var cells = ConsoleShellView.AllocateContextBarCells(status, 36);

        Assert.Equal([9, 9, 9, 9], cells);
    }

    /// <summary>Verifies narrow and wide bars preserve total width while keeping each share within one cell of its ideal size.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(35)]
    [InlineData(36)]
    public void ContextBar_RoundedShares_PreserveWidthAndProportions(int width)
    {
        var status = CreateStatus(117, 503, 271, 1_001);
        double[] shares = [117, 503, 271, 110];

        var cells = ConsoleShellView.AllocateContextBarCells(status, width);

        Assert.Equal(width, cells.Sum());
        for (var index = 0; index < shares.Length; index++)
        {
            Assert.InRange(Math.Abs(cells[index] - shares[index] * width / 1_001d), 0d, 1d);
        }
    }

    /// <summary>Verifies that a tiny occupied share cannot exaggerate usage by forcing every nonempty category to one cell.</summary>
    [Fact]
    public void ContextBar_TinyShares_PreserveMostlyFreeCapacity()
    {
        var cells = ConsoleShellView.AllocateContextBarCells(CreateStatus(1, 1, 1, 16_000), 36);

        Assert.Equal([0, 0, 0, 36], cells);
    }

    /// <summary>Verifies that an exceeded budget fills the bar in actual category proportions without inventing free space.</summary>
    [Fact]
    public void ContextBar_ExceededBudget_PreservesCategoryRatios()
    {
        var cells = ConsoleShellView.AllocateContextBarCells(CreateStatus(200, 400, 200, 400), 36);

        Assert.Equal([9, 18, 9, 0], cells);
    }

    /// <summary>Verifies large valid counts cannot overflow during proportional allocation.</summary>
    [Fact]
    public void ContextBar_LargeCounts_DoNotOverflow()
    {
        var status = ShellRuntimeStatus.FromSettings(AppSettings.Default) with
        {
            SystemInstructionTokens = long.MaxValue,
            UserMessageTokens = long.MaxValue,
            AssistantMessageTokens = long.MaxValue,
            ContextBudgetTokens = long.MaxValue,
            HasContextBreakdown = true
        };

        Assert.Equal([12, 12, 12, 0], ConsoleShellView.AllocateContextBarCells(status, 36));
    }

    /// <summary>Verifies an impossible guide subset is rejected instead of silently misrepresenting the legend.</summary>
    [Fact]
    public void ContextBar_GuideExceedsSystem_RejectsInvalidBreakdown()
    {
        var status = CreateStatus(100, 100, 100, 1_000) with { GuideTokens = 101 };

        Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleShellView.AllocateContextBarCells(status, 36));
    }

    /// <summary>Verifies every language retains full numeric categories and the included guide label in narrow colorless output.</summary>
    [Theory]
    [InlineData("en", 48)]
    [InlineData("it", 48)]
    [InlineData("fr", 48)]
    [InlineData("de", 48)]
    [InlineData("es", 48)]
    [InlineData("vi", 48)]
    [InlineData("it", 120)]
    public void ContextLegend_ColorlessConsole_RendersLocalizedCountsAndGuideSubset(string language, int width)
    {
        var (_, output, text, shell) = CreateConsole(width, language);
        var status = CreateStatus(400, 400, 400, 1_600) with { GuideTokens = 125 };

        shell.RenderRuntimeStatus(status);

        var compact = Compact(output.ToString());
        Assert.Contains(Compact("[S] " + text.Text("Shell.ContextSystem") + ":" + text.Text("Shell.ContextTokenEstimate", "400")), compact, StringComparison.Ordinal);
        Assert.Contains(Compact("[U] " + text.Text("Shell.ContextUser") + ":" + text.Text("Shell.ContextTokenEstimate", "400")), compact, StringComparison.Ordinal);
        Assert.Contains(Compact("[A] " + text.Text("Shell.ContextAssistant") + ":" + text.Text("Shell.ContextTokenEstimate", "400")), compact, StringComparison.Ordinal);
        Assert.Contains(Compact("[.] " + text.Text("Shell.ContextFree") + ":" + text.Text("Shell.ContextTokenEstimate", "400")), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.ContextGuideIncluded") + ":" + text.Text("Shell.ContextTokenEstimate", "125")), compact, StringComparison.Ordinal);
        Assert.DoesNotContain("Shell.Context", compact, StringComparison.Ordinal);
        Assert.DoesNotContain("\u001b[2J", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("\u001b[3J", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Verifies bar role symbols remain readable without colors and their segment lengths match the available width.</summary>
    [Fact]
    public void ContextBar_ColorlessConsole_RendersMatchingRoleSymbols()
    {
        var (_, output, _, shell) = CreateConsole(160, "en");

        shell.RenderRuntimeStatus(CreateStatus(400, 400, 400, 1_600));

        Assert.Contains("SSSSSSSSSUUUUUUUUUAAAAAAAAA.........", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Verifies optional guide selection cost appears in this turn while last-call usage remains the final response usage.</summary>
    [Fact]
    public void RuntimeStatus_AggregateTurnCost_DoesNotAlterLastCallUsage()
    {
        var (_, output, text, shell) = CreateConsole(120, "en");
        var status = CreateStatus(400, 400, 400, 1_600) with
        {
            PromptCostUsd = 0.01m,
            ResponseCostUsd = 0.02m,
            TurnCostUsd = 0.08m,
            HasTurnCost = true,
            InputTokens = 850,
            OutputTokens = 75
        };

        shell.RenderRuntimeStatus(status);

        var compact = Compact(output.ToString());
        Assert.Contains(Compact(text.Text("Shell.TurnCost") + ":" + $"${0.08m:0.00000000}"), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.LastInput") + ":850"), compact, StringComparison.Ordinal);
        Assert.Contains(Compact(text.Text("Shell.LastOutput") + ":75"), compact, StringComparison.Ordinal);
    }

    /// <summary>Verifies that missing pricing for one call cannot make a partial amount appear as the full turn cost.</summary>
    [Fact]
    public void RuntimeStatus_IncompleteTurnCost_RendersUnavailable()
    {
        var (_, output, text, shell) = CreateConsole(120, "en");
        var status = CreateStatus(400, 400, 400, 1_600) with
        {
            PromptCostUsd = 0.01m,
            ResponseCostUsd = 0.02m,
            TurnCostUsd = null,
            HasTurnCost = true
        };

        shell.RenderRuntimeStatus(status);

        Assert.Contains(Compact(text.Text("Shell.TurnCost") + ":" + text.Text("Costs.Unavailable")), Compact(output.ToString()), StringComparison.Ordinal);
    }

    /// <summary>Verifies an unpriced recorded call cannot make a partial session total appear complete.</summary>
    [Fact]
    public void RuntimeStatus_IncompleteSessionCost_RendersUnavailable()
    {
        var (_, output, text, shell) = CreateConsole(120, "en");
        var status = CreateStatus(400, 400, 400, 1_600) with
        {
            RunningCostUsd = 0.05m,
            SessionCostKnown = false
        };

        shell.RenderRuntimeStatus(status);

        Assert.Contains(Compact(text.Text("Shell.SessionCost") + ":" + text.Text("Costs.Unavailable")), Compact(output.ToString()), StringComparison.Ordinal);
        Assert.DoesNotContain($"${0.05m:0.00000000}", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Creates a coherent active estimate with the guide initially absent from system content.</summary>
    private static ShellRuntimeStatus CreateStatus(long system, long user, long assistant, long budget) =>
        ShellRuntimeStatus.FromSettings(AppSettings.Default) with
        {
            SystemInstructionTokens = system,
            UserMessageTokens = user,
            AssistantMessageTokens = assistant,
            ActiveContextTokens = system + user + assistant,
            ContextBudgetTokens = budget,
            HasContextBreakdown = true
        };

    /// <summary>Creates an in-memory colorless console with explicit language and width without touching the application.</summary>
    private static (IAnsiConsole Console, StringWriter Output, LocalizationService Text, ConsoleShellView Shell) CreateConsole(int width, string language)
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            // CI enrichers would otherwise re-enable ANSI after applying the explicit test settings.
            Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false },
            Out = new AnsiConsoleOutput(output)
        });
        console.Profile.Width = width;
        var text = new LocalizationService();
        text.SetLanguage(language);
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(NoAnimation: true, NoEmoji: true));
        return (console, output, text, shell);
    }

    /// <summary>Removes layout whitespace while preserving labels and numeric values through terminal wrapping.</summary>
    private static string Compact(string value) =>
        Regex.Replace(value, @"\s", string.Empty, RegexOptions.CultureInvariant);
}

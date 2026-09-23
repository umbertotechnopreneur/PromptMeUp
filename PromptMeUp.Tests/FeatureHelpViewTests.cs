// SPDX-License-Identifier: MIT

using System.Reflection;
using System.Text.RegularExpressions;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class FeatureHelpViewTests
{
    private static readonly (string Command, string HelpKey, string OldKey)[] Commands =
    [
        ("--skills", "Help.Skills", "Lab.Skills"),
        ("--learning", "Help.Learning", "Lab.CaptureNotice"),
        ("--proposals", "Help.Proposals", "Lab.Proposals"),
        ("--dream", "Help.Dream", "Lab.NeedSessions"),
        ("--heartbeat", "Help.Heartbeat", "Lab.Heartbeat")
    ];

    /// <summary>Both help surfaces share purpose descriptions, not generic titles or operational consent messages.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("vi")]
    public void FeatureSection_AllLanguages_MapsCommandsToShortPurposeDescriptions(string language)
    {
        var (view, text, _) = Create(language, 120);
        var sections = (IReadOnlyList<HelpSection>)typeof(HelpView)
            .GetMethod("CreateSections", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, [null])!;
        var section = Assert.Single(sections, item => item.Entries.Any(entry => entry.Command == "--skills"));

        Assert.Equal(Commands.Select(item => item.Command), section.Entries.Select(entry => entry.Command));
        foreach (var (command, helpKey, oldKey) in Commands)
        {
            var description = Assert.Single(section.Entries, entry => entry.Command == command).Description;
            Assert.Equal(text.Text(helpKey), description);
            Assert.NotEqual(helpKey, description);
            Assert.NotEqual(text.Text(oldKey), description);
            Assert.NotEqual(text.Text("Lab.CaptureNotice"), description);
            Assert.NotEqual(text.Text("Lab.NeedSessions"), description);
            Assert.InRange(description.Length, 15, 300);
            Assert.DoesNotContain('\n', description);
        }
        Assert.Equal(Commands.Length, section.Entries.Select(entry => entry.Description).Distinct(StringComparer.Ordinal).Count());
        var memories = Assert.Single(sections, item => item.Entries.Any(entry => entry.Command == "--memories"));
        Assert.Equal(new[] { "--memories", "--remember <text>", "--forget <id or description>" },
            memories.Entries.Select(entry => entry.Command));
        Assert.All(memories.Entries, entry => Assert.DoesNotContain("[global|project]", entry.Description, StringComparison.Ordinal));
    }

    /// <summary>Static help visibly explains all five commands in every language without entering an alternate buffer.</summary>
    [Theory]
    [InlineData("en", 60)]
    [InlineData("it", 60)]
    [InlineData("fr", 120)]
    [InlineData("de", 120)]
    [InlineData("es", 120)]
    [InlineData("vi", 120)]
    public void RenderStatic_AllLanguages_IncludesCompleteFeatureDescriptions(string language, int width)
    {
        var (view, text, output) = Create(language, width);

        view.RenderStatic();

        var rendered = Compact(output.ToString());
        foreach (var (command, helpKey, _) in Commands)
        {
            Assert.Contains(command, rendered, StringComparison.Ordinal);
            Assert.Contains(Compact(text.Text(helpKey)), rendered, StringComparison.Ordinal);
        }
        Assert.DoesNotContain(Compact(text.Text("Lab.CaptureNotice")), rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(Compact(text.Text("Lab.NeedSessions")), rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("\u001b[2J", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("\u001b[3J", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Creates passive help with in-memory output and no interactive, filesystem, or provider operations.</summary>
    private static (HelpView View, LocalizationService Text, StringWriter Output) Create(string language, int width)
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
        text.SetLanguage(language);
        var shell = new ConsoleShellView(console, text, new AlwaysShowProjectBannerSchedule());
        shell.Configure(new ConsoleRenderOptions(true, true));
        var about = TestProxy.Create<IAboutView>((method, _) => throw new NotSupportedException(method.Name));
        return (new HelpView(console, text, shell, about), text, output);
    }

    /// <summary>Ignores terminal wrapping while retaining every visible command and explanatory character.</summary>
    private static string Compact(string value) => Regex.Replace(value, @"\s+", string.Empty, RegexOptions.CultureInvariant);
}

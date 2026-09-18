// SPDX-License-Identifier: MIT

using System.Reflection;
using System.Text.RegularExpressions;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;
using Spectre.Console.Rendering;
using Xunit.Abstractions;

namespace PromptMeUp.Tests;

public sealed class FullscreenFeatureFormTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>Retains captured frames in test output for human-readable layout review.</summary>
    public FullscreenFeatureFormTests(ITestOutputHelper output) => _output = output;

    /// <summary>Focused package details remain scrollable in bounded compact and ordinary viewports.</summary>
    [Theory]
    [InlineData(60, 20)]
    [InlineData(100, 32)]
    public void FocusedOverview_RendersBoundedAndScrollsCompleteLiteralSource(int width, int height)
    {
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(output)
        });
        console.Profile.Width = width;
        console.Profile.Height = height;
        var text = new LocalizationService();
        var form = new FullscreenForm(console, text, new(true, true));
        var fields = Enumerable.Range(0, 20).Select(index => new FormField("skill-" + index, "Settings.FeatureSkills", () => "true", _ => { })
        {
            Label = () => "[red]literal[/]\u001b",
            ValueColor = () => TerminalTheme.Success,
            Overview = () => new Text("SOURCE_START\n" + string.Join('\n', Enumerable.Range(0, 80).Select(line => "source line " + line)) + "\nSOURCE_END")
        }).ToArray();
        var pages = new[] { new FormPage("Settings.Skills", fields) { HelpKey = "Settings.FeaturesDraftHelp" }, new FormPage("Settings.Privacy", []) };
        SetField(form, "_allowSectionNavigation", true);
        SetField(form, "_focus", 5);

        Render(form, pages, fields);

        var first = Plain(output);
        _output.WriteLine($"Viewport {width}x{height}:\n{first}");
        Assert.Contains("SOURCE_START", first, StringComparison.Ordinal);
        Assert.Contains(text.Text("Form.Save"), first, StringComparison.Ordinal);
        Assert.Contains(text.Text("Form.Cancel"), first, StringComparison.Ordinal);
        Assert.Contains("[red]", first, StringComparison.Ordinal);
        AssertViewport(first, width, height);
        Assert.DoesNotContain("\u001b[3J", output.ToString(), StringComparison.Ordinal);
        output.GetStringBuilder().Clear();
        SetField(form, "_overviewOffset", 10_000);

        Render(form, pages, fields);

        var last = Plain(output);
        Assert.Contains("SOURCE_END", last, StringComparison.Ordinal);
        Assert.DoesNotContain("SOURCE_START", last, StringComparison.Ordinal);
        AssertViewport(last, width, height);
    }

    /// <summary>The new tabs use shared icons and omit emoji when the existing no-emoji preference is active.</summary>
    [Theory]
    [InlineData("Settings.Skills", "🧩")]
    [InlineData("Settings.Learning", "💭")]
    [InlineData("Settings.Memories", "📚")]
    [InlineData("Settings.Privacy", "🔒")]
    public void SectionIcons_RespectExistingNoEmojiOption(string key, string icon)
    {
        var text = new LocalizationService();
        var page = new FormPage(key, []);

        Assert.StartsWith(icon + " ", FullscreenForm.SectionTitle(page, text, new(true, false)), StringComparison.Ordinal);
        Assert.DoesNotContain(icon, FullscreenForm.SectionTitle(page, text, new(true, true)), StringComparison.Ordinal);
    }

    /// <summary>Calls only the passive renderer with in-memory output, without opening a real terminal buffer.</summary>
    private static void Render(FullscreenForm form, IReadOnlyList<FormPage> pages, IReadOnlyList<FormField> fields) =>
        typeof(FullscreenForm).GetMethod("Render", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, ["Settings.Title", pages, fields]);

    /// <summary>Positions the renderer's existing navigation state without adding production test hooks.</summary>
    private static void SetField(FullscreenForm form, string name, object value) =>
        typeof(FullscreenForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, value);

    /// <summary>Checks that rendering never writes beyond its disposable viewport.</summary>
    private static void AssertViewport(string output, int width, int height)
    {
        var lines = output.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        Assert.True(lines.Length < height, $"Rendered {lines.Length} rows in a {height}-row viewport.");
        Assert.All(lines, line => Assert.True(new Segment(line).CellCount() < width, $"Rendered an over-wide line: {line}"));
    }

    /// <summary>Removes terminal escapes while preserving the rendered text and line boundaries.</summary>
    private static string Plain(StringWriter output) => Regex.Replace(output.ToString(), @"\x1B\[[0-?]*[ -/]*[@-~]", string.Empty, RegexOptions.CultureInvariant);
}

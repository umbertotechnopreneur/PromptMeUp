// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Shows build information and project links in an optional full-width, theme-aware card.</summary>
internal sealed class InstallationInfo(ILocalizationService text, BuildInformation buildInformation, bool renderCard = true) : IRenderable
{
    private const string RepositoryUrl = "https://github.com/umbertotechnopreneur/PromptMeUp";
    private const string AuthorUrl = "https://umbertogiacobbi.biz";

    /// <summary>Measures the details using the width assigned by the containing view.</summary>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        CreateContent(maxWidth).Measure(options, maxWidth);

    /// <summary>Renders complete installation details for the containing view to display and scroll.</summary>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        CreateContent(maxWidth).Render(options, maxWidth);

    /// <summary>Expands the optional card to the available width and keeps the unboxed variant suitable for About.</summary>
    private IRenderable CreateContent(int width)
    {
        var title = $"[bold {TerminalTheme.Accent}]{Markup.Escape(text.Text("About.Installation"))}[/]";
        var body = CreateBody(renderCard ? Math.Max(1, width - 4) : width);
        return renderCard
            ? new Panel(body)
            {
                Header = new PanelHeader(title),
                Border = BoxBorder.Rounded,
                BorderStyle = Style.Parse(TerminalTheme.Divider),
                Padding = new Padding(1, 0, 1, 0),
                Expand = true
            }
            : new Rows(new Rule(title).RuleStyle(TerminalTheme.Divider), new Text(" "), body);
    }

    /// <summary>Uses aligned label-value rows with a stacked layout when the available content width is narrow.</summary>
    private IRenderable CreateBody(int width)
    {
        var details = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn(new GridColumn().LeftAligned());
        var narrowDetails = new List<IRenderable>();
        AddDetail(details, narrowDetails, "Footer.Version", buildInformation.Version);
        AddDetail(details, narrowDetails, "About.BuildDate",
            buildInformation.BuiltAtUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));
        AddDetail(details, narrowDetails, "About.BuildMachine", buildInformation.MachineName);
        AddDetail(details, narrowDetails, "About.Author", "Umberto Giacobbi");
        AddDetail(details, narrowDetails, "About.License", "MIT");
        AddDetail(details, narrowDetails, "About.Platforms", "Windows / Linux / macOS");
        return new Rows(
            width >= 52 ? details : new Rows(narrowDetails),
            new Text(text.Text("About.Repository"), Style.Parse(TerminalTheme.Muted)),
            new Markup($"[underline {TerminalTheme.Info} link={RepositoryUrl}]{RepositoryUrl}[/]"),
            new Text(" "),
            new Text(text.Text("About.Website"), Style.Parse(TerminalTheme.Muted)),
            new Markup($"[underline {TerminalTheme.Info} link={AuthorUrl}]{AuthorUrl}[/]"));
    }

    /// <summary>Separates each right-aligned label and left-aligned value with one trailing blank row.</summary>
    private void AddDetail(Grid grid, List<IRenderable> narrowRows, string labelKey, string value)
    {
        var label = new Text(text.Text(labelKey) + ":", Style.Parse(TerminalTheme.Muted));
        var renderedValue = new Text(value, Style.Parse(TerminalTheme.Primary));
        grid.AddRow(label, renderedValue);
        grid.AddEmptyRow();
        narrowRows.AddRange([label, renderedValue, new Text(" ")]);
    }
}

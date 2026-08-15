// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IHelpView
{
    void Render();
}

public sealed class HelpView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : IHelpView
{
    /// <summary>Renders the public CLI contract as grouped, scannable Spectre surfaces.</summary>
    public void Render()
    {
        TerminalTheme.WriteHeading(console, text.Text("Help.Title"), text.Text("Help.Usage"));
        var exampleIcon = TerminalTheme.Icon(shell.Options, "⚡", ">");
        console.Write(TerminalTheme.Panel(
            new Rows(
                new Markup($"[bold {TerminalTheme.Primary}]hm[/] [{TerminalTheme.Muted}]\"how do I undo the last local git commit?\"[/]"),
                new Markup($"[bold {TerminalTheme.Primary}]hm --chat[/] [{TerminalTheme.Muted}]{Markup.Escape(text.Text("Help.Chat"))}[/]")),
            $"{exampleIcon} {text.Text("Help.Examples")}"));
        console.WriteLine();

        RenderGroup(
            "💬",
            "Help.Group.Ai",
            [
                ("--query, -q <text>", text.Text("Help.Query")),
                ("--chat", text.Text("Help.Chat")),
                ("--test-ai", text.Text("Help.Test"))
            ]);
        RenderGroup(
            "📊",
            "Help.Group.Insight",
            [
                ("--version, -v", text.Text("Help.Version")),
                ("--status", text.Text("Help.Status")),
                ("--costs", text.Text("Help.Costs")),
                ("--where, -where", text.Text("Help.Where")),
                ("--third-party", text.Text("Help.ThirdParty"))
            ]);
        RenderGroup(
            "⚙",
            "Help.Group.Setup",
            [
                ("--setup", text.Text("Help.Setup")),
                ("--path [install|remove|status]", text.Text("Help.Path")),
                ("--install-font [--dry-run]", text.Text("Help.Font")),
                ("--language, -l <code>", text.Text("Help.Language"))
            ]);
        RenderGroup(
            "🛡",
            "Help.Group.Safety",
            [
                ("--no-animation | --no-emoji", text.Text("Help.Rendering")),
                ("--yes, -y", text.Text("Help.Yes")),
                ("--dry-run", text.Text("Help.DryRun"))
            ]);
    }

    /// <summary>Renders one cohesive command category without turning the help screen into a flat flag dump.</summary>
    private void RenderGroup(
        string icon,
        string titleKey,
        IReadOnlyList<(string Command, string Description)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var table = new Table().Border(TableBorder.None).HideHeaders();
        table.AddColumn(new TableColumn("command").NoWrap());
        table.AddColumn(new TableColumn("description"));
        foreach (var (command, description) in entries)
        {
            table.AddRow(
                new Markup($"[bold {TerminalTheme.Accent}]{Markup.Escape(command)}[/]"),
                new Markup($"[{TerminalTheme.Primary}]{Markup.Escape(description)}[/]"));
        }

        console.Write(TerminalTheme.Panel(
            table,
            $"{TerminalTheme.Icon(shell.Options, icon, ">")} {text.Text(titleKey)}"));
        console.WriteLine();
    }
}

public interface IMainMenuView
{
    MainMenuAction Select();
}

public sealed class MainMenuView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : IMainMenuView
{
    /// <summary>Returns one action from the branded interactive command center.</summary>
    public MainMenuAction Select() => console.Prompt(
        new SelectionPrompt<MainMenuAction>()
            .Title($"[bold {TerminalTheme.Primary}]{Markup.Escape(text.Text("Main.Title"))}[/]\n[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Main.Choose"))}[/]")
            .PageSize(12)
            .HighlightStyle(new Style(Color.MediumPurple2))
            .UseConverter(Label)
            .AddChoices(
                MainMenuAction.Query,
                MainMenuAction.Chat,
                MainMenuAction.Costs,
                MainMenuAction.Status,
                MainMenuAction.Setup,
                MainMenuAction.TestAi,
                MainMenuAction.Where,
                MainMenuAction.Path,
                MainMenuAction.InstallFont,
                MainMenuAction.ThirdParty,
                MainMenuAction.Exit));

    /// <summary>Maps menu actions to localized labels with portable icon fallbacks.</summary>
    private string Label(MainMenuAction action) => action switch
    {
        MainMenuAction.Query => MenuLabel("✦", ">", text.Text("Main.Query")),
        MainMenuAction.Chat => MenuLabel("💬", ">", text.Text("Main.Chat")),
        MainMenuAction.Costs => MenuLabel("📊", "=", text.Text("Main.Costs")),
        MainMenuAction.Status => MenuLabel("🪞", "=", text.Text("Main.Status")),
        MainMenuAction.Setup => MenuLabel("⚙", "~", text.Text("Main.Setup")),
        MainMenuAction.TestAi => MenuLabel("↻", "~", text.Text("Main.Test")),
        MainMenuAction.Where => MenuLabel("⌖", "@", text.Text("Main.Where")),
        MainMenuAction.Path => MenuLabel("↔", "<>", text.Text("Path.Title")),
        MainMenuAction.InstallFont => MenuLabel("✎", "#", text.Text("Main.Font")),
        MainMenuAction.ThirdParty => MenuLabel("⚖", "=", text.Text("ThirdParty.Title")),
        _ => MenuLabel("↩", "x", text.Text("Main.Exit"))
    };

    /// <summary>Formats one menu label with a high-contrast leading visual cue.</summary>
    private string MenuLabel(string icon, string fallback, string label) =>
        $"[{TerminalTheme.Info}]{Markup.Escape(TerminalTheme.Icon(shell.Options, icon, fallback))}[/] [bold {TerminalTheme.Primary}]{Markup.Escape(label)}[/]";
}

public interface IExecutableLocationView
{
    ExecutableLocationAction RenderAndSelect(ExecutableLocationInfo location, bool interactive);

    bool ConfirmOpen(ExecutableLocationInfo location);

    void RenderResult(ExecutableLocationInfo location, ExecutableLocationAction action);
}

public sealed class ExecutableLocationView(IAnsiConsole console, ILocalizationService text) : IExecutableLocationView
{
    /// <summary>Shows the exact executable location and selects a safe next action when input is interactive.</summary>
    public ExecutableLocationAction RenderAndSelect(ExecutableLocationInfo location, bool interactive)
    {
        ArgumentNullException.ThrowIfNull(location);
        TerminalTheme.WriteHeading(console, text.Text("Where.Title"));
        var locationGrid = new Grid();
        locationGrid.AddColumn(new GridColumn().NoWrap());
        locationGrid.AddColumn();
        locationGrid.AddRow(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Where.Executable"))}[/]"),
            new Text(location.ExecutablePath));
        locationGrid.AddRow(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Where.Directory"))}[/]"),
            new Text(location.DirectoryPath));
        console.Write(locationGrid);
        console.WriteLine();

        if (!interactive)
        {
            return ExecutableLocationAction.ShowChangeDirectoryCommand;
        }

        return console.Prompt(
            new SelectionPrompt<ExecutableLocationAction>()
                .Title(Markup.Escape(text.Text("Where.Action")))
                .UseConverter(action => action == ExecutableLocationAction.OpenContainingFolder
                    ? text.Text("Where.Open")
                    : text.Text("Where.ShowCd"))
                .AddChoices(
                    ExecutableLocationAction.ShowChangeDirectoryCommand,
                    ExecutableLocationAction.OpenContainingFolder));
    }

    /// <summary>Previews the exact file-manager invocation and requests explicit authorization.</summary>
    public bool ConfirmOpen(ExecutableLocationInfo location)
    {
        ArgumentNullException.ThrowIfNull(location);
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Where.OpenPreview"))}:[/] [{TerminalTheme.Primary}]{Markup.Escape(location.OpenFolderPreview)}[/]");
        return console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Where.Confirm")))
        {
            DefaultValue = false
        });
    }

    /// <summary>Reports the authorized file-manager launch or a command that changes the calling terminal manually.</summary>
    public void RenderResult(ExecutableLocationInfo location, ExecutableLocationAction action)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (action == ExecutableLocationAction.OpenContainingFolder)
        {
            console.MarkupLine($"[green]{Markup.Escape(text.Text("Where.Opened"))}[/]");
            return;
        }

        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Where.ChangeDirectoryHint"))}[/]");
        console.WriteLine(location.ChangeDirectoryCommand);
    }
}

public interface IThirdPartyView
{
    void Render();
}

public sealed class ThirdPartyView(IAnsiConsole console, ILocalizationService text) : IThirdPartyView
{
    private static readonly (string Package, string Version, string License)[] Packages =
    [
        ("Microsoft.Data.Sqlite", "10.0.10", "MIT"),
        ("Microsoft.Extensions.DependencyInjection", "10.0.10", "MIT"),
        ("Microsoft.Extensions.Http", "10.0.10", "MIT"),
        ("Microsoft.Extensions.Logging", "10.0.10", "MIT"),
        ("Serilog", "4.4.0", "Apache-2.0"),
        ("Serilog.Extensions.Logging", "10.0.0", "Apache-2.0"),
        ("Serilog.Sinks.File", "7.0.0", "Apache-2.0"),
        ("Spectre.Console", "0.57.2", "MIT"),
        ("SQLitePCLRaw.bundle_e_sqlite3", "2.1.12", "Apache-2.0 / Public Domain"),
        ("YamlDotNet", "18.1.0", "MIT")
    ];

    /// <summary>Renders direct runtime dependencies and their declared licenses.</summary>
    public void Render()
    {
        TerminalTheme.WriteHeading(console, text.Text("ThirdParty.Title"), text.Text("ThirdParty.Subtitle"));
        var table = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(TerminalTheme.Divider));
        table.AddColumn(text.Text("ThirdParty.Package"));
        table.AddColumn(text.Text("ThirdParty.Version"));
        table.AddColumn(text.Text("ThirdParty.License"));
        foreach (var package in Packages)
        {
            table.AddRow(Markup.Escape(package.Package), Markup.Escape(package.Version), Markup.Escape(package.License));
        }
        console.Write(table);
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("ThirdParty.FullNotices"))}[/]");
    }
}

public interface IPortablePathView
{
    PortablePathAction SelectAction();

    bool PreviewAndConfirm(PortablePathPlan plan, bool preauthorized);

    void RenderResult(PortablePathResult result);
}

public sealed class PortablePathView(IAnsiConsole console, ILocalizationService text) : IPortablePathView
{
    /// <summary>Collects an install, remove, or status PATH action.</summary>
    public PortablePathAction SelectAction() => console.Prompt(
        new SelectionPrompt<PortablePathAction>()
            .Title(Markup.Escape(text.Text("Path.Action")))
            .UseConverter(action => action switch
            {
                PortablePathAction.Install => text.Text("Path.Install"),
                PortablePathAction.Remove => text.Text("Path.Remove"),
                _ => text.Text("Path.Status")
            })
            .AddChoices(PortablePathAction.Status, PortablePathAction.Install, PortablePathAction.Remove));

    /// <summary>Shows the exact persistent target and asks before a mutating PATH operation.</summary>
    public bool PreviewAndConfirm(PortablePathPlan plan, bool preauthorized)
    {
        ArgumentNullException.ThrowIfNull(plan);
        TerminalTheme.WriteHeading(console, text.Text("Path.Title"));
        TerminalTheme.WriteBlock(
            console,
            plan.Preview,
            $"Target: {plan.PersistenceTarget}\nDirectory: {plan.ExecutableDirectory}",
            TerminalTheme.Accent);
        if (plan.Action == PortablePathAction.Status || !plan.RequiresChange)
        {
            return false;
        }

        return preauthorized || console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Path.Confirm")))
        {
            DefaultValue = false
        });
    }

    /// <summary>Reports persistent PATH presence after inspection or mutation.</summary>
    public void RenderResult(PortablePathResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var message = result.IsPresent ? text.Text("Path.Present") : text.Text("Path.Missing");
        console.MarkupLine($"[{(result.IsPresent ? "green" : "yellow")}]{Markup.Escape(message)}[/]");
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(result.PersistenceTarget)} · {Markup.Escape(result.ExecutableDirectory)}[/]");
    }
}

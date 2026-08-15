// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IHelpView
{
    void Render();
}

public sealed class HelpView(IAnsiConsole console, ILocalizationService text) : IHelpView
{
    /// <summary>Renders the compact public CLI contract with hm-first examples.</summary>
    public void Render()
    {
        TerminalTheme.WriteHeading(console, text.Text("Help.Title"), text.Text("Help.Usage"));
        console.MarkupLine("[bold]hm[/] [grey]\"how do I undo the last local git commit?\"[/]");
        console.MarkupLine("[bold]hm --chat[/]");
        console.WriteLine();
        var table = new Table().Border(TableBorder.None).HideHeaders();
        table.AddColumn("Switch");
        table.AddColumn("Description");
        Add(table, "--setup", text.Text("Help.Setup"));
        Add(table, "--version, -v", text.Text("Help.Version"));
        Add(table, "--status", text.Text("Help.Status"));
        Add(table, "--query, -q <text>", text.Text("Help.Query"));
        Add(table, "--chat", text.Text("Help.Chat"));
        Add(table, "--test-ai", text.Text("Help.Test"));
        Add(table, "--costs", text.Text("Help.Costs"));
        Add(table, "--third-party", text.Text("Help.ThirdParty"));
        Add(table, "--where, -where", text.Text("Help.Where"));
        Add(table, "--path [install|remove|status]", text.Text("Help.Path"));
        Add(table, "--install-font [--dry-run]", text.Text("Help.Font"));
        Add(table, "--language, -l <code>", text.Text("Help.Language"));
        Add(table, "--no-animation | --no-emoji", text.Text("Help.Rendering"));
        Add(table, "--yes, -y", text.Text("Help.Yes"));
        Add(table, "--dry-run", text.Text("Help.DryRun"));
        console.Write(table);
    }

    /// <summary>Adds one escaped switch-description pair.</summary>
    private static void Add(Table table, string command, string description) =>
        table.AddRow(new Markup($"[mediumpurple2]{Markup.Escape(command)}[/]"), new Text(description));
}

public interface IMainMenuView
{
    MainMenuAction Select();
}

public sealed class MainMenuView(IAnsiConsole console, ILocalizationService text) : IMainMenuView
{
    /// <summary>Returns one action from the lightweight interactive command center.</summary>
    public MainMenuAction Select() => console.Prompt(
        new SelectionPrompt<MainMenuAction>()
            .Title($"[bold]{Markup.Escape(text.Text("Main.Title"))}[/] · {Markup.Escape(text.Text("Main.Choose"))}")
            .PageSize(12)
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

    /// <summary>Maps menu actions to localized product labels.</summary>
    private string Label(MainMenuAction action) => action switch
    {
        MainMenuAction.Query => text.Text("Main.Query"),
        MainMenuAction.Chat => text.Text("Main.Chat"),
        MainMenuAction.Costs => text.Text("Main.Costs"),
        MainMenuAction.Status => text.Text("Main.Status"),
        MainMenuAction.Setup => text.Text("Main.Setup"),
        MainMenuAction.TestAi => text.Text("Main.Test"),
        MainMenuAction.Where => text.Text("Main.Where"),
        MainMenuAction.Path => text.Text("Path.Title"),
        MainMenuAction.InstallFont => text.Text("Main.Font"),
        MainMenuAction.ThirdParty => text.Text("ThirdParty.Title"),
        _ => text.Text("Main.Exit")
    };
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
            new Markup($"[grey]{Markup.Escape(text.Text("Where.Executable"))}[/]"),
            new Text(location.ExecutablePath));
        locationGrid.AddRow(
            new Markup($"[grey]{Markup.Escape(text.Text("Where.Directory"))}[/]"),
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
        console.MarkupLine($"[grey]{Markup.Escape(text.Text("Where.OpenPreview"))}:[/] {Markup.Escape(location.OpenFolderPreview)}");
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

        console.MarkupLine($"[grey]{Markup.Escape(text.Text("Where.ChangeDirectoryHint"))}[/]");
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
        var table = new Table().Border(TableBorder.None);
        table.AddColumn(text.Text("ThirdParty.Package"));
        table.AddColumn(text.Text("ThirdParty.Version"));
        table.AddColumn(text.Text("ThirdParty.License"));
        foreach (var package in Packages)
        {
            table.AddRow(Markup.Escape(package.Package), Markup.Escape(package.Version), Markup.Escape(package.License));
        }
        console.Write(table);
        console.MarkupLine($"[grey]{Markup.Escape(text.Text("ThirdParty.FullNotices"))}[/]");
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
        console.MarkupLine($"[grey]{Markup.Escape(result.PersistenceTarget)} · {Markup.Escape(result.ExecutableDirectory)}[/]");
    }
}

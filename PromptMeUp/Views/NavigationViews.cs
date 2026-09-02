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
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "⌨", ">") + text.Text("Help.Title"),
            TerminalTheme.Accent);
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Help.Usage"))}[/]");
        var exampleIcon = TerminalTheme.IconPrefix(shell.Options, "⚡", ">");
        TerminalTheme.WriteRule(console, $"{exampleIcon}{text.Text("Help.Examples")}", TerminalTheme.Accent);
        var examples = new Grid();
        examples.AddColumn(new GridColumn().RightAligned().NoWrap());
        examples.AddColumn(new GridColumn().LeftAligned());
        examples.AddRow(
            new Markup($"[bold {TerminalTheme.Primary}]hm[/]"),
            new Markup($"[{TerminalTheme.Muted}]\"{Markup.Escape(text.Text("Help.ExamplePrompt"))}\"[/]"));
        examples.AddRow(
            new Markup($"[bold {TerminalTheme.Primary}]hm --chat[/]"),
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Help.Chat"))}[/]"));
        console.Write(examples);
        console.WriteLine();

        RenderGroup(
            "💬",
            "Help.Group.Ai",
            [
                (text.Text("Help.QuerySyntax"), text.Text("Help.Query")),
                ("--diagnose [--file <path>]", text.Text("Diagnose.Help")),
                ("--script <request> [--file <path>] [--output <path>]", text.Text("Script.Help")),
                ("--plan <goal> | --plan --resume <id>", text.Text("Plan.Help")),
                ("--preview <copy|move|rename|delete> --file <path>", text.Text("Preview.Help")),
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
                (text.Text("Help.LanguageSyntax"), text.Text("Help.Language"))
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
        table.AddColumn(new TableColumn("command").RightAligned().NoWrap());
        table.AddColumn(new TableColumn("description"));
        foreach (var (command, description) in entries)
        {
            table.AddRow(
                new Markup($"[bold {TerminalTheme.Accent}]{Markup.Escape(command)}[/]"),
                new Markup($"[{TerminalTheme.Primary}]{Markup.Escape(description)}[/]"));
        }

        TerminalTheme.WriteRule(
            console,
            $"{TerminalTheme.IconPrefix(shell.Options, icon, ">")}{text.Text(titleKey)}",
            TerminalTheme.Accent);
        console.Write(table);
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
    public MainMenuAction Select()
    {
        TerminalTheme.WriteRule(
            console,
            $"{TerminalTheme.IconPrefix(shell.Options, "🎛", ">")}{text.Text("Main.Title")}",
            TerminalTheme.Accent);
        return console.Prompt(
            new SelectionPrompt<MainMenuAction>()
            .Title($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Main.Choose"))}[/]")
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
    }

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
        $"[{TerminalTheme.Info}]{Markup.Escape(TerminalTheme.IconPrefix(shell.Options, icon, fallback))}[/][bold {TerminalTheme.Primary}]{Markup.Escape(label)}[/]";
}

public interface IExecutableLocationView
{
    ExecutableLocationAction RenderAndSelect(ExecutableLocationInfo location, bool interactive);

    bool ConfirmOpen(ExecutableLocationInfo location);

    void RenderResult(ExecutableLocationInfo location, ExecutableLocationAction action);
}

public sealed class ExecutableLocationView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : IExecutableLocationView
{
    /// <summary>Shows the exact executable location and selects a safe next action when input is interactive.</summary>
    public ExecutableLocationAction RenderAndSelect(ExecutableLocationInfo location, bool interactive)
    {
        ArgumentNullException.ThrowIfNull(location);
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "⌖", "@") + text.Text("Where.Title"),
            TerminalTheme.Accent);
        var locationGrid = TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(text.Text("Where.Executable"), location.ExecutablePath),
            TerminalTheme.CompactMetric(text.Text("Where.Directory"), location.DirectoryPath)
        ], preferredPairs: 1, width: console.Profile.Width);
        console.Write(locationGrid);
        console.WriteLine();

        if (!interactive)
        {
            return ExecutableLocationAction.ShowChangeDirectoryCommand;
        }

        return console.Prompt(
            new SelectionPrompt<ExecutableLocationAction>()
                .Title(Markup.Escape(text.Text("Where.Action")))
                .UseConverter(ActionLabel)
                .AddChoices(
                    ExecutableLocationAction.DoNothing,
                    ExecutableLocationAction.ShowChangeDirectoryCommand,
                    ExecutableLocationAction.OpenContainingFolder));
    }

    /// <summary>Previews the exact file-manager invocation and requests explicit authorization.</summary>
    public bool ConfirmOpen(ExecutableLocationInfo location)
    {
        ArgumentNullException.ThrowIfNull(location);
        console.Write(TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(text.Text("Where.OpenPreview"), location.OpenFolderPreview)
        ], preferredPairs: 1, width: console.Profile.Width));
        console.WriteLine();
        return console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Where.Confirm")))
        {
            DefaultValue = false
        });
    }

    /// <summary>Reports the authorized file-manager launch or a command that changes the calling terminal manually.</summary>
    public void RenderResult(ExecutableLocationInfo location, ExecutableLocationAction action)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (action == ExecutableLocationAction.DoNothing)
        {
            return;
        }

        if (action == ExecutableLocationAction.OpenContainingFolder)
        {
            console.MarkupLine($"[green]{Markup.Escape(text.Text("Where.Opened"))}[/]");
            return;
        }

        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Where.ChangeDirectoryHint"))}[/]");
        console.WriteLine(location.ChangeDirectoryCommand);
    }

    /// <summary>Converts executable-location actions into localized, intentionally ordered menu labels.</summary>
    private string ActionLabel(ExecutableLocationAction action) => action switch
    {
        ExecutableLocationAction.DoNothing => text.Text("Where.None"),
        ExecutableLocationAction.ShowChangeDirectoryCommand => text.Text("Where.ShowCd"),
        ExecutableLocationAction.OpenContainingFolder => text.Text("Where.Open"),
        _ => throw new InvalidOperationException("Unsupported executable-location action.")
    };
}

public interface IThirdPartyView
{
    void Render();
}

public sealed class ThirdPartyView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : IThirdPartyView
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
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "⚖", "=") + text.Text("ThirdParty.Title"),
            TerminalTheme.Accent);
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("ThirdParty.Subtitle"))}[/]");
        var table = new Table().Border(TableBorder.Rounded).BorderStyle(Style.Parse(TerminalTheme.Divider));
        table.AddColumn(text.Text("ThirdParty.Package"));
        table.AddColumn(text.Text("ThirdParty.Version"));
        table.AddColumn(text.Text("ThirdParty.License"));
        foreach (var package in Packages)
        {
            table.AddRow(Markup.Escape(package.Package), Markup.Escape(package.Version), Markup.Escape(package.License));
        }
        console.Write(table);
        console.WriteLine();
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("ThirdParty.FullNotices"))}[/]");
    }
}

public interface IPortablePathView
{
    PortablePathAction SelectAction();

    bool PreviewAndConfirm(PortablePathPlan plan, bool preauthorized);

    void RenderResult(PortablePathResult result);
}

public sealed class PortablePathView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : IPortablePathView
{
    /// <summary>Collects an install, remove, or status PATH action.</summary>
    public PortablePathAction SelectAction()
    {
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "↔", "<>") + text.Text("Path.Title"),
            TerminalTheme.Accent);
        return console.Prompt(new SelectionPrompt<PortablePathAction>()
            .Title(Markup.Escape(text.Text("Path.Action")))
            .UseConverter(action => action switch
            {
                PortablePathAction.Install => text.Text("Path.Install"),
                PortablePathAction.Remove => text.Text("Path.Remove"),
                _ => text.Text("Path.Status")
            })
            .AddChoices(PortablePathAction.Status, PortablePathAction.Install, PortablePathAction.Remove));
    }

    /// <summary>Shows the exact persistent target and asks before a mutating PATH operation.</summary>
    public bool PreviewAndConfirm(PortablePathPlan plan, bool preauthorized)
    {
        ArgumentNullException.ThrowIfNull(plan);
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "↔", "<>") + text.Text("Path.Title"),
            TerminalTheme.Accent);
        console.MarkupLine($"[bold {TerminalTheme.Info}]{Markup.Escape(PreviewText(plan.Action))}[/]");
        console.Write(TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(text.Text("Path.Target"), DisplayTarget(plan.PersistenceTarget)),
            TerminalTheme.CompactMetric(text.Text("Path.Directory"), plan.ExecutableDirectory)
        ], preferredPairs: 1, width: console.Profile.Width));
        console.WriteLine();
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
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, result.IsPresent ? "✅" : "⚠", result.IsPresent ? "+" : "!") + text.Text("Path.Title"),
            result.IsPresent ? TerminalTheme.Success : "yellow");
        console.MarkupLine($"[{(result.IsPresent ? "green" : "yellow")}]{Markup.Escape(message)}[/]");
        console.Write(TerminalTheme.PairGrid(
        [
            TerminalTheme.CompactMetric(text.Text("Path.Target"), DisplayTarget(result.PersistenceTarget)),
            TerminalTheme.CompactMetric(text.Text("Path.Directory"), result.ExecutableDirectory)
        ], preferredPairs: 1, width: console.Profile.Width));
    }

    /// <summary>Returns the localized intent of one portable PATH operation without altering its exact target data.</summary>
    private string PreviewText(PortablePathAction action) => action switch
    {
        PortablePathAction.Install => text.Text("Path.Preview.Install"),
        PortablePathAction.Remove => text.Text("Path.Preview.Remove"),
        _ => text.Text("Path.Preview.Status")
    };

    /// <summary>Localizes the Windows target description while preserving literal Unix profile paths.</summary>
    private string DisplayTarget(string target) => target.Equals("Windows user PATH", StringComparison.Ordinal)
        ? text.Text("Path.WindowsUserTarget")
        : target;
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IHelpView
{
    void Render(Action? openMemories = null);

    /// <summary>Prints help in the current buffer when an error or redirected output must remain visible.</summary>
    void RenderStatic() => Render();
}

/// <summary>Presents the command reference as a keyboard browser or ordinary scrolling output.</summary>
public sealed class HelpView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell,
    IAboutView about) : IHelpView
{
    /// <summary>Opens section navigation when the terminal supports a disposable fullscreen viewport.</summary>
    public void Render(Action? openMemories = null)
    {
        if (FullscreenHelpView.CanUse(console))
        {
            var originalOptions = shell.Options;
            try
            {
                shell.Configure(originalOptions with { SuppressFooter = true });
                new FullscreenHelpView(console, text, shell.Options).Render(CreateSections(openMemories));
            }
            finally
            {
                shell.Configure(originalOptions);
            }
            return;
        }
        RenderStatic();
    }

    /// <summary>Prints every section without hiding errors, entering an alternate buffer, or waiting for input.</summary>
    public void RenderStatic()
    {
        TerminalTheme.WriteRule(
            console,
            TerminalTheme.IconPrefix(shell.Options, "⌨", ">") + text.Text("Help.Title"),
            TerminalTheme.Accent);
        console.Write(HelpCommandLine.CreateDescription(text.Text("Help.Usage"), "hm"));
        console.WriteLine();
        foreach (var section in CreateSections())
        {
            RenderGroup(section);
        }
        shell.RenderProjectBanner();
    }

    /// <summary>Shares the complete localized command catalog between fullscreen and scrolling help.</summary>
    private IReadOnlyList<HelpSection> CreateSections(Action? openMemories = null) =>
    [
        new("⚡", text.Text("Help.Examples"), text.Text("Help.Browse.Examples"),
            [
                new($"hm \"{text.Text("Help.ExamplePrompt")}\"", text.Text("Help.Query"))
                {
                    Arguments = [new($"\"{text.Text("Help.ExamplePrompt")}\"", text.Text("Help.Argument.Question"))]
                },
                new("hm --chat", text.Text("Help.Chat")),
                new("hm --help", text.Text("Help.Usage"))
            ]),
        new("💬", text.Text("Help.Group.Ai"), text.Text("Help.Browse.Ai"),
            [
                new(text.Text("Help.QuerySyntax"), text.Text("Help.Query"))
                {
                    Example = $"hm --query \"{text.Text("Help.ExamplePrompt")}\"",
                    Arguments = [new($"\"{text.Text("Help.ExamplePrompt")}\"", text.Text("Help.Argument.Question"))]
                },
                new("--diagnose [--file <path>]", text.Text("Diagnose.Help"))
                {
                    Example = $"hm --diagnose \"{text.Text("Help.Browse.DiagnoseExample")}\"",
                    Arguments = [new($"\"{text.Text("Help.Browse.DiagnoseExample")}\"", text.Text("Help.Argument.Error"))]
                },
                new("--script <request> [--file <path>] [--output <path>]", text.Text("Script.Help"))
                {
                    Example = $"hm --script \"{text.Text("Help.Browse.ScriptExample")}\"",
                    Arguments = [new($"\"{text.Text("Help.Browse.ScriptExample")}\"", text.Text("Help.Argument.Script"))]
                },
                new("--plan <goal> | --plan --resume <id>", text.Text("Plan.Help"))
                {
                    Example = $"hm --plan \"{text.Text("Help.Browse.PlanExample")}\"",
                    Arguments = [new($"\"{text.Text("Help.Browse.PlanExample")}\"", text.Text("Help.Argument.Goal"))]
                },
                new("--preview <copy|move|rename|delete> --file <path>", text.Text("Preview.Help"))
                {
                    Example = "hm --preview rename --file . --pattern '*.txt' --prefix reviewed-",
                    Arguments =
                    [
                        new("rename", text.Text("Help.Argument.Rename")),
                        new("--file", text.Text("Help.Argument.File")),
                        new(".", text.Text("Help.Argument.CurrentFolder")),
                        new("--pattern", text.Text("Help.Argument.Pattern")),
                        new("'*.txt'", text.Text("Help.Argument.TextFiles")),
                        new("reviewed-", text.Text("Help.Argument.Prefix"))
                    ]
                },
                new("--recipes [list|show|save|import|export|run]", text.Text("Recipe.Help"))
                {
                    Example = "hm --recipes list",
                    Arguments = [new("list", text.Text("Help.Argument.List"))]
                },
                new("--chat", text.Text("Help.Chat")),
                new("--test-ai", text.Text("Help.Test"))
            ]),
        new("📊", text.Text("Help.Group.Insight"), text.Text("Help.Browse.Insight"),
            [
                new("--version, -v", text.Text("Help.Version")) { Example = "hm --version" },
                new("--status", text.Text("Help.Status")),
                new("--lenna, lenna", text.Text("Help.Lenna")) { Example = "hm lenna" },
                new("--costs", text.Text("Help.Costs")),
                new("--where, -where", text.Text("Help.Where")) { Example = "hm --where" },
                new("--third-party", text.Text("Help.ThirdParty"))
            ]),
        new("⚙", text.Text("Help.Group.Setup"), text.Text("Help.Browse.Setup"),
            [
                new("--setup", text.Text("Help.Setup")),
                new("--ai-setup, --ai-settings", text.Text("AiSettings.Help")) { Example = "hm --ai-setup" },
                new("--theme", text.Text("Theme.Help")),
                new("--path [install|remove|status]", text.Text("Help.Path"))
                {
                    Example = "hm --path status",
                    Arguments = [new("status", text.Text("Help.Argument.PathStatus"))]
                },
                new("--install-font [--dry-run]", text.Text("Help.Font")) { Example = "hm --install-font" },
                new(text.Text("Help.LanguageSyntax"), text.Text("Help.Language"))
                {
                    Example = $"hm --status --language {text.Language}",
                    Arguments = [new(text.Language, text.Text("Help.Argument.Language"))]
                }
            ]),
        new("🛡", text.Text("Help.Group.Safety"), text.Text("Help.Browse.Safety"),
            [
                new("--no-animation | --no-emoji", text.Text("Help.Rendering"))
                {
                    Example = "hm --status --no-animation --no-emoji",
                    Arguments =
                    [
                        new("--no-animation", text.Text("Help.Argument.NoAnimation")),
                        new("--no-emoji", text.Text("Help.Argument.NoEmoji"))
                    ]
                },
                new("--yes, -y", text.Text("Help.Yes"))
                {
                    Example = "hm --install-font --yes",
                    Arguments = [new("--yes", text.Text("Help.Argument.Yes"))]
                },
                new("--dry-run", text.Text("Help.DryRun"))
                {
                    Example = "hm --install-font --dry-run",
                    Arguments = [new("--dry-run", text.Text("Help.Argument.DryRun"))]
                }
            ]),
        new("🧠", text.Text("Settings.Memories"), text.Text("Settings.Memories"),
            [new("--memories", text.Text("Help.Memories"))])
        {
            Open = openMemories,
            OpenHintKey = "MemoryManager.OpenHint"
        },
        new("ℹ️", text.Text("About.Title"), text.Text("About.MenuLabel"),
            [new("--about, about", text.Text("Help.About")) { Example = "hm about" }])
        {
            Open = about.Render,
            OpenHintKey = "About.OpenHint"
        }
    ];

    /// <summary>Renders one cohesive command category without turning the help screen into a flat flag dump.</summary>
    private void RenderGroup(HelpSection section)
    {
        TerminalTheme.WriteRule(
            console,
            $"{TerminalTheme.IconPrefix(shell.Options, section.Icon, ">")}{section.Title}",
            TerminalTheme.Accent);
        console.Write(new Rows(section.Entries.Select(FullscreenHelpView.RenderEntry)));
        console.WriteLine();
    }
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
            console.MarkupLine($"[{TerminalTheme.Success}]{Markup.Escape(text.Text("Where.Opened"))}[/]");
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
        ("Microsoft.Data.Sqlite", "10.0.12", "MIT"),
        ("Microsoft.Extensions.DependencyInjection", "10.0.12", "MIT"),
        ("Microsoft.Extensions.Http", "10.0.12", "MIT"),
        ("Microsoft.Extensions.Logging", "10.0.12", "MIT"),
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
        var table = new Table().Border(TableBorder.None);
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
            result.IsPresent ? TerminalTheme.Success : TerminalTheme.Warning);
        console.MarkupLine($"[{(result.IsPresent ? TerminalTheme.Success : TerminalTheme.Warning)}]{Markup.Escape(message)}[/]");
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

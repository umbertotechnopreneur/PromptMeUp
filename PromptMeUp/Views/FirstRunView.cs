// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Supplies passive first-run steps without accessing credentials or the network.</summary>
public interface IFirstRunView
{
    void RenderWelcome();
    void RenderSetupRequired();
    Task<FirstRunInput<string>> ChooseLanguageAsync(string language, CancellationToken ct);
    Task<FirstRunInput<string?>> ReadKeyAsync(bool configured, bool protectedStorage, CancellationToken ct);
    Task<FirstRunAction> ReadConnectionFailureAsync(string errorKey, CancellationToken ct);
    Task<FirstRunInput<FirstRunPreferences>> ReadPreferencesAsync(string currentName,
        SettingsFeatureOverview overview, CancellationToken ct);
    Task<bool> ChooseDesktopAsync(CancellationToken ct);
    void RenderReady(string name, string guidePath);
    Task<bool> ChooseGuideAsync(CancellationToken ct);
}

/// <summary>Renders an open, scrolling welcome with accessible keyboard actions.</summary>
public sealed class FirstRunView(IAnsiConsole console, ILocalizationService text,
    ISensitiveDataRedactor redactor, IConsoleShellView shell) : IFirstRunView
{
    /// <summary>Introduces the product before asking for the first preference.</summary>
    public void RenderWelcome()
    {
        WelcomeBanner.Render(console);
        console.MarkupLine($"[bold {TerminalTheme.Accent}]{Markup.Escape(Icon("✨", "*") + text.Text("Oobe.Welcome"))}[/]");
        Write("Benefit");
        Write("Journey", TerminalTheme.Muted);
        console.WriteLine();
        Link(Icon("🔒", ">") + text.Text("Oobe.Privacy"), "https://umbertogiacobbi.biz/privacy/");
        Link(text.Text("Oobe.Terms"), "https://umbertogiacobbi.biz/terms/");
        Link(Icon("🔗", ">") + "GitHub", "https://github.com/umbertotechnopreneur/PromptMeUp");
        Link(Icon("❤️", "<3") + "Made with love by Umberto", "https://umbertogiacobbi.biz");
    }

    /// <summary>Explains how to resume when terminal input is redirected.</summary>
    public void RenderSetupRequired()
    {
        RenderWelcome();
        Write("InteractiveRequired", TerminalTheme.Warning);
    }

    /// <summary>Offers the detected language first and displays its ASCII flag.</summary>
    public async Task<FirstRunInput<string>> ChooseLanguageAsync(string language, CancellationToken ct)
    {
        Step(1, "Language", "Start");
        RenderFlag(language);
        var selected = SupportedLanguages.All.Single(item => item.Code == language);
        console.MarkupLine(Markup.Escape(text.Text("Oobe.Detected", selected.NativeName)));
        var choice = await ChooseAsync([text.Text("Oobe.KeepLanguage", selected.NativeName),
            text.Text("Oobe.ChangeLanguage"), text.Text("Oobe.Exit")], ct).ConfigureAwait(false);
        if (choice == 2)
        {
            return new(FirstRunAction.Exit, language);
        }
        if (choice == 1)
        {
            var languages = SupportedLanguages.All.OrderByDescending(item => item.Code == language).ToArray();
            var index = await ChooseAsync(languages.Select(item => item.NativeName).ToArray(), ct).ConfigureAwait(false);
            language = languages[index].Code;
            text.SetLanguage(language);
            RenderFlag(language);
        }
        return new(FirstRunAction.Next, language);
    }

    /// <summary>Collects a fully masked key or selects the existing credential without revealing it.</summary>
    public async Task<FirstRunInput<string?>> ReadKeyAsync(bool configured, bool protectedStorage, CancellationToken ct)
    {
        Step(2, "Connect", "KeyHelp");
        RenderConnectionIntro(protectedStorage);
        var labels = configured
            ? new[] { text.Text("Oobe.VerifyExisting"), text.Text("Oobe.ReplaceKey"), text.Text("Oobe.Back"), text.Text("Oobe.Exit") }
            : new[] { text.Text("Oobe.EnterKey"), text.Text("Oobe.Back"), text.Text("Oobe.Exit") };
        var choice = await ChooseAsync(labels, ct).ConfigureAwait(false);
        if (choice == labels.Length - 1) { return new(FirstRunAction.Exit, null); }
        if (choice == labels.Length - 2) { return new(FirstRunAction.Back, null); }
        if (configured && choice == 0) { return new(FirstRunAction.Next, null); }
        var key = await new TextPrompt<string>($"{Markup.Escape(text.Text("Oobe.KeyLabel").PadLeft(18))}  ")
            .Secret('•').AllowEmpty()
            .Validate(value => string.IsNullOrWhiteSpace(value) || (!value.Contains('*') && OpenAiKeyPolicy.IsPlausible(value.Trim()))
                ? ValidationResult.Success() : ValidationResult.Error(text.Text("Oobe.InvalidKey")))
            .ShowAsync(console, ct).ConfigureAwait(false);
        console.WriteLine();
        return string.IsNullOrWhiteSpace(key)
            ? new(FirstRunAction.Back, null) : new(FirstRunAction.Next, key.Trim());
    }

    /// <summary>Places the key guidance beside a compact OpenAI ASCII mark on wide terminals.</summary>
    private void RenderConnectionIntro(bool protectedStorage)
    {
        var content = new Rows(
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Oobe." + (protectedStorage ? "Vault" : "SessionStorage")))}[/]"),
            new Text(" "),
            new Markup($"[underline {TerminalTheme.Info} link=https://platform.openai.com/api-keys]"
                + $"{Markup.Escape(text.Text("Oobe.Portal"))}[/]"),
            new Markup($"[underline {TerminalTheme.Info} link=https://developers.openai.com/api/docs/quickstart#create-and-export-an-api-key]"
                + $"{Markup.Escape(text.Text("Oobe.Guide"))}[/]"),
            new Text(" "),
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Oobe.Example"))}[/]"),
            new Markup($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Oobe.Cost"))}[/]"));
        if (console.Profile.Width >= 110)
        {
            var logo = new Rows(
                new Text("     .-==-.     \n   .'/ /\\ \\'.   \n  / / /  \\ \\ \\  \n | | | /\\ | | | \n  \\ \\ \\/ / / /  \n   '.\\_\\/_.''   \n     '-..-'     ", Style.Parse(TerminalTheme.Info)),
                new Text("     OpenAI", Style.Parse("bold " + TerminalTheme.Primary)));
            var grid = new Grid()
                .AddColumn(new GridColumn { Width = console.Profile.Width - 38 })
                .AddColumn(new GridColumn { Width = 28 });
            grid.AddRow(content, logo);
            console.Write(grid);
        }
        else
        {
            console.Write(content);
        }
        console.WriteLine();
    }

    /// <summary>Shows a secret-free failure and allows retrying without retyping the key.</summary>
    public async Task<FirstRunAction> ReadConnectionFailureAsync(string errorKey, CancellationToken ct)
    {
        Write(errorKey, TerminalTheme.Warning);
        Link(text.Text("Oobe.Portal"), "https://platform.openai.com/api-keys");
        Link(text.Text("Oobe.Billing"), "https://platform.openai.com/settings/organization/billing/overview");
        var choice = await ChooseAsync([text.Text("Oobe.Retry"), text.Text("Oobe.ReplaceKey"), text.Text("Oobe.Exit")], ct)
            .ConfigureAwait(false);
        return choice switch { 0 => FirstRunAction.Next, 1 => FirstRunAction.Back, _ => FirstRunAction.Exit };
    }

    /// <summary>Collects the optional name, independent learning consent, command mode, and reviewed skills.</summary>
    public async Task<FirstRunInput<FirstRunPreferences>> ReadPreferencesAsync(string currentName,
        SettingsFeatureOverview overview, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(overview);
        Step(3, "Personalize", "TwoLeft");
        Section("👋", "Name");
        Write("NameHelp");
        var prompt = new TextPrompt<string>($"{Markup.Escape(text.Text("Oobe.NameLabel").PadLeft(18))}  ")
            .AllowEmpty().Validate(ValidateName);
        if (!string.IsNullOrEmpty(currentName)) { prompt.DefaultValue(currentName); }
        var name = await prompt.ShowAsync(console, ct).ConfigureAwait(false);
        name = PreferredNamePolicy.Normalize(name, redactor);

        Section("🧠", "Memory");
        Write("MemoryHelp");
        var enabled = await YesNoAsync("EnableMemory", overview.Settings.Enabled, ct).ConfigureAwait(false);
        var capture = false;
        if (enabled)
        {
            Write("RecordingHelp", TerminalTheme.Muted);
            capture = await YesNoAsync("EnableRecording", false, ct).ConfigureAwait(false);
        }
        else
        {
            Write("RecordingOff", TerminalTheme.Muted);
        }
        if ((overview.Settings.Enabled && !enabled) || (overview.Settings.CaptureObservations && !capture))
        {
            Write("ClearLearning", TerminalTheme.Warning);
            if (!await YesNoAsync("ConfirmClear", false, ct).ConfigureAwait(false))
            {
                return new(FirstRunAction.Back, new(name, enabled, capture, false, []));
            }
        }

        var selectedSkills = Array.Empty<string>();
        if (enabled)
        {
            Section("🧩", "SkillTitle");
            Write("SkillHelp");
            var available = overview.Skills.Where(item => item.Skill.Origin == "bundled"
                && item.Skill.UnavailableReason is null).ToArray();
            if (available.Length == 0)
            {
                Write("SkillsEmpty", TerminalTheme.Muted);
            }
            else if (await YesNoAsync("EnableSkills", false, ct).ConfigureAwait(false))
            {
                selectedSkills = await SelectSkillsAsync(available, ct).ConfigureAwait(false);
            }
        }

        Section("🛡", "CommandMode");
        Write("DirectNotice", TerminalTheme.Muted);
        var confirmCommands = await YesNoAsync("ConfirmCommands", false, ct).ConfigureAwait(false);
        Write("HistoryNotice", TerminalTheme.Muted);
        return new(FirstRunAction.Next, new(name, enabled, capture, confirmCommands, selectedSkills));
    }

    /// <summary>Separates related onboarding choices with a short theme gradient and an explicit icon.</summary>
    private void Section(string emoji, string key)
    {
        console.WriteLine();
        console.Write(new ThemeSeparator(Icon(emoji, ">") + text.Text("Oobe." + key),
            TerminalTheme.Accent, Math.Min(console.Profile.Width, 88)));
        console.WriteLine();
    }

    /// <summary>Lists only inspected, usable skill packages and returns exactly the user's checked names.</summary>
    private async Task<string[]> SelectSkillsAsync(SettingsSkillState[] available, CancellationToken ct)
    {
        var selected = await new MultiSelectionPrompt<SettingsSkillState>()
            .Title($"[bold {TerminalTheme.Accent}]{Markup.Escape(text.Text("Oobe.SkillsPrompt"))}[/]")
            .NotRequired()
            .InstructionsText(text.Text("Oobe.SkillsInstructions"))
            .HighlightStyle(Style.Parse(TerminalTheme.Accent))
            .AddChoices(available)
            .UseConverter(item =>
            {
                var skill = item.Skill;
                var icon = shell.Options.NoEmoji ? "* " : Markup.Escape(skill.Icon) + " ";
                return $"{icon}[bold {TerminalTheme.Primary}]{Markup.Escape(SkillLabel(skill))}[/] "
                    + $"[{TerminalTheme.Muted}]· {Markup.Escape(skill.Name)}[/]";
            })
            .ShowAsync(console, ct).ConfigureAwait(false);
        var names = selected.Select(item => item.Skill.Name).ToArray();
        console.MarkupLine($"[{TerminalTheme.Success}]{Markup.Escape(text.Text("Oobe.SkillsSelected", names.Length))}[/]");
        foreach (var item in selected)
        {
            console.MarkupLine($"  [{TerminalTheme.Primary}]{Markup.Escape(Icon(item.Skill.Icon, "*") + SkillLabel(item.Skill))}[/]");
        }
        return names;
    }

    /// <summary>Uses translated bundled names while preserving each skill's stable package identity.</summary>
    private string SkillLabel(SkillDefinition skill)
    {
        var key = "Lab.SkillName." + skill.Name;
        var label = text.Text(key);
        return label == key ? skill.Name : label;
    }

    /// <summary>Offers an unchecked desktop shortcut option after the privacy choices.</summary>
    public async Task<bool> ChooseDesktopAsync(CancellationToken ct)
    {
        console.WriteLine();
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Home.DesktopHelp"))}[/]");
        var selected = await new MultiSelectionPrompt<string>()
            .Title($"[bold {TerminalTheme.Accent}]{Markup.Escape(TerminalTheme.IconPrefix(shell.Options, "🖥️", ">") + text.Text("Home.Desktop"))}[/]")
            .NotRequired().InstructionsText(text.Text("Home.CheckboxHelp"))
            .HighlightStyle(Style.Parse(TerminalTheme.Accent))
            .AddChoices(text.Text("Home.Desktop"))
            .ShowAsync(console, ct).ConfigureAwait(false);
        return selected.Count > 0;
    }

    /// <summary>Closes onboarding without opening settings or starting an unrequested conversation.</summary>
    public void RenderReady(string name, string guidePath)
    {
        console.WriteLine();
        TerminalTheme.WriteRule(console, Icon("🎉", "*") + text.Text("Oobe.Ready"), TerminalTheme.Success);
        console.MarkupLine($"[bold {TerminalTheme.Primary}]{Markup.Escape(string.IsNullOrEmpty(name)
            ? text.Text("Oobe.Thanks") : text.Text("Oobe.ThanksName", name))}[/]");
        Write("StartUsing", TerminalTheme.Muted);
        RenderStarterCommands();
        console.WriteLine();
        console.Write(new ThemeSeparator(Icon("📄", ">") + text.Text("Guide.Title"),
            TerminalTheme.Accent, Math.Min(console.Profile.Width, 88)));
        console.WriteLine();
        Link(text.Text("Guide.Title"), new Uri(guidePath).AbsoluteUri, showAddress: false);
        console.MarkupLine($"[{TerminalTheme.Muted}]{Markup.Escape(text.Text("Guide.Description"))}[/]");
        console.WriteLine();
        Write("ChangeLater", TerminalTheme.Muted);
        console.WriteLine();
    }

    /// <summary>Offers the installed PDF after setup, with finishing selected by default.</summary>
    public async Task<bool> ChooseGuideAsync(CancellationToken ct) =>
        await ChooseAsync([text.Text("Guide.Finish"), text.Text("Guide.Open")], ct).ConfigureAwait(false) == 1;

    /// <summary>Shows three practical starting points in an open two-column command grid.</summary>
    private void RenderStarterCommands()
    {
        console.WriteLine();
        console.Write(new ThemeSeparator(Icon("🚀", ">") + text.Text("Oobe.TryFirst"),
            TerminalTheme.Accent, Math.Min(console.Profile.Width, 88)));
        console.WriteLine();
        var grid = new Grid()
            .AddColumn(new GridColumn().LeftAligned())
            .AddColumn(new GridColumn());
        foreach (var example in new (string Command, string Description)[]
        {
            ($"hm \"{text.Text("Oobe.TryAskCommand")}\"", "TryAsk"),
            ("hm --chat", "TryChat"),
            ("hm --diagnose", "TryDiagnose")
        })
        {
            grid.AddRow(
                new Markup($"[bold {TerminalTheme.Info}]{Markup.Escape(example.Command)}[/]"),
                new Markup($"[{TerminalTheme.Primary}]{Markup.Escape(text.Text("Oobe." + example.Description))}[/]"));
        }
        console.Write(grid);
    }

    /// <summary>Renders one numbered section beneath the completed steps without clearing scrollback.</summary>
    private void Step(int number, string title, string hint)
    {
        var icon = number switch { 1 => "🌍", 2 => "🔑", 3 => "👋", _ => "🧠" };
        TerminalTheme.WriteRule(console,
            Icon(icon, ">") + text.Text("Oobe.Step", number) + " · " + text.Text("Oobe." + title), TerminalTheme.Accent);
        Write(hint, TerminalTheme.Muted);
        console.WriteLine();
    }

    /// <summary>Writes escaped localized text in the shared palette.</summary>
    private void Write(string key, string? color = null)
    {
        var copy = Markup.Escape(text.Text("Oobe." + key));
        foreach (var command in new[]
        {
            "hm --prepare-logs", "hm --learning", "hm --skills", "hm --status", "hm --setup", "hm --help"
        })
        {
            copy = copy.Replace(command, $"[bold {TerminalTheme.Info}]{command}[/]", StringComparison.Ordinal);
        }
        var line = new Grid().AddColumn(new GridColumn { Width = Math.Min(console.Profile.Width, 104) });
        line.AddRow(new Markup($"[{color ?? TerminalTheme.Primary}]{copy}[/]"));
        console.Write(line);
    }

    /// <summary>Respects the user's emoji preference and the shared one-space icon convention.</summary>
    private string Icon(string emoji, string fallback) => TerminalTheme.IconPrefix(shell.Options, emoji, fallback);

    /// <summary>Renders a terminal hyperlink together with its copyable address.</summary>
    private void Link(string label, string url, bool showAddress = true) =>
        console.MarkupLine($"[underline {TerminalTheme.Info} link={url}]{Markup.Escape(label)}[/]"
            + (showAddress ? $" [{TerminalTheme.Muted}]{Markup.Escape(url)}[/]" : string.Empty));

    /// <summary>Shows keyboard choices with the terminal's visible focus marker.</summary>
    private async Task<int> ChooseAsync(string[] labels, CancellationToken ct)
    {
        var selected = await new SelectionPrompt<int>().HighlightStyle(Style.Parse(TerminalTheme.Accent))
            .AddChoices(Enumerable.Range(0, labels.Length)).UseConverter(index => ActionLabel(labels[index]))
            .ShowAsync(console, ct).ConfigureAwait(false);
        console.MarkupLine($"[{TerminalTheme.Success}]{Markup.Escape(Icon("✓", "v") + labels[selected])}[/]");
        return selected;
    }

    /// <summary>Uses semantic colors for positive, negative, and navigation choices.</summary>
    private string ActionLabel(string label)
    {
        var color = label == text.Text("Oobe.Exit") || label == text.Text("Common.No")
            ? TerminalTheme.Warning
            : label == text.Text("Oobe.Back") ? TerminalTheme.Muted : TerminalTheme.Success;
        return $"[{color}]{Markup.Escape(label)}[/]";
    }

    /// <summary>Places the intended default first without implicitly accepting consent.</summary>
    private async Task<bool> YesNoAsync(string title, bool initial, CancellationToken ct)
    {
        Write(title, TerminalTheme.Accent);
        var choices = initial ? new[] { "Common.Yes", "Common.No" } : new[] { "Common.No", "Common.Yes" };
        var selected = await ChooseAsync(choices.Select(key => text.Text(key)).ToArray(), ct).ConfigureAwait(false);
        console.WriteLine();
        return choices[selected] == "Common.Yes";
    }

    /// <summary>Validates a nickname without echoing rejected text.</summary>
    private ValidationResult ValidateName(string value)
    {
        try { PreferredNamePolicy.Normalize(value, redactor); return ValidationResult.Success(); }
        catch (ArgumentException) { return ValidationResult.Error(text.Text("Setup.PreferredNameInvalid")); }
    }

    /// <summary>Draws compact language flags with ASCII glyphs and colors.</summary>
    private void RenderFlag(string language)
    {
        IEnumerable<string> rows = language switch
        {
            "it" => Enumerable.Repeat("[green]||||||[/][white]||||||[/][red]||||||[/]", 5),
            "fr" => Enumerable.Repeat("[blue]||||||[/][white]||||||[/][red]||||||[/]", 5),
            "de" => ["[white on black]==================[/]", "[white on black]==================[/]", "[red]==================[/]", "[yellow]==================[/]", "[yellow]==================[/]"],
            "es" => ["[red]==================[/]", "[yellow]==================[/]", "[yellow]==================[/]", "[yellow]==================[/]", "[red]==================[/]"],
            "vi" => ["[red]==================[/]", "[red]========[/][yellow]*[/][red]=========[/]", "[red]======[/][yellow]*****[/][red]=======[/]", "[red]=======[/][yellow]* *[/][red]========[/]", "[red]==================[/]"],
            _ => ["[white on blue]* * * *[/][red]===========[/]", "[white on blue] * * * [/][white]===========[/]", "[white on blue]* * * *[/][red]===========[/]", "[white]==================[/]", "[red]==================[/]"]
        };
        foreach (var row in rows) { console.MarkupLine("  " + row); }
        console.WriteLine();
    }
}

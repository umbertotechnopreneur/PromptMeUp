// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Supplies passive first-run steps without accessing credentials or the network.</summary>
public interface IFirstRunView
{
    void RenderWelcome();
    void RenderSetupRequired();
    Task<FirstRunInput<string>> ChooseLanguageAsync(string language, CancellationToken ct);
    Task<FirstRunInput<string?>> ReadKeyAsync(bool configured, bool protectedStorage, CancellationToken ct);
    Task<FirstRunAction> ReadConnectionFailureAsync(string errorKey, CancellationToken ct);
    Task<FirstRunInput<string>> ReadNameAsync(string current, CancellationToken ct);
    Task<FirstRunInput<FirstRunMemoryChoice>> ReadMemoryAsync(SkillsAndMemorySettings current, CancellationToken ct);
    Task<bool> ChooseDesktopAsync(CancellationToken ct);
    void RenderReady(string name);
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
        Write(protectedStorage ? "Vault" : "SessionStorage", TerminalTheme.Muted);
        Link(text.Text("Oobe.Portal"), "https://platform.openai.com/api-keys");
        Link(text.Text("Oobe.Guide"), "https://developers.openai.com/api/docs/quickstart#create-and-export-an-api-key");
        console.WriteLine();
        Write("Example", TerminalTheme.Muted);
        Write("Cost", TerminalTheme.Muted);
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

    /// <summary>Collects an optional nickname without accepting recognizable credentials.</summary>
    public async Task<FirstRunInput<string>> ReadNameAsync(string current, CancellationToken ct)
    {
        Step(3, "Name", "TwoLeft");
        Write("NameHelp");
        var prompt = new TextPrompt<string>($"{Markup.Escape(text.Text("Oobe.NameLabel").PadLeft(18))}  ")
            .AllowEmpty().Validate(ValidateName);
        if (!string.IsNullOrEmpty(current)) { prompt.DefaultValue(current); }
        var name = await prompt.ShowAsync(console, ct).ConfigureAwait(false);
        console.WriteLine();
        return new(await NavigationAsync(ct).ConfigureAwait(false), PreferredNamePolicy.Normalize(name, redactor));
    }

    /// <summary>Separates memory activation from explicit consent to collect redacted learning material.</summary>
    public async Task<FirstRunInput<FirstRunMemoryChoice>> ReadMemoryAsync(SkillsAndMemorySettings current, CancellationToken ct)
    {
        Step(4, "Memory", "AlmostThere");
        Write("MemoryHelp");
        var enabled = await YesNoAsync("EnableMemory", current.Enabled, ct).ConfigureAwait(false);
        Write("DreamHelp");
        Write("RecordingHelp", TerminalTheme.Muted);
        Write("HistoryNotice", TerminalTheme.Muted);
        var capture = enabled && await YesNoAsync("EnableRecording", false, ct).ConfigureAwait(false);
        if (!enabled) { Write("RecordingOff", TerminalTheme.Muted); }
        if ((current.Enabled && !enabled) || (current.CaptureObservations && !capture))
        {
            Write("ClearLearning", TerminalTheme.Warning);
            if (!await YesNoAsync("ConfirmClear", false, ct).ConfigureAwait(false))
            {
                return new(FirstRunAction.Back, new(enabled, capture, false));
            }
        }
        Write("DirectNotice", TerminalTheme.Muted);
        var confirmCommands = await YesNoAsync("ConfirmCommands", false, ct).ConfigureAwait(false);
        return new(await NavigationAsync(ct).ConfigureAwait(false), new(enabled, capture, confirmCommands));
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
    public void RenderReady(string name)
    {
        console.WriteLine();
        TerminalTheme.WriteRule(console, Icon("🎉", "*") + text.Text("Oobe.Ready"), TerminalTheme.Success);
        console.MarkupLine($"[bold {TerminalTheme.Primary}]{Markup.Escape(string.IsNullOrEmpty(name)
            ? text.Text("Oobe.Thanks") : text.Text("Oobe.ThanksName", name))}[/]");
        Write("StartUsing");
        Write("Skills", TerminalTheme.Muted);
        Write("ChangeLater", TerminalTheme.Muted);
        console.WriteLine();
    }

    /// <summary>Renders a numbered section without clearing terminal history.</summary>
    private void Step(int number, string title, string hint)
    {
        console.WriteLine();
        var icon = number switch { 1 => "🌍", 2 => "🔑", 3 => "👋", _ => "🧠" };
        TerminalTheme.WriteRule(console, Icon(icon, ">") + text.Text("Oobe.Step", number) + " · " + text.Text("Oobe." + title), TerminalTheme.Accent);
        Write(hint, TerminalTheme.Muted);
        console.WriteLine();
    }

    /// <summary>Writes escaped localized text in the shared palette.</summary>
    private void Write(string key, string? color = null)
    {
        var copy = Markup.Escape(text.Text("Oobe." + key));
        foreach (var command in new[] { "hm --help", "hm --skills", "hm --learning", "hm --setup" })
        {
            copy = copy.Replace(command, $"[bold {TerminalTheme.Info}]{command}[/]", StringComparison.Ordinal);
        }
        console.MarkupLine($"[{color ?? TerminalTheme.Primary}]{copy}[/]");
    }

    /// <summary>Respects the user's emoji preference and the shared one-space icon convention.</summary>
    private string Icon(string emoji, string fallback) => TerminalTheme.IconPrefix(shell.Options, emoji, fallback);

    /// <summary>Renders a terminal hyperlink together with its copyable address.</summary>
    private void Link(string label, string url) =>
        console.MarkupLine($"[underline {TerminalTheme.Info} link={url}]{Markup.Escape(label)}[/] [{TerminalTheme.Muted}]{url}[/]");

    /// <summary>Shows keyboard choices with the terminal's visible focus marker.</summary>
    private Task<int> ChooseAsync(string[] labels, CancellationToken ct)
    {
        TerminalTheme.WriteRule(console, string.Empty, TerminalTheme.Muted);
        return new SelectionPrompt<int>().HighlightStyle(Style.Parse(TerminalTheme.Accent))
            .AddChoices(Enumerable.Range(0, labels.Length)).UseConverter(index => ActionLabel(labels[index]))
            .ShowAsync(console, ct);
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

    /// <summary>Offers consistent unboxed continuation, back, and exit actions.</summary>
    private async Task<FirstRunAction> NavigationAsync(CancellationToken ct)
    {
        var selected = await ChooseAsync([text.Text("Oobe.Continue"), text.Text("Oobe.Back"), text.Text("Oobe.Exit")], ct)
            .ConfigureAwait(false);
        return selected switch { 0 => FirstRunAction.Next, 1 => FirstRunAction.Back, _ => FirstRunAction.Exit };
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

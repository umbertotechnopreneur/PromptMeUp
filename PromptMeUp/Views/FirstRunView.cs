// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IFirstRunView
{
    /// <summary>Shows why setup must be completed before commands can run.</summary>
    void RenderSetupRequired();

    /// <summary>Asks whether setup should open before the requested command continues.</summary>
    bool ConfirmSetup();
}

/// <summary>Shows the localized first-run choice without owning setup or persistence behavior.</summary>
public sealed class FirstRunView(
    IAnsiConsole console,
    ILocalizationService text,
    IConsoleShellView shell) : IFirstRunView
{
    private const string PrivacyUrl = "https://umbertogiacobbi.biz/privacy/";
    private const string TermsUrl = "https://umbertogiacobbi.biz/terms/";
    private const string RepositoryUrl = "https://github.com/umbertotechnopreneur/PromptMeUp";
    private const string AuthorUrl = "https://umbertogiacobbi.biz";

    /// <summary>Asks whether setup should open now and treats Escape or cancel as a return to the shell.</summary>
    public bool ConfirmSetup()
    {
        var copy = FirstRunCopy.For(text.Language);
        RenderSetupRequired();

        var choice = console.Prompt(
            new SelectionPrompt<FirstRunChoice>()
                .Title($"[bold {TerminalTheme.Accent}]{Markup.Escape(copy.Question)}[/]")
                .HighlightStyle(Style.Parse(TerminalTheme.Accent))
                .AddChoices(FirstRunChoice.Setup, FirstRunChoice.Cancel)
                .UseConverter(value => value == FirstRunChoice.Setup
                    ? $"[bold {TerminalTheme.Success}]✓ {Markup.Escape(copy.Ok)}[/]"
                    : $"[{TerminalTheme.Warning}]↩ {Markup.Escape(copy.Cancel)}[/]"));

        return choice == FirstRunChoice.Setup;
    }

    /// <summary>Renders the localized first-run requirement without requesting terminal input.</summary>
    public void RenderSetupRequired()
    {
        var copy = FirstRunCopy.For(text.Language);
        var icon = TerminalTheme.IconPrefix(shell.Options, "✨", "*");
        var repositoryIcon = TerminalTheme.IconPrefix(shell.Options, "🔗", ">");
        var authorIcon = TerminalTheme.IconPrefix(shell.Options, "❤️", "<3");

        console.WriteLine();
        TerminalTheme.WriteRule(console, icon + copy.Title, TerminalTheme.Accent);
        console.MarkupLine($"[{TerminalTheme.Primary}]{Markup.Escape(copy.Message)}[/]");
        console.MarkupLine($"[{TerminalTheme.Muted}]Privacy:[/] [underline {TerminalTheme.Info} link={PrivacyUrl}]{PrivacyUrl}[/]  ·  [{TerminalTheme.Muted}]Terms:[/] [underline {TerminalTheme.Info} link={TermsUrl}]{TermsUrl}[/]");
        console.WriteLine();
        console.MarkupLine($"{Markup.Escape(repositoryIcon)}[underline {TerminalTheme.Info} link={RepositoryUrl}]{RepositoryUrl}[/]  ·  {Markup.Escape(authorIcon)}[{TerminalTheme.Muted}]Made with love by [underline {TerminalTheme.Info} link={AuthorUrl}]umbertogiacobbi.biz[/][/]");
        console.WriteLine();
    }

    private enum FirstRunChoice
    {
        Setup,
        Cancel
    }

    private sealed record FirstRunCopy(
        string Title,
        string Message,
        string Question,
        string Ok,
        string Cancel)
    {
        /// <summary>Returns complete first-run copy in each supported runtime language.</summary>
        public static FirstRunCopy For(string language) => language switch
        {
            "it" => new(
                "Primo avvio",
                "Ho rilevato che questo è il primo avvio. Devi prima configurare le opzioni per continuare.",
                "Vuoi aprire ora il setup?",
                "OK",
                "Annulla"),
            "fr" => new(
                "Premier démarrage",
                "J’ai détecté qu’il s’agit du premier démarrage. Vous devez d’abord configurer les options pour continuer.",
                "Voulez-vous ouvrir la configuration maintenant ?",
                "OK",
                "Annuler"),
            "de" => new(
                "Erster Start",
                "Dies ist der erste Start. Du musst zuerst die Optionen einrichten, um fortzufahren.",
                "Möchtest du die Einrichtung jetzt öffnen?",
                "OK",
                "Abbrechen"),
            "es" => new(
                "Primer inicio",
                "He detectado que este es el primer inicio. Primero debes configurar las opciones para continuar.",
                "¿Quieres abrir ahora la configuración?",
                "Aceptar",
                "Cancelar"),
            "vi" => new(
                "Lần chạy đầu tiên",
                "Đây là lần chạy đầu tiên. Bạn cần thiết lập các tùy chọn trước khi tiếp tục.",
                "Bạn có muốn mở phần thiết lập ngay bây giờ không?",
                "OK",
                "Hủy"),
            _ => new(
                "First run",
                "I detected that this is the first run. You need to configure the options before continuing.",
                "Open setup now?",
                "OK",
                "Cancel")
        };
    }
}

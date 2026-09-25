// SPDX-License-Identifier: MIT

using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

/// <summary>Keeps every settings section accessible when the terminal cannot host an alternate-buffer form.</summary>
internal sealed class SettingsPromptForm(IAnsiConsole console, ILocalizationService text, ConsoleRenderOptions options)
{
    internal int SelectedPageIndex { get; private set; }

    /// <summary>Edits the shared draft from the requested section and saves only through the explicit action.</summary>
    internal bool Run(IReadOnlyList<FormPage> pages, int initialPage, Func<string?> validate)
    {
        var pageIndex = initialPage;
        while (true)
        {
            SelectedPageIndex = pageIndex;
            var page = pages[pageIndex];
            var fields = page.Fields.Where(field => field.IsVisible?.Invoke() != false).ToArray();
            TerminalTheme.WriteSection(console,
                TerminalTheme.IconPrefix(options, "⚙️", "~") + text.Text("Settings.Title"), FullscreenForm.SectionTitle(page, text, options));
            RenderFields(fields);
            if (page.Overview is { } overview)
            {
                console.Write(overview());
                console.WriteLine();
            }
            if (page.Preview is { } preview)
            {
                console.Write(preview());
                console.WriteLine();
            }
            var actions = fields.Select((field, index) => new PromptAction("field", index, FullscreenForm.FieldLabel(field, text)))
                .Concat(pages.Select((section, index) => new PromptAction("section", index,
                    text.Text("Form.Sections") + ": " + FullscreenForm.SectionTitle(section, text, options))))
                .Append(new PromptAction("save", 0, TerminalTheme.IconPrefix(options, "💾", "+") + text.Text("Form.Save")))
                .Append(new PromptAction("cancel", 0, TerminalTheme.IconPrefix(options, "↩️", "x") + text.Text("Form.Cancel")))
                .ToArray();
            var menuChoices = actions.Select(action => new TerminalMenuChoice<PromptAction>(action, action.Label,
                Tone: action.Kind switch
                {
                    "save" => TerminalMenuTone.Positive,
                    "cancel" => TerminalMenuTone.Caution,
                    _ => TerminalMenuTone.Primary
                })).ToArray();
            var selected = TerminalChoiceMenu.Select(console, menuChoices, text.Text(page.HelpKey ?? "Form.Help"));
            switch (selected.Kind)
            {
                case "field":
                    Edit(fields[selected.Index]);
                    break;
                case "section":
                    if (pages[selected.Index].Open is { } open)
                    {
                        open();
                    }
                    else
                    {
                        pageIndex = selected.Index;
                    }
                    break;
                case "save":
                    var error = validate();
                    if (error is null)
                    {
                        return true;
                    }
                    console.Write(new Text(error, Style.Parse(TerminalTheme.Error)));
                    console.WriteLine();
                    break;
                case "cancel":
                    return false;
                default:
                    throw new InvalidOperationException("Unsupported settings action.");
            }
        }
    }

    /// <summary>Shows aligned draft values with blank rows while keeping secret contents hidden.</summary>
    private void RenderFields(IReadOnlyList<FormField> fields)
    {
        var grid = new Grid().AddColumn(new GridColumn().RightAligned()).AddColumn(new GridColumn().LeftAligned());
        foreach (var field in fields)
        {
            var value = field.Secret
                ? field.Display?.Invoke() ?? text.Text("Form.SecretInput")
                : field.Choices?.Invoke().FirstOrDefault(choice => choice.Value == field.Read())?.Label ?? field.Read();
            grid.AddRow(
                new Text(FullscreenForm.FieldLabel(field, text), Style.Parse(TerminalTheme.Muted)),
                new Text(SafeText(value), Style.Parse(field.ValueColor?.Invoke() ?? TerminalTheme.FieldValue)));
            grid.AddRow(new Text(" "), new Text(" "));
        }
        console.Write(grid);
    }

    /// <summary>Uses the same choice and validation rules as the fullscreen editor without echoing credentials.</summary>
    private void Edit(FormField field)
    {
        if (field.Overview is { } overview)
        {
            console.Write(overview());
            console.WriteLine();
        }
        var help = field.Help?.Invoke() ?? (field.HelpKey is null ? null : text.Text(field.HelpKey));
        if (!string.IsNullOrWhiteSpace(help))
        {
            console.Write(new Text(help, Style.Parse(TerminalTheme.Muted)));
            console.WriteLine();
        }
        string value;
        if (field.Choices is not null)
        {
            var choices = field.Choices();
            if (choices.Count == 0)
            {
                throw new InvalidOperationException("A choice field needs at least one available value.");
            }
            var ordered = choices.OrderBy(choice => choice.Value == field.Read() ? 0 : 1)
                .Select(choice => new TerminalMenuChoice<FormChoice>(choice, choice.Label)).ToArray();
            value = TerminalChoiceMenu.Select(console, ordered, FullscreenForm.FieldLabel(field, text)).Value;
        }
        else
        {
            var prompt = new TextPrompt<string>(Markup.Escape(FullscreenForm.FieldLabel(field, text))).AllowEmpty();
            if (field.Secret)
            {
                prompt.Secret(mask: null);
            }
            else if (field.DefaultToCurrentValue && !string.IsNullOrEmpty(field.Read()))
            {
                prompt.DefaultValue(field.Read());
            }
            value = console.Prompt(prompt.Validate(candidate =>
            {
                var error = candidate.Length > field.MaxLength
                    ? text.Text("Form.InputTooLong", field.MaxLength)
                    : field.Validate?.Invoke(candidate);
                return error is null ? ValidationResult.Success() : ValidationResult.Error(Markup.Escape(error));
            }));
        }
        field.Write(value);
    }

    /// <summary>Removes terminal controls while preserving the visible local text of ordinary preferences.</summary>
    private static string SafeText(string value) =>
        new(value.ReplaceLineEndings(" ").Select(character => char.IsControl(character) ? ' ' : character).ToArray());

    private sealed record PromptAction(string Kind, int Index, string Label);
}

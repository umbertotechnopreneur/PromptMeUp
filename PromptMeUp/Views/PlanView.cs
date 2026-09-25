// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;

namespace PromptMeUp.Views;

public interface IPlanView
{
    void Render(ExecutionPlan plan);
    bool ConfirmStart();
    bool ConfirmOutcome(PlanStep step);
}

public sealed class PlanView(IAnsiConsole console, ILocalizationService text, IConsoleShellView shell) : IPlanView
{
    /// <summary>Displays ordered progress and the explicit resume command without executing any step.</summary>
    public void Render(ExecutionPlan plan)
    {
        TerminalTheme.WriteRule(console, text.Text("Plan.Help"), TerminalTheme.Accent);
        console.Write(new Rows(
            new Text(plan.Goal, Style.Parse(TerminalTheme.Primary)),
            new Text(plan.Directory, Style.Parse(TerminalTheme.Muted))));
        console.WriteLine();
        var table = new Table().Border(TableBorder.Simple).AddColumn("#").AddColumn(text.Text("Plan.Step")).AddColumn(text.Text("Plan.Status"));
        for (var index = 0; index < plan.Steps.Count; index++)
        {
            var step = plan.Steps[index];
            table.AddRow(new Text(StepIndicator(index)), new Text(step.Label + "\n" + step.Expected), new Text(text.Text("Plan." + step.Status)));
        }
        console.Write(table);
        console.WriteLine();
        console.Write(new Text(text.Text("Plan.Resume"), Style.Parse(TerminalTheme.Muted)));
        console.WriteLine();
        console.Write(new Text("hm --plan --resume " + plan.Id));
        console.WriteLine();
        console.WriteLine();
    }

    /// <summary>Confirms starting or resuming guidance while each command still requires its own approval.</summary>
    public bool ConfirmStart()
    {
        TerminalPromptDock.Align(console, reservedRows: 2);
        return console.Prompt(new ConfirmationPrompt(text.Text("Plan.Start")) { DefaultValue = false });
    }

    /// <summary>Requires the user to compare observed output with the declared outcome after a successful check.</summary>
    public bool ConfirmOutcome(PlanStep step)
    {
        TerminalPromptDock.Align(console, reservedRows: 2);
        return console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Plan.Outcome", step.Expected))) { DefaultValue = false });
    }

    /// <summary>Returns a compact zero-based visual marker while preserving a text-only fallback.</summary>
    private string StepIndicator(int index) => shell.Options.NoEmoji
        ? index.ToString()
        : index switch
        {
            0 => "0️⃣ ",
            1 => "1️⃣ ",
            2 => "2️⃣ ",
            3 => "3️⃣ ",
            4 => "4️⃣ ",
            5 => "5️⃣ ",
            6 => "6️⃣ ",
            7 => "7️⃣ ",
            8 => "8️⃣ ",
            9 => "9️⃣ ",
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
}

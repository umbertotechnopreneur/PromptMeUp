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

public sealed class PlanView(IAnsiConsole console, ILocalizationService text) : IPlanView
{
    /// <summary>Displays ordered progress and the explicit resume command without executing any step.</summary>
    public void Render(ExecutionPlan plan)
    {
        TerminalTheme.WriteRule(console, text.Text("Plan.Help"), TerminalTheme.Accent);
        console.Write(new Panel(new Text(plan.Goal + "\n" + plan.Directory)).BorderColor(Color.Cyan1));
        var table = new Table().Border(TableBorder.Rounded).AddColumn("#").AddColumn(text.Text("Plan.Step")).AddColumn(text.Text("Plan.Status"));
        for (var index = 0; index < plan.Steps.Count; index++)
        {
            var step = plan.Steps[index];
            table.AddRow(new Text((index + 1).ToString()), new Text(step.Label + "\n" + step.Expected), new Text(text.Text("Plan." + step.Status)));
        }
        console.Write(table);
        console.Write(new Text("hm --plan --resume " + plan.Id));
        console.WriteLine();
    }

    /// <summary>Confirms starting or resuming guidance while each command still requires its own approval.</summary>
    public bool ConfirmStart() => console.Prompt(new ConfirmationPrompt(text.Text("Plan.Start")) { DefaultValue = false });

    /// <summary>Requires the user to compare observed output with the declared outcome after a successful check.</summary>
    public bool ConfirmOutcome(PlanStep step) => console.Prompt(new ConfirmationPrompt(Markup.Escape(text.Text("Plan.Outcome", step.Expected))) { DefaultValue = false });
}

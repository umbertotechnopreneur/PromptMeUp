// SPDX-License-Identifier: MIT

using System.Globalization;
using PromptMeUp.Models;

namespace PromptMeUp.Application;

public sealed partial class ExperimentalWorkflow
{
    /// <summary>Creates, lists, and cancels project reminders through explicit, default-cancel confirmation screens.</summary>
    public async Task RunRemindersAsync(AppSettings settings, CancellationToken ct)
    {
        _ = settings;
        if (!await reminders.IsAvailableAsync(ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException(text.Text("Lab.Activate"));
        }
        shell.RenderNotice(text.Text("Reminder.Notice"));
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var pending = await reminders.ListAsync(ct).ConfigureAwait(false);
            view.Render(text.Text("Reminder.Title"), pending.Count == 0
                ? [(text.Text("Reminder.Pending"), text.Text("Lab.None"))]
                : pending.Select(reminder => (ReminderDate(reminder), reminder.Message)));
            var choice = view.Choose(text.Text("Reminder.Title"),
                [text.Text("Lab.Back"), text.Text("Reminder.Create"), .. pending.Select(reminder => ReminderDate(reminder) + " — " + reminder.Message)]);
            if (choice == 0)
            {
                return;
            }
            if (choice == 1)
            {
                var due = reminders.ParseTime(view.Read(text.Text("Reminder.At")));
                var note = view.Read(text.Text("Reminder.Message"));
                view.Render(text.Text("Reminder.Create"),
                    [(text.Text("Reminder.At"), due.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)),
                        (text.Text("Reminder.Message"), note)]);
                if (view.Confirm(text.Text("Reminder.ConfirmCreate")))
                {
                    await reminders.CreateAsync(due, note, ct).ConfigureAwait(false);
                    shell.RenderSuccess(text.Text("Lab.Saved"));
                }
            }
            else
            {
                var reminder = pending[choice - 2];
                view.Render(text.Text("Reminder.Title"),
                    [(text.Text("Reminder.At"), ReminderDate(reminder)), (text.Text("Reminder.Message"), reminder.Message)]);
                if (view.Confirm(text.Text("Reminder.ConfirmCancel")))
                {
                    if (!await reminders.CancelAsync(reminder, ct).ConfigureAwait(false))
                    {
                        throw new InvalidOperationException(text.Text("Lab.Invalid"));
                    }
                    shell.RenderSuccess(text.Text("Reminder.Cancelled"));
                }
            }
        }
    }

    /// <summary>Shows the complete date, clock time, and explicit offset that the user approved.</summary>
    private static string ReminderDate(Reminder reminder) => reminder.DueAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
}

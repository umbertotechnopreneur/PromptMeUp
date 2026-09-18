// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Coordinates explicitly enabled skills and learning using passive PromptMeUp views.</summary>
public sealed partial class ExperimentalWorkflow(
    SkillCatalogService skills, SkillActionService actions, ExperimentalStore store,
    IAuthorizedCommandWorkflow commands, IActivityAuditService audit,
    ExperimentalView view, IConsoleShellView shell, ILocalizationService text,
    MemoryReflectionService reflection, PersistentMemoryService memories, ArtifactAssistant assistant,
    IEnvironmentSecretService secrets, ReminderService reminders)
{
    /// <summary>Dispatches interactive experiment commands and reports unsafe local input without leaking package contents.</summary>
    public async Task RunAsync(AppCommand command, AppSettings settings, CancellationToken ct)
    {
        try
        {
            switch (command)
            {
                case AppCommand.Skills:
                    await RunSkillsAsync(settings, ct).ConfigureAwait(false);
                    break;
                case AppCommand.Learning:
                    await RunLearningAsync(settings, ct).ConfigureAwait(false);
                    break;
                case AppCommand.Proposals:
                    await RunProposalsAsync(settings, ct).ConfigureAwait(false);
                    break;
                case AppCommand.Dream:
                case AppCommand.Heartbeat:
                    await RunReflectionAsync(command == AppCommand.Dream, settings, ct).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or YamlDotNet.Core.YamlException or System.Text.Json.JsonException or ArgumentException)
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
    }

    /// <summary>Confirms the memory deletion and its explicit learning-evidence purge, defaulting to cancellation.</summary>
    public bool ConfirmMemoryForget() => view.Confirm(text.Text("Lab.ForgetNotice"));

    /// <summary>Manages project activation, exact package approvals, imports, and skill selection.</summary>
    public async Task RunSkillsAsync(AppSettings settings, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var preferences = await store.SettingsAsync(ct).ConfigureAwait(false);
            if (!preferences.Enabled)
            {
                if (view.Choose(text.Text("Lab.Title"), text.Text("Lab.Back"), text.Text("Lab.Enable")) == 0)
                {
                    return;
                }
                await store.SaveSettingsAsync(preferences with { Enabled = true }, ct).ConfigureAwait(false);
                continue;
            }
            var catalog = skills.List();
            var choices = new List<string>
            {
                text.Text("Lab.Back"), text.Text("Lab.Disable"), text.Text("Lab.Import"),
                text.Text("Lab.Automatic") + " — " + text.Text(preferences.AutomaticSkills ? "Lab.Enabled" : "Lab.Off"),
                text.Text("Lab.ClearSelection")
            };
            foreach (var skill in catalog)
            {
                var enabled = await skills.IsEnabledAsync(skill, ct).ConfigureAwait(false);
                choices.Add(skill.Name + " — " + (skill.UnavailableReason ?? text.Text(enabled ? "Lab.Enabled" : "Lab.Off")));
            }
            var selected = view.Choose(text.Text("Lab.Skills"), choices.ToArray());
            switch (selected)
            {
                case 0:
                    return;
                case 1:
                    if (view.Confirm(text.Text("Lab.Disable")))
                    {
                        await store.SaveSettingsAsync(new(), ct).ConfigureAwait(false);
                    }
                    break;
                case 2:
                    Import();
                    break;
                case 3:
                    await store.SaveSettingsAsync(preferences with { AutomaticSkills = !preferences.AutomaticSkills }, ct).ConfigureAwait(false);
                    break;
                case 4:
                    await store.SetAsync("selected-skill", string.Empty, ct).ConfigureAwait(false);
                    break;
                default:
                    await ManageSkillAsync(catalog[selected - 5], settings, ct).ConfigureAwait(false);
                    break;
            }
        }
    }

    /// <summary>Shows the exact inspected instructions and scripts before enabling or running a package.</summary>
    private async Task ManageSkillAsync(SkillDefinition skill, AppSettings settings, CancellationToken ct)
    {
        view.Render(skill.Name + " " + skill.Version,
            [(text.Text("Lab.Source"), skill.Directory), (text.Text("Lab.Package"), skill.Description + "\n\n" + skill.Instructions),
                .. skill.Scripts.Select(script => (script.Key + ".ps1", script.Value))]);
        shell.RenderNotice(text.Text("Lab.ManualOnly"));
        if (skill.UnavailableReason is not null)
        {
            shell.RenderWarning(skill.UnavailableReason);
            return;
        }
        var enabled = await skills.IsEnabledAsync(skill, ct).ConfigureAwait(false);
        var selected = view.Choose(text.Text("Lab.Skills"), text.Text("Lab.Back"),
            text.Text(enabled ? "Lab.Deactivate" : "Lab.Activate"), text.Text("Lab.Select"), text.Text("Lab.Run"));
        if (selected == 1)
        {
            await skills.EnableAsync(skill, !enabled, ct).ConfigureAwait(false);
        }
        else if (selected is 2 or 3)
        {
            if (!enabled)
            {
                throw new InvalidOperationException(text.Text("Lab.Activate"));
            }
            if (selected == 2)
            {
                await store.SetAsync("selected-skill", skill.Name, ct).ConfigureAwait(false);
                return;
            }
            var availableActions = actions.Actions(skill);
            if (availableActions.Count == 0)
            {
                shell.RenderNotice(text.Text("Lab.None"));
                return;
            }
            var action = view.Choose(text.Text("Lab.Run"), [text.Text("Lab.Back"), .. availableActions]);
            if (action == 0)
            {
                return;
            }
            var name = availableActions[action - 1];
            if (skill.Origin == "bundled" && skill.Name == "set_reminder")
            {
                await RunRemindersAsync(settings, ct).ConfigureAwait(false);
                return;
            }
            if (skill.Origin == "bundled" && skill.Name == "screenshot")
            {
                shell.RenderWarning(text.Text("Lab.ScreenshotPrivacy"));
            }
            var native = skill.Origin == "bundled" && skill.Name is "git" or "filesystem";
            var command = native
                ? actions.NativeCommand(skill, name, view.Read(text.Text("Lab.Path"), Environment.CurrentDirectory))
                : actions.ScriptCommand(skill, name, view.Read(text.Text("Lab.Arguments"), actions.DefaultParameters(skill)));
            await using var session = await AuditSessionScope.StartAsync(audit, "skill", settings,
                new { skill.Name, skill.Fingerprint }, AuditSessionOutcome.Failed, ct).ConfigureAwait(false);
            var result = await commands.RunForResultAsync(session.Id, command, settings, ct).ConfigureAwait(false);
            session.Outcome = result is null ? AuditSessionOutcome.Cancelled
                : result.ExitCode == 0 && !result.TimedOut ? AuditSessionOutcome.Completed : AuditSessionOutcome.Failed;
        }
    }

    /// <summary>Stages a local archive and publishes it only after displaying its complete executable content.</summary>
    private void Import()
    {
        var staged = skills.StageZip(view.Read(text.Text("Lab.Path")));
        try
        {
            var skill = skills.Inspect(staged);
            view.Render(skill.Name, [(text.Text("Lab.Package"), skill.Description + "\n" + skill.Instructions),
                .. skill.Scripts.Select(script => (script.Key + ".ps1", script.Value))]);
            if (view.Confirm(text.Text("Lab.Import")))
            {
                skills.Import(skill);
                shell.RenderSuccess(text.Text("Lab.Saved"));
            }
        }
        finally
        {
            skills.DiscardStaging(staged);
        }
    }
}

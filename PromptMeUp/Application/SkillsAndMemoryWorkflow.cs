// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Coordinates explicitly enabled skills and learning using passive PromptMeUp views.</summary>
public sealed partial class SkillsAndMemoryWorkflow(
    SkillCatalogService skills, SkillActionService actions, SkillsAndMemoryStore store,
    IAuthorizedCommandWorkflow commands, IActivityAuditService audit,
    SkillsAndMemoryView view, IConsoleShellView shell, ILocalizationService text,
    MemoryReflectionService reflection, PersistentMemoryService memories, ArtifactAssistant assistant,
    IEnvironmentSecretService secrets, ReminderService reminders)
{
    /// <summary>Dispatches interactive skills and memory commands without leaking unsafe package contents.</summary>
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

    /// <summary>Manages global activation, exact package approvals, imports, and skill selection.</summary>
    public async Task RunSkillsAsync(AppSettings settings, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var preferences = await store.SettingsAsync(ct).ConfigureAwait(false);
            var groups = await BuildSkillGroups(preferences, ct).ConfigureAwait(false);
            var selected = view.ChooseSkills(text.Text("Lab.Skills"), groups, inline =>
            {
                if (inline.Item.Action != SkillMenuAction.ToggleSkill)
                {
                    return null;
                }

                RunSkillCommandAsync(inline, settings, ct).GetAwaiter().GetResult();
                preferences = store.SettingsAsync(ct).GetAwaiter().GetResult();
                return BuildSkillGroups(preferences, ct).GetAwaiter().GetResult();
            });
            if (selected is null)
            {
                return;
            }
            switch (selected.Item.Action)
            {
                case SkillMenuAction.EnableSkillsAndMemory:
                    await store.SaveSettingsAsync(preferences with { Enabled = true }, preferences, ct).ConfigureAwait(false);
                    break;
                case SkillMenuAction.DisableSkillsAndMemory:
                    if (view.Confirm(text.Text("Lab.Disable")))
                    {
                        await store.SaveSettingsAsync(new(), preferences, ct).ConfigureAwait(false);
                    }
                    break;
                case SkillMenuAction.Import:
                    Import(selected.Input ?? string.Empty);
                    break;
                case SkillMenuAction.ToggleAutomatic:
                    await store.SaveSettingsAsync(preferences with { AutomaticSkills = !preferences.AutomaticSkills }, preferences, ct).ConfigureAwait(false);
                    break;
                case SkillMenuAction.ClearSelection:
                    await store.SetAsync("selected-skill", string.Empty, ct).ConfigureAwait(false);
                    break;
                case SkillMenuAction.SelectSkill:
                case SkillMenuAction.RunSkill:
                    await RunSkillCommandAsync(selected, settings, ct).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported skills menu action.");
            }
        }
    }

    /// <summary>Builds the Skills menu with concise global state labels and complete package details below the commands.</summary>
    private async Task<IReadOnlyList<SkillMenuGroup>> BuildSkillGroups(SkillsAndMemorySettings preferences, CancellationToken ct)
    {
        IReadOnlyList<SkillMenuItem> generalCommands = preferences.Enabled
            ?
            [
                new(SkillMenuAction.ToggleAutomatic,
                    text.Text("Lab.Automatic") + " — " + text.Text(preferences.AutomaticSkills ? "Lab.Enabled" : "Lab.Off"),
                    text.Text("Lab.Automatic"), Icon: "⚡"),
                new(SkillMenuAction.ClearSelection, text.Text("Lab.ClearSelection"), text.Text("Lab.ClearSelection"), Icon: "🧹"),
                new(SkillMenuAction.DisableSkillsAndMemory, text.Text("Lab.Disable"), text.Text("Lab.Disable"), Icon: "⏻")
            ]
            :
            [
                new SkillMenuItem(SkillMenuAction.EnableSkillsAndMemory, text.Text("Lab.Enable"), text.Text("Lab.Enable"), Icon: "⏻")
            ];
        IReadOnlyList<SkillMenuItem> importCommands = preferences.Enabled
            ? [new SkillMenuItem(SkillMenuAction.Import, text.Text("Lab.Import"), text.Text("Lab.ImportDescription"),
                Icon: "📦", InputLabel: text.Text("Lab.Path"))]
            : [];
        var installedCommands = new List<SkillMenuItem>();
        if (preferences.Enabled)
        {
            foreach (var skill in skills.List())
            {
                var enabled = await skills.IsEnabledAsync(skill, ct).ConfigureAwait(false);
                var status = skill.UnavailableReason ?? text.Text(enabled ? "Lab.Enabled" : "Lab.Off");
                var state = skill.UnavailableReason is null
                    ? TerminalTheme.IconPrefix(shell.Options, enabled ? "🟢" : "🔴", enabled ? "+" : "-") + status
                    : status;
                if (skill.UnavailableReason is not null)
                {
                    installedCommands.Add(new SkillMenuItem(SkillMenuAction.ToggleSkill,
                        skill.Name + " — " + state, skill.Description, skill, skill.Icon, CanExecute: false));
                    continue;
                }
                installedCommands.Add(new SkillMenuItem(SkillMenuAction.ToggleSkill,
                    skill.Name + " — " + state, skill.Description, skill, skill.Icon));
                if (enabled)
                {
                    installedCommands.Add(new SkillMenuItem(SkillMenuAction.SelectSkill,
                        skill.Name + " — " + text.Text("Lab.Select"), text.Text("Lab.Select"), skill, "📌"));
                    foreach (var action in actions.Actions(skill))
                    {
                        var reminder = skill.Origin == "bundled" && skill.Name == "set_reminder";
                        var native = skill.Origin == "bundled" && skill.Name is "git" or "filesystem";
                        installedCommands.Add(new SkillMenuItem(SkillMenuAction.RunSkill,
                            skill.Name + " — " + action,
                            text.Text("Lab.RunSkillAction", action, skill.Description), skill, "▶️", action,
                            InputLabel: reminder ? null : text.Text(native ? "Lab.Path" : "Lab.Arguments"),
                            InitialInput: reminder ? string.Empty : native
                                ? Environment.CurrentDirectory
                                : actions.DefaultParameters(skill),
                            MultilineInput: !reminder && !native));
                    }
                }
            }
        }

        return
        [
            new(text.Text("Lab.GeneralGroup"), text.Text(preferences.Enabled
                ? "Lab.GeneralEnabledDescription"
                : "Lab.GeneralDisabledDescription"), generalCommands, "⚙️"),
            new(text.Text("Lab.InstalledGroup"), text.Text(preferences.Enabled
                ? "Lab.InstalledDescription"
                : "Lab.Disabled"), installedCommands, "🧩"),
            new(text.Text("Lab.ImportGroup"), text.Text(preferences.Enabled
                ? "Lab.ImportDescription"
                : "Lab.Disabled"), importCommands, "📦")
        ];
    }

    /// <summary>Shows the exact inspected package and applies one command selected from the central skills panel.</summary>
    private async Task RunSkillCommandAsync(SkillMenuSelection selection, AppSettings settings, CancellationToken ct)
    {
        var selected = selection.Item;
        var skill = selected.Skill
            ?? throw new InvalidOperationException("A skill command requires a skill.");
        var executionMode = settings.DirectModeEnabled ? CommandExecutionMode.Direct : CommandExecutionMode.Confirm;
        if (skill.UnavailableReason is not null)
        {
            shell.RenderWarning(skill.UnavailableReason);
            return;
        }
        var enabled = await skills.IsEnabledAsync(skill, ct).ConfigureAwait(false);
        switch (selected.Action)
        {
            case SkillMenuAction.ToggleSkill:
                await skills.EnableAsync(skill, !enabled, ct).ConfigureAwait(false);
                return;
            case SkillMenuAction.SelectSkill:
                if (!enabled)
                {
                    throw new InvalidOperationException(text.Text("Lab.Activate"));
                }
                await skills.SelectForQuestionsAsync(skill, ct).ConfigureAwait(false);
                return;
            case SkillMenuAction.RunSkill:
                if (!enabled)
                {
                    throw new InvalidOperationException(text.Text("Lab.Activate"));
                }
                var availableActions = actions.Actions(skill);
                var action = selected.ActionName;
                if (action is null || !availableActions.Contains(action, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(text.Text("Lab.SkillActionInvalid"));
                }
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
                    ? actions.NativeCommand(skill, action, selection.Input ?? string.Empty)
                    : actions.ScriptCommand(skill, action, selection.Input ?? string.Empty);
                await using (var session = await AuditSessionScope.StartAsync(audit, "skill", settings,
                    new { skill.Name, skill.Fingerprint }, AuditSessionOutcome.Failed, ct).ConfigureAwait(false))
                {
                    var result = await commands.RunForResultAsync(
                        session.Id, command, settings, ct, executionMode).ConfigureAwait(false);
                    session.Outcome = result is null ? AuditSessionOutcome.Cancelled
                        : result.ExitCode == 0 && !result.TimedOut ? AuditSessionOutcome.Completed : AuditSessionOutcome.Failed;
                }
                return;
            default:
                throw new InvalidOperationException("Unsupported inline skill command.");
        }
    }

    /// <summary>Stages a local archive and publishes it only after displaying its complete executable content.</summary>
    private void Import(string path)
    {
        var staged = skills.StageZip(path.Trim());
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

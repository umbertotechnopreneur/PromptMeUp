// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface ICommandRiskAssessmentService
{
    Task<CommandRiskAssessment> AssessAsync(
        string command,
        bool useAi,
        AppSettings settings,
        string language,
        CancellationToken cancellationToken);
}

public sealed partial class CommandRiskAssessmentService : ICommandRiskAssessmentService
{
    private readonly IOpenAiService _openAi;
    private readonly IEnvironmentSecretService _secrets;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly ILogger<CommandRiskAssessmentService> _logger;

    /// <summary>Creates the combined deterministic and optional AI command reviewer.</summary>
    public CommandRiskAssessmentService(
        IOpenAiService openAi,
        IEnvironmentSecretService secrets,
        ISensitiveDataRedactor redactor,
        ILogger<CommandRiskAssessmentService> logger)
    {
        _openAi = openAi ?? throw new ArgumentNullException(nameof(openAi));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Returns a conservative local score, optionally enriched by an advisory AI review.</summary>
    public async Task<CommandRiskAssessment> AssessAsync(
        string command,
        bool useAi,
        AppSettings settings,
        string language,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(settings);
        var local = AssessLocally(command, language);
        if (!useAi || !settings.AiEnabled || !_secrets.IsConfigured(settings.ApiKeyVariable))
        {
            return local;
        }

        try
        {
            var ai = await _openAi.AssessCommandAsync(_redactor.Redact(command), settings, language, cancellationToken).ConfigureAwait(false);
            var score = Math.Max(local.Score, ai.Score);
            var level = ScoreToLevel(score);
            var advisory = local.Score > ai.Score
                ? Translate(language, "La regola locale più prudente ha prevalso sul punteggio AI.", "The more conservative local rule overrode the AI score.")
                : Translate(language, "La revisione AI è consultiva: controlla comunque il comando.", "The AI review is advisory: inspect the command yourself.");
            return new CommandRiskAssessment(score, level, ai.DescriptionMarkdown, true, advisory);
        }
        catch (Exception exception) when (exception is OpenAiRequestException or HttpRequestException or JsonException)
        {
            _logger.LogWarning("Optional AI command review failed; local assessment retained. ErrorType={ErrorType}", exception.GetType().Name);
            return local with
            {
                Advisory = Translate(language, "Revisione AI non disponibile; è mostrata la valutazione locale.", "AI review unavailable; showing the local assessment.")
            };
        }
    }

    /// <summary>Scores obvious command effects without sending command text outside the machine.</summary>
    internal static CommandRiskAssessment AssessLocally(string command, string language)
    {
        var normalized = command.Trim();
        var score = 35;
        var effect = Translate(language, "Il comando può modificare lo stato del sistema; verifica argomenti e percorso.", "The command may change system state; verify its arguments and path.");

        if (CriticalPattern().IsMatch(normalized))
        {
            score = 95;
            effect = Translate(language, "Sono state rilevate operazioni distruttive, di arresto o ad ampio raggio.", "Destructive, shutdown, or broad-scope operations were detected.");
        }
        else if (HighPattern().IsMatch(normalized))
        {
            score = 75;
            effect = Translate(language, "Sono state rilevate modifiche a file, repository, servizi o configurazione di sistema.", "File, repository, service, or system-configuration changes were detected.");
        }
        else if (MediumPattern().IsMatch(normalized))
        {
            score = 50;
            effect = Translate(language, "Sono state rilevate attività di rete, installazione o avvio di processi.", "Network, installation, or process-launch activity was detected.");
        }
        else if (IsReadOnlyCommand(normalized))
        {
            score = 15;
            effect = Translate(language, "Il comando appare prevalentemente diagnostico o in sola lettura.", "The command appears primarily diagnostic or read-only.");
        }

        var description = Translate(
            language,
            $"## Valutazione locale\n\n- **Effetto rilevato:** {effect}\n- **Stato:** anteprima soltanto; il comando non è ancora stato eseguito.",
            $"## Local review\n\n- **Detected effect:** {effect}\n- **State:** preview only; the command has not run yet.");
        return new CommandRiskAssessment(score, ScoreToLevel(score), description, false, null);
    }

    /// <summary>Maps a conservative score to the four visible severity bands.</summary>
    private static CommandRiskLevel ScoreToLevel(int score) => score switch
    {
        >= 85 => CommandRiskLevel.Critical,
        >= 60 => CommandRiskLevel.High,
        >= 30 => CommandRiskLevel.Medium,
        _ => CommandRiskLevel.Low
    };

    /// <summary>Uses Italian text for Italian and an English fallback understood in every supported locale.</summary>
    private static string Translate(string language, string italian, string english) =>
        string.Equals(SupportedLanguages.Normalize(language), "it", StringComparison.Ordinal) ? italian : english;

    /// <summary>Recognizes only complete inspection shapes without evaluation, pipelines, redirects, or unknown Git options.</summary>
    private static bool IsReadOnlyCommand(string command)
    {
        if (command.IndexOfAny(['|', ';', '&', '$', '`', '>', '<', '{', '}', '(', ')', '@', '\r', '\n']) >= 0)
        {
            return false;
        }

        return ReadOnlyPattern().IsMatch(command) || GitReadOnlyPattern().IsMatch(command);
    }

    /// <summary>Matches operations that can erase broad state, reboot the machine, or discard repository work.</summary>
    [GeneratedRegex(@"(?ix)(\bformat(?:\.com)?\b|\bdiskpart\b|\bclear-disk\b|\binitialize-disk\b|\bremove-item\b[^\r\n]*(?:-recurse|-force)|\brm\b[^\r\n]*\s-rf\b|\bgit\s+(?:reset\s+--hard|clean\s+-[^\s]*f)|\b(?:stop|restart)-computer\b|\bshutdown(?:\.exe)?\b)")]
    private static partial Regex CriticalPattern();

    /// <summary>Matches direct file, registry, service, policy, or force-push mutations.</summary>
    [GeneratedRegex(@"(?ix)(\bremove-item\b|\bdel(?:ete)?\b|\berase\b|\brd\b|\bmove-item\b|\b(?:set|add|clear)-content\b|\bout-file\b|\bset-itemproperty\b|\bnew-item\b|\breg(?:\.exe)?\s+(?:add|delete)\b|\bset-executionpolicy\b|\b(?:start|stop|restart)-service\b|\bgit\s+push\b[^\r\n]*--force|\bgit\s+branch\b[^\r\n]*(?:\s-[dDmMcCfF]\b|--(?:delete|move|copy|force)\b)|\b(?:invoke-expression|iex|sudo|doas|runas(?:\.exe)?)\b|\b-verb\s+runas\b)")]
    private static partial Regex HighPattern();

    /// <summary>Matches network downloads, installers, package managers, elevation, and dynamic evaluation.</summary>
    [GeneratedRegex(@"(?ix)(\binvoke-webrequest\b|\binvoke-restmethod\b|\bcurl(?:\.exe)?\b|\bwget\b|\bwinget\b|\bchoco\b|\bscoop\b|\bmsiexec\b|\bstart-process\b)")]
    private static partial Regex MediumPattern();

    /// <summary>Matches a small allowlist of familiar inspection-only command shapes.</summary>
    [GeneratedRegex(@"(?ix)^\s*(?:(?:get-(?:location|date|childitem|content|item|itemproperty|filehash|process|service|command|help)|test-path|resolve-path)(?:\s+[^\r\n]+)?|dotnet\s+(?:--info|--list-sdks|--version)|pwd|whoami|hostname)\s*$")]
    private static partial Regex ReadOnlyPattern();

    /// <summary>Allows only known inspection-only Git verbs and options, excluding branch mutation and external diff helpers.</summary>
    [GeneratedRegex(@"(?ix)^\s*git\s+(?:status(?:\s+(?:--short|--branch|--porcelain(?:=[12])?|-s|-b|-sb|--untracked-files(?:=(?:no|normal|all))?))*|branch(?:\s+(?:--list|--all|--remotes|--show-current|--verbose|--no-color|-a|-r|-v|-vv))*|(?:log|show)(?:\s+(?:--oneline|--stat|--name-only|--name-status|--no-patch|--no-color|--decorate(?:=short)?|-\d+))*|diff(?:\s+(?:--stat|--name-only|--name-status|--no-color|--cached|--staged|--check))*\s+--no-ext-diff\s+--no-textconv)\s*$")]
    private static partial Regex GitReadOnlyPattern();
}

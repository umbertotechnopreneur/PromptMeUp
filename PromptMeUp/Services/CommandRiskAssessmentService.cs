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
                ? Translate(language, "Risk.LocalWins")
                : Translate(language, "Risk.Advisory");
            return new CommandRiskAssessment(score, level, ai.DescriptionMarkdown, true, advisory);
        }
        catch (Exception exception) when (exception is OpenAiRequestException or HttpRequestException or JsonException)
        {
            _logger.LogWarning("Optional AI command review failed; local assessment retained. ErrorType={ErrorType}", exception.GetType().Name);
            return local with
            {
                Advisory = Translate(language, "Risk.Unavailable")
            };
        }
    }

    /// <summary>Scores obvious command effects without sending command text outside the machine.</summary>
    internal static CommandRiskAssessment AssessLocally(string command, string language)
    {
        var normalized = command.Trim();
        var score = 35;
        var effect = Translate(language, "Risk.Unknown");

        if (CriticalPattern().IsMatch(normalized))
        {
            score = 95;
            effect = Translate(language, "Risk.Critical");
        }
        else if (HighPattern().IsMatch(normalized))
        {
            score = 75;
            effect = Translate(language, "Risk.High");
        }
        else if (MediumPattern().IsMatch(normalized))
        {
            score = 50;
            effect = Translate(language, "Risk.Medium");
        }
        else if (IsReadOnlyCommand(normalized))
        {
            score = 15;
            effect = Translate(language, "Risk.Low");
        }

        var description = Translate(language, "Risk.Description", effect);
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

    /// <summary>Resolves local risk copy through the shared six-language catalog.</summary>
    private static string Translate(string language, string key, params object?[] args)
    {
        var normalized = SupportedLanguages.Normalize(language);
        if (!FeatureText.TryGet(key, normalized, out var template))
        {
            throw new InvalidOperationException("Missing risk-review translation.");
        }
        return string.Format(SupportedLanguages.Culture(normalized), template, args);
    }

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

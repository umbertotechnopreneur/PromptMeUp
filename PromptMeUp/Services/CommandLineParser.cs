// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface ICommandLineParser
{
    CommandLineParseResult Parse(IReadOnlyList<string> args);
}

public sealed class CommandLineParser : ICommandLineParser
{
    private readonly ILocalizationService _text;

    /// <summary>Creates the strict command-line parser with localized validation messages.</summary>
    public CommandLineParser(ILocalizationService text)
    {
        _text = text;
    }

    /// <summary>Parses one strict hm invocation without performing environment or network work.</summary>
    public CommandLineParseResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var command = AppCommand.Main;
        string? language = null;
        var noAnimation = false;
        var noEmoji = false;
        var yes = false;
        var dryRun = false;
        string? pathAction = null;
        var commandWasSelected = false;
        var queryOptionWasSpecified = false;
        var queryParts = new List<string>();

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument.ToLowerInvariant())
            {
                case "--help" or "-h" or "/?":
                    if (!TrySelect(AppCommand.Help, ref command, ref commandWasSelected, out var helpError))
                    {
                        return FailureMessage(helpError);
                    }
                    break;
                case "--version" or "-v":
                    if (!TrySelect(AppCommand.Version, ref command, ref commandWasSelected, out var versionError))
                    {
                        return FailureMessage(versionError);
                    }
                    break;
                case "--setup":
                    if (!TrySelect(AppCommand.Setup, ref command, ref commandWasSelected, out var setupError))
                    {
                        return FailureMessage(setupError);
                    }
                    break;
                case "--status":
                    if (!TrySelect(AppCommand.Status, ref command, ref commandWasSelected, out var statusError))
                    {
                        return FailureMessage(statusError);
                    }
                    break;
                case "--chat":
                    if (!TrySelect(AppCommand.Chat, ref command, ref commandWasSelected, out var chatError))
                    {
                        return FailureMessage(chatError);
                    }
                    break;
                case "--test-ai":
                    if (!TrySelect(AppCommand.TestAi, ref command, ref commandWasSelected, out var testError))
                    {
                        return FailureMessage(testError);
                    }
                    break;
                case "--costs":
                    if (!TrySelect(AppCommand.Costs, ref command, ref commandWasSelected, out var costsError))
                    {
                        return FailureMessage(costsError);
                    }
                    break;
                case "--third-party":
                    if (!TrySelect(AppCommand.ThirdParty, ref command, ref commandWasSelected, out var thirdPartyError))
                    {
                        return FailureMessage(thirdPartyError);
                    }
                    break;
                case "--where" or "-where":
                    if (!TrySelect(AppCommand.Where, ref command, ref commandWasSelected, out var whereError))
                    {
                        return FailureMessage(whereError);
                    }
                    break;
                case "--install-font":
                    if (!TrySelect(AppCommand.InstallFont, ref command, ref commandWasSelected, out var fontError))
                    {
                        return FailureMessage(fontError);
                    }
                    break;
                case "--path":
                    if (!TrySelect(AppCommand.Path, ref command, ref commandWasSelected, out var pathError))
                    {
                        return FailureMessage(pathError);
                    }
                    if (index + 1 < args.Count && !args[index + 1].StartsWith('-'))
                    {
                        pathAction = args[++index].Trim().ToLowerInvariant();
                        if (!IsPathAction(pathAction))
                        {
                            return Failure("Cli.PathAction");
                        }
                    }
                    break;
                case "--query" or "-q":
                    if (queryOptionWasSpecified)
                    {
                        return Failure("Cli.QueryDuplicate");
                    }
                    if (!TrySelect(AppCommand.Query, ref command, ref commandWasSelected, out var queryError))
                    {
                        return FailureMessage(queryError);
                    }
                    if (!TryReadValue(args, ref index, argument, out var queryValue, out var missingQuery))
                    {
                        return FailureMessage(missingQuery);
                    }
                    queryOptionWasSpecified = true;
                    queryParts.Add(queryValue!);
                    break;
                case "--language" or "-l":
                    if (!TryReadValue(args, ref index, argument, out language, out var missingLanguage))
                    {
                        return FailureMessage(missingLanguage);
                    }
                    if (!SupportedLanguages.IsSupported(language))
                    {
                        return Failure("Cli.UnsupportedLanguage", language, string.Join(", ", SupportedLanguages.Codes));
                    }
                    language = SupportedLanguages.Normalize(language!);
                    _text.SetLanguage(language);
                    break;
                case "--no-animation":
                    noAnimation = true;
                    break;
                case "--no-emoji":
                    noEmoji = true;
                    break;
                case "--yes" or "-y":
                    yes = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    if (argument.StartsWith("--query=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (queryOptionWasSpecified)
                        {
                            return Failure("Cli.QueryDuplicate");
                        }
                        if (!TrySelect(AppCommand.Query, ref command, ref commandWasSelected, out var inlineQueryError))
                        {
                            return FailureMessage(inlineQueryError);
                        }
                        var inlineQueryValue = argument[(argument.IndexOf('=') + 1)..].Trim();
                        if (string.IsNullOrWhiteSpace(inlineQueryValue))
                        {
                            return Failure("Cli.QueryText");
                        }
                        queryOptionWasSpecified = true;
                        queryParts.Add(inlineQueryValue);
                    }
                    else if (argument.StartsWith("--language=", StringComparison.OrdinalIgnoreCase))
                    {
                        language = argument[(argument.IndexOf('=') + 1)..].Trim();
                        if (!SupportedLanguages.IsSupported(language))
                        {
                            return Failure("Cli.UnsupportedLanguage", language, string.Join(", ", SupportedLanguages.Codes));
                        }
                        language = SupportedLanguages.Normalize(language);
                        _text.SetLanguage(language);
                    }
                    else if (argument.StartsWith("--path=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!TrySelect(AppCommand.Path, ref command, ref commandWasSelected, out var inlinePathError))
                        {
                            return FailureMessage(inlinePathError);
                        }
                        pathAction = argument[(argument.IndexOf('=') + 1)..].Trim().ToLowerInvariant();
                        if (!IsPathAction(pathAction))
                        {
                            return Failure("Cli.PathAction");
                        }
                    }
                    else
                    {
                        if (argument.StartsWith('-'))
                        {
                            return Failure("Cli.UnknownArgument", argument);
                        }

                        queryParts.Add(argument);
                    }
                    break;
            }
        }

        string? query = null;
        if (queryParts.Count > 0)
        {
            if (commandWasSelected && command != AppCommand.Query)
            {
                return Failure("Cli.PositionalConflict");
            }

            command = AppCommand.Query;
            commandWasSelected = true;
            query = string.Join(' ', queryParts).Trim();
        }

        if (command == AppCommand.Query && string.IsNullOrWhiteSpace(query))
        {
            return Failure("Cli.QueryText");
        }

        if (dryRun && command != AppCommand.InstallFont)
        {
            return Failure("Cli.DryRunScope");
        }

        return new CommandLineParseResult(
            new CommandLineOptions(command, query, language, noAnimation, noEmoji, yes, dryRun, pathAction),
            null);
    }

    /// <summary>Selects one top-level command and rejects ambiguous command combinations.</summary>
    private bool TrySelect(
        AppCommand selected,
        ref AppCommand command,
        ref bool commandWasSelected,
        out string? error)
    {
        if (commandWasSelected && command != selected)
        {
            error = _text.Text("Cli.CommandConflict", $"--{ToSwitch(command)}", $"--{ToSwitch(selected)}");
            return false;
        }

        command = selected;
        commandWasSelected = true;
        error = null;
        return true;
    }

    /// <summary>Reads one required non-empty value following a command-line option.</summary>
    private bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        out string? value,
        out string? error)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith('-'))
        {
            value = null;
            error = _text.Text("Cli.ValueRequired", option);
            return false;
        }

        value = args[++index].Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            error = _text.Text("Cli.NonEmptyValueRequired", option);
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Creates a failed parse result with a stable user-facing message.</summary>
    private CommandLineParseResult Failure(string key, params object?[] arguments) =>
        new(null, _text.Text(key, arguments));

    /// <summary>Creates a failed parse result from an already localized validation message.</summary>
    private CommandLineParseResult FailureMessage(string? message) =>
        new(null, message ?? _text.Text("Cli.Invalid"));

    /// <summary>Checks the bounded action vocabulary for portable PATH management.</summary>
    private static bool IsPathAction(string? action) => action is "install" or "remove" or "status";

    /// <summary>Maps command identifiers to their public kebab-case switch names.</summary>
    private static string ToSwitch(AppCommand command) => command switch
    {
        AppCommand.TestAi => "test-ai",
        AppCommand.InstallFont => "install-font",
        AppCommand.ThirdParty => "third-party",
        AppCommand.Path => "path",
        _ => command.ToString().ToLowerInvariant()
    };
}

// SPDX-License-Identifier: MIT

using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface ICommandLineParser
{
    CommandLineParseResult Parse(IReadOnlyList<string> args);
}

public sealed class CommandLineParser : ICommandLineParser
{
    /// <summary>Parses one strict hm invocation without performing environment or network work.</summary>
    public CommandLineParseResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var command = AppCommand.Main;
        string? query = null;
        string? language = null;
        var noAnimation = false;
        var noEmoji = false;
        var yes = false;
        var dryRun = false;
        string? pathAction = null;
        var commandWasSelected = false;
        var positionalQuery = new List<string>();

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument.ToLowerInvariant())
            {
                case "--help" or "-h" or "/?":
                    if (!TrySelect(AppCommand.Help, ref command, ref commandWasSelected, out var helpError))
                    {
                        return Failure(helpError);
                    }
                    break;
                case "--version" or "-v":
                    if (!TrySelect(AppCommand.Version, ref command, ref commandWasSelected, out var versionError))
                    {
                        return Failure(versionError);
                    }
                    break;
                case "--setup":
                    if (!TrySelect(AppCommand.Setup, ref command, ref commandWasSelected, out var setupError))
                    {
                        return Failure(setupError);
                    }
                    break;
                case "--status":
                    if (!TrySelect(AppCommand.Status, ref command, ref commandWasSelected, out var statusError))
                    {
                        return Failure(statusError);
                    }
                    break;
                case "--chat":
                    if (!TrySelect(AppCommand.Chat, ref command, ref commandWasSelected, out var chatError))
                    {
                        return Failure(chatError);
                    }
                    break;
                case "--test-ai":
                    if (!TrySelect(AppCommand.TestAi, ref command, ref commandWasSelected, out var testError))
                    {
                        return Failure(testError);
                    }
                    break;
                case "--costs":
                    if (!TrySelect(AppCommand.Costs, ref command, ref commandWasSelected, out var costsError))
                    {
                        return Failure(costsError);
                    }
                    break;
                case "--third-party":
                    if (!TrySelect(AppCommand.ThirdParty, ref command, ref commandWasSelected, out var thirdPartyError))
                    {
                        return Failure(thirdPartyError);
                    }
                    break;
                case "--install-font":
                    if (!TrySelect(AppCommand.InstallFont, ref command, ref commandWasSelected, out var fontError))
                    {
                        return Failure(fontError);
                    }
                    break;
                case "--path":
                    if (!TrySelect(AppCommand.Path, ref command, ref commandWasSelected, out var pathError))
                    {
                        return Failure(pathError);
                    }
                    if (index + 1 < args.Count && !args[index + 1].StartsWith('-'))
                    {
                        pathAction = args[++index].Trim().ToLowerInvariant();
                        if (!IsPathAction(pathAction))
                        {
                            return Failure("--path accepts install, remove, or status.");
                        }
                    }
                    break;
                case "--query" or "-q":
                    if (!TrySelect(AppCommand.Query, ref command, ref commandWasSelected, out var queryError))
                    {
                        return Failure(queryError);
                    }
                    if (!TryReadValue(args, ref index, argument, out query, out var missingQuery))
                    {
                        return Failure(missingQuery);
                    }
                    break;
                case "--language" or "-l":
                    if (!TryReadValue(args, ref index, argument, out language, out var missingLanguage))
                    {
                        return Failure(missingLanguage);
                    }
                    if (!SupportedLanguages.IsSupported(language))
                    {
                        return Failure($"Unsupported language '{language}'. Use: {string.Join(", ", SupportedLanguages.Codes)}.");
                    }
                    language = SupportedLanguages.Normalize(language!);
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
                        if (!TrySelect(AppCommand.Query, ref command, ref commandWasSelected, out var inlineQueryError))
                        {
                            return Failure(inlineQueryError);
                        }
                        query = argument[(argument.IndexOf('=') + 1)..].Trim();
                        if (string.IsNullOrWhiteSpace(query))
                        {
                            return Failure("--query requires non-empty text.");
                        }
                    }
                    else if (argument.StartsWith("--language=", StringComparison.OrdinalIgnoreCase))
                    {
                        language = argument[(argument.IndexOf('=') + 1)..].Trim();
                        if (!SupportedLanguages.IsSupported(language))
                        {
                            return Failure($"Unsupported language '{language}'. Use: {string.Join(", ", SupportedLanguages.Codes)}.");
                        }
                        language = SupportedLanguages.Normalize(language);
                    }
                    else if (argument.StartsWith("--path=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!TrySelect(AppCommand.Path, ref command, ref commandWasSelected, out var inlinePathError))
                        {
                            return Failure(inlinePathError);
                        }
                        pathAction = argument[(argument.IndexOf('=') + 1)..].Trim().ToLowerInvariant();
                        if (!IsPathAction(pathAction))
                        {
                            return Failure("--path accepts install, remove, or status.");
                        }
                    }
                    else
                    {
                        if (argument.StartsWith('-'))
                        {
                            return Failure($"Unknown argument '{argument}'. Use --help for command syntax.");
                        }

                        positionalQuery.Add(argument);
                    }
                    break;
            }
        }

        if (positionalQuery.Count > 0)
        {
            if (commandWasSelected && command != AppCommand.Query)
            {
                return Failure("A positional question cannot be combined with another command.");
            }

            command = AppCommand.Query;
            commandWasSelected = true;
            query = string.Join(' ', positionalQuery).Trim();
        }

        if (command == AppCommand.Query && string.IsNullOrWhiteSpace(query))
        {
            return Failure("--query requires non-empty text.");
        }

        if (dryRun && command != AppCommand.InstallFont)
        {
            return Failure("--dry-run is currently supported only with --install-font.");
        }

        return new CommandLineParseResult(
            new CommandLineOptions(command, query, language, noAnimation, noEmoji, yes, dryRun, pathAction),
            null);
    }

    /// <summary>Selects one top-level command and rejects ambiguous command combinations.</summary>
    private static bool TrySelect(
        AppCommand selected,
        ref AppCommand command,
        ref bool commandWasSelected,
        out string? error)
    {
        if (commandWasSelected && command != selected)
        {
            error = $"Commands '--{ToSwitch(command)}' and '--{ToSwitch(selected)}' cannot be combined.";
            return false;
        }

        command = selected;
        commandWasSelected = true;
        error = null;
        return true;
    }

    /// <summary>Reads one required non-empty value following a command-line option.</summary>
    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        out string? value,
        out string? error)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith('-'))
        {
            value = null;
            error = $"{option} requires a value.";
            return false;
        }

        value = args[++index].Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"{option} requires a non-empty value.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Creates a failed parse result with a stable user-facing message.</summary>
    private static CommandLineParseResult Failure(string? error) => new(null, error ?? "Invalid command line.");

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

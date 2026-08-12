// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IPortablePathService
{
    PortablePathPlan CreatePlan(PortablePathAction action);

    Task<PortablePathResult> ApplyAsync(PortablePathPlan plan, CancellationToken cancellationToken);
}

public sealed class PortablePathService : IPortablePathService
{
    private const string MarkerStart = "# >>> PromptMeUp hm PATH >>>";
    private const string MarkerEnd = "# <<< PromptMeUp hm PATH <<<";
    private readonly ILogger<PortablePathService> _logger;

    /// <summary>Creates the portable PATH manager.</summary>
    public PortablePathService(ILogger<PortablePathService> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Builds a human-readable, non-mutating PATH operation preview.</summary>
    public PortablePathPlan CreatePlan(PortablePathAction action)
    {
        var executableDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        var target = ResolvePersistenceTarget();
        var isPresent = IsPersisted(executableDirectory, target);
        var requiresChange = action switch
        {
            PortablePathAction.Install => !isPresent,
            PortablePathAction.Remove => isPresent,
            _ => false
        };
        var preview = action switch
        {
            PortablePathAction.Install => $"Add '{executableDirectory}' to {target}.",
            PortablePathAction.Remove => $"Remove the PromptMeUp-managed entry '{executableDirectory}' from {target}.",
            _ => $"Inspect whether '{executableDirectory}' is persisted in {target}."
        };
        return new PortablePathPlan(action, executableDirectory, target, preview, isPresent, requiresChange);
    }

    /// <summary>Applies an already previewed PATH plan to user scope without installing or moving binaries.</summary>
    public async Task<PortablePathResult> ApplyAsync(PortablePathPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        cancellationToken.ThrowIfCancellationRequested();
        if (plan.Action == PortablePathAction.Status || !plan.RequiresChange)
        {
            return new PortablePathResult(plan.Action, plan.ExecutableDirectory, plan.PersistenceTarget, false, plan.IsPresent);
        }

        if (OperatingSystem.IsWindows())
        {
            ApplyWindowsPath(plan);
        }
        else
        {
            await ApplyUnixProfileAsync(plan, cancellationToken).ConfigureAwait(false);
        }

        UpdateCurrentProcessPath(plan);
        var isPresent = IsPersisted(plan.ExecutableDirectory, plan.PersistenceTarget);
        _logger.LogInformation(
            "Portable PATH updated. Action={Action}, Target={Target}, Changed={Changed}, Present={Present}",
            plan.Action,
            plan.PersistenceTarget,
            true,
            isPresent);
        return new PortablePathResult(plan.Action, plan.ExecutableDirectory, plan.PersistenceTarget, true, isPresent);
    }

    /// <summary>Adds or removes the executable directory from the Windows user PATH.</summary>
    private static void ApplyWindowsPath(PortablePathPlan plan)
    {
        var entries = SplitPath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)).ToList();
        entries.RemoveAll(entry => PathsEqual(entry, plan.ExecutableDirectory));
        if (plan.Action == PortablePathAction.Install)
        {
            entries.Add(plan.ExecutableDirectory);
        }

        Environment.SetEnvironmentVariable(
            "PATH",
            string.Join(Path.PathSeparator, entries.Distinct(PathComparer)),
            EnvironmentVariableTarget.User);
    }

    /// <summary>Maintains one clearly marked PATH block in the active Unix shell profile.</summary>
    private static async Task ApplyUnixProfileAsync(PortablePathPlan plan, CancellationToken cancellationToken)
    {
        var profile = plan.PersistenceTarget;
        var directory = Path.GetDirectoryName(profile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var current = File.Exists(profile)
            ? await File.ReadAllTextAsync(profile, cancellationToken).ConfigureAwait(false)
            : string.Empty;
        var withoutManagedBlock = RemoveManagedBlock(current).TrimEnd();
        var updated = withoutManagedBlock;
        if (plan.Action == PortablePathAction.Install)
        {
            var shellLine = IsFishProfile(profile)
                ? $"fish_add_path --global {QuoteForPosixShell(plan.ExecutableDirectory)}"
                : $"export PATH={QuoteForPosixShell(plan.ExecutableDirectory)}:\"$PATH\"";
            updated = string.IsNullOrEmpty(withoutManagedBlock)
                ? $"{MarkerStart}{Environment.NewLine}{shellLine}{Environment.NewLine}{MarkerEnd}{Environment.NewLine}"
                : $"{withoutManagedBlock}{Environment.NewLine}{Environment.NewLine}{MarkerStart}{Environment.NewLine}{shellLine}{Environment.NewLine}{MarkerEnd}{Environment.NewLine}";
        }
        else if (!string.IsNullOrEmpty(updated))
        {
            updated += Environment.NewLine;
        }

        await File.WriteAllTextAsync(profile, updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates the child process environment so status checks are immediately consistent.</summary>
    private static void UpdateCurrentProcessPath(PortablePathPlan plan)
    {
        var entries = SplitPath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process)).ToList();
        entries.RemoveAll(entry => PathsEqual(entry, plan.ExecutableDirectory));
        if (plan.Action == PortablePathAction.Install)
        {
            entries.Insert(0, plan.ExecutableDirectory);
        }

        Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator, entries), EnvironmentVariableTarget.Process);
    }

    /// <summary>Checks the platform-specific persistent user configuration for the exact executable directory.</summary>
    private static bool IsPersisted(string executableDirectory, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            return SplitPath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User))
                .Any(entry => PathsEqual(entry, executableDirectory));
        }

        if (!File.Exists(target))
        {
            return false;
        }

        var content = File.ReadAllText(target);
        return content.Contains(MarkerStart, StringComparison.Ordinal)
               && content.Contains(executableDirectory, StringComparison.Ordinal);
    }

    /// <summary>Returns the Windows user scope or a profile file appropriate for the current Unix shell.</summary>
    private static string ResolvePersistenceTarget()
    {
        if (OperatingSystem.IsWindows())
        {
            return "Windows user PATH";
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            throw new InvalidOperationException("The user profile directory could not be resolved.");
        }

        var shell = Path.GetFileName(Environment.GetEnvironmentVariable("SHELL") ?? string.Empty);
        return shell.Equals("zsh", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(userProfile, ".zprofile")
            : shell.Equals("fish", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(userProfile, ".config", "fish", "config.fish")
                : Path.Combine(userProfile, ".profile");
    }

    /// <summary>Removes only the block owned by PromptMeUp and leaves surrounding profile content untouched.</summary>
    private static string RemoveManagedBlock(string content)
    {
        var start = content.IndexOf(MarkerStart, StringComparison.Ordinal);
        if (start < 0)
        {
            return content;
        }

        var end = content.IndexOf(MarkerEnd, start, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidDataException("The PromptMeUp PATH block is incomplete; edit it manually before retrying.");
        }

        end += MarkerEnd.Length;
        while (end < content.Length && content[end] is '\r' or '\n')
        {
            end++;
        }

        return content.Remove(start, end - start);
    }

    /// <summary>Splits PATH while dropping blank entries and surrounding quotes.</summary>
    private static IEnumerable<string> SplitPath(string? value) =>
        (value ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Trim('"'))
            .Where(entry => !string.IsNullOrWhiteSpace(entry));

    /// <summary>Compares normalized paths using platform-appropriate casing.</summary>
    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return PathComparer.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.ExpandEnvironmentVariables(left))),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.ExpandEnvironmentVariables(right))));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return PathComparer.Equals(left.Trim(), right.Trim());
        }
    }

    /// <summary>Quotes one literal path for POSIX-compatible shell configuration.</summary>
    private static string QuoteForPosixShell(string value) => "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";

    /// <summary>Identifies fish configuration because its PATH syntax differs from POSIX shells.</summary>
    private static bool IsFishProfile(string profile) => profile.EndsWith("config.fish", StringComparison.OrdinalIgnoreCase);

    private static StringComparer PathComparer { get; } = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}

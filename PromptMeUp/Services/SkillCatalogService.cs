// SPDX-License-Identifier: MIT

using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace PromptMeUp.Services;

/// <summary>Adapts CLI-Intelligence's package model with bounded parsing and content-bound activation.</summary>
public sealed class SkillCatalogService(AppPaths paths, ExperimentalStore store, ILocalizationService text, IPromptCatalogService prompts)
{
    internal const int MaximumContextTokens = 1800;
    private static readonly JsonSerializerOptions ContextJson = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
    private const int MaximumFiles = 128;
    private const int MaximumFileBytes = 65_536;
    private const int MaximumPackageBytes = 1_048_576;
    private string LocalRoot => Path.Combine(paths.DataDirectory, "skills");
    private string BundledRoot => Path.Combine(AppContext.BaseDirectory, "skills");

    /// <summary>Loads bundled then local packages, preserving local precedence and reporting incompatibilities.</summary>
    public IReadOnlyList<SkillDefinition> List()
    {
        var result = new Dictionary<string, SkillDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var (root, origin) in new[] { (BundledRoot, "bundled"), (LocalRoot, "local") })
        {
            if (!Directory.Exists(root))
            {
                continue;
            }
            RejectLinks(root);
            var directories = Directory.EnumerateDirectories(root).Take(65).ToArray();
            if (directories.Length > 64)
            {
                throw Invalid();
            }
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var directory in directories.Order(StringComparer.Ordinal))
            {
                var skill = Inspect(directory, origin);
                if (!names.Add(skill.Name))
                {
                    throw Invalid();
                }
                result[skill.Name] = skill;
            }
        }
        return result.Values.OrderBy(skill => skill.Name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Parses the YAML front matter and hashes every bounded regular file in the package.</summary>
    public SkillDefinition Inspect(string directory, string origin = "local")
    {
        try
        {
            return InspectCore(Path.GetFullPath(directory), origin);
        }
        catch (Exception exception) when (exception is YamlException or DecoderFallbackException or IOException
            or UnauthorizedAccessException or ArgumentException)
        {
            throw Invalid();
        }
    }

    /// <summary>Validates package structure and reads strict metadata from a bounded UTF-8 snapshot.</summary>
    private SkillDefinition InspectCore(string directory, string origin)
    {
        var files = PackageFiles(directory);
        var definition = files.SingleOrDefault(file => Path.GetRelativePath(directory, file) == "SKILL.md") ?? throw Invalid();
        var snapshot = files.ToDictionary(file => file, ReadBytes);
        if (snapshot.Values.Any(bytes => bytes.Length > MaximumFileBytes) || snapshot.Values.Sum(bytes => bytes.Length) > MaximumPackageBytes)
        {
            throw Invalid();
        }
        var utf8 = new UTF8Encoding(false, true);
        var content = utf8.GetString(snapshot[definition]).TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!content.StartsWith("---\n", StringComparison.Ordinal))
        {
            throw Invalid();
        }
        var end = content.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0 || end > 8192 || content[..end].Contains('&') || content[..end].Contains('*'))
        {
            throw Invalid();
        }
        var yaml = new YamlStream();
        var header = content[4..end];
        ValidateYamlDepth(header);
        using var reader = new StringReader(header);
        yaml.Load(reader);
        if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            throw Invalid();
        }
        var name = Scalar(root, "name").ToLowerInvariant();
        var description = Scalar(root, "description");
        var version = Scalar(root, "version", "1.0.0");
        if (!ValidName(name) || !ValidPathPart(name) || name == "metals-dev-monitor" || string.IsNullOrWhiteSpace(description) || description.Length > 500
            || version.Length is 0 or > 64 || version.Any(char.IsControl))
        {
            throw Invalid();
        }
        var instructions = content[(end + 5)..].Trim();
        if (instructions.Length is 0 or > 12_000)
        {
            throw Invalid();
        }
        var metadata = Mapping(Mapping(root, "metadata"), "openclaw");
        var os = Values(metadata, "os");
        if (os.Count == 0)
        {
            os = Values(root, "os");
        }
        var currentOs = OperatingSystem.IsWindows() ? "win32" : OperatingSystem.IsMacOS() ? "darwin" : "linux";
        string? reason = os.Count > 0 && !os.Contains("any") && !os.Contains(currentOs) ? text.Text("Lab.Platform") : null;
        var requirements = Mapping(metadata, "requires") ?? Mapping(root, "requires");
        foreach (var binary in Values(requirements, "bins"))
        {
            if (!ValidName(binary) || !HasBinary(binary))
            {
                reason = text.Text("Lab.Dependency", binary);
            }
        }
        // Environment requirements are metadata only: their values never enter a prompt or a command.
        foreach (var variable in Values(requirements, "env"))
        {
            if (variable.Length is 0 or > 100 || !(char.IsAsciiLetter(variable[0]) || variable[0] == '_')
                || !variable.All(character => char.IsAsciiLetterOrDigit(character) || character == '_'))
            {
                throw Invalid();
            }
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
            {
                reason = text.Text("Lab.Dependency", variable);
            }
        }
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
            hash.AppendData(Encoding.UTF8.GetBytes(relative + "\0" + snapshot[file].Length.ToString(CultureInfo.InvariantCulture) + "\0"));
            hash.AppendData(snapshot[file]);
            if (relative.StartsWith("scripts/", StringComparison.Ordinal)
                && string.Equals(Path.GetExtension(file), ".ps1", StringComparison.OrdinalIgnoreCase))
            {
                var action = Path.GetFileNameWithoutExtension(file);
                var source = utf8.GetString(snapshot[file]).TrimStart('\uFEFF');
                if (!ValidName(action) || source.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t'))
                    || !scripts.TryAdd(action, source))
                {
                    throw Invalid();
                }
            }
        }
        return new SkillDefinition(name, description, version, instructions,
            Path.GetFullPath(directory), origin, Convert.ToHexString(hash.GetHashAndReset()), reason,
            scripts);
    }

    /// <summary>Records the exact inspected content as enabled, or removes its activation.</summary>
    public async Task EnableAsync(SkillDefinition skill, bool enabled, CancellationToken ct)
    {
        if (enabled)
        {
            await EnsureContextFitsAsync(skill, ct).ConfigureAwait(false);
        }
        await store.SetAsync("skill:" + skill.Name, enabled ? skill.Fingerprint : string.Empty, ct).ConfigureAwait(false);
    }

    /// <summary>Rejects an unusable manual selection before replacing the user's current choice.</summary>
    public async Task SelectForQuestionsAsync(SkillDefinition skill, CancellationToken ct)
    {
        if (!await IsEnabledAsync(skill, ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException(text.Text("Lab.Activate"));
        }
        await EnsureContextFitsAsync(skill, ct).ConfigureAwait(false);
        await store.SetAsync("selected-skill", skill.Name, ct).ConfigureAwait(false);
    }

    /// <summary>Requires current availability and an exact match with the user's approved package content.</summary>
    public async Task<bool> IsEnabledAsync(SkillDefinition skill, CancellationToken ct) =>
        skill.UnavailableReason is null && await store.GetAsync("skill:" + skill.Name, ct).ConfigureAwait(false) == skill.Fingerprint;

    /// <summary>Selects enabled packages after resolving localized instructions and budgeting their complete serialized context.</summary>
    public async Task<IReadOnlyList<SkillDefinition>> SelectAsync(string query, CancellationToken ct, ICollection<string>? warnings = null)
    {
        var settings = await store.SettingsAsync(ct).ConfigureAwait(false);
        if (!settings.Enabled)
        {
            return [];
        }
        var explicitName = await store.GetAsync("selected-skill", ct).ConfigureAwait(false);
        var words = query.Split([' ', '\n', '.', ',', ':', '/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 3).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var selected = new List<SkillDefinition>();
        var template = (await prompts.GetAsync("skill-context", ct).ConfigureAwait(false)).ResolveText(text.Language);
        foreach (var candidate in List().Select(skill => (Skill: skill, Score: string.Equals(skill.Name, explicitName, StringComparison.OrdinalIgnoreCase) ? int.MaxValue
                     : settings.AutomaticSkills ? words.Count(word => (skill.Name + " " + skill.Description).Contains(word, StringComparison.OrdinalIgnoreCase)) : 0))
                 .Where(item => item.Score > 0).OrderByDescending(item => item.Score).ThenBy(item => item.Skill.Name, StringComparer.Ordinal))
        {
            if (!await IsEnabledAsync(candidate.Skill, ct).ConfigureAwait(false))
            {
                continue;
            }
            var resolved = await ResolveInstructionsAsync(candidate.Skill, ct).ConfigureAwait(false);
            if (ContextTokenEstimator.Messages([new("user", SerializeContext([.. selected, resolved], template))]) <= MaximumContextTokens)
            {
                selected.Add(resolved);
            }
            else if (candidate.Score == int.MaxValue)
            {
                warnings?.Add(text.Text("Lab.SkillTooLarge", candidate.Skill.Name, MaximumContextTokens));
            }
        }
        return selected;
    }

    /// <summary>Builds exactly the localized context used during selection, without rewriting its approved instructions.</summary>
    internal async Task<string> ContextAsync(IReadOnlyList<SkillDefinition> selected, CancellationToken ct)
    {
        if (selected.Count == 0)
        {
            return string.Empty;
        }
        var template = (await prompts.GetAsync("skill-context", ct).ConfigureAwait(false)).ResolveText(text.Language);
        return SerializeContext(selected, template);
    }

    /// <summary>Explains oversized instructions before activation or manual selection can appear to succeed.</summary>
    private async Task EnsureContextFitsAsync(SkillDefinition skill, CancellationToken ct)
    {
        var resolved = await ResolveInstructionsAsync(skill, ct).ConfigureAwait(false);
        var context = await ContextAsync([resolved], ct).ConfigureAwait(false);
        if (ContextTokenEstimator.Messages([new("user", context)]) > MaximumContextTokens)
        {
            throw new InvalidOperationException(text.Text("Lab.SkillTooLarge", skill.Name, MaximumContextTokens));
        }
    }

    /// <summary>Resolves bundled YAML before budgeting while retaining local package instructions unchanged.</summary>
    private async Task<SkillDefinition> ResolveInstructionsAsync(SkillDefinition skill, CancellationToken ct) =>
        skill.Origin == "bundled"
            ? skill with { Instructions = (await prompts.GetAsync("skill-" + skill.Name, ct).ConfigureAwait(false)).ResolveText(text.Language) }
            : skill;

    /// <summary>Uses the same JSON escaping and wrapper for admission checks and provider-bound context.</summary>
    private static string SerializeContext(IEnumerable<SkillDefinition> selected, string template) =>
        template.Replace("{skills}", JsonSerializer.Serialize(selected.Select(skill => new { skill.Name, instructions = skill.Instructions }), ContextJson), StringComparison.Ordinal);

    /// <summary>Inspects a bounded ZIP into an isolated staging directory; importing never executes scripts.</summary>
    public string StageZip(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length > MaximumPackageBytes
            || !string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw Invalid();
        }
        var staging = Path.Combine(paths.DataDirectory, "skill-staging", Guid.NewGuid().ToString("N"));
        RejectLinks(staging);
        Directory.CreateDirectory(staging);
        try
        {
            using var zip = ZipFile.OpenRead(path);
            long total = 0;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pathsSeen = new Dictionary<string, (string Spelling, bool Directory)>(StringComparer.OrdinalIgnoreCase);
            if (zip.Entries.Count is 0 or > MaximumFiles)
            {
                throw Invalid();
            }
            foreach (var entry in zip.Entries)
            {
                var relative = entry.FullName.Replace('\\', '/');
                var parts = relative.TrimEnd('/').Split('/');
                var mode = (entry.ExternalAttributes >> 16) & 0xF000;
                if (parts.Any(part => !ValidPathPart(part)) || !names.Add(relative.TrimEnd('/'))
                    || mode is not (0 or 0x8000 or 0x4000) || entry.Length > MaximumFileBytes
                    || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0
                    || (relative.EndsWith('/') && (entry.Length != 0 || mode == 0x8000))
                    || (!relative.EndsWith('/') && mode == 0x4000)
                    || (total += entry.Length) > MaximumPackageBytes)
                {
                    throw Invalid();
                }
                // Register implicit directories too, so case aliases and file/directory collisions fail on every OS.
                for (var index = 0; index < parts.Length; index++)
                {
                    var segment = string.Join('/', parts.Take(index + 1));
                    var isDirectory = index < parts.Length - 1 || relative.EndsWith('/');
                    if (pathsSeen.TryGetValue(segment, out var seen)
                        && (seen.Spelling != segment || seen.Directory != isDirectory))
                    {
                        throw Invalid();
                    }
                    pathsSeen[segment] = (segment, isDirectory);
                }
                var target = Path.GetFullPath(Path.Combine(staging, relative));
                if (!target.StartsWith(staging + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    throw Invalid();
                }
                if (relative.EndsWith('/'))
                {
                    Directory.CreateDirectory(target);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    using var source = entry.Open();
                    using var destination = new FileStream(target, FileMode.CreateNew);
                    var buffer = new byte[4096];
                    var written = 0;
                    int count;
                    while ((count = source.Read(buffer)) > 0)
                    {
                        if ((written += count) > MaximumFileBytes || written > entry.Length)
                        {
                            throw Invalid();
                        }
                        destination.Write(buffer, 0, count);
                    }
                    if (written != entry.Length)
                    {
                        throw Invalid();
                    }
                }
            }
            var roots = Directory.GetDirectories(staging);
            if (roots.Length != 1 || Directory.GetFiles(staging).Length != 0)
            {
                throw Invalid();
            }
            _ = Inspect(roots[0]);
            return roots[0];
        }
        catch (Exception exception)
        {
            RejectLinks(staging);
            Directory.Delete(staging, true);
            if (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
            {
                throw Invalid();
            }
            throw;
        }
    }

    /// <summary>Publishes an unchanged reviewed staging package without overwriting existing local skills.</summary>
    public void Import(SkillDefinition reviewed)
    {
        ValidateStaging(reviewed.Directory);
        var current = Inspect(reviewed.Directory);
        if (current.Fingerprint != reviewed.Fingerprint || current.Name != reviewed.Name)
        {
            throw Invalid();
        }
        RejectLinks(LocalRoot);
        Directory.CreateDirectory(LocalRoot);
        var lockPath = Path.Combine(LocalRoot, ".import.lock");
        RejectLinks(lockPath);
        // Keep this file in place so concurrent processes lock the same file across imports.
        using var importLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (Directory.EnumerateDirectories(LocalRoot).Take(64).Count() >= 64)
        {
            throw Invalid();
        }
        Directory.Move(reviewed.Directory, Path.Combine(LocalRoot, reviewed.Name));
    }

    /// <summary>Removes only the generated staging parent after an import or cancellation.</summary>
    public void DiscardStaging(string directory)
    {
        ValidateStaging(directory);
        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)))!;
        if (Directory.Exists(parent))
        {
            Directory.Delete(parent, true);
        }
    }

    /// <summary>Checks that staging cleanup cannot escape a generated GUID child.</summary>
    private void ValidateStaging(string directory)
    {
        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)))!;
        if (!string.Equals(Path.GetDirectoryName(parent), Path.GetFullPath(Path.Combine(paths.DataDirectory, "skill-staging")),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
            || !Guid.TryParseExact(Path.GetFileName(parent), "N", out _))
        {
            throw Invalid();
        }
        RejectLinks(parent);
    }

    /// <summary>Enumerates only bounded regular package files without following links.</summary>
    private IReadOnlyList<string> PackageFiles(string directory)
    {
        var files = new List<string>();
        var pending = new Queue<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        pending.Enqueue(directory);
        var entries = 0;
        long bytes = 0;
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            RejectLinks(current);
            foreach (var item in Directory.EnumerateFileSystemEntries(current))
            {
                RejectLinks(item);
                if (++entries > MaximumFiles || !ValidPathPart(Path.GetFileName(item))
                    || !names.Add(Path.GetRelativePath(directory, item).Replace('\\', '/')))
                {
                    throw Invalid();
                }
                if (Directory.Exists(item))
                {
                    pending.Enqueue(item);
                    continue;
                }
                var size = new FileInfo(item).Length;
                if (size > MaximumFileBytes || (bytes += size) > MaximumPackageBytes)
                {
                    throw Invalid();
                }
                files.Add(item);
            }
        }
        return files.Order(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Rejects symbolic links and redirected ancestors in a skill path.</summary>
    private void RejectLinks(string path)
    {
        string? current = Path.GetFullPath(path);
        while (current is not null)
        {
            if (File.Exists(current) || Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    current = MacOsSystemAliasTarget(current) ?? throw Invalid();
                    continue;
                }
            }
            current = Path.GetDirectoryName(current);
        }
    }

    /// <summary>Recognizes only macOS's fixed root aliases and leaves their canonical ancestors subject to link rejection.</summary>
    private static string? MacOsSystemAliasTarget(string path)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return null;
        }
        var expected = path switch
        {
            "/var" => "/private/var",
            "/tmp" => "/private/tmp",
            "/etc" => "/private/etc",
            _ => null
        };
        return expected is not null && Directory.Exists(expected)
            && string.Equals(Directory.ResolveLinkTarget(path, returnFinalTarget: false)?.FullName, expected, StringComparison.Ordinal)
                ? expected : null;
    }

    /// <summary>Restricts identifiers to a portable filename-safe vocabulary.</summary>
    private static bool ValidName(string value) => value.Length is > 0 and <= 64
        && char.IsAsciiLetter(value[0]) && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    /// <summary>Rejects traversal, streams, trailing-dot aliases, and device names in archive paths.</summary>
    private static bool ValidPathPart(string part) => part.Length is > 0 and <= 100 && part is not ("." or "..")
        && !part.EndsWith('.') && !part.EndsWith(' ') && part.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')
        && !new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }
            .Contains(part.Split('.')[0], StringComparer.OrdinalIgnoreCase);

    /// <summary>Checks executable presence without running commands during catalog discovery.</summary>
    private static bool HasBinary(string name)
    {
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!Path.IsPathFullyQualified(directory))
            {
                continue;
            }
            var path = Path.Combine(directory, OperatingSystem.IsWindows() ? name + ".exe" : name);
            if (File.Exists(path) && (OperatingSystem.IsWindows()
                || (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Returns an optional nested YAML mapping without flattening its scope.</summary>
    private YamlMappingNode? Mapping(YamlMappingNode? node, string key) => Child(node, key) switch
    {
        null => null,
        YamlMappingNode mapping => mapping,
        _ => throw Invalid()
    };

    /// <summary>Reads a scalar metadata field.</summary>
    private string Scalar(YamlMappingNode? node, string key, string fallback = "") => Child(node, key) switch
    {
        null => fallback,
        YamlScalarNode { Value: not null } scalar => scalar.Value,
        _ => throw Invalid()
    };

    /// <summary>Reads a scoped scalar or sequence requirement.</summary>
    private IReadOnlyList<string> Values(YamlMappingNode? node, string key) =>
        Child(node, key) switch
        {
            null => [],
            YamlSequenceNode sequence when sequence.Children.Count <= 32 => sequence.Children
                .Select(item => item is YamlScalarNode { Value: not null } scalar && scalar.Value.Length is > 0 and <= 100
                    ? scalar.Value : throw Invalid()).ToArray(),
            YamlScalarNode { Value: not null } scalar when scalar.Value.Length is > 0 and <= 100 => [scalar.Value],
            _ => throw Invalid()
        };

    /// <summary>Produces a localized fail-closed package validation error.</summary>
    private InvalidOperationException Invalid() => new(text.Text("Lab.Invalid"));

    /// <summary>Caps YAML nesting before constructing a recursive representation of untrusted front matter.</summary>
    private void ValidateYamlDepth(string header)
    {
        using var reader = new StringReader(header);
        var parser = new Parser(reader);
        var depth = 0;
        while (parser.MoveNext())
        {
            if (parser.Current is YamlDotNet.Core.Events.MappingStart or YamlDotNet.Core.Events.SequenceStart)
            {
                if (++depth > 8)
                {
                    throw Invalid();
                }
            }
            else if (parser.Current is YamlDotNet.Core.Events.MappingEnd or YamlDotNet.Core.Events.SequenceEnd)
            {
                depth--;
            }
        }
    }

    /// <summary>Reads at most one package file limit even when a file grows during inspection.</summary>
    private byte[] ReadBytes(string path)
    {
        using var stream = File.OpenRead(path);
        var bytes = new byte[MaximumFileBytes + 1];
        var length = stream.ReadAtLeast(bytes, bytes.Length, throwOnEndOfStream: false);
        if (length > MaximumFileBytes)
        {
            throw Invalid();
        }
        return bytes[..length];
    }

    /// <summary>Looks up one exact YAML key without depending on dictionary implementation details.</summary>
    private static YamlNode? Child(YamlMappingNode? node, string key) =>
        node is not null && node.Children.TryGetValue(new YamlScalarNode(key), out var child) ? child : null;
}

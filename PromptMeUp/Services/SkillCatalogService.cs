// SPDX-License-Identifier: MIT

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PromptMeUp.Infrastructure;
using PromptMeUp.Models;
using YamlDotNet.RepresentationModel;

namespace PromptMeUp.Services;

/// <summary>Adapts CLI-Intelligence's package model with bounded parsing and content-bound activation.</summary>
public sealed class SkillCatalogService(AppPaths paths, ExperimentalStore store, ILocalizationService text)
{
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
        var files = PackageFiles(directory);
        var definition = files.SingleOrDefault(file => Path.GetRelativePath(directory, file) == "SKILL.md") ?? throw Invalid();
        var snapshot = files.ToDictionary(file => file, ReadBytes);
        if (snapshot.Values.Any(bytes => bytes.Length > MaximumFileBytes) || snapshot.Values.Sum(bytes => bytes.Length) > MaximumPackageBytes)
        {
            throw Invalid();
        }
        var content = Encoding.UTF8.GetString(snapshot[definition]).TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal);
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
        using var reader = new StringReader(content[4..end]);
        yaml.Load(reader);
        if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            throw Invalid();
        }
        var name = Scalar(root, "name");
        var description = Scalar(root, "description");
        if (!ValidName(name) || name == "metals-dev-monitor" || string.IsNullOrWhiteSpace(description) || description.Length > 500)
        {
            throw Invalid();
        }
        var instructions = content[(end + 5)..].Trim();
        if (instructions.Length > 12_000)
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
        foreach (var binary in Values(Mapping(metadata, "requires"), "bins"))
        {
            if (!ValidName(binary) || !HasBinary(binary))
            {
                reason = text.Text("Lab.Dependency", binary);
            }
        }
        // Environment requirements are metadata only: their values never enter a prompt or a command.
        foreach (var variable in Values(Mapping(metadata, "requires"), "env"))
        {
            if (variable.Length > 100 || string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
            {
                reason = text.Text("Lab.Dependency", variable);
            }
        }
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(directory, file).Replace('\\', '/') + "\0"));
            hash.AppendData(snapshot[file]);
        }
        return new SkillDefinition(name, description, Scalar(root, "version", "1.0.0"), instructions,
            Path.GetFullPath(directory), origin, Convert.ToHexString(hash.GetHashAndReset()), reason,
            files.Where(file => Path.GetRelativePath(directory, file).Replace('\\', '/').StartsWith("scripts/", StringComparison.Ordinal)
                && Path.GetExtension(file) == ".ps1").ToDictionary(file => Path.GetFileNameWithoutExtension(file), file => Encoding.UTF8.GetString(snapshot[file])));
    }

    /// <summary>Records the exact inspected content as enabled, or removes its activation.</summary>
    public Task EnableAsync(SkillDefinition skill, bool enabled, CancellationToken ct) =>
        store.SetAsync("skill:" + skill.Name, enabled ? skill.Fingerprint : string.Empty, ct);

    /// <summary>Requires current availability and an exact match with the user's approved package content.</summary>
    public async Task<bool> IsEnabledAsync(SkillDefinition skill, CancellationToken ct) =>
        skill.UnavailableReason is null && await store.GetAsync("skill:" + skill.Name, ct).ConfigureAwait(false) == skill.Fingerprint;

    /// <summary>Selects enabled explicit or contextually relevant packages within an estimated instruction budget.</summary>
    public async Task<IReadOnlyList<SkillDefinition>> SelectAsync(string query, CancellationToken ct)
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
        long tokens = 0;
        foreach (var candidate in List().Select(skill => (Skill: skill, Score: skill.Name == explicitName ? int.MaxValue
                     : settings.AutomaticSkills ? words.Count(word => (skill.Name + " " + skill.Description).Contains(word, StringComparison.OrdinalIgnoreCase)) : 0))
                 .Where(item => item.Score > 0).OrderByDescending(item => item.Score).ThenBy(item => item.Skill.Name, StringComparer.Ordinal))
        {
            var cost = ContextTokenEstimator.Text(candidate.Skill.Instructions) + 100;
            if (tokens + cost <= 1800 && await IsEnabledAsync(candidate.Skill, ct).ConfigureAwait(false))
            {
                selected.Add(candidate.Skill);
                tokens += cost;
            }
        }
        return selected;
    }

    /// <summary>Inspects a bounded ZIP into an isolated staging directory; importing never executes scripts.</summary>
    public string StageZip(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length > MaximumPackageBytes || Path.GetExtension(path) != ".zip")
        {
            throw Invalid();
        }
        var staging = Path.Combine(paths.DataDirectory, "skill-staging", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            using var zip = ZipFile.OpenRead(path);
            long total = 0;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (zip.Entries.Count > MaximumFiles)
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
                    || (total += entry.Length) > MaximumPackageBytes)
                {
                    throw Invalid();
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
        catch
        {
            Directory.Delete(staging, true);
            throw;
        }
    }

    /// <summary>Publishes an unchanged reviewed staging package without overwriting existing local skills.</summary>
    public void Import(SkillDefinition reviewed)
    {
        ValidateStaging(reviewed.Directory);
        if (Inspect(reviewed.Directory).Fingerprint != reviewed.Fingerprint)
        {
            throw Invalid();
        }
        Directory.CreateDirectory(LocalRoot);
        RejectLinks(LocalRoot);
        Directory.Move(reviewed.Directory, Path.Combine(LocalRoot, reviewed.Name));
    }

    /// <summary>Removes only the generated staging parent after an import or cancellation.</summary>
    public void DiscardStaging(string directory)
    {
        ValidateStaging(directory);
        var parent = Path.GetDirectoryName(directory)!;
        if (Directory.Exists(parent))
        {
            Directory.Delete(parent, true);
        }
    }

    /// <summary>Checks that staging cleanup cannot escape a generated GUID child.</summary>
    private void ValidateStaging(string directory)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(directory))!;
        if (Path.GetDirectoryName(parent) != Path.Combine(paths.DataDirectory, "skill-staging")
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
                if (++entries > MaximumFiles)
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
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        {
            if (File.Exists(current) || Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    throw Invalid();
                }
            }
        }
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
    private static bool HasBinary(string name) => (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Any(directory => Path.IsPathFullyQualified(directory) && File.Exists(Path.Combine(directory, OperatingSystem.IsWindows() ? name + ".exe" : name)));

    /// <summary>Returns an optional nested YAML mapping without flattening its scope.</summary>
    private static YamlMappingNode? Mapping(YamlMappingNode? node, string key) =>
        Child(node, key) as YamlMappingNode;

    /// <summary>Reads a scalar metadata field.</summary>
    private static string Scalar(YamlMappingNode? node, string key, string fallback = "") =>
        (Child(node, key) as YamlScalarNode)?.Value ?? fallback;

    /// <summary>Reads a scoped scalar or sequence requirement.</summary>
    private static IReadOnlyList<string> Values(YamlMappingNode? node, string key) =>
        Child(node, key) switch
        {
            YamlSequenceNode sequence => sequence.Children.Select(item => ((YamlScalarNode)item).Value ?? string.Empty).ToArray(),
            YamlScalarNode scalar when scalar.Value is not null => [scalar.Value],
            _ => []
        };

    /// <summary>Produces a localized fail-closed package validation error.</summary>
    private InvalidOperationException Invalid() => new(text.Text("Lab.Invalid"));

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

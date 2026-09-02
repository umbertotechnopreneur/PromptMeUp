// SPDX-License-Identifier: MIT

using System.IO.Enumeration;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public sealed class FilePreviewService(ILocalizationService text, BoundedTextInput input)
{
    public const int MaximumFiles = 1_000;

    /// <summary>Builds a bounded, non-recursive local file-effect snapshot without running shell or provider code.</summary>
    public FilePreview Build(CommandLineOptions options)
    {
        try
        {
            var source = Path.GetFullPath(input.Sanitize(options.InputFile!, 4096, fromArgument: true));
            EnsureUnlinked(source);
            var operation = options.PreviewAction!;
            if (operation is not ("copy" or "move" or "rename" or "delete"))
            {
                throw new InvalidOperationException(text.Text("Preview.Usage"));
            }
            var pattern = options.Pattern ?? "*";
            if (pattern.IndexOfAny(['/', '\\', '\0', ':']) >= 0)
            {
                throw new InvalidOperationException(text.Text("Preview.Usage"));
            }
            var prefix = options.Prefix ?? string.Empty;
            if (prefix.Length > 0)
            {
                input.Sanitize(prefix, 100, fromArgument: true);
            }
            if (operation == "rename" && (prefix.Length == 0 || prefix.Length > 100
                || prefix.Any(character => char.IsControl(character) || "<>:\"/\\|?*".Contains(character))))
            {
                throw new InvalidOperationException(text.Text("Preview.Usage"));
            }
            string? destinationDirectory = null;
            if (operation is "copy" or "move")
            {
                destinationDirectory = Path.GetFullPath(input.Sanitize(options.OutputFile!, 4096, fromArgument: true));
                if (!Directory.Exists(destinationDirectory))
                {
                    throw new InvalidOperationException(text.Text("Preview.Destination"));
                }
                EnsureUnlinked(destinationDirectory);
            }
            var candidates = Directory.Exists(source) ? Directory.EnumerateFiles(source) : [source];
            var selected = new List<FileEffect>();
            var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var scanned = 0;
            foreach (var candidate in candidates)
            {
                if (++scanned > 10_000)
                {
                    throw new InvalidOperationException(text.Text("Preview.TooMany", 10_000));
                }
                if (!FileSystemName.MatchesSimpleExpression(pattern, Path.GetFileName(candidate), ignoreCase: true))
                {
                    continue;
                }
                if (selected.Count == MaximumFiles)
                {
                    throw new InvalidOperationException(text.Text("Preview.TooMany", MaximumFiles));
                }
                EnsureUnlinked(candidate);
                var info = new FileInfo(candidate);
                if (!info.Exists)
                {
                    throw new InvalidOperationException(text.Text("Preview.Changed"));
                }
                var destination = operation switch
                {
                    "copy" or "move" => Path.Combine(destinationDirectory!, info.Name),
                    "rename" => Path.Combine(info.DirectoryName!, prefix + info.Name),
                    _ => null
                };
                var collision = destination is not null && (File.Exists(destination) || Directory.Exists(destination) || !destinations.Add(destination));
                selected.Add(new FileEffect(info.FullName, destination, info.Length, info.LastWriteTimeUtc.Ticks, collision));
            }
            if (selected.Count == 0)
            {
                throw new InvalidOperationException(text.Text("Preview.Empty"));
            }
            return new FilePreview(operation, selected);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new InvalidOperationException(text.Text("Preview.ReadError"));
        }
    }

    /// <summary>Checks whether source metadata, link ancestry, and the destination still match the displayed snapshot.</summary>
    public bool IsCurrent(FileEffect effect)
    {
        try
        {
            EnsureUnlinked(effect.Source);
            var info = new FileInfo(effect.Source);
            if (!info.Exists || info.Length != effect.Bytes || info.LastWriteTimeUtc.Ticks != effect.LastWriteTicks)
            {
                return false;
            }
            if (effect.Destination is not null)
            {
                EnsureUnlinked(Path.GetDirectoryName(effect.Destination)!);
                return !File.Exists(effect.Destination) && !Directory.Exists(effect.Destination);
            }
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Produces the exact effect with runtime snapshot guards and atomic no-overwrite copy or move semantics.</summary>
    public string BuildCommand(FilePreview preview, FileEffect effect)
    {
        if (!preview.Effects.Contains(effect) || effect.Collision || !IsCurrent(effect))
        {
            throw new InvalidOperationException(text.Text("Preview.Changed"));
        }
        var source = ScriptArtifactService.Quote(effect.Source);
        var destination = effect.Destination is null ? string.Empty : ScriptArtifactService.Quote(effect.Destination);
        var error = ScriptArtifactService.Quote(text.Text("Preview.Changed"));
        var guard = "$ErrorActionPreference = 'Stop'; $item = [IO.FileInfo]::new(" + source + "); " +
            $"if (!$item.Exists -or $item.Length -ne {effect.Bytes} -or $item.LastWriteTimeUtc.Ticks -ne {effect.LastWriteTicks} " +
            $"-or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {{ throw {error} }}; " +
            "$parent = $item.Directory; " + ParentGuard(error);
        if (effect.Destination is not null)
        {
            guard += "$parent = [IO.DirectoryInfo]::new(" + ScriptArtifactService.Quote(Path.GetDirectoryName(effect.Destination)!) + "); " + ParentGuard(error);
        }
        return guard + (preview.Operation switch
        {
            "copy" => $"[IO.File]::Copy({source}, {destination}, $false)",
            "move" or "rename" => $"[IO.File]::Move({source}, {destination}, $false)",
            "delete" => $"[IO.File]::Delete({source})",
            _ => throw new InvalidOperationException(text.Text("Preview.Usage"))
        });
    }

    /// <summary>Checks every existing path component and rejects symbolic links and reparse points.</summary>
    private void EnsureUnlinked(string path)
    {
        FileSystemInfo? current = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
        while (current is not null)
        {
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(text.Text("Preview.Links"));
            }
            current = current is FileInfo file ? file.Directory : ((DirectoryInfo)current).Parent;
        }
    }

    /// <summary>Builds visible link-ancestry guards that run again after the user's approval.</summary>
    private static string ParentGuard(string error) =>
        $"while ($null -ne $parent) {{ if (!$parent.Exists -or ($parent.Attributes -band [IO.FileAttributes]::ReparsePoint)) {{ throw {error} }}; $parent = $parent.Parent }}; ";
}

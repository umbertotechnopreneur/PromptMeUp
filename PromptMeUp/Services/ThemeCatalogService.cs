// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

public interface IThemeCatalogService
{
    IReadOnlyList<TerminalThemeDefinition> Themes { get; }

    TerminalThemeDefinition Resolve(string id);
}

/// <summary>Loads bounded local theme files and rejects unsupported or unreadable palettes.</summary>
public sealed class ThemeCatalogService : IThemeCatalogService
{
    private const int MaximumThemeCount = 32;
    private const int MaximumThemeFileBytes = 16 * 1024;
    private static readonly string[] LegacyDefinitionProperties = ["version", "id", "name", "colors"];
    private static readonly string[] AttributionDefinitionProperties = ["version", "id", "name", "author", "description", "colors"];
    private static readonly string[] DefinitionProperties = ["version", "id", "name", "author", "website", "description", "colors"];
    private static readonly string[] ColorProperties =
    [
        "background", "primary", "muted", "accent", "info", "divider", "success", "warning", "error",
        "selectionBackground", "selectionForeground"
    ];

    /// <summary>Loads every JSON theme from the specified application theme directory.</summary>
    public ThemeCatalogService(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        try
        {
            var files = Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                .Take(MaximumThemeCount + 1)
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .ToArray();
            if (files.Length is 0 or > MaximumThemeCount)
            {
                throw new InvalidOperationException($"The theme directory must contain between 1 and {MaximumThemeCount} JSON files.");
            }

            var themes = files.Select(ReadTheme).ToArray();
            if (themes.Select(theme => theme.Id).Distinct(StringComparer.Ordinal).Count() != themes.Length)
            {
                throw new InvalidOperationException("The theme directory contains duplicate theme identifiers.");
            }
            if (!themes.Any(theme => theme.Id == "cyan"))
            {
                throw new InvalidOperationException("The theme directory must contain the default cyan theme.");
            }

            Themes = Array.AsReadOnly(themes.OrderBy(theme => theme.Id == "cyan" ? 0 : 1)
                .ThenBy(theme => theme.Name, StringComparer.Ordinal).ToArray());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("The application theme directory could not be read.", exception);
        }
    }

    public IReadOnlyList<TerminalThemeDefinition> Themes { get; }

    /// <summary>Returns the exact configured theme or rejects a missing identifier.</summary>
    public TerminalThemeDefinition Resolve(string id)
    {
        if (!IsValidId(id))
        {
            throw new InvalidOperationException("The selected terminal theme identifier is invalid.");
        }

        return Themes.FirstOrDefault(theme => theme.Id == id)
            ?? throw new InvalidOperationException($"The selected terminal theme '{id}' is not available.");
    }

    /// <summary>Checks identifiers that can safely be persisted and matched across platforms.</summary>
    internal static bool IsValidId(string? id) => id is { Length: >= 1 and <= 32 }
        && id[0] is >= 'a' and <= 'z'
        && id.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    /// <summary>Reads at most the supported file size and reports errors against the theme filename.</summary>
    private static TerminalThemeDefinition ReadTheme(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            var buffer = new byte[MaximumThemeFileBytes + 1];
            var count = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            if (count is 0 or > MaximumThemeFileBytes)
            {
                throw new InvalidOperationException($"Theme files must contain between 1 and {MaximumThemeFileBytes} bytes.");
            }

            using var document = JsonDocument.Parse(buffer.AsMemory(0, count), new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("version", out var schema)
                || schema.ValueKind != JsonValueKind.Number
                || !schema.TryGetInt32(out var version) || version is not (1 or 2 or 3))
            {
                throw new InvalidOperationException("Only theme schema versions 1, 2, and 3 are supported.");
            }
            RequireProperties(root, version switch
            {
                1 => LegacyDefinitionProperties,
                2 => AttributionDefinitionProperties,
                _ => DefinitionProperties
            });

            var id = ReadString(root, "id");
            var name = ReadString(root, "name");
            if (!IsValidId(id) || !string.Equals(Path.GetFileName(file), id + ".json", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The theme identifier must match its lowercase JSON filename.");
            }
            if (name.Length is < 1 or > 48 || name != name.Trim() || name.Any(char.IsControl))
            {
                throw new InvalidOperationException("Theme names must contain 1 to 48 visible characters without surrounding whitespace.");
            }

            var colors = root.GetProperty("colors");
            RequireProperties(colors, ColorProperties);
            var values = ColorProperties.Select(property => ReadColor(colors, property)).ToArray();
            var palette = new TerminalThemeColors(values[0], values[1], values[2], values[3], values[4], values[5],
                values[6], values[7], values[8], values[9], values[10]);
            ValidateContrast(palette);
            return new TerminalThemeDefinition(version, id, name, palette)
            {
                Author = version >= 2 ? ReadMetadata(root, "author", 80) : null,
                Website = version >= 3 ? ReadWebsite(root) : null,
                Description = version >= 2 ? ReadMetadata(root, "description", 240) : null,
                SourcePath = Path.GetFullPath(file)
            };
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Invalid terminal theme '{Path.GetFileName(file)}': {exception.Message}", exception);
        }
    }

    /// <summary>Rejects duplicate, unknown, or missing properties at either schema object level.</summary>
    private static void RequireProperties(JsonElement element, IReadOnlyList<string> expected)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Theme definitions and colors must be JSON objects.");
        }

        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!expected.Contains(property.Name, StringComparer.Ordinal) || !found.Add(property.Name))
            {
                throw new InvalidOperationException("Theme JSON contains an unknown or duplicate property.");
            }
        }
        if (found.Count != expected.Count)
        {
            throw new InvalidOperationException("Theme JSON is missing a required property.");
        }
    }

    /// <summary>Reads a required JSON string without accepting coercion or null.</summary>
    private static string ReadString(JsonElement element, string property) =>
        element.GetProperty(property).ValueKind == JsonValueKind.String
            ? element.GetProperty(property).GetString()!
            : throw new InvalidOperationException($"Theme property '{property}' must be a string.");

    /// <summary>Validates bounded, single-line theme attribution without accepting empty or padded metadata.</summary>
    private static string ReadMetadata(JsonElement element, string property, int maximumLength)
    {
        var value = ReadString(element, property);
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength || value != value.Trim() || value.Any(char.IsControl))
        {
            throw new InvalidOperationException($"Theme '{property}' must contain 1 to {maximumLength} visible characters without surrounding whitespace.");
        }
        return value;
    }

    /// <summary>Accepts a bounded absolute HTTP(S) website without credentials or malformed URL syntax.</summary>
    private static string ReadWebsite(JsonElement element)
    {
        var value = ReadMetadata(element, "website", 2048);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var website)
            || website.Scheme is not ("http" or "https")
            || string.IsNullOrWhiteSpace(website.Host)
            || !string.IsNullOrEmpty(website.UserInfo)
            || !website.IsWellFormedOriginalString())
        {
            throw new InvalidOperationException("Theme 'website' must be an absolute HTTP(S) URL without credentials.");
        }
        return value;
    }

    /// <summary>Accepts only explicit six-digit RGB colors without terminal markup or named colors.</summary>
    private static string ReadColor(JsonElement element, string property)
    {
        var value = ReadString(element, property);
        if (value.Length == 7 && value[0] == '#' && value.Skip(1).All(char.IsAsciiHexDigit))
        {
            return value.ToUpperInvariant();
        }

        throw new InvalidOperationException($"Theme color '{property}' must use #RRGGBB notation.");
    }

    /// <summary>Requires readable text and distinct dividers against each theme's explicit background.</summary>
    private static void ValidateContrast(TerminalThemeColors colors)
    {
        string[] textColors = [colors.Primary, colors.Muted, colors.Accent, colors.Info, colors.Success, colors.Warning, colors.Error, "#F5F5F5"];
        if (textColors.Any(color => ContrastRatio(color, colors.Background) < 4.5d)
            || ContrastRatio(colors.SelectionForeground, colors.SelectionBackground) < 4.5d
            || ContrastRatio(colors.Divider, colors.Background) < 3d)
        {
            throw new InvalidOperationException("Theme contrast must reach 4.5:1 for text and selections, and 3:1 for dividers.");
        }
    }

    /// <summary>Computes the relative luminance ratio between two validated RGB colors.</summary>
    private static double ContrastRatio(string first, string second)
    {
        var firstLuminance = Luminance(first);
        var secondLuminance = Luminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05d) / (Math.Min(firstLuminance, secondLuminance) + 0.05d);
    }

    /// <summary>Converts sRGB channels to linear light before applying luminance weights.</summary>
    private static double Luminance(string color) =>
        (0.2126d * LinearChannel(color.AsSpan(1, 2)))
        + (0.7152d * LinearChannel(color.AsSpan(3, 2)))
        + (0.0722d * LinearChannel(color.AsSpan(5, 2)));

    /// <summary>Linearizes one hexadecimal sRGB channel for contrast validation.</summary>
    private static double LinearChannel(ReadOnlySpan<char> channel)
    {
        var value = int.Parse(channel, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;
        return value <= 0.04045d ? value / 12.92d : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
    }
}

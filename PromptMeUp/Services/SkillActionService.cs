// SPDX-License-Identifier: MIT

using System.Text.Json;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Builds inert skill commands; execution remains exclusively owned by AuthorizedCommandWorkflow.</summary>
public sealed class SkillActionService(ISensitiveDataRedactor redactor, ILocalizationService text)
{
    /// <summary>Lists supported native actions or the inspected PowerShell script names.</summary>
    public IReadOnlyList<string> Actions(SkillDefinition skill) => skill.Origin == "bundled" ? skill.Name switch
    {
        "git" => ["status", "log", "diff"],
        "filesystem" => ["list", "read", "info"],
        "set_reminder" => ["manage"],
        _ => skill.Scripts.Keys.Order(StringComparer.Ordinal).ToArray()
    } : skill.Scripts.Keys.Order(StringComparer.Ordinal).ToArray();

    /// <summary>Provides editable non-sensitive examples only for known bundled skill contracts.</summary>
    public string DefaultParameters(SkillDefinition skill) => skill.Origin != "bundled" ? "{}" : skill.Name switch
    {
        "concat-files" => "{\"InputFolder\":\".\"}",
        "clipboard" => "{\"Action\":\"read\"}",
        "screenshot" => "{\"Mode\":\"active_window\",\"OutputPath\":\"./screenshot.png\"}",
        "system_info" => "{\"Action\":\"overview\"}",
        "http_request" => "{\"Url\":\"https://example.com\",\"Method\":\"GET\"}",
        "web_search" => "{\"Query\":\"PowerShell Get-FileHash\",\"MaxResults\":5}",
        "timezone_convert" => "{\"ToTimeZone\":\"UTC\"}",
        _ => "{}"
    };

    /// <summary>Creates a narrowly defined native command using literal quoting and no free-form flags.</summary>
    public string NativeCommand(SkillDefinition skill, string action, string path)
    {
        if (skill.Origin != "bundled" || !Actions(skill).Contains(action) || string.IsNullOrWhiteSpace(path)
            || path.Any(char.IsControl))
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        try
        {
            path = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        if (path != redactor.Redact(path))
        {
            throw new InvalidOperationException(text.Text("Memory.Secret"));
        }
        var literal = ScriptArtifactService.Quote(path);
        var fileError = ScriptArtifactService.Quote(text.Text("Input.FileError"));
        return (skill.Name, action) switch
        {
            ("git", "status") => $"git --no-pager --no-optional-locks -C {literal} status --short --branch",
            ("git", "log") => $"git --no-pager --no-optional-locks -C {literal} log -n 20 --oneline --no-decorate",
            ("git", "diff") => $"git --no-pager --no-optional-locks -C {literal} diff --no-ext-diff --no-textconv --",
            ("filesystem", "list") => $"Get-ChildItem -LiteralPath {literal} -Force | Select-Object -First 200 Mode,Length,Name",
            ("filesystem", "info") => $"Get-Item -LiteralPath {literal} | Select-Object Name,Length,LastWriteTime,Attributes",
            ("filesystem", "read") => $"$item = Get-Item -LiteralPath {literal} -Force -ErrorAction Stop; if ($item.PSIsContainer -or $item.Length -gt 102400 -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {{ throw {fileError} }}; Get-Content -LiteralPath {literal} -TotalCount 500 -ErrorAction Stop",
            _ => throw new InvalidOperationException(text.Text("Lab.Invalid"))
        };
    }

    /// <summary>Embeds the inspected script snapshot and literal parameters so later file edits cannot change execution.</summary>
    public string ScriptCommand(SkillDefinition skill, string action, string parameters)
    {
        try
        {
            return ScriptCommandCore(skill, action, parameters);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
    }

    /// <summary>Validates scalar parameters before composing the reviewed script as a literal snapshot.</summary>
    private string ScriptCommandCore(SkillDefinition skill, string action, string parameters)
    {
        if (!skill.Scripts.TryGetValue(action, out var source) || parameters.Length > 4096
            || source.Contains("$PSScriptRoot", StringComparison.OrdinalIgnoreCase)
            || source.Contains("$PSCommandPath", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        if (parameters != redactor.Redact(parameters))
        {
            throw new InvalidOperationException(text.Text("Memory.Secret"));
        }
        using var json = JsonDocument.Parse(parameters, new JsonDocumentOptions { MaxDepth = 2 });
        if (json.RootElement.ValueKind != JsonValueKind.Object || json.RootElement.EnumerateObject().Count() > 20)
        {
            throw new InvalidOperationException(text.Text("Lab.Invalid"));
        }
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bound = new List<string>();
        var bundledScript = skill.Origin == "bundled";
        var failure = text.Text(skill.Name == "concat-files" ? "Lab.ConcatInvalid" : "Lab.SkillActionInvalid");
        foreach (var property in json.RootElement.EnumerateObject())
        {
            if (!names.Add(property.Name) || property.Name.Length is 0 or > 60
                || !(char.IsAsciiLetter(property.Name[0]) || property.Name[0] == '_')
                || !property.Name.All(character => char.IsAsciiLetterOrDigit(character) || character == '_')
                || property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                || (bundledScript && property.Name.Equals("_ValidationError", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(text.Text("Lab.Invalid"));
            }
            if (CredentialFieldPolicy.IsSecret(property.Name))
            {
                throw new InvalidOperationException(text.Text("Memory.Secret"));
            }
            if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString()!.Any(character =>
                char.IsControl(character) && character is not ('\r' or '\n' or '\t')))
            {
                throw new InvalidOperationException(text.Text("Lab.Invalid"));
            }
            var value = property.Value.ValueKind switch
            {
                JsonValueKind.True => "$true",
                JsonValueKind.False => "$false",
                _ => ScriptArtifactService.Quote(property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString()! : property.Value.GetRawText())
            };
            bound.Add($"{ScriptArtifactService.Quote(property.Name)} = {value}");
        }
        if (bundledScript)
        {
            bound.Add("'_ValidationError' = " + ScriptArtifactService.Quote(failure));
        }
        var command = "$skillParameters = @{ " + string.Join("; ", bound) + " }; & {\n" + source + "\n} @skillParameters";
        if (bundledScript)
        {
            command = "try { " + command + "; } catch { throw " + ScriptArtifactService.Quote(failure) + " }";
        }
        if (command != redactor.Redact(command))
        {
            throw new InvalidOperationException(text.Text("Memory.Secret"));
        }
        return command;
    }
}

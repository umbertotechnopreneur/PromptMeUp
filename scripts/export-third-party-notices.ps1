# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
  Export resolved package attribution and full upstream notices into a release payload.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)]
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string]$Runtime
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$assets = Get-Content (Join-Path $root 'PromptMeUp/obj/project.assets.json') -Raw | ConvertFrom-Json -AsHashtable
$licenseRoot = Join-Path $OutputDirectory 'LICENSES'
if (Test-Path -LiteralPath $licenseRoot) { throw 'Use a fresh payload directory for notice export.' }
New-Item -ItemType Directory -Path $licenseRoot -Force | Out-Null
Copy-Item -Path (Join-Path $root 'LICENSES/*') -Destination $licenseRoot
$inventory = [System.Collections.Generic.List[object]]::new()

function Resolve-PackageDirectory {
    param([Parameter(Mandatory)][string]$RelativePath)
    foreach ($folder in $assets.packageFolders.Keys) {
        $candidate = Join-Path $folder $RelativePath
        if (Test-Path -LiteralPath $candidate -PathType Container) { return $candidate }
    }
    throw "Restored package is missing: $RelativePath"
}

function Add-PackageNotices {
    param([Parameter(Mandatory)][string]$Id, [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string]$Directory, [Parameter(Mandatory)][string[]]$Upstream)
    [xml]$spec = Get-Content (Join-Path $Directory "$($Id.ToLowerInvariant()).nuspec") -Raw
    $metadata = $spec.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
    $license = $metadata.SelectSingleNode('*[local-name()="license"]')
    if ($null -eq $license) { throw "Missing declared license: $Id/$Version" }
    if ($license.GetAttribute('type') -ne 'expression' -or $license.InnerText -notin @('MIT', 'Apache-2.0')) {
        throw "Review the new license before packaging: $Id/$Version"
    }
    $noticeFiles = [System.Collections.Generic.List[string]]::new()
    foreach ($name in $Upstream) {
        if (-not (Test-Path -LiteralPath (Join-Path $licenseRoot $name))) { throw "Missing upstream notice: $name" }
        $noticeFiles.Add("LICENSES/$name")
    }
    foreach ($file in Get-ChildItem -LiteralPath $Directory -Recurse -File) {
        if ($file.Name -notmatch '^(LICENSE|NOTICE|THIRD[-_]PARTY|COPYING)') { continue }
        $relative = [System.IO.Path]::GetRelativePath($Directory, $file.FullName)
        $destination = Join-Path $licenseRoot "$Id/$Version/$relative"
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
        $noticeFiles.Add("LICENSES/$Id/$Version/" + $relative.Replace('\', '/'))
    }
    $repository = $metadata.SelectSingleNode('*[local-name()="repository"]')
    $copyright = $metadata.SelectSingleNode('*[local-name()="copyright"]')
    $authors = $metadata.SelectSingleNode('*[local-name()="authors"]')
    $inventory.Add([ordered]@{
        id = $Id
        version = $Version
        license = $license.InnerText
        usage = if ($Id -eq 'Microsoft.NET.ILLink.Tasks') { 'build-only' } else { 'application-or-runtime' }
        authors = if ($authors) { $authors.InnerText } else { '' }
        copyright = if ($copyright) { $copyright.InnerText } else { '' }
        repository = if ($repository) { $repository.GetAttribute('url') } else { '' }
        sourceCommit = if ($repository) { $repository.GetAttribute('commit') } else { '' }
        notices = $noticeFiles.ToArray()
    })
}

foreach ($entry in $assets.libraries.GetEnumerator() | Sort-Object Key) {
    if ($entry.Value.type -ne 'package') { continue }
    $id, $version = $entry.Key.Split('/')
    $upstream = switch -Wildcard ($id) {
        'Microsoft.Data.Sqlite*' { @('dotnet-LICENSE.txt'); break }
        'Microsoft.Extensions.*' { @('dotnet-LICENSE.txt'); break }
        'Microsoft.NET.ILLink.Tasks' { @('dotnet-LICENSE.txt'); break }
        'Serilog' { @('serilog-LICENSE.txt'); break }
        'Serilog.Extensions.Logging' { @('serilog-extensions-logging-LICENSE.txt'); break }
        'Serilog.Sinks.File' { @('serilog-sinks-file-LICENSE.txt'); break }
        'Spectre.Console*' { @('spectre-console-LICENSE.txt'); break }
        'SQLitePCLRaw.*' { @('sqlitepclraw-LICENSE.txt', 'sqlitepclraw-NOTICE.txt'); break }
        'YamlDotNet' { @('yamldotnet-LICENSE.txt', 'libyaml-LICENSE.txt'); break }
        default { throw "Add upstream license attribution for new package: $id/$version" }
    }
    $directory = Resolve-PackageDirectory $entry.Value.path
    Add-PackageNotices -Id $id -Version $version -Directory $directory -Upstream $upstream
}
# Self-contained runtime packs are restore downloads, not entries in the NuGet library graph.
$runtimeId = "Microsoft.NETCore.App.Runtime.$Runtime"
$packs = @($assets.project.frameworks.Values.downloadDependencies | Where-Object { $_.name -eq $runtimeId })
if ($packs.Count -ne 1) { throw "Cannot determine the exact runtime pack: $runtimeId" }
$versions = $packs[0].version.Trim('[', ']').Split(',').Trim()
if ($versions.Count -ne 2 -or $versions[0] -ne $versions[1]) { throw 'Runtime pack version must be exact.' }
$runtimeVersion = $versions[0]
$runtimeDirectory = Resolve-PackageDirectory "$($runtimeId.ToLowerInvariant())/$runtimeVersion"
foreach ($required in @('LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT')) {
    if (-not (Test-Path -LiteralPath (Join-Path $runtimeDirectory $required))) { throw "Missing .NET runtime notice: $required" }
}
Add-PackageNotices -Id $runtimeId -Version $runtimeVersion -Directory $runtimeDirectory -Upstream @('dotnet-LICENSE.txt')
[ordered]@{ schemaVersion = 1; runtime = $Runtime; packages = $inventory.ToArray() } |
    ConvertTo-Json -Depth 8 | Set-Content (Join-Path $OutputDirectory 'THIRD_PARTY_INVENTORY.json') -Encoding utf8NoBOM
Write-Host "Exported attribution for $($inventory.Count) packages, including the .NET runtime."

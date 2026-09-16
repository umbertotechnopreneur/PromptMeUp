# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
  Package a prepared portable Windows payload as an unsigned, per-user EXE installer.
.DESCRIPTION
  Requires Inno Setup 6.3 or later in the 6.x series. Creates a fresh output folder
  below artifacts. Never installs the package, changes PATH, or runs the application.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$PublishDirectory,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$OutputDirectory,
    [Parameter(Mandatory)][ValidateSet('x64', 'arm64')][string]$Architecture,
    [string]$IsccPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Windows installer packaging requires Windows.' }

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = Join-Path $repositoryRoot 'artifacts'
$publishRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($PublishDirectory, $repositoryRoot))
$outputRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot))

function Assert-NoReparseAncestor {
    param([Parameter(Mandatory)][string]$Path)
    for ($candidate = $Path; -not [string]::IsNullOrEmpty($candidate); $candidate = [IO.Path]::GetDirectoryName($candidate)) {
        if (Test-Path -LiteralPath $candidate) {
            $item = Get-Item -LiteralPath $candidate -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Installer paths cannot pass through a link or junction: $candidate"
            }
        }
    }
}

function Get-PayloadFiles {
    param([Parameter(Mandatory)][string]$Directory)
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push($Directory)
    while ($pending.Count -gt 0) {
        foreach ($item in Get-ChildItem -LiteralPath $pending.Pop() -Force) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "The payload contains a link or junction: $($item.FullName)"
            }
            if ($item.PSIsContainer) {
                $pending.Push($item.FullName)
                continue
            }
            $relative = [IO.Path]::GetRelativePath($Directory, $item.FullName).Replace('\', '/')
            $allowed = $relative -match '^(?:hm\.exe|LICENSE|THIRD_PARTY_NOTICES\.md|THIRD_PARTY_INVENTORY\.json|BUILD_INFO\.txt|hm-path\.(?:ps1|sh))$' `
                -or $relative -match '^(?:prompt/[^/]+\.yaml|themes/[^/]+\.json|LICENSES/[A-Za-z0-9_./-]+)$'
            if (-not $allowed -or $item.Name -match '(?i)(?:^\.env(?:\.|$)|\.(?:db|sqlite|log|pfx|p12|pem|key)$)') {
                throw "Unexpected payload file '$relative'. Use a fresh single-file portable publish with exported notices."
            }
            if ($relative.Contains('"') -or $relative.Contains('{') -or $relative.Contains('}')) {
                throw "The payload filename cannot contain installer syntax: $relative"
            }
            $item
        }
    }
}

function Assert-PeArchitecture {
    param([Parameter(Mandatory)][string]$Path)
    $stream = [IO.File]::OpenRead($Path)
    $reader = [IO.BinaryReader]::new($stream)
    try {
        if ($stream.Length -lt 64 -or $reader.ReadUInt16() -ne 0x5A4D) { throw 'The payload is not a Windows executable.' }
        $stream.Position = 0x3C
        $offset = $reader.ReadInt32()
        if ($offset -lt 64 -or $offset -gt $stream.Length - 6) { throw 'The executable has an invalid PE header.' }
        $stream.Position = $offset
        if ($reader.ReadUInt32() -ne 0x00004550) { throw 'The executable has an invalid PE signature.' }
        $machine = $reader.ReadUInt16()
        $expected = if ($Architecture -eq 'x64') { 0x8664 } else { 0xAA64 }
        if ($machine -ne $expected) { throw "The executable does not match architecture '$Architecture'." }
    }
    finally { $reader.Dispose() }
}

if (-not (Test-Path -LiteralPath $publishRoot -PathType Container)) { throw 'PublishDirectory must contain a prepared portable payload.' }
if (-not $outputRoot.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be a new subdirectory below artifacts.'
}
if (Test-Path -LiteralPath $outputRoot) { throw 'OutputDirectory already exists. Choose a fresh directory.' }
if ($outputRoot.StartsWith($publishRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The output directory cannot be inside the payload.'
}
foreach ($path in @($publishRoot, $outputRoot, $repositoryRoot)) {
    Assert-NoReparseAncestor $path
    if ($path.IndexOfAny([char[]]'"{}') -ge 0) { throw 'Installer source and output paths cannot contain quotes or braces.' }
}
$files = @(Get-PayloadFiles $publishRoot)
if ($files.Count -eq 0) { throw 'The prepared payload is empty.' }
foreach ($name in @('hm.exe', 'LICENSE', 'THIRD_PARTY_NOTICES.md', 'THIRD_PARTY_INVENTORY.json', 'BUILD_INFO.txt',
        'prompt/chat-system.yaml', 'themes/cyan.json', 'LICENSES/README.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishRoot $name) -PathType Leaf)) { throw "The payload is missing '$name'." }
}
$executable = Join-Path $publishRoot 'hm.exe'
Assert-PeArchitecture $executable
[xml]$project = Get-Content -LiteralPath (Join-Path $repositoryRoot 'PromptMeUp/PromptMeUp.csproj') -Raw
$version = [string]$project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$' -or @($version.Split('.') | Where-Object { [long]$_ -gt 65535 }).Count -gt 0) {
    throw 'The project version must contain three numeric parts between 0 and 65535.'
}
$info = [Diagnostics.FileVersionInfo]::GetVersionInfo($executable)
$fileVersion = @($info.FileMajorPart, $info.FileMinorPart, $info.FileBuildPart, $info.FilePrivatePart) -join '.'
if ($fileVersion -cne "$version.0") { throw "The executable version '$fileVersion' does not match project version '$version'." }
$inventory = Get-Content -LiteralPath (Join-Path $publishRoot 'THIRD_PARTY_INVENTORY.json') -Raw | ConvertFrom-Json
if ($inventory.runtime -cne "win-$Architecture") { throw 'The exported license inventory does not match the requested runtime.' }
$buildInfo = Get-Content -LiteralPath (Join-Path $publishRoot 'BUILD_INFO.txt')
if ($buildInfo -cnotcontains "PromptMeUp $version" -or $buildInfo -cnotcontains "Runtime: win-$Architecture") {
    throw 'BUILD_INFO.txt does not match the requested version and architecture.'
}

if ([string]::IsNullOrWhiteSpace($IsccPath)) {
    $compilerCandidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6/ISCC.exe')
    )
    $command = Get-Command ISCC.exe -CommandType Application -ErrorAction SilentlyContinue
    if ($command) { $compilerCandidates += $command.Source }
    $IsccPath = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($IsccPath) -or -not (Test-Path -LiteralPath $IsccPath -PathType Leaf)) {
    throw 'Inno Setup 6 was not found. Install Inno Setup 6.7.1 or pass -IsccPath to its ISCC.exe.'
}
$template = Join-Path $repositoryRoot 'packaging/windows/PromptMeUp.iss'
$outputName = "PromptMeUp-$version-win-$Architecture-setup"
New-Item -ItemType Directory -Path $outputRoot | Out-Null
$compilerArguments = @(
    '/Qp',
    "/DAppVersion=$version",
    "/DAppArchitecture=$Architecture",
    "/DPublishDirectory=$publishRoot",
    "/DRepositoryDirectory=$repositoryRoot",
    "/O$outputRoot",
    "/F$outputName",
    $template
)
& $IsccPath @compilerArguments | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed with exit code $LASTEXITCODE." }
$installer = Join-Path $outputRoot "$outputName.exe"
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) { throw 'Inno Setup did not create the expected installer.' }
Write-Host "Created unsigned installer: $installer"

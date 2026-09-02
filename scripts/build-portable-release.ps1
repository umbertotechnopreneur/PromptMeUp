# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
  Build one self-contained portable archive with full redistribution notices.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string]$Runtime,
    [ValidateNotNullOrEmpty()][string]$OutputDirectory = 'artifacts/portable'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ($IsWindows -and -not $Runtime.StartsWith('win-')) {
    throw 'Build Unix archives on a Unix host to preserve executable permissions.'
}
[xml]$project = Get-Content (Join-Path $root 'PromptMeUp/PromptMeUp.csproj') -Raw
$version = [string]$project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'The project version must contain three numeric parts.' }
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory, $root)
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $root 'artifacts'))
if (-not $outputRoot.StartsWith($artifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Release output must stay in a subdirectory of artifacts.'
}
$payload = Join-Path $outputRoot "$version/$Runtime/payload"
$archiveRoot = Join-Path $outputRoot 'packages'
if (Test-Path -LiteralPath $payload) { throw 'Payload already exists. Choose a fresh -OutputDirectory below artifacts for another rehearsal.' }
New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null
$publishArgs = @('publish', (Join-Path $root 'PromptMeUp/PromptMeUp.csproj'),
    '--configuration', 'Release', '--runtime', $Runtime, '--self-contained', 'true',
    '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:DebugType=None', '-p:DebugSymbols=false', '-p:ContinuousIntegrationBuild=true', '--output', $payload)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw 'Portable publish failed.' }
& (Join-Path $PSScriptRoot 'export-third-party-notices.ps1') -OutputDirectory $payload -Runtime $Runtime
foreach ($file in @('LICENSE', 'THIRD_PARTY_NOTICES.md', 'THIRD_PARTY_INVENTORY.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $payload $file))) { throw "Payload is missing $file." }
}
if (@(Get-ChildItem (Join-Path $payload 'prompt') -Filter '*.yaml').Count -lt 4) { throw 'Runtime prompts are missing.' }
$revision = & git -C $root rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot record source revision.' }
@("PromptMeUp $version", "Runtime: $Runtime", "Source commit: $revision") |
    Set-Content (Join-Path $payload 'BUILD_INFO.txt') -Encoding utf8NoBOM
$hostOs = if ($IsWindows) { 'win' } elseif ($IsMacOS) { 'osx' } else { 'linux' }
$hostArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
if ($Runtime -eq "$hostOs-$hostArch") {
    $executable = Join-Path $payload $(if ($IsWindows) { 'hm.exe' } else { 'hm' })
    $oldData = $env:PROMPTMEUP_DATA_DIR
    $oldKey = $env:OPENAI_API_KEY
    $oldAdminKey = $env:OPENAI_ADMIN_KEY
    try {
        $env:PROMPTMEUP_DATA_DIR = Join-Path $outputRoot "smoke/$([guid]::NewGuid())"
        $env:OPENAI_API_KEY = $null
        $env:OPENAI_ADMIN_KEY = $null
        foreach ($command in @('--version', '--help', '--third-party')) {
            & $executable $command --no-animation --no-emoji
            if ($LASTEXITCODE -ne 0) { throw "Packaged CLI smoke test failed: $command" }
        }
    }
    finally {
        $env:PROMPTMEUP_DATA_DIR = $oldData
        $env:OPENAI_API_KEY = $oldKey
        $env:OPENAI_ADMIN_KEY = $oldAdminKey
    }
}
else { Write-Host "Cross-published $($Runtime): executable smoke test requires a matching host." }
$name = "PromptMeUp-$version-$Runtime"
if ($Runtime.StartsWith('win-')) {
    $archive = Join-Path $archiveRoot "$name.zip"
    if (Test-Path -LiteralPath $archive) { throw 'Archive already exists.' }
    Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $archive
}
else {
    $archive = Join-Path $archiveRoot "$name.tar.gz"
    if (Test-Path -LiteralPath $archive) { throw 'Archive already exists.' }
    & tar -czf $archive -C $payload .
    if ($LASTEXITCODE -ne 0) { throw 'Archive creation failed.' }
}
Write-Host "Created $([System.IO.Path]::GetFileName($archive))"

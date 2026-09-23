# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
  Build unsigned x64 and ARM64 Release MSIX packages for Microsoft Store submission.
.DESCRIPTION
  Publishes the app locally, exports redistribution notices, and packages both
  architectures with the reserved Store identity. Does not sign, install, upload,
  submit, or publish either package.
#>
[CmdletBinding()]
param(
    [ValidatePattern('^[1-9][0-9]*\.[0-9]+\.[0-9]+$')][string]$Version = '1.0.0',
    [string]$SdkBinDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Store MSIX packaging requires Windows.' }
foreach ($part in $Version.Split('.')) {
    $number = 0
    if (-not [int]::TryParse($part, [ref]$number) -or $number -gt 65535) {
        throw 'Each Store version component must be between 0 and 65535.'
    }
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$packageVersion = "$Version.0"
$outputRoot = Join-Path $repositoryRoot "artifacts/msix/store/$packageVersion"
$publishRoot = Join-Path $repositoryRoot "artifacts/msix/store-work/$packageVersion"
$architectures = @('x64', 'arm64')
foreach ($architecture in $architectures) {
    if (Test-Path -LiteralPath (Join-Path $outputRoot $architecture)) {
        throw "Store package output already exists for $architecture. Choose a new version or move the existing output first."
    }
    if (Test-Path -LiteralPath (Join-Path $publishRoot $architecture)) {
        throw "Store publish output already exists for $architecture. Choose a new version or move the existing output first."
    }
}

Write-Host "Building Store MSIX $packageVersion on $([Environment]::MachineName)" -ForegroundColor Cyan
$project = Join-Path $repositoryRoot 'PromptMeUp/PromptMeUp.csproj'
$exportNotices = Join-Path $PSScriptRoot 'export-third-party-notices.ps1'
$packageMsix = Join-Path $PSScriptRoot 'package-msix.ps1'
foreach ($architecture in $architectures) {
    $runtime = "win-$architecture"
    $publishDirectory = Join-Path $publishRoot $architecture
    $packageDirectory = Join-Path $outputRoot $architecture
    Write-Host "Publishing $runtime in Release" -ForegroundColor Cyan
    & dotnet publish $project --configuration Release --runtime $runtime --self-contained true `
        -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false `
        "-p:Version=$Version" "-p:FileVersion=$packageVersion" -p:TreatWarningsAsErrors=true `
        --output $publishDirectory
    if ($LASTEXITCODE -ne 0) { throw "Release publish failed for $runtime." }

    & $exportNotices -OutputDirectory $publishDirectory -Runtime $runtime
    $packageArguments = @{
        PublishDirectory = $publishDirectory
        OutputDirectory = $packageDirectory
        Architecture = $architecture
        Channel = 'Store'
        Version = $packageVersion
        Unsigned = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($SdkBinDirectory)) {
        $packageArguments.SdkBinDirectory = $SdkBinDirectory
    }
    & $packageMsix @packageArguments

    $metadata = Get-Content -LiteralPath (Join-Path $packageDirectory 'package.json') -Raw | ConvertFrom-Json
    if ($metadata.packageName -cne 'UmbertoGiacobbiDotBiz.PromptMeUp' -or
        $metadata.publisher -cne 'CN=82BCDD1C-1D59-48A0-9BDF-6352BE319510' -or
        $metadata.version -cne $packageVersion -or
        $metadata.architecture -cne $architecture -or
        $metadata.channel -cne 'Store' -or $metadata.signed -ne $false -or
        $metadata.minimumWindowsVersion -cne '10.0.22000.0') {
        throw "Store package metadata is invalid for $architecture."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $packageDirectory $metadata.file) -PathType Leaf)) {
        throw "Store MSIX is missing for $architecture."
    }
}
Write-Host "Both unsigned Store MSIX packages are ready in $outputRoot" -ForegroundColor Cyan

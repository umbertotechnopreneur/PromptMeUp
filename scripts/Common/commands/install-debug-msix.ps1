# SPDX-License-Identifier: MIT
# VBWR B
#
# Project: PromptMeUp
# Repository: https://github.com/umbertotechnopreneur/PromptMeUp
# Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
#
# VibeWare: Human intent, AI execution, and plenty of tokens
# Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
#
# Modified with AI: OpenAI Codex; added this header on 2026-10-10.
# Human guidance: Umberto Giacobbi; requested VibeWare branding.
#
# Copyright (c) 2026 Umberto Giacobbi
# License: MIT - see LICENSE
#
# VBWR E

#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+\.\d+$')][string]$Version,
    [ValidateSet('x64', 'arm64')][string]$Architecture = 'x64',
    [ValidatePattern('^[A-Fa-f0-9]{40}$')][string]$CertificateThumbprint,
    [string]$SdkBinDirectory,
    [uri]$TimestampServer
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Debug MSIX installation requires Windows.' }

$components = @($Version.Split('.') | ForEach-Object { [int]$_ })
if (@($components | Where-Object { $_ -gt 65535 }).Count -gt 0 -or @($components | Where-Object { $_ -ne 0 }).Count -eq 0) {
    throw 'Choose a nonzero four-part MSIX version with each component at most 65535.'
}
$packageName = 'UmbertoGiacobbiDotBiz.PromptMeUp'
$publisher = 'CN=82BCDD1C-1D59-48A0-9BDF-6352BE319510'
$installed = @(Get-AppxPackage -Name $packageName)
if ($installed.Count -gt 1) { throw 'More than one current-user PromptMeUp package was found.' }
if ($installed.Count -eq 1 -and [version]$installed[0].Version -ge [version]$Version) {
    throw "The installed version is $($installed[0].Version). Choose a higher Debug MSIX version."
}

if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
    $certificates = @(Get-ChildItem Cert:\CurrentUser\My | Where-Object {
        $_.Subject -ceq $publisher -and $_.HasPrivateKey -and $_.Verify() -and
        @($_.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq '1.3.6.1.5.5.7.3.3' }).Count -gt 0
    })
    if ($certificates.Count -ne 1) {
        throw 'Expected one trusted current-user code-signing certificate for PromptMeUp; supply -CertificateThumbprint if there are several.'
    }
    $certificate = $certificates[0]
}
else {
    $certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint" -ErrorAction Stop
}
if ($certificate.Subject -cne $publisher -or -not $certificate.HasPrivateKey -or -not $certificate.Verify() -or
    @($certificate.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq '1.3.6.1.5.5.7.3.3' }).Count -eq 0) {
    throw 'The signing certificate must be trusted, usable for code signing, and match the PromptMeUp publisher.'
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$repositoryPrefix = $repositoryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $artifactsRoot.StartsWith($repositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to delete an artifacts path outside the repository: $artifactsRoot"
}
if (Test-Path -LiteralPath $artifactsRoot) {
    if (((Get-Item -LiteralPath $artifactsRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Refusing to delete a linked artifacts directory: $artifactsRoot"
    }
    Remove-Item -LiteralPath $artifactsRoot -Recurse -Force
}
$runtime = "win-$Architecture"
$invocationId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$publishDirectory = Join-Path $repositoryRoot "artifacts\publish\debug-msix\$Version\$Architecture\$invocationId"
$packageDirectory = Join-Path $repositoryRoot "artifacts\msix\debug\$Version\$Architecture\$invocationId"
$logDirectory = Join-Path $repositoryRoot 'artifacts\logs\debug-msix'
$packageLog = Join-Path $logDirectory "$Version-$Architecture-$invocationId.log"
$project = Join-Path $repositoryRoot 'PromptMeUp\PromptMeUp.csproj'
$publishStarted = $false

try {
    $publishStarted = $true
    & dotnet publish $project --configuration Debug --runtime $runtime --self-contained true `
        -p:PublishSingleFile=false --output $publishDirectory
    if ($LASTEXITCODE -ne 0) { throw 'PromptMeUp Debug publish failed.' }

    & (Join-Path $PSScriptRoot 'export-third-party-notices.ps1') -Runtime $runtime -OutputDirectory $publishDirectory
    [IO.Directory]::CreateDirectory($logDirectory) | Out-Null
    $packageArguments = @{
        PublishDirectory = $publishDirectory
        OutputDirectory = $packageDirectory
        Architecture = $Architecture
        Channel = 'Debug'
        Version = $Version
        CertificateThumbprint = $certificate.Thumbprint
    }
    if ($SdkBinDirectory) { $packageArguments.SdkBinDirectory = $SdkBinDirectory }
    if ($TimestampServer) { $packageArguments.TimestampServer = $TimestampServer }
    & (Join-Path $PSScriptRoot 'package-msix.ps1') @packageArguments *> $packageLog

    $packagePath = Join-Path $packageDirectory "PromptMeUp-$Version-$runtime.msix"
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        throw "Debug MSIX packaging failed. See $packageLog"
    }
    Add-AppxPackage -Path $packagePath -ForceUpdateFromAnyVersion -ForceApplicationShutdown -ErrorAction Stop

    $registered = Get-AppxPackage -Name $packageName
    if ($null -eq $registered -or [version]$registered.Version -ne [version]$Version -or
        $registered.Status -ne 'Ok' -or $registered.Architecture.ToString() -ine $Architecture) {
        throw "The installed PromptMeUp package did not report version $Version, $Architecture, and Ok status."
    }
    Write-Output "Installed PromptMeUp Debug MSIX $Version ($Architecture): $packagePath"
}
finally {
    if ($publishStarted) {
        try {
            & dotnet clean $project --configuration Debug --runtime $runtime --verbosity quiet
            if ($LASTEXITCODE -ne 0) { Write-Warning 'PromptMeUp Debug build cleanup failed.' }
        }
        catch { Write-Warning "PromptMeUp Debug build cleanup failed: $($_.Exception.Message)" }
    }
}

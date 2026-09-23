# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
  Single entry point for PromptMeUp repository operations.

.DESCRIPTION
  Opens an interactive menu when no parameters are supplied. Use -Command for
  automation and CI. Command implementations remain private under Common.
#>
[CmdletBinding()]
param(
    [string]$Command,
    [switch]$Help,
    [switch]$Verify,
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string]$Runtime,
    [string]$OutputDirectory,
    [string]$PublishDirectory,
    [ValidateSet('x64', 'arm64')]
    [string]$Architecture,
    [string]$IsccPath,
    [string]$Version,
    [ValidateSet('x64', 'arm64')]
    [string[]]$PortableArchitectures,
    [ValidateSet('x64')]
    [string[]]$MsiArchitectures,
    [string]$PackageIdentifier,
    [string]$ArtifactBaseUrl,
    [string]$Wix3BinDirectory,
    [switch]$SkipMsi,
    [switch]$SkipRestore,
    [switch]$SkipWingetValidation,
    [switch]$PlanOnly,
    [ValidatePattern('^[A-Fa-f0-9]{40}$')]
    [string]$CertificateThumbprint,
    [ValidateSet('Debug', 'Store')]
    [string]$Channel,
    [switch]$Unsigned,
    [string]$SdkBinDirectory,
    [uri]$TimestampServer,
    [ValidateSet('install', 'remove', 'status')]
    [string]$Action,
    [string]$ExecutablePath,
    [switch]$Yes
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$commandRoot = Join-Path $scriptRoot 'Common\commands'
$boundParameters = $PSBoundParameters

. (Join-Path $scriptRoot 'Common\menu-ui.ps1')

# Writes the consolidated command reference.
function Show-PromptMeUpHelp {
    @'
USAGE
  pwsh -NoProfile -File .\scripts\PromptMeUp.ps1
  pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command <command> [options]

COMMANDS
  menu                         Open the interactive launcher.
  preflight                    Check required repository tools and files.
  status                       Show the current Git working-tree status.
  format                       Apply dotnet formatting; add -Verify to check only.
  check-format                 Verify formatting without changing files.
  check-xml                    Check XML summaries on C# implementation methods.
  notices                      Export third-party notices for -Runtime.
  path                         Run hm --path with -Action install|remove|status.
  portable                     Build one portable archive.
  release-artifacts            Build or plan the legacy release artifact set.
  windows-installer            Package a prepared portable Windows payload.
  msix                         Package a prepared Windows publish directory as MSIX.
  store-msix                   Build unsigned x64 and ARM64 Store MSIX packages.
  help                         Show this reference.

ALIASES
  repo-status, check-xml-comments, export-third-party-notices,
  build-portable-release, build-release-artifacts,
  build-windows-installer, package-msix, build-store-msix

EXAMPLES
  pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command preflight
  pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command format -Verify
  pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command notices -Runtime win-x64 -OutputDirectory .\artifacts\notices
  pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command msix -PublishDirectory .\artifacts\publish -Architecture x64 -Channel Debug -CertificateThumbprint <thumbprint>
'@ | Write-Host
}

# Adds one named value to a private command parameter map when supplied.
function Add-PromptMeUpValueArgument {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Arguments,
        [Parameter(Mandatory)][string]$Name,
        $Value
    )

    if ($null -eq $Value) {
        return
    }
    if ($Value -is [string] -and [string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $Arguments[$Name] = $Value
}

# Adds one enabled switch to a private command parameter map.
function Add-PromptMeUpSwitchArgument {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Arguments,
        [Parameter(Mandatory)][string]$Name,
        [switch]$Enabled
    )

    if ($Enabled) {
        $Arguments[$Name] = $true
    }
}

# Invokes one private command implementation in its own script scope.
function Invoke-PromptMeUpInternalCommand {
    param(
        [Parameter(Mandatory)][string]$ScriptName,
        [Collections.IDictionary]$Arguments = @{}
    )

    $scriptPath = Join-Path $commandRoot $ScriptName
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw "Internal PromptMeUp command not found: $scriptPath"
    }

    & $scriptPath @Arguments | Out-Host
    return 0
}

# Requires a non-empty parameter before a direct command is dispatched.
function Assert-PromptMeUpParameter {
    param(
        [Parameter(Mandatory)][string]$Name,
        $Value
    )

    if ($null -eq $Value -or ($Value -is [string] -and [string]::IsNullOrWhiteSpace($Value))) {
        throw "-$Name is required for -Command $Command."
    }
}

# Creates the exact parameter map for a selected command.
function Get-PromptMeUpCommandArguments {
    param([Parameter(Mandatory)][string]$NormalizedCommand)

    $arguments = @{}
    switch ($NormalizedCommand) {
        'format' {
            Add-PromptMeUpSwitchArgument $arguments 'Verify' -Enabled:$Verify
        }
        'check-format' {
            $arguments['Verify'] = $true
        }
        'notices' {
            Assert-PromptMeUpParameter 'Runtime' $Runtime
            Assert-PromptMeUpParameter 'OutputDirectory' $OutputDirectory
            Add-PromptMeUpValueArgument $arguments 'Runtime' $Runtime
            Add-PromptMeUpValueArgument $arguments 'OutputDirectory' $OutputDirectory
        }
        'path' {
            Add-PromptMeUpValueArgument $arguments 'Action' $Action
            Add-PromptMeUpValueArgument $arguments 'ExecutablePath' $ExecutablePath
            Add-PromptMeUpSwitchArgument $arguments 'Yes' -Enabled:$Yes
        }
        'portable' {
            Assert-PromptMeUpParameter 'Runtime' $Runtime
            Add-PromptMeUpValueArgument $arguments 'Runtime' $Runtime
            Add-PromptMeUpValueArgument $arguments 'OutputDirectory' $OutputDirectory
        }
        'release-artifacts' {
            Add-PromptMeUpValueArgument $arguments 'Version' $Version
            Add-PromptMeUpValueArgument $arguments 'PortableArchitectures' $PortableArchitectures
            Add-PromptMeUpValueArgument $arguments 'MsiArchitectures' $MsiArchitectures
            Add-PromptMeUpValueArgument $arguments 'PackageIdentifier' $PackageIdentifier
            Add-PromptMeUpValueArgument $arguments 'ArtifactBaseUrl' $ArtifactBaseUrl
            Add-PromptMeUpValueArgument $arguments 'Wix3BinDirectory' $Wix3BinDirectory
            Add-PromptMeUpSwitchArgument $arguments 'SkipMsi' -Enabled:$SkipMsi
            Add-PromptMeUpSwitchArgument $arguments 'SkipRestore' -Enabled:$SkipRestore
            Add-PromptMeUpSwitchArgument $arguments 'SkipWingetValidation' -Enabled:$SkipWingetValidation
            Add-PromptMeUpSwitchArgument $arguments 'PlanOnly' -Enabled:$PlanOnly
        }
        'windows-installer' {
            Assert-PromptMeUpParameter 'PublishDirectory' $PublishDirectory
            Assert-PromptMeUpParameter 'OutputDirectory' $OutputDirectory
            Assert-PromptMeUpParameter 'Architecture' $Architecture
            Add-PromptMeUpValueArgument $arguments 'PublishDirectory' $PublishDirectory
            Add-PromptMeUpValueArgument $arguments 'OutputDirectory' $OutputDirectory
            Add-PromptMeUpValueArgument $arguments 'Architecture' $Architecture
            Add-PromptMeUpValueArgument $arguments 'IsccPath' $IsccPath
        }
        'msix' {
            Assert-PromptMeUpParameter 'PublishDirectory' $PublishDirectory
            Add-PromptMeUpValueArgument $arguments 'PublishDirectory' $PublishDirectory
            Add-PromptMeUpValueArgument $arguments 'OutputDirectory' $OutputDirectory
            Add-PromptMeUpValueArgument $arguments 'CertificateThumbprint' $CertificateThumbprint
            Add-PromptMeUpValueArgument $arguments 'Architecture' $Architecture
            Add-PromptMeUpValueArgument $arguments 'Channel' $Channel
            Add-PromptMeUpValueArgument $arguments 'Version' $Version
            Add-PromptMeUpSwitchArgument $arguments 'Unsigned' -Enabled:$Unsigned
            Add-PromptMeUpValueArgument $arguments 'SdkBinDirectory' $SdkBinDirectory
            Add-PromptMeUpValueArgument $arguments 'TimestampServer' $TimestampServer
        }
        'store-msix' {
            Add-PromptMeUpValueArgument $arguments 'Version' $Version
            Add-PromptMeUpValueArgument $arguments 'SdkBinDirectory' $SdkBinDirectory
        }
    }
    return $arguments
}

# Maps public command names and compatibility aliases to internal implementations.
function Resolve-PromptMeUpCommand {
    param([Parameter(Mandatory)][string]$Value)

    switch ($Value.Trim().ToLowerInvariant()) {
        'menu' { return [pscustomobject]@{ Name = 'menu'; Script = $null } }
        'help' { return [pscustomobject]@{ Name = 'help'; Script = $null } }
        'preflight' { return [pscustomobject]@{ Name = 'preflight'; Script = 'preflight.ps1' } }
        'status' { return [pscustomobject]@{ Name = 'status'; Script = 'repo-status.ps1' } }
        'repo-status' { return [pscustomobject]@{ Name = 'status'; Script = 'repo-status.ps1' } }
        'format' { return [pscustomobject]@{ Name = 'format'; Script = 'format.ps1' } }
        'check-format' { return [pscustomobject]@{ Name = 'check-format'; Script = 'format.ps1' } }
        'check-xml' { return [pscustomobject]@{ Name = 'check-xml'; Script = 'check-xml-comments.ps1' } }
        'check-xml-comments' { return [pscustomobject]@{ Name = 'check-xml'; Script = 'check-xml-comments.ps1' } }
        'notices' { return [pscustomobject]@{ Name = 'notices'; Script = 'export-third-party-notices.ps1' } }
        'export-third-party-notices' { return [pscustomobject]@{ Name = 'notices'; Script = 'export-third-party-notices.ps1' } }
        'path' { return [pscustomobject]@{ Name = 'path'; Script = 'hm-path.ps1' } }
        'portable' { return [pscustomobject]@{ Name = 'portable'; Script = 'build-portable-release.ps1' } }
        'build-portable-release' { return [pscustomobject]@{ Name = 'portable'; Script = 'build-portable-release.ps1' } }
        'release-artifacts' { return [pscustomobject]@{ Name = 'release-artifacts'; Script = 'build-release-artifacts.ps1' } }
        'build-release-artifacts' { return [pscustomobject]@{ Name = 'release-artifacts'; Script = 'build-release-artifacts.ps1' } }
        'windows-installer' { return [pscustomobject]@{ Name = 'windows-installer'; Script = 'build-windows-installer.ps1' } }
        'build-windows-installer' { return [pscustomobject]@{ Name = 'windows-installer'; Script = 'build-windows-installer.ps1' } }
        'msix' { return [pscustomobject]@{ Name = 'msix'; Script = 'package-msix.ps1' } }
        'package-msix' { return [pscustomobject]@{ Name = 'msix'; Script = 'package-msix.ps1' } }
        'store-msix' { return [pscustomobject]@{ Name = 'store-msix'; Script = 'build-store-msix.ps1' } }
        'build-store-msix' { return [pscustomobject]@{ Name = 'store-msix'; Script = 'build-store-msix.ps1' } }
        default { throw "Unknown PromptMeUp command '$Value'. Use -Command help." }
    }
}

# Reads a required value for an interactive command.
function Read-PromptMeUpRequiredValue {
    param([Parameter(Mandatory)][string]$Prompt)

    while ($true) {
        $value = Read-Host "  $Prompt"
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }
        Write-Host '  A value is required.' -ForegroundColor Red
    }
}

# Runs the interactive launcher until the user exits.
function Invoke-PromptMeUpMenu {
    Initialize-PromptMeUpMenu
    while ($true) {
        Write-PromptMeUpMenuHeader
        Write-PromptMeUpMenuItem 1 '🔎' 'Preflight' 'Check required tools and files'
        Write-PromptMeUpMenuItem 2 '🌿' 'Git status' 'Show branch and local changes'
        Write-PromptMeUpMenuItem 3 '✅' 'Verify formatting' 'Read-only dotnet format check'
        Write-PromptMeUpMenuItem 4 '🧹' 'Apply formatting' 'Apply supported dotnet fixes'
        Write-PromptMeUpMenuItem 5 '📝' 'Check XML comments' 'Validate C# method summaries'
        Write-PromptMeUpMenuItem 6 '🛤️' 'PATH helper' 'Inspect, install, or remove the hm entry'
        Write-PromptMeUpMenuItem 7 '📜' 'Export notices' 'Prepare third-party attribution files'
        Write-PromptMeUpMenuItem 8 '📦' 'Portable build' 'Build one existing portable target'
        Write-PromptMeUpMenuItem 9 '🪟' 'Windows installer' 'Package a prepared portable payload'
        Write-PromptMeUpMenuItem 10 '🧭' 'Release plan' 'Preview the release artifact workflow'
        Write-PromptMeUpMenuItem 11 '🔐' 'Package MSIX' 'Create a signed or unsigned MSIX'
        Write-PromptMeUpMenuItem 12 '🏪' 'Build Store MSIX' 'Build unsigned x64 and ARM64 packages'

        $choice = Read-PromptMeUpMenuChoice -Maximum 12
        if ($choice -eq 0) {
            return
        }

        $result = 0
        try {
            switch ($choice) {
            1 { $result = Invoke-PromptMeUpInternalCommand 'preflight.ps1' }
            2 { $result = Invoke-PromptMeUpInternalCommand 'repo-status.ps1' }
            3 { $result = Invoke-PromptMeUpInternalCommand 'format.ps1' @{ Verify = $true } }
            4 { $result = Invoke-PromptMeUpInternalCommand 'format.ps1' }
            5 { $result = Invoke-PromptMeUpInternalCommand 'check-xml-comments.ps1' }
            6 {
                $pathAction = Read-PromptMeUpRequiredValue 'Action [install|remove|status]'
                $pathArguments = @{ Action = $pathAction }
                if ($pathAction -in @('install', 'remove')) {
                    $confirm = Read-Host '  Pass --yes to hm? [y/N]'
                    if ($confirm -match '^(?i:y|yes)$') {
                        $pathArguments['Yes'] = $true
                    }
                }
                $result = Invoke-PromptMeUpInternalCommand 'hm-path.ps1' $pathArguments
            }
            7 {
                $noticeRuntime = Read-PromptMeUpRequiredValue 'Runtime [win-x64|win-arm64|linux-x64|linux-arm64|osx-x64|osx-arm64]'
                $noticeOutput = Read-PromptMeUpRequiredValue 'Output directory'
                $result = Invoke-PromptMeUpInternalCommand 'export-third-party-notices.ps1' @{ Runtime = $noticeRuntime; OutputDirectory = $noticeOutput }
            }
            8 {
                $portableRuntime = Read-PromptMeUpRequiredValue 'Runtime [win-x64|win-arm64|linux-x64|linux-arm64|osx-x64|osx-arm64]'
                $portableOutput = Read-Host '  Output directory [artifacts/portable]'
                $portableArguments = @{ Runtime = $portableRuntime }
                if (-not [string]::IsNullOrWhiteSpace($portableOutput)) {
                    $portableArguments['OutputDirectory'] = $portableOutput.Trim()
                }
                $result = Invoke-PromptMeUpInternalCommand 'build-portable-release.ps1' $portableArguments
            }
            9 {
                $installerPublish = Read-PromptMeUpRequiredValue 'Prepared publish directory'
                $installerOutput = Read-PromptMeUpRequiredValue 'Output directory below artifacts'
                $installerArchitecture = Read-PromptMeUpRequiredValue 'Architecture [x64|arm64]'
                $result = Invoke-PromptMeUpInternalCommand 'build-windows-installer.ps1' @{ PublishDirectory = $installerPublish; OutputDirectory = $installerOutput; Architecture = $installerArchitecture }
            }
            10 { $result = Invoke-PromptMeUpInternalCommand 'build-release-artifacts.ps1' @{ PlanOnly = $true } }
            11 {
                $msixPublish = Read-PromptMeUpRequiredValue 'Prepared Windows publish directory'
                $msixArchitecture = Read-PromptMeUpRequiredValue 'Architecture [x64|arm64]'
                $msixChannel = Read-PromptMeUpRequiredValue 'Channel [Debug|Store]'
                $thumbprint = Read-Host '  Certificate thumbprint, or leave blank for unsigned'
                $msixArguments = @{ PublishDirectory = $msixPublish; Architecture = $msixArchitecture; Channel = $msixChannel }
                if ([string]::IsNullOrWhiteSpace($thumbprint)) {
                    $msixArguments['Unsigned'] = $true
                }
                else {
                    $msixArguments['CertificateThumbprint'] = $thumbprint.Trim()
                }
                $result = Invoke-PromptMeUpInternalCommand 'package-msix.ps1' $msixArguments
            }
            12 {
                $storeVersion = Read-Host '  Three-part Store version [1.0.0]'
                $storeArguments = @{}
                if (-not [string]::IsNullOrWhiteSpace($storeVersion)) {
                    $storeArguments['Version'] = $storeVersion.Trim()
                }
                $result = Invoke-PromptMeUpInternalCommand 'build-store-msix.ps1' $storeArguments
            }
            }
        }
        catch {
            Write-Host "`n  $($_.Exception.Message)" -ForegroundColor Red
            $result = 1
        }

        if ($result -ne 0) {
            Write-Host "`n  Command failed with exit code $result." -ForegroundColor Red
        }
        Wait-PromptMeUpMenu
    }
}

Set-Location -LiteralPath $repositoryRoot
if ($Help) {
    Show-PromptMeUpHelp
    exit 0
}
if ([string]::IsNullOrWhiteSpace($Command)) {
    if ($boundParameters.Count -gt 0) {
        throw '-Command is required when command options are supplied.'
    }
    Invoke-PromptMeUpMenu
    exit 0
}

$resolvedCommand = Resolve-PromptMeUpCommand $Command
if ($resolvedCommand.Name -eq 'menu') {
    Invoke-PromptMeUpMenu
    exit 0
}
if ($resolvedCommand.Name -eq 'help') {
    Show-PromptMeUpHelp
    exit 0
}

$commandArguments = Get-PromptMeUpCommandArguments $resolvedCommand.Name
$exitCode = Invoke-PromptMeUpInternalCommand $resolvedCommand.Script $commandArguments
exit $exitCode

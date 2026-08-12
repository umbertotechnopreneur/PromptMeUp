<#
.SYNOPSIS
  Read-only preflight for the PromptMeUp repository.
#>
[CmdletBinding()]
param([switch]$Help)

if ($Help) { Get-Help $PSCommandPath -Detailed; exit 0 }
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

. (Join-Path $PSScriptRoot 'Common\boot-banner.ps1')
. (Join-Path $PSScriptRoot 'Common\footer-banner.ps1')
. (Join-Path $PSScriptRoot 'Common\preflight-checks.ps1')

$started = Get-Date
Show-Banner -Title 'PromptMeUp' -Subtitle 'Repository preflight (read-only)'

$ok = Invoke-CommonPreflight `
    -Title 'Required tooling and files' `
    -RequiredCommands @('pwsh', 'dotnet', 'git') `
    -RequiredFiles @(
        (Join-Path $root 'PromptMeUp.slnx'),
        (Join-Path $root 'PromptMeUp\PromptMeUp.csproj')
    )

Show-Footer -ScriptName 'preflight.ps1' -Status $(if ($ok) { 'COMPLETED' } else { 'FAILED' }) -StartTime $started -EndTime (Get-Date)
if (-not $ok) { exit 1 }

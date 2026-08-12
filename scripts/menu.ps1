<#
.SYNOPSIS
  Safe interactive menu for common PromptMeUp repository checks.
#>
[CmdletBinding()]
param([switch]$Help)

if ($Help) { Get-Help $PSCommandPath -Detailed; exit 0 }
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pwsh = (Get-Command pwsh -ErrorAction Stop).Source

Write-Host 'PromptMeUp' -ForegroundColor Cyan
Write-Host '[1] Preflight   [2] Git status   [0] Exit'

switch (Read-Host 'Select') {
    '1' { & $pwsh -NoProfile -File (Join-Path $root 'scripts\preflight.ps1') }
    '2' { & $pwsh -NoProfile -File (Join-Path $root 'scripts\repo-status.ps1') }
    default { Write-Host 'Nothing selected.' }
}

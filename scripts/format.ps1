<#
.SYNOPSIS
  Applies or verifies the repository's .NET formatting rules.

.DESCRIPTION
  The default mode applies all fixes supported by dotnet format. The Verify
  mode is read-only and fails when formatting changes would still be required.
#>
[CmdletBinding()]
param(
    [switch]$Verify
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'PromptMeUp.slnx'

if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) {
    throw "Solution not found: $solution"
}

if (-not (Get-Command -Name dotnet -ErrorAction SilentlyContinue)) {
    throw 'The dotnet command is required to format this repository.'
}

$arguments = @(
    'format'
    $solution
    '--no-restore'
    '--verbosity'
    'minimal'
)

if ($Verify) {
    $arguments += '--verify-no-changes'
    Write-Host 'Verifying .NET formatting without changing files.'
}
else {
    Write-Host 'Applying supported .NET formatting fixes.'
}

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($Verify) {
    Write-Host 'Formatting verification completed successfully.'
}
else {
    Write-Host 'Formatting fixes completed. Run this script with -Verify to check the result.'
}

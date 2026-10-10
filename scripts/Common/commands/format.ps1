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
# SPDX-License-Identifier: MIT
#
# VBWR E

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

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
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
    throw "dotnet format failed with exit code $LASTEXITCODE."
}

if ($Verify) {
    Write-Host 'Formatting verification completed successfully.'
}
else {
    Write-Host 'Formatting fixes completed. Run this script with -Verify to check the result.'
}

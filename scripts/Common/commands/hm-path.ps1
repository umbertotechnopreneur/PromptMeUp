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
  Previews, installs, removes, or inspects the portable PromptMeUp PATH entry.
#>
[CmdletBinding()]
param(
    [ValidateSet('install', 'remove', 'status')]
    [string]$Action = 'status',

    [string]$ExecutablePath,

    [switch]$Yes
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$candidates = @(
    $ExecutablePath,
    (Join-Path $PSScriptRoot 'hm.exe'),
    (Join-Path $PSScriptRoot 'hm'),
    (Join-Path $repositoryRoot 'scripts\hm.exe'),
    (Join-Path $repositoryRoot 'scripts\hm'),
    (Join-Path $repositoryRoot 'PromptMeUp\bin\Release\net10.0\hm.exe'),
    (Join-Path $repositoryRoot 'PromptMeUp\bin\Release\net10.0\hm')
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$executable = $candidates |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1
if (-not $executable) {
    $resolvedCommand = Get-Command hm -CommandType Application -ErrorAction SilentlyContinue
    $executable = $resolvedCommand.Source
}

if (-not $executable) {
    throw 'hm was not found. Publish or build PromptMeUp first, or pass -ExecutablePath.'
}

$arguments = @("--path=$Action")
if ($Yes) {
    $arguments += '--yes'
}

& $executable @arguments
if ($LASTEXITCODE -ne 0) {
    throw "hm --path failed with exit code $LASTEXITCODE."
}

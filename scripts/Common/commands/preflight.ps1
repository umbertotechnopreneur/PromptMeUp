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
  Read-only preflight for the PromptMeUp repository.
#>
[CmdletBinding()]
param([switch]$Help)

if ($Help) { Get-Help $PSCommandPath -Detailed; return }
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))

. (Join-Path $PSScriptRoot '..\boot-banner.ps1')
. (Join-Path $PSScriptRoot '..\footer-banner.ps1')
. (Join-Path $PSScriptRoot '..\preflight-checks.ps1')

$started = Get-Date
Show-Banner -Title 'PromptMeUp' -Subtitle 'Repository preflight (read-only)'

$ok = Invoke-CommonPreflight `
    -Title 'Required tooling and files' `
    -RequiredCommands @('pwsh', 'dotnet', 'git') `
    -RequiredFiles @(
        (Join-Path $root 'PromptMeUp.slnx'),
        (Join-Path $root 'PromptMeUp\PromptMeUp.csproj')
    )

Show-Footer -ScriptName 'PromptMeUp.ps1 -Command preflight' -Status $(if ($ok) { 'COMPLETED' } else { 'FAILED' }) -StartTime $started -EndTime (Get-Date)
if (-not $ok) { throw 'PromptMeUp repository preflight failed.' }

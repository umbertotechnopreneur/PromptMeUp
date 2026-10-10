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

function Show-Footer {
    [CmdletBinding()]
    param(
        [string]$ScriptName = 'Script',
        [string]$Status = 'COMPLETED',
        [datetime]$StartTime = (Get-Date),
        [datetime]$EndTime = (Get-Date)
    )

    $elapsed = $EndTime - $StartTime
    $line = ('=' * 76)
    Write-Host ''
    Write-Host $line -ForegroundColor DarkGray
    Write-Host ("[{0}] {1}" -f $Status, $ScriptName) -ForegroundColor Green
    Write-Host ("Started: {0}" -f $StartTime.ToString('yyyy-MM-dd HH:mm:ss')) -ForegroundColor Gray
    Write-Host ("Ended:   {0}" -f $EndTime.ToString('yyyy-MM-dd HH:mm:ss')) -ForegroundColor Gray
    Write-Host ("Elapsed: {0:mm\:ss}" -f $elapsed) -ForegroundColor Gray
    Write-Host $line -ForegroundColor DarkGray
}

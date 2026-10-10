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

function Invoke-CommonPreflight {
    [CmdletBinding()]
    param(
        [string]$Title = 'Preflight',
        [string[]]$RequiredCommands = @(),
        [string[]]$RequiredFiles = @(),
        [switch]$RequireClipboard,
        [scriptblock]$ValidationScript
    )

    $checks = [System.Collections.Generic.List[object]]::new()

    $checks.Add([pscustomobject]@{
        Check = 'PowerShell'
        Status = if ($PSVersionTable.PSVersion.Major -ge 7) { 'OK' } else { 'FAIL' }
        Details = ("Detected {0}" -f $PSVersionTable.PSVersion)
    })

    foreach ($commandName in $RequiredCommands) {
        $command = Get-Command -Name $commandName -ErrorAction SilentlyContinue | Select-Object -First 1
        $checks.Add([pscustomobject]@{
            Check = "Command: $commandName"
            Status = if ($command) { 'OK' } else { 'FAIL' }
            Details = if ($command) { $command.Source } else { 'Not found' }
        })
    }

    foreach ($filePath in $RequiredFiles) {
        $exists = Test-Path -LiteralPath $filePath -PathType Leaf
        $checks.Add([pscustomobject]@{
            Check = "File: $(Split-Path -Leaf $filePath)"
            Status = if ($exists) { 'OK' } else { 'FAIL' }
            Details = $filePath
        })
    }

    if ($RequireClipboard) {
        $clipboardCommand = Get-Command -Name Set-Clipboard -ErrorAction SilentlyContinue | Select-Object -First 1
        $checks.Add([pscustomobject]@{
            Check = 'Clipboard'
            Status = if ($clipboardCommand) { 'OK' } else { 'FAIL' }
            Details = if ($clipboardCommand) { $clipboardCommand.Source } else { 'Set-Clipboard not available' }
        })
    }

    if ($ValidationScript) {
        try {
            & $ValidationScript
            $checks.Add([pscustomobject]@{
                Check = 'Credential probe'
                Status = 'OK'
                Details = 'Credential decrypted successfully for the current Windows user.'
            })
        }
        catch {
            $checks.Add([pscustomobject]@{
                Check = 'Credential probe'
                Status = 'FAIL'
                Details = $_.Exception.Message
            })
        }
    }

    Write-Section -Title $Title
    $checks | Format-Table -AutoSize | Out-String | Write-Host

    return -not ($checks.Status -contains 'FAIL')
}

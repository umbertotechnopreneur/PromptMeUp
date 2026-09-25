# SPDX-License-Identifier: MIT

# Prepares UTF-8 output for the interactive repository launcher.
function Initialize-PromptMeUpMenu {
    try {
        [Console]::OutputEncoding = [Text.Encoding]::UTF8
        $global:OutputEncoding = [Text.Encoding]::UTF8
    }
    catch {
        # Continue with the host encoding when no console stream is available.
    }
}

# Writes the open-layout launcher heading without clearing terminal scrollback.
function Write-PromptMeUpMenuHeader {
    Write-Host ''
    Write-Host 'PromptMeUp repository tools' -ForegroundColor Cyan
    Write-Host ('-' * 58) -ForegroundColor DarkCyan
}

# Writes one numbered launcher action.
function Write-PromptMeUpMenuItem {
    param(
        [Parameter(Mandatory)][int]$Number,
        [Parameter(Mandatory)][string]$Icon,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$Description
    )

    Write-Host ('  [{0,2}]  ' -f $Number) -ForegroundColor DarkGray -NoNewline
    Write-Host "$Icon " -NoNewline
    Write-Host ('{0,-28}' -f $Label) -ForegroundColor Yellow -NoNewline
    Write-Host "  $Description" -ForegroundColor Gray
}

# Reads a bounded numeric choice, treating zero and Escape text as exit.
function Read-PromptMeUpMenuChoice {
    param([Parameter(Mandatory)][int]$Maximum)

    while ($true) {
        $value = Read-Host "`n  Select [1-$Maximum], or 0 / Esc to exit"
        if ($null -eq $value) {
            return 0
        }

        $trimmed = $value.Trim()
        if ($trimmed -in @('0', 'esc', 'exit', 'quit')) {
            return 0
        }

        $number = 0
        if ([int]::TryParse($trimmed, [ref]$number) -and $number -ge 1 -and $number -le $Maximum) {
            return $number
        }

        Write-Host "  Enter a number between 1 and $Maximum." -ForegroundColor Red
    }
}

# Pauses after an interactive action so its output remains readable.
function Wait-PromptMeUpMenu {
    Write-Host ''
    $null = Read-Host '  Press Enter to return to the menu'
}

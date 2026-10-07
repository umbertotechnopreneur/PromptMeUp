param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path $PSScriptRoot -Parent
$archivePath = Join-Path $repositoryRoot '.github/tasks/archive.md'
$changelogPath = Join-Path $repositoryRoot 'CHANGELOG.md'
$updates = [System.Collections.Generic.List[object]]::new()
$currentDate = $null

foreach ($line in [System.IO.File]::ReadAllLines($archivePath)) {
    if ($line -match '^## (\d{4}-\d{2}-\d{2})\s+[—-]\s+') {
        $currentDate = $Matches[1]
        continue
    }

    if ($line -match '^Public update:\s*(.*)$') {
        $description = $Matches[1].Trim()
        if (-not $currentDate -or -not $description) {
            throw 'Each Public update needs text beneath a dated archive heading.'
        }

        $updates.Add([pscustomobject]@{ Date = $currentDate; Description = $description })
    }
}

if ($updates.Count -eq 0) {
    throw 'No Public update lines were found in the task archive.'
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# What's new")
$lines.Add('')
$lines.Add('Selected user-facing changes to PromptMeUp. These are task completion dates, not release dates. See [GitHub Releases](https://github.com/umbertotechnopreneur/PromptMeUp/releases) for published versions.')
$lines.Add('')

foreach ($group in ($updates | Group-Object Date | Sort-Object Name -Descending)) {
    $lines.Add("## $($group.Name)")
    $lines.Add('')
    foreach ($update in $group.Group) {
        $lines.Add("- $($update.Description)")
    }
    $lines.Add('')
}

$expected = $lines -join "`n"
if ($Check) {
    if (-not [System.IO.File]::Exists($changelogPath)) {
        throw 'CHANGELOG.md is missing. Run pwsh -NoProfile -File ./scripts/Generate-Changelog.ps1 and commit it with this PR.'
    }

    $actual = [System.IO.File]::ReadAllText($changelogPath) -replace "`r`n?", "`n"
    if ($actual -cne $expected) {
        throw 'CHANGELOG.md is out of date. Run pwsh -NoProfile -File ./scripts/Generate-Changelog.ps1 and commit the result with this PR.'
    }

    Write-Host 'CHANGELOG.md matches the public task updates.'
    return
}

$encoding = [System.Text.UTF8Encoding]::new($true)
[System.IO.File]::WriteAllText($changelogPath, $expected.Replace("`n", "`r`n"), $encoding)
Write-Host "Updated $changelogPath"

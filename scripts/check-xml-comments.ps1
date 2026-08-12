<#
.SYNOPSIS
  Verifies the repository rule requiring a brief XML summary on every C# implementation method.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sourceRoots = @(
    (Join-Path $repositoryRoot 'PromptMeUp'),
    (Join-Path $repositoryRoot 'PromptMeUp.Tests')
)
$methodPattern = '^\s*(public|private|protected|internal)\s+(?:(?:static|async|sealed|partial|virtual|override|new)\s+)*(?!readonly\b)(?!(?:class|record|interface|enum)\b)(?:[\w<>,?\[\].]+\s+)?\w+\s*\('
$violations = [System.Collections.Generic.List[string]]::new()

foreach ($sourceRoot in $sourceRoots) {
    if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
        continue
    }

    $files = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' }
    foreach ($file in $files) {
        $lines = Get-Content -LiteralPath $file.FullName
        for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++) {
            if ($lines[$lineIndex] -notmatch $methodPattern -or
                $lines[$lineIndex] -match '^\s*(?:public|private|protected|internal)\s+(?:static\s+)?readonly\b') {
                continue
            }

            $commentIndex = $lineIndex - 1
            while ($commentIndex -ge 0 -and
                ([string]::IsNullOrWhiteSpace($lines[$commentIndex]) -or $lines[$commentIndex].TrimStart().StartsWith('['))) {
                $commentIndex--
            }

            if ($commentIndex -lt 0 -or -not $lines[$commentIndex].TrimStart().StartsWith('///')) {
                $relativePath = [System.IO.Path]::GetRelativePath($repositoryRoot, $file.FullName)
                $violations.Add("${relativePath}:$($lineIndex + 1)")
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Error ("Missing XML method summaries:`n - " + ($violations -join "`n - "))
    exit 1
}

Write-Host 'XML method comment check passed.' -ForegroundColor Green

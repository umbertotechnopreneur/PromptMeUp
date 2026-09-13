# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
    Extracts the legacy Lenna ANSI portrait as a compact embedded RGB resource.
.DESCRIPTION
    Reads the original Lenna.ps1 as text without executing it. Preserves its
    78-by-78 RGB pixels, including background-only cells and lower half-blocks.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourcePath,
    [string]$OutputPath = (Join-Path $PSScriptRoot 'lenna.rgb.gz')
)

$ErrorActionPreference = 'Stop'
$source = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $SourcePath).Path)
$literal = [regex]::Match($source, '(?s)printf "(?<art>.*?)";')
if (-not $literal.Success) { throw 'The expected Lenna ANSI literal was not found.' }
$rows = @($literal.Groups['art'].Value.TrimEnd("`r", "`n") -split '\r?\n')
$width = 78
$height = 78
if ($rows.Count -ne $height / 2) { throw 'Expected exactly 39 ANSI rows.' }
$pixels = [byte[]]::new($width * $height * 3)

for ($row = 0; $row -lt $rows.Count; $row++) {
    $column = 0
    $position = 0
    foreach ($token in [regex]::Matches($rows[$row], '\\e\[(?<codes>[0-9;]*)m(?<glyph>[▄ ]?)')) {
        if ($token.Index -ne $position) { throw 'Unsupported content in the ANSI image.' }
        $position += $token.Length
        $glyph = $token.Groups['glyph'].Value
        $codeText = $token.Groups['codes'].Value
        if ($glyph.Length -eq 0) {
            if ($codeText.Length -ne 0 -or $position -ne $rows[$row].Length) {
                throw 'Only a final empty SGR reset is supported.'
            }
            continue
        }
        if ($column -ge $width) { throw 'An ANSI row is wider than expected.' }
        $codes = @($codeText.Split(';') | ForEach-Object { [int]::Parse($_, [Globalization.CultureInfo]::InvariantCulture) })
        if ($glyph -eq '▄' -and $codes.Count -eq 10 -and $codes[0] -eq 38 -and $codes[1] -eq 2 -and $codes[5] -eq 48 -and $codes[6] -eq 2) {
            # A lower half-block paints its foreground below its background.
            $top = $codes[7..9]
            $bottom = $codes[2..4]
        }
        elseif ($glyph -eq ' ' -and $codes.Count -eq 5 -and $codes[0] -eq 48 -and $codes[1] -eq 2) {
            $top = $codes[2..4]
            $bottom = $top
        }
        else { throw 'Unsupported color or glyph in the ANSI image.' }
        for ($channel = 0; $channel -lt 3; $channel++) {
            $pixels[(($row * 2) * $width + $column) * 3 + $channel] = [byte]$top[$channel]
            $pixels[(($row * 2 + 1) * $width + $column) * 3 + $channel] = [byte]$bottom[$channel]
        }
        $column++
    }
    if ($position -ne $rows[$row].Length -or $column -ne $width) {
        throw 'An ANSI row is incomplete or has unexpected dimensions.'
    }
}

$compressed = [IO.MemoryStream]::new()
try {
    $gzip = [IO.Compression.GZipStream]::new($compressed, [IO.Compression.CompressionLevel]::SmallestSize, $true)
    try { $gzip.Write($pixels, 0, $pixels.Length) }
    finally { $gzip.Dispose() }
    [IO.File]::WriteAllBytes([IO.Path]::GetFullPath($OutputPath), $compressed.ToArray())
}
finally { $compressed.Dispose() }

Write-Output "Imported $width x $height RGB pixels ($($pixels.Length) decoded bytes)."

# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
  Rebuild the PromptMeUp Windows icon from simple vector geometry.
.DESCRIPTION
  Uses Windows System.Drawing without external packages, images, or fonts.
  Writes PNG-backed ICO frames at 16, 24, 32, 48, 64, 128, and 256 pixels.
  Frames at 128 pixels and above use green phosphor, glow, and subtle scanlines.
  Optional PNG previews must stay in the repository's ignored artifacts directory.
#>
[CmdletBinding()]
param([string]$PreviewDirectory)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Icon generation requires Windows System.Drawing.' }
Add-Type -AssemblyName System.Drawing

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$previewRoot = $null
if (-not [string]::IsNullOrWhiteSpace($PreviewDirectory)) {
    $previewRoot = [IO.Path]::GetFullPath($PreviewDirectory, $repositoryRoot)
    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
    if (-not $previewRoot.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'PNG previews must be placed below the ignored artifacts directory.'
    }
    for ($candidate = $previewRoot; -not [string]::IsNullOrEmpty($candidate); $candidate = [IO.Path]::GetDirectoryName($candidate)) {
        if ((Test-Path -LiteralPath $candidate) -and
            ((Get-Item -LiteralPath $candidate -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'Preview output cannot pass through a link or junction.'
        }
    }
    New-Item -ItemType Directory -Path $previewRoot -Force | Out-Null
}

function New-IconFrame {
    param([Parameter(Mandatory)][int]$Size)
    $canvas = [Drawing.Bitmap]::new($Size * 4, $Size * 4, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($canvas)
    $background = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(11, 18, 32))
    $pen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(103, 232, 249), [single]2.2)
    $matrixStyle = $Size -ge 128
    if ($matrixStyle) {
        $background.Color = [Drawing.Color]::FromArgb(2, 8, 5)
        $pen.Color = [Drawing.Color]::FromArgb(104, 255, 144)
    }
    $tile = [Drawing.Drawing2D.GraphicsPath]::new()
    $letters = [Drawing.Drawing2D.GraphicsPath]::new()
    $bitmap = [Drawing.Bitmap]::new($Size, $Size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $output = [Drawing.Graphics]::FromImage($bitmap)
    $stream = [IO.MemoryStream]::new()
    try {
        $graphics.Clear([Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.ScaleTransform([single]($Size / 8), [single]($Size / 8))
        $tile.AddArc([single]0, [single]0, [single]8, [single]8, [single]180, [single]90)
        $tile.AddArc([single]24, [single]0, [single]8, [single]8, [single]270, [single]90)
        $tile.AddArc([single]24, [single]24, [single]8, [single]8, [single]0, [single]90)
        $tile.AddArc([single]0, [single]24, [single]8, [single]8, [single]90, [single]90)
        $tile.CloseFigure()
        $graphics.FillPath($background, $tile)

        # Lowercase h and m use the same rounded terminal strokes at every resolution.
        $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
        $pen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
        $letters.AddLine([single]4, [single]9, [single]4, [single]22)
        $letters.StartFigure()
        $letters.AddBezier([single]4, [single]17, [single]4, [single]13, [single]10, [single]13, [single]10, [single]17)
        $letters.AddLine([single]10, [single]17, [single]10, [single]22)
        $letters.StartFigure()
        $letters.AddLine([single]14, [single]15, [single]14, [single]22)
        $letters.StartFigure()
        $letters.AddBezier([single]14, [single]17, [single]14, [single]14, [single]18, [single]14, [single]18, [single]17)
        $letters.AddLine([single]18, [single]17, [single]18, [single]22)
        $letters.StartFigure()
        $letters.AddBezier([single]18, [single]17, [single]18, [single]14, [single]22, [single]14, [single]22, [single]17)
        $letters.AddLine([single]22, [single]17, [single]22, [single]22)
        $letters.StartFigure()
        $letters.AddLine([single]26, [single]22, [single]29, [single]22)
        if ($matrixStyle) {
            $graphics.SetClip($tile)
            foreach ($layer in @(@{ Width = 6.6; Alpha = 6 }, @{ Width = 5.0; Alpha = 10 }, @{ Width = 3.8; Alpha = 18 }, @{ Width = 2.8; Alpha = 28 })) {
                $glow = [Drawing.Pen]$pen.Clone()
                try {
                    $glow.Color = [Drawing.Color]::FromArgb($layer.Alpha, 40, 255, 95)
                    $glow.Width = [single]$layer.Width
                    $graphics.DrawPath($glow, $letters)
                }
                finally { $glow.Dispose() }
            }
        }
        $graphics.DrawPath($pen, $letters)
        if ($matrixStyle) {
            # Fixed three-pixel spacing gives the larger frames a restrained CRT texture.
            $scanline = [Drawing.Pen]::new([Drawing.Color]::FromArgb(48, 0, 5, 1), [single](22.4 / $Size))
            try {
                for ($scanY = 0.0; $scanY -lt 32; $scanY += 96.0 / $Size) {
                    $graphics.DrawLine($scanline, [single]0, [single]$scanY, [single]32, [single]$scanY)
                }
            }
            finally { $scanline.Dispose() }
            $graphics.ResetClip()
        }

        $output.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $output.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $output.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $output.DrawImage($canvas, [Drawing.Rectangle]::new(0, 0, $Size, $Size))
        $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
        return ,$stream.ToArray()
    }
    finally {
        $stream.Dispose()
        $output.Dispose()
        $bitmap.Dispose()
        $letters.Dispose()
        $tile.Dispose()
        $pen.Dispose()
        $background.Dispose()
        $graphics.Dispose()
        $canvas.Dispose()
    }
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = [Collections.Generic.List[byte[]]]::new()
foreach ($size in $sizes) {
    $frame = New-IconFrame $size
    $frames.Add($frame)
    if ($null -ne $previewRoot) { [IO.File]::WriteAllBytes((Join-Path $previewRoot "PromptMeUp-$size.png"), $frame) }
}

$iconPath = Join-Path $PSScriptRoot 'PromptMeUp.ico'
$iconStream = [IO.MemoryStream]::new()
$writer = [IO.BinaryWriter]::new($iconStream)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + (16 * $sizes.Count)
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        # ICO stores 256-pixel dimensions as zero; every frame contains a complete PNG.
        $dimension = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $frames[$index].Length
    }
    foreach ($frame in $frames) { $writer.Write($frame) }
    $writer.Flush()
    [IO.File]::WriteAllBytes($iconPath, $iconStream.ToArray())
}
finally { $writer.Dispose() }
Write-Host "Created assets/PromptMeUp.ico with $($sizes.Count) resolutions."

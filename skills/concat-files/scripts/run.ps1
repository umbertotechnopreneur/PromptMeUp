# Adapted for PromptMeUp from the author's CLI-Intelligence concat-files skill.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InputFolder,
    [string]$OutputFolder = '',
    [ValidateRange(1000, 500000)][int]$GeneratedFileCharLimit = 200000,
    [string]$AllowedExtensions = '.cs,.md,.ps1,.ts,.js,.vue,.txt',
    [string]$IgnoredFolders = 'bin,obj,.git,.vs,node_modules,dist,concatenated-output',
    [string]$_ValidationError = 'The selected paths or files are invalid, linked, or exceed the concatenation limits.'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$inputRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($InputFolder))
if (-not [IO.Directory]::Exists($inputRoot) -or $inputRoot -eq [IO.Path]::GetPathRoot($inputRoot)) { throw $_ValidationError }
if ([string]::IsNullOrWhiteSpace($OutputFolder)) { $OutputFolder = Join-Path $inputRoot 'concatenated-output' }
$outputRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($OutputFolder))
$pathComparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
if ($outputRoot -eq [IO.Path]::GetPathRoot($outputRoot) -or [string]::Equals($inputRoot, $outputRoot, $pathComparison)) { throw $_ValidationError }
# Check every existing ancestor before reading or writing through the selected roots.
foreach ($root in @($inputRoot, $outputRoot)) {
    $ancestor = $root
    while ($ancestor) {
        if (Test-Path -LiteralPath $ancestor) {
            if ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw $_ValidationError }
        }
        $ancestor = [IO.Path]::GetDirectoryName($ancestor)
    }
}
$extensions = $AllowedExtensions.Split(',', [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim().ToLowerInvariant() }
$ignored = $IgnoredFolders.Split(',', [StringSplitOptions]::RemoveEmptyEntries)
$queue = [Collections.Generic.Queue[string]]::new()
$queue.Enqueue($inputRoot)
$groups = @{}
$entryCount = 0
$totalBytes = 0L
while ($queue.Count -gt 0) {
    foreach ($itemPath in [IO.Directory]::EnumerateFileSystemEntries($queue.Dequeue())) {
        $entryCount++
        if ($entryCount -gt 10000) { throw $_ValidationError }
        $item = Get-Item -LiteralPath $itemPath -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        if ($item.PSIsContainer) {
            if (-not [string]::Equals($item.FullName, $outputRoot, $pathComparison) -and $item.Name -notin $ignored) { $queue.Enqueue($item.FullName) }
            continue
        }
        if ($item.Extension.ToLowerInvariant() -notin $extensions -or $item.Name.StartsWith('.env') -or $item.Length -gt 1048576) { continue }
        $totalBytes += $item.Length
        if ($totalBytes -gt 8388608) { throw $_ValidationError }
        if (-not $groups.ContainsKey($item.Extension)) { $groups[$item.Extension] = [Collections.Generic.List[string]]::new() }
        $relative = [IO.Path]::GetRelativePath($inputRoot, $item.FullName)
        # Read only the inspected byte count; a growing file cannot turn one entry into an unbounded allocation.
        $stream = [IO.File]::OpenRead($item.FullName)
        try {
            $bytes = [byte[]]::new([int]$item.Length + 1)
            $read = 0
            while ($read -lt $bytes.Length) {
                $count = $stream.Read($bytes, $read, $bytes.Length - $read)
                if ($count -eq 0) { break }
                $read += $count
            }
            if ($read -gt $item.Length) { throw $_ValidationError }
            $content = [Text.UTF8Encoding]::new($false, $true).GetString($bytes, 0, $read).TrimStart([char]0xFEFF)
        }
        finally { $stream.Dispose() }
        $entry = "# File: $relative" + [Environment]::NewLine + $content + [Environment]::NewLine
        if ($entry.Length -gt $GeneratedFileCharLimit) { throw $_ValidationError }
        $groups[$item.Extension].Add($entry)
    }
}
[void][IO.Directory]::CreateDirectory($outputRoot)
$runId = [Guid]::NewGuid().ToString('N')
foreach ($extension in ($groups.Keys | Sort-Object)) {
    $buffer = [Text.StringBuilder]::new()
    $part = 0
    foreach ($entry in $groups[$extension]) {
        if ($buffer.Length -gt 0 -and $buffer.Length + $entry.Length -gt $GeneratedFileCharLimit) {
            $part++
            $destination = Join-Path $outputRoot ("bundle-$runId-$($extension.TrimStart('.'))-$part.txt")
            [IO.File]::WriteAllText($destination, $buffer.ToString(), [Text.UTF8Encoding]::new($false))
            Write-Output $destination
            [void]$buffer.Clear()
        }
        [void]$buffer.Append($entry)
    }
    if ($buffer.Length -gt 0) {
        $part++
        $destination = Join-Path $outputRoot ("bundle-$runId-$($extension.TrimStart('.'))-$part.txt")
        [IO.File]::WriteAllText($destination, $buffer.ToString(), [Text.UTF8Encoding]::new($false))
        Write-Output $destination
    }
}

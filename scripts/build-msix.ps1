# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
  Package a prepared Windows publish folder as a signed MSIX with the hm execution alias.
.DESCRIPTION
  Requires a self-contained, non-single-file publish folder with exported notices,
  Windows SDK packaging tools, and an existing trusted code-signing certificate in
  CurrentUser\My. Creates a new directory below artifacts; never installs the package,
  changes certificate trust or PATH, or launches the application.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$PublishDirectory,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$OutputDirectory,
    [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{40}$')][string]$CertificateThumbprint,
    [Parameter(Mandatory)][ValidateSet('x64', 'arm64')][string]$Architecture,
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')][string]$Version,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9.-]{2,49}$')]
    [string]$PackageName = 'UmbertoGiacobbi.PromptMeUp',
    [string]$SdkBinDirectory,
    [uri]$TimestampServer
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common/bundled-skills.ps1')
$bundledSkillFiles = @(Get-BundledSkillFiles)
if (-not $IsWindows) { throw 'MSIX packaging and certificate-store signing require Windows.' }
# This creates a local test package only. Distribution remains a GitHub Actions release concern.

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$publishRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($PublishDirectory, $repositoryRoot))
$outputRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot))

function Assert-NoReparseAncestor {
    param([Parameter(Mandatory)][string]$Path)
    for ($candidate = $Path; -not [string]::IsNullOrEmpty($candidate); $candidate = [IO.Path]::GetDirectoryName($candidate)) {
        if (Test-Path -LiteralPath $candidate) {
            $item = Get-Item -LiteralPath $candidate -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Package paths cannot pass through a link or junction: $candidate"
            }
        }
    }
}

function Get-PayloadFiles {
    param([Parameter(Mandatory)][string]$Directory)
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push($Directory)
    while ($pending.Count -gt 0) {
        foreach ($item in Get-ChildItem -LiteralPath $pending.Pop() -Force) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "The publish folder contains a link or junction: $($item.FullName)"
            }
            if ($item.PSIsContainer) {
                $pending.Push($item.FullName)
                continue
            }
            $relative = [IO.Path]::GetRelativePath($Directory, $item.FullName).Replace('\', '/')
            $allowed = $relative -match '^(?:[^/]+\.dll|(?:hm|createdump)\.exe|hm\.(?:deps|runtimeconfig)\.json|LICENSE|THIRD_PARTY_NOTICES\.md|THIRD_PARTY_INVENTORY\.json|BUILD_INFO\.txt|hm-path\.(?:ps1|sh))$' `
                -or $relative -match '^(?:prompt/[^/]+\.yaml|themes/[^/]+\.json|LICENSES/.+|[A-Za-z]{2,3}(?:-[A-Za-z0-9]+)*/[^/]+\.resources\.dll)$' `
                -or $bundledSkillFiles -ccontains $relative
            if (-not $allowed) { throw "Unexpected publish content '$relative'. Use a clean Release publish folder with exported notices." }
            if ($item.Name -match '(?i)(?:^\.env(?:\.|$)|\.(?:db|sqlite|log|pfx|p12|pem|key)$)') {
                throw "Local data or credential files cannot be packaged: $relative"
            }
            $item
        }
    }
}

function Assert-PeArchitecture {
    param([Parameter(Mandatory)][string]$Path)
    $stream = [IO.File]::OpenRead($Path)
    $reader = [IO.BinaryReader]::new($stream)
    try {
        if ($stream.Length -lt 64 -or $reader.ReadUInt16() -ne 0x5A4D) { throw "Invalid Windows executable: $Path" }
        $stream.Position = 0x3C
        $offset = $reader.ReadInt32()
        if ($offset -lt 64 -or $offset -gt $stream.Length - 6) { throw "Invalid PE header: $Path" }
        $stream.Position = $offset
        if ($reader.ReadUInt32() -ne 0x00004550) { throw "Invalid PE signature: $Path" }
        $machine = $reader.ReadUInt16()
        $expected = if ($Architecture -eq 'x64') { 0x8664 } else { 0xAA64 }
        if ($machine -ne $expected) { throw "Published executable '$Path' does not match architecture '$Architecture'." }
    }
    finally { $reader.Dispose() }
}

function Invoke-SdkCommand {
    param([Parameter(Mandatory)][string]$Executable, [Parameter(Mandatory)][string[]]$Arguments)
    & $Executable @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$([IO.Path]::GetFileName($Executable)) failed with exit code $LASTEXITCODE." }
}

function New-PackageLogos {
    param([Parameter(Mandatory)][string]$Directory)
    $iconPath = Join-Path $repositoryRoot 'assets/PromptMeUp.ico'
    if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) { throw 'The project icon is missing: assets/PromptMeUp.ico.' }
    Add-Type -AssemblyName System.Drawing
    $iconBytes = [IO.File]::ReadAllBytes($iconPath)
    if ($iconBytes.Length -lt 6 -or [BitConverter]::ToUInt16($iconBytes, 0) -ne 0 -or [BitConverter]::ToUInt16($iconBytes, 2) -ne 1) {
        throw 'The project icon has an invalid ICO header.'
    }
    $frameCount = [BitConverter]::ToUInt16($iconBytes, 4)
    $directoryEnd = 6 + (16 * $frameCount)
    if ($iconBytes.Length -lt $directoryEnd) { throw 'The project icon has an incomplete frame directory.' }
    $pngOffset = 0
    $pngLength = 0
    for ($index = 0; $index -lt $frameCount; $index++) {
        $entry = 6 + (16 * $index)
        # Read the zero-encoded 256px entry directly; native Icon selection can return a smaller frame.
        if ($iconBytes[$entry] -eq 0 -and $iconBytes[$entry + 1] -eq 0) {
            $pngLength = [BitConverter]::ToUInt32($iconBytes, $entry + 8)
            $pngOffset = [BitConverter]::ToUInt32($iconBytes, $entry + 12)
            break
        }
    }
    if ($pngLength -le 0 -or $pngOffset -lt $directoryEnd -or ([long]$pngOffset + $pngLength) -gt $iconBytes.Length) {
        throw 'The project icon must contain a complete 256-pixel frame.'
    }
    $sourceStream = [IO.MemoryStream]::new($iconBytes, [int]$pngOffset, [int]$pngLength, $false)
    try {
        $source = [Drawing.Image]::FromStream($sourceStream)
        try {
            if ($source.Width -ne 256 -or $source.Height -ne 256 -or $source.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) {
                throw 'The project icon must contain a 256-pixel PNG frame.'
            }
            New-Item -ItemType Directory -Path $Directory | Out-Null
            foreach ($size in @(44, 50, 150)) {
                $bitmap = [Drawing.Bitmap]::new($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
                $graphics = [Drawing.Graphics]::FromImage($bitmap)
                try {
                    $graphics.Clear([Drawing.Color]::Transparent)
                    $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
                    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                    $graphics.DrawImage($source, [Drawing.Rectangle]::new(0, 0, $size, $size))
                    $bitmap.Save((Join-Path $Directory "Logo$size.png"), [Drawing.Imaging.ImageFormat]::Png)
                }
                finally {
                    $graphics.Dispose()
                    $bitmap.Dispose()
                }
            }
        }
        finally { $source.Dispose() }
    }
    finally { $sourceStream.Dispose() }
}

if (-not (Test-Path -LiteralPath $publishRoot -PathType Container)) { throw 'PublishDirectory must be an existing prepared publish folder.' }
if (-not $outputRoot.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be a new subdirectory of the ignored artifacts directory in this repository.'
}
if (Test-Path -LiteralPath $outputRoot) { throw 'OutputDirectory already exists. Choose a fresh directory; existing output is never overwritten.' }
if ($outputRoot.StartsWith($publishRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The output directory cannot be inside the publish folder.'
}
Assert-NoReparseAncestor $publishRoot
Assert-NoReparseAncestor $outputRoot
$files = @(Get-PayloadFiles $publishRoot)
foreach ($name in @('hm.exe', 'hm.dll', 'hm.deps.json', 'hm.runtimeconfig.json', 'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll',
        'LICENSE', 'THIRD_PARTY_NOTICES.md', 'THIRD_PARTY_INVENTORY.json', 'prompt/chat-system.yaml', 'themes/cyan.json', 'LICENSES/README.md') + $bundledSkillFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishRoot $name) -PathType Leaf)) { throw "Prepared publish folder is missing '$name'." }
}
Assert-PeArchitecture (Join-Path $publishRoot 'hm.exe')
Assert-PeArchitecture (Join-Path $publishRoot 'coreclr.dll')
$inventory = Get-Content -LiteralPath (Join-Path $publishRoot 'THIRD_PARTY_INVENTORY.json') -Raw | ConvertFrom-Json
if ($inventory.runtime -ne "win-$Architecture") { throw 'The exported license inventory does not match the requested runtime.' }
$runtimeConfig = Get-Content -LiteralPath (Join-Path $publishRoot 'hm.runtimeconfig.json') -Raw | ConvertFrom-Json -AsHashtable
if (@($runtimeConfig.runtimeOptions.includedFrameworks | Where-Object { $_.name -eq 'Microsoft.NETCore.App' }).Count -ne 1) {
    throw 'Use a self-contained publish folder with Microsoft.NETCore.App included.'
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $info = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $publishRoot 'hm.exe'))
    $Version = @($info.FileMajorPart, $info.FileMinorPart, $info.FileBuildPart, $info.FilePrivatePart) -join '.'
}
if (@($Version.Split('.') | Where-Object { [long]$_ -gt 65535 }).Count -gt 0) { throw 'Every MSIX version component must be between 0 and 65535.' }

$certificatePath = "Cert:\CurrentUser\My\$CertificateThumbprint"
if (-not (Test-Path -LiteralPath $certificatePath)) { throw 'The requested certificate is not in CurrentUser\My.' }
$certificate = Get-Item -LiteralPath $certificatePath
if (-not $certificate.HasPrivateKey -or [DateTime]::Now -lt $certificate.NotBefore -or [DateTime]::Now -gt $certificate.NotAfter) {
    throw 'The signing certificate must have an accessible private key and be within its validity period.'
}
$codeSigning = @($certificate.Extensions | Where-Object { $_ -is [Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension] } |
    ForEach-Object { $_.EnhancedKeyUsages } | Where-Object { $_.Value -eq '1.3.6.1.5.5.7.3.3' })
if ($codeSigning.Count -eq 0) { throw 'The selected certificate must include the Code Signing enhanced key usage.' }
$publisher = [Security.SecurityElement]::Escape($certificate.Subject)
if ([string]::IsNullOrWhiteSpace($publisher)) { throw 'The signing certificate has no Publisher subject.' }
if ($null -ne $TimestampServer -and (-not $TimestampServer.IsAbsoluteUri -or $TimestampServer.Scheme -ne 'https' `
        -or $TimestampServer.UserInfo -or $TimestampServer.Query -or $TimestampServer.Fragment)) {
    throw 'TimestampServer must be an absolute HTTPS URL without credentials, a query, or a fragment.'
}

if ([string]::IsNullOrWhiteSpace($SdkBinDirectory)) {
    $sdkRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)) 'Windows Kits/10/bin'
    $sdkHost = if ([Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [Runtime.InteropServices.Architecture]::Arm64) { 'arm64' } else { 'x64' }
    $sdk = Get-ChildItem -LiteralPath $sdkRoot -Directory |
        Where-Object { $_.Name -match '^10\.0\.\d+\.0$' -and [version]$_.Name -ge [version]'10.0.19041.0' } |
        Sort-Object { [version]$_.Name } -Descending |
        Where-Object { (Test-Path -LiteralPath (Join-Path $_.FullName "$sdkHost/MakeAppx.exe")) -and
            (Test-Path -LiteralPath (Join-Path $_.FullName "$sdkHost/SignTool.exe")) } | Select-Object -First 1
    if ($null -eq $sdk) { throw 'Install a Windows 10/11 SDK or supply -SdkBinDirectory containing MakeAppx.exe and SignTool.exe.' }
    $SdkBinDirectory = Join-Path $sdk.FullName $sdkHost
}
$SdkBinDirectory = [IO.Path]::GetFullPath($SdkBinDirectory, $repositoryRoot)
$makeAppx = Join-Path $SdkBinDirectory 'MakeAppx.exe'
$signTool = Join-Path $SdkBinDirectory 'SignTool.exe'
foreach ($tool in @($makeAppx, $signTool)) {
    if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) { throw "Required Windows SDK tool is missing: $tool" }
}

$stage = Join-Path $outputRoot 'payload'
New-Item -ItemType Directory -Path $stage | Out-Null
foreach ($file in $files) {
    $target = Join-Path $stage ([IO.Path]::GetRelativePath($publishRoot, $file.FullName))
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
}
New-PackageLogos (Join-Path $stage 'Assets')
$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:uap5="http://schemas.microsoft.com/appx/manifest/uap/windows10/5"
  xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap uap5 uap10 rescap">
  <Identity Name="$PackageName" Publisher="$publisher" Version="$Version" ProcessorArchitecture="$Architecture" />
  <Properties>
    <DisplayName>PromptMeUp</DisplayName>
    <PublisherDisplayName>PromptMeUp</PublisherDisplayName>
    <Logo>Assets\Logo50.png</Logo>
    <Description>Help with your next terminal command.</Description>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>
  <Resources>
    <Resource Language="en-US" /><Resource Language="it-IT" /><Resource Language="fr-FR" />
    <Resource Language="de-DE" /><Resource Language="es-ES" /><Resource Language="vi-VN" />
  </Resources>
  <Applications>
    <Application Id="PromptMeUp" Executable="hm.exe" uap10:RuntimeBehavior="win32App"
      uap10:TrustLevel="mediumIL" uap10:Subsystem="console" uap10:SupportsMultipleInstances="true">
      <uap:VisualElements DisplayName="PromptMeUp" Description="Help with your next terminal command."
        BackgroundColor="transparent" Square44x44Logo="Assets\Logo44.png"
        Square150x150Logo="Assets\Logo150.png" AppListEntry="none" />
      <Extensions>
        <uap5:Extension Category="windows.appExecutionAlias">
          <uap5:AppExecutionAlias uap10:Subsystem="console">
            <uap5:ExecutionAlias Alias="hm.exe" />
          </uap5:AppExecutionAlias>
        </uap5:Extension>
      </Extensions>
    </Application>
  </Applications>
  <Capabilities><rescap:Capability Name="runFullTrust" /></Capabilities>
</Package>
"@
$manifestPath = Join-Path $stage 'AppxManifest.xml'
[IO.File]::WriteAllText($manifestPath, $manifest, [Text.UTF8Encoding]::new($false))
$packagePath = Join-Path $outputRoot "PromptMeUp-$Version-win-$Architecture.msix"
Write-Host 'Validating and packing the prepared application.' -ForegroundColor Cyan
Invoke-SdkCommand $makeAppx @('pack', '/d', $stage, '/p', $packagePath)
$signArguments = @('sign', '/fd', 'SHA256', '/s', 'My', '/sha1', $CertificateThumbprint)
if ($null -ne $TimestampServer) { $signArguments += @('/tr', $TimestampServer.AbsoluteUri, '/td', 'SHA256') }
$signArguments += $packagePath
Write-Host 'Signing with the selected current-user certificate and verifying the package.' -ForegroundColor Cyan
Invoke-SdkCommand $signTool $signArguments
Invoke-SdkCommand $signTool @('verify', '/pa', '/all', $packagePath)
$hash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
"$hash  $([IO.Path]::GetFileName($packagePath))" | Set-Content -LiteralPath (Join-Path $outputRoot 'SHA256SUMS.txt') -Encoding utf8NoBOM
[ordered]@{
    packageName = $PackageName
    version = $Version
    architecture = $Architecture
    executionAlias = 'hm.exe'
    minimumWindowsVersion = '10.0.19041.0'
    certificateThumbprint = $CertificateThumbprint.ToUpperInvariant()
    file = [IO.Path]::GetFileName($packagePath)
    sha256 = $hash
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputRoot 'package.json') -Encoding utf8NoBOM
Write-Host "MSIX ready: $packagePath" -ForegroundColor Cyan
[pscustomobject]@{ PackagePath = $packagePath; ManifestPath = $manifestPath; PackageName = $PackageName; Version = $Version; Sha256 = $hash }

[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [ValidateSet('x64', 'arm64')]
    [string[]]$PortableArchitectures = @('x64', 'arm64'),

    [ValidateSet('x64')]
    [string[]]$MsiArchitectures = @('x64'),

    [ValidatePattern('^[A-Za-z0-9]+(?:\.[A-Za-z0-9]+)+$')]
    [string]$PackageIdentifier = 'UmbertoGiacobbi.PromptMeUp',

    [ValidateNotNullOrEmpty()]
    [string]$ArtifactBaseUrl = 'http://127.0.0.1:8765',

    [string]$Wix3BinDirectory,

    [switch]$SkipMsi,

    [switch]$SkipRestore,

    [switch]$SkipWingetValidation,

    [switch]$PlanOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = Join-Path $repositoryRoot 'PromptMeUp.slnx'
$projectPath = Join-Path $repositoryRoot 'PromptMeUp\PromptMeUp.csproj'
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))

function Write-Step {
    param([Parameter(Mandatory)][string]$Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Format-CommandArgument {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    return '"' + $Value.Replace('"', '\"', [System.StringComparison]::Ordinal) + '"'
}

function Invoke-ExternalCommand {
    param(
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    $preview = @($Executable) + ($Arguments | ForEach-Object { Format-CommandArgument $_ })
    Write-Host ('> ' + ($preview -join ' ')) -ForegroundColor DarkGray
    & $Executable @Arguments 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Executable"
    }
}

function Resolve-Executable {
    param([Parameter(Mandatory)][string]$Name)

    $command = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        throw "Required executable '$Name' was not found."
    }

    return $command.Source
}

function Resolve-Wix3Tools {
    if (-not [string]::IsNullOrWhiteSpace($Wix3BinDirectory)) {
        $candidate = [System.IO.Path]::GetFullPath($Wix3BinDirectory)
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:WIX)) {
        $candidate = [System.IO.Path]::GetFullPath((Join-Path $env:WIX 'bin'))
    }
    else {
        $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
        $candidate = [System.IO.Path]::GetFullPath((Join-Path $programFilesX86 'WiX Toolset v3.14\bin'))
    }

    $tools = @{
        Heat = Join-Path $candidate 'heat.exe'
        Candle = Join-Path $candidate 'candle.exe'
        Light = Join-Path $candidate 'light.exe'
    }
    foreach ($tool in $tools.GetEnumerator()) {
        if (-not (Test-Path -LiteralPath $tool.Value -PathType Leaf)) {
            throw "WiX Toolset 3.14 tool '$($tool.Key)' was not found at '$($tool.Value)'. Install WiX 3.14 or pass -Wix3BinDirectory."
        }
    }

    return $tools
}

function Get-ProjectVersion {
    [xml]$project = Get-Content -Raw -LiteralPath $projectPath
    $declaredVersion = [string]($project.Project.PropertyGroup.Version | Select-Object -First 1)
    if ($declaredVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "PromptMeUp.csproj must declare a three-part numeric Version before packaging. Found '$declaredVersion'."
    }

    return $declaredVersion
}

function Assert-MsiVersion {
    param([Parameter(Mandatory)][string]$Value)

    $parts = $Value.Split('.') | ForEach-Object { [int]$_ }
    if ($parts[0] -gt 255 -or $parts[1] -gt 255 -or $parts[2] -gt 65535) {
        throw "MSI version '$Value' exceeds Windows Installer limits (255.255.65535)."
    }
}

function Assert-ReleasePath {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $prefix = $artifactsRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify '$resolved' because release output must remain below '$artifactsRoot'."
    }
}

function Copy-PackagePayload {
    param(
        [Parameter(Mandatory)][string]$PublishDirectory,
        [Parameter(Mandatory)][string]$StageDirectory
    )

    New-Item -ItemType Directory -Path $StageDirectory -Force | Out-Null
    foreach ($name in @('hm.exe', 'LICENSE', 'THIRD_PARTY_NOTICES.md', 'THIRD_PARTY_INVENTORY.json', 'hm-path.ps1', 'hm-path.sh')) {
        $source = Join-Path $PublishDirectory $name
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            throw "Published payload is missing '$source'."
        }

        Copy-Item -LiteralPath $source -Destination (Join-Path $StageDirectory $name)
    }

    Copy-Item -LiteralPath (Join-Path $PublishDirectory 'LICENSES') -Destination (Join-Path $StageDirectory 'LICENSES') -Recurse
    $promptSource = Join-Path $PublishDirectory 'prompt'
    if (-not (Test-Path -LiteralPath $promptSource -PathType Container)) {
        throw "Published payload is missing '$promptSource'."
    }

    Copy-Item -LiteralPath $promptSource -Destination (Join-Path $StageDirectory 'prompt') -Recurse
    $promptFiles = @(Get-ChildItem -LiteralPath (Join-Path $StageDirectory 'prompt') -File -Filter '*.yaml')
    if ($promptFiles.Count -lt 1) {
        throw 'The staged package must contain at least one YAML prompt resource.'
    }
}

function New-DeterministicZip {
    param(
        [Parameter(Mandatory)][string]$SourceDirectory,
        [Parameter(Mandatory)][string]$DestinationPath
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $stream = [System.IO.File]::Open($DestinationPath, [System.IO.FileMode]::CreateNew)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new(
            $stream,
            [System.IO.Compression.ZipArchiveMode]::Create,
            $false)
        try {
            $normalizedTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
            $files = Get-ChildItem -LiteralPath $SourceDirectory -Recurse -File | Sort-Object FullName
            foreach ($file in $files) {
                $relativePath = [System.IO.Path]::GetRelativePath($SourceDirectory, $file.FullName).Replace('\', '/')
                $entry = $archive.CreateEntry($relativePath, [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = $normalizedTimestamp
                $entryStream = $entry.Open()
                try {
                    $fileStream = $file.OpenRead()
                    try {
                        $fileStream.CopyTo($entryStream)
                    }
                    finally {
                        $fileStream.Dispose()
                    }
                }
                finally {
                    $entryStream.Dispose()
                }
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function ConvertTo-StableGuid {
    param([Parameter(Mandatory)][string]$Value)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    $guidBytes = [byte[]]::new(16)
    [Array]::Copy($hash, $guidBytes, 16)
    $guidBytes[7] = ($guidBytes[7] -band 0x0f) -bor 0x50
    $guidBytes[8] = ($guidBytes[8] -band 0x3f) -bor 0x80
    return ([Guid]::new($guidBytes)).ToString('B').ToUpperInvariant()
}

function Escape-XmlAttribute {
    param([Parameter(Mandatory)][string]$Value)

    return [System.Security.SecurityElement]::Escape($Value)
}

function ConvertTo-PerUserHarvest {
    param(
        [Parameter(Mandatory)][string]$HarvestPath,
        [Parameter(Mandatory)][string]$Architecture
    )

    [xml]$document = Get-Content -Raw -LiteralPath $HarvestPath
    $namespace = 'http://schemas.microsoft.com/wix/2006/wi'
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespaceManager.AddNamespace('w', $namespace)

    $components = @($document.SelectNodes('//w:Component', $namespaceManager))
    foreach ($component in $components) {
        $component.SetAttribute(
            'Guid',
            (ConvertTo-StableGuid "$PackageIdentifier|$Architecture|payload|$($component.Id)"))

        foreach ($file in @($component.SelectNodes('w:File', $namespaceManager))) {
            $file.RemoveAttribute('KeyPath')
        }

        $registryValue = $document.CreateElement('RegistryValue', $namespace)
        $registryValue.SetAttribute('Id', "reg_$($component.Id)")
        $registryValue.SetAttribute('Root', 'HKCU')
        $registryValue.SetAttribute('Key', 'Software\Umberto Giacobbi\PromptMeUp\Components')
        $registryValue.SetAttribute('Name', "$Architecture-$($component.Id)")
        $registryValue.SetAttribute('Type', 'integer')
        $registryValue.SetAttribute('Value', '1')
        $registryValue.SetAttribute('KeyPath', 'yes')
        $component.AppendChild($registryValue) | Out-Null
    }

    $installDirectory = $document.SelectSingleNode('//w:DirectoryRef[@Id="INSTALLFOLDER"]', $namespaceManager)
    $userDirectories = @($installDirectory) + @($installDirectory.SelectNodes('.//w:Directory', $namespaceManager))
    foreach ($directory in $userDirectories) {
        $component = $directory.SelectSingleNode('w:Component', $namespaceManager)
        if ($null -eq $component) {
            throw "Harvested user directory '$($directory.Id)' has no direct component for uninstall cleanup."
        }

        $removeFolder = $document.CreateElement('RemoveFolder', $namespace)
        $removeFolder.SetAttribute('Id', "remove_$($directory.Id)")
        $removeFolder.SetAttribute('Directory', [string]$directory.Id)
        $removeFolder.SetAttribute('On', 'uninstall')
        $component.AppendChild($removeFolder) | Out-Null
    }

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $settings.Indent = $true
    $settings.NewLineChars = [Environment]::NewLine
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $writer = [System.Xml.XmlWriter]::Create($HarvestPath, $settings)
    try {
        $document.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

function New-MsiInstaller {
    param(
        [Parameter(Mandatory)][string]$Architecture,
        [Parameter(Mandatory)][string]$StageDirectory,
        [Parameter(Mandatory)][string]$WorkRoot,
        [Parameter(Mandatory)][string]$OutputDirectory,
        [Parameter(Mandatory)][hashtable]$WixTools
    )

    if ($Architecture -ne 'x64') {
        throw "WiX Toolset 3.14 cannot build native '$Architecture' MSI packages."
    }

    $workDirectory = Join-Path $WorkRoot "wix-$Architecture"
    New-Item -ItemType Directory -Path $workDirectory -Force | Out-Null
    $harvestPath = Join-Path $workDirectory 'Payload.wxs'
    $productPath = Join-Path $workDirectory 'Product.wxs'
    $payloadObject = Join-Path $workDirectory 'Payload.wixobj'
    $productObject = Join-Path $workDirectory 'Product.wixobj'
    $msiName = "PromptMeUp-$Version-win-$Architecture.msi"
    $msiPath = Join-Path $OutputDirectory $msiName

    Invoke-ExternalCommand $WixTools.Heat @(
        'dir', $StageDirectory,
        '-nologo',
        '-ag',
        '-cg', 'ProductComponents',
        '-dr', 'INSTALLFOLDER',
        '-scom',
        '-sfrag',
        '-srd',
        '-sreg',
        '-var', 'var.PublishDir',
        '-out', $harvestPath)
    ConvertTo-PerUserHarvest -HarvestPath $harvestPath -Architecture $Architecture

    $productCode = ConvertTo-StableGuid "$PackageIdentifier|$Architecture|$Version|product"
    $upgradeCode = ConvertTo-StableGuid "$PackageIdentifier|$Architecture|upgrade"
    $pathComponentGuid = ConvertTo-StableGuid "$PackageIdentifier|$Architecture|path"
    $productXml = @"
<?xml version="1.0" encoding="utf-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="$productCode"
           Name="PromptMeUp"
           Language="1033"
           Version="$(Escape-XmlAttribute $Version)"
           Manufacturer="Umberto Giacobbi"
           UpgradeCode="$upgradeCode">
    <Package InstallerVersion="500"
             Compressed="yes"
             InstallScope="perUser"
             InstallPrivileges="limited"
             Description="Ask from the terminal and explicitly approve every command before it runs." />
    <MajorUpgrade DowngradeErrorMessage="A newer version of PromptMeUp is already installed."
                  Schedule="afterInstallInitialize" />
    <MediaTemplate EmbedCab="yes" CompressionLevel="high" />
    <Property Id="ARPHELPLINK" Value="https://github.com/umbertotechnopreneur/PromptMeUp/issues" />
    <Property Id="ARPURLINFOABOUT" Value="https://github.com/umbertotechnopreneur/PromptMeUp" />
    <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />

    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="LocalAppDataFolder">
        <Directory Id="ProgramsFolder" Name="Programs">
          <Directory Id="INSTALLFOLDER" Name="PromptMeUp" />
        </Directory>
      </Directory>
    </Directory>

    <DirectoryRef Id="INSTALLFOLDER">
      <Component Id="PathComponent" Guid="$pathComponentGuid" Win64="yes">
        <Environment Id="PathEnvironment"
                     Name="PATH"
                     Value="[INSTALLFOLDER]"
                     Action="set"
                     Part="last"
                     Permanent="no"
                     System="no" />
        <RegistryValue Root="HKCU"
                       Key="Software\Umberto Giacobbi\PromptMeUp"
                       Name="InstallPath"
                       Type="string"
                       Value="[INSTALLFOLDER]"
                       KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <Feature Id="ProductFeature" Title="PromptMeUp" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
      <ComponentRef Id="PathComponent" />
    </Feature>

    <UIRef Id="WixUI_InstallDir" />
  </Product>
</Wix>
"@
    [System.IO.File]::WriteAllText($productPath, $productXml, [System.Text.UTF8Encoding]::new($false))

    Invoke-ExternalCommand $WixTools.Candle @(
        '-nologo', '-wx', '-arch', 'x64', "-dPublishDir=$StageDirectory", '-out', $payloadObject, $harvestPath)
    Invoke-ExternalCommand $WixTools.Candle @(
        '-nologo', '-wx', '-arch', 'x64', '-out', $productObject, $productPath)
    Invoke-ExternalCommand $WixTools.Light @(
        '-nologo', '-wx', '-sice:ICE64', '-sice:ICE91', '-ext', 'WixUIExtension', '-cultures:en-us', '-spdb', '-out', $msiPath, $productObject, $payloadObject)

    return [pscustomobject]@{
        Architecture = $Architecture
        Path = $msiPath
        Name = $msiName
        Sha256 = (Get-FileHash -LiteralPath $msiPath -Algorithm SHA256).Hash
        ProductCode = $productCode
    }
}

function Quote-Yaml {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    return "'" + $Value.Replace("'", "''", [System.StringComparison]::Ordinal) + "'"
}

function New-WingetManifests {
    param(
        [Parameter(Mandatory)][object[]]$PortablePackages,
        [Parameter(Mandatory)][string]$ManifestDirectory
    )

    New-Item -ItemType Directory -Path $ManifestDirectory -Force | Out-Null
    $schemaVersion = '1.12.0'
    $versionManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.$schemaVersion.schema.json

PackageIdentifier: $(Quote-Yaml $PackageIdentifier)
PackageVersion: $(Quote-Yaml $Version)
DefaultLocale: en-US
ManifestType: version
ManifestVersion: $schemaVersion
"@
    $localeManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.$schemaVersion.schema.json

PackageIdentifier: $(Quote-Yaml $PackageIdentifier)
PackageVersion: $(Quote-Yaml $Version)
PackageLocale: en-US
Publisher: Umberto Giacobbi
PublisherUrl: https://github.com/umbertotechnopreneur
PublisherSupportUrl: https://github.com/umbertotechnopreneur/PromptMeUp/issues
Author: Umberto Giacobbi
PackageName: PromptMeUp
PackageUrl: https://github.com/umbertotechnopreneur/PromptMeUp
License: MIT
LicenseUrl: https://github.com/umbertotechnopreneur/PromptMeUp/blob/main/LICENSE
ShortDescription: Ask from the terminal, understand the answer, and explicitly approve every command before it runs.
Description: PromptMeUp exposes the hm command for concise AI help, short conversations, and carefully reviewed command execution.
Moniker: hm
Tags:
- ai
- cli
- openai
- powershell
- terminal
ManifestType: defaultLocale
ManifestVersion: $schemaVersion
"@

    $installerLines = [System.Collections.Generic.List[string]]::new()
    $installerLines.Add("# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.$schemaVersion.schema.json")
    $installerLines.Add('')
    $installerLines.Add("PackageIdentifier: $(Quote-Yaml $PackageIdentifier)")
    $installerLines.Add("PackageVersion: $(Quote-Yaml $Version)")
    $installerLines.Add('InstallerType: zip')
    $installerLines.Add('NestedInstallerType: portable')
    $installerLines.Add('UpgradeBehavior: install')
    $installerLines.Add('Commands:')
    $installerLines.Add('- hm')
    $installerLines.Add('Dependencies:')
    $installerLines.Add('  PackageDependencies:')
    $installerLines.Add('  - PackageIdentifier: Microsoft.PowerShell')
    $installerLines.Add('    MinimumVersion: 7.0.0')
    $installerLines.Add('ArchiveBinariesDependOnPath: true')
    $installerLines.Add('Installers:')
    foreach ($package in ($PortablePackages | Sort-Object Architecture)) {
        $installerLines.Add("- Architecture: $($package.Architecture)")
        $installerLines.Add('  NestedInstallerFiles:')
        $installerLines.Add('  - RelativeFilePath: hm.exe')
        $installerLines.Add('    PortableCommandAlias: hm')
        $installerLines.Add("  InstallerUrl: $(Quote-Yaml ($ArtifactBaseUrl.TrimEnd('/') + '/' + $package.Name))")
        $installerLines.Add("  InstallerSha256: $($package.Sha256)")
    }
    $installerLines.Add('ManifestType: installer')
    $installerLines.Add("ManifestVersion: $schemaVersion")

    $utf8 = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText(
        (Join-Path $ManifestDirectory "$PackageIdentifier.yaml"),
        $versionManifest.Trim() + [Environment]::NewLine,
        $utf8)
    [System.IO.File]::WriteAllText(
        (Join-Path $ManifestDirectory "$PackageIdentifier.locale.en-US.yaml"),
        $localeManifest.Trim() + [Environment]::NewLine,
        $utf8)
    [System.IO.File]::WriteAllText(
        (Join-Path $ManifestDirectory "$PackageIdentifier.installer.yaml"),
        ($installerLines -join [Environment]::NewLine) + [Environment]::NewLine,
        $utf8)
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-ProjectVersion
}
Assert-MsiVersion $Version

if ($PortableArchitectures.Count -ne @($PortableArchitectures | Select-Object -Unique).Count) {
    throw 'PortableArchitectures cannot contain duplicate values.'
}
if (-not $SkipMsi) {
    foreach ($msiArchitecture in $MsiArchitectures) {
        if ($msiArchitecture -notin $PortableArchitectures) {
            throw "MSI architecture '$msiArchitecture' must also be included in PortableArchitectures."
        }
    }
}

$baseUri = $null
$isValidBaseUri = [Uri]::TryCreate($ArtifactBaseUrl, [UriKind]::Absolute, [ref]$baseUri)
if (-not $isValidBaseUri -or $baseUri.Scheme -notin @('http', 'https')) {
    throw "ArtifactBaseUrl must be an absolute HTTP or HTTPS URL. Found '$ArtifactBaseUrl'."
}
$baseUriContainsSensitiveParts = -not [string]::IsNullOrEmpty($baseUri.UserInfo) `
    -or -not [string]::IsNullOrEmpty($baseUri.Query) `
    -or -not [string]::IsNullOrEmpty($baseUri.Fragment)
if ($baseUriContainsSensitiveParts) {
    throw 'ArtifactBaseUrl cannot contain credentials, a query string, or a fragment.'
}

$releaseRoot = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "release\$Version"))
Assert-ReleasePath $releaseRoot
$packageDirectory = Join-Path $releaseRoot 'packages'
$publishRoot = Join-Path $releaseRoot 'publish'
$stageRoot = Join-Path $releaseRoot 'stage'
$installerWorkRoot = Join-Path $releaseRoot 'installer'
$smokeDataRoot = Join-Path $releaseRoot 'smoke-data'
$manifestDirectory = Join-Path $releaseRoot "winget\$PackageIdentifier\$Version"

Write-Step 'Release artifact plan'
Write-Host "Repository:            $repositoryRoot"
Write-Host "Version:               $Version"
Write-Host "Portable architectures: $($PortableArchitectures -join ', ')"
Write-Host "MSI architectures:      $(if ($SkipMsi) { 'skipped' } else { $MsiArchitectures -join ', ' })"
Write-Host "Artifact base URL:       $ArtifactBaseUrl"
Write-Host "Output:                  $releaseRoot"
if ($PlanOnly) {
    Write-Host "`nPlan only: no files were created and no external commands were executed." -ForegroundColor Yellow
    return
}

$dotnet = Resolve-Executable 'dotnet'
$winget = if ($SkipWingetValidation) { $null } else { Resolve-Executable 'winget' }
$wixTools = if ($SkipMsi) { $null } else { Resolve-Wix3Tools }

if (Test-Path -LiteralPath $releaseRoot) {
    Assert-ReleasePath $releaseRoot
    Remove-Item -LiteralPath $releaseRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $packageDirectory, $publishRoot, $stageRoot, $installerWorkRoot -Force | Out-Null

if (-not $SkipRestore) {
    Write-Step 'Restore solution'
    Invoke-ExternalCommand $dotnet @('restore', $solutionPath)
}

$portablePackages = [System.Collections.Generic.List[object]]::new()
$currentArchitecture = switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
    ([System.Runtime.InteropServices.Architecture]::X64) { 'x64' }
    ([System.Runtime.InteropServices.Architecture]::Arm64) { 'arm64' }
    default { $null }
}
foreach ($architecture in $PortableArchitectures) {
    $runtime = "win-$architecture"
    $publishDirectory = Join-Path $publishRoot $runtime
    $stageDirectory = Join-Path $stageRoot $runtime
    Write-Step "Publish $runtime"
    Invoke-ExternalCommand $dotnet @(
        'publish', $projectPath,
        '--configuration', 'Release',
        '--runtime', $runtime,
        '--self-contained', 'true',
        '--no-restore',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:DebugType=None',
        '-p:DebugSymbols=false',
        '-p:ContinuousIntegrationBuild=true',
        "-p:Version=$Version",
        '--output', $publishDirectory)
    & (Join-Path $PSScriptRoot 'export-third-party-notices.ps1') -OutputDirectory $publishDirectory -Runtime $runtime
    Copy-PackagePayload -PublishDirectory $publishDirectory -StageDirectory $stageDirectory
    if ($architecture -eq $currentArchitecture) {
        Write-Step "Smoke test staged $runtime executable"
        $previousDataDirectory = $env:PROMPTMEUP_DATA_DIR
        try {
            $env:PROMPTMEUP_DATA_DIR = Join-Path $smokeDataRoot $runtime
            Invoke-ExternalCommand (Join-Path $stageDirectory 'hm.exe') @('--version', '--no-animation', '--no-emoji')
            Invoke-ExternalCommand (Join-Path $stageDirectory 'hm.exe') @('-where', '--no-animation', '--no-emoji')
        }
        finally {
            $env:PROMPTMEUP_DATA_DIR = $previousDataDirectory
        }
    }

    $archiveName = "PromptMeUp-$Version-$runtime.zip"
    $archivePath = Join-Path $packageDirectory $archiveName
    Write-Step "Create $archiveName"
    New-DeterministicZip -SourceDirectory $stageDirectory -DestinationPath $archivePath
    $portablePackages.Add([pscustomobject]@{
        Architecture = $architecture
        Runtime = $runtime
        Path = $archivePath
        Name = $archiveName
        Sha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    })
}

$msiPackages = [System.Collections.Generic.List[object]]::new()
if (-not $SkipMsi) {
    foreach ($architecture in $MsiArchitectures) {
        Write-Step "Build traditional MSI for $architecture"
        $stageDirectory = Join-Path $stageRoot "win-$architecture"
        $msiPackages.Add((New-MsiInstaller `
            -Architecture $architecture `
            -StageDirectory $stageDirectory `
            -WorkRoot $installerWorkRoot `
            -OutputDirectory $packageDirectory `
            -WixTools $wixTools))
    }
}

Write-Step 'Generate WinGet manifests'
New-WingetManifests -PortablePackages $portablePackages.ToArray() -ManifestDirectory $manifestDirectory
if (-not $SkipWingetValidation) {
    Invoke-ExternalCommand $winget @('validate', $manifestDirectory)
}

$allPackages = @($portablePackages.ToArray()) + @($msiPackages.ToArray())
$checksums = $allPackages | Sort-Object Name |
    ForEach-Object { "$($_.Sha256)  $($_.Name)" }
[System.IO.File]::WriteAllLines(
    (Join-Path $packageDirectory 'SHA256SUMS.txt'),
    $checksums,
    [System.Text.UTF8Encoding]::new($false))

$summary = [ordered]@{
    packageIdentifier = $PackageIdentifier
    version = $Version
    artifactBaseUrl = $ArtifactBaseUrl
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    portable = @($portablePackages | ForEach-Object {
        [ordered]@{ architecture = $_.Architecture; file = $_.Name; sha256 = $_.Sha256 }
    })
    msi = @($msiPackages | ForEach-Object {
        [ordered]@{ architecture = $_.Architecture; file = $_.Name; sha256 = $_.Sha256; productCode = $_.ProductCode }
    })
    wingetManifestDirectory = [System.IO.Path]::GetRelativePath($releaseRoot, $manifestDirectory)
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $releaseRoot 'release.json') -Encoding utf8NoBOM

Write-Step 'Remove successful-build intermediates'
foreach ($intermediateDirectory in @($publishRoot, $stageRoot, $installerWorkRoot, $smokeDataRoot)) {
    if (Test-Path -LiteralPath $intermediateDirectory) {
        Assert-ReleasePath $intermediateDirectory
        Remove-Item -LiteralPath $intermediateDirectory -Recurse -Force
    }
}

Write-Step 'Artifacts ready'
Get-ChildItem -LiteralPath $packageDirectory -File |
    Sort-Object Name |
    Select-Object Name, Length, @{ Name = 'Sha256'; Expression = { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } } |
    Format-Table -AutoSize
Write-Host "WinGet manifests: $manifestDirectory"

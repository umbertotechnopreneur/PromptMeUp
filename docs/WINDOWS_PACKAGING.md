# Windows release packaging

One release command turns PromptMeUp into a Windows-ready set for users who prefer a portable ZIP, WinGet, or an optional per-user MSI. `scripts/build-release-artifacts.ps1` creates those artifacts without publishing them or installing anything on the workstation.

## What users receive

For version `0.1.5`, the default output is `artifacts/release/0.1.5/`:

```text
packages/
  PromptMeUp-0.1.5-win-x64.zip
  PromptMeUp-0.1.5-win-arm64.zip
  PromptMeUp-0.1.5-win-x64.msi
  SHA256SUMS.txt
winget/UmbertoGiacobbi.PromptMeUp/0.1.5/
  UmbertoGiacobbi.PromptMeUp.yaml
  UmbertoGiacobbi.PromptMeUp.installer.yaml
  UmbertoGiacobbi.PromptMeUp.locale.en-US.yaml
release.json
```

The ZIP archives contain only `hm.exe`, the required `prompt` YAML resources, `LICENSE`, and `THIRD_PARTY_NOTICES.md`. They are self-contained and do not require a separate .NET runtime. The WinGet manifest declares PowerShell 7 as a package dependency because approved command execution uses `pwsh`.

After a successful build, publish, staging, WiX, and smoke-test intermediates are removed automatically. They remain only when a build fails, where they are useful for diagnosis. The completed release directory therefore contains only the packages, WinGet manifests, checksums, and `release.json` needed for testing or publication.

The x64 MSI uses WiX Toolset 3.14, installs under `%LOCALAPPDATA%\Programs\PromptMeUp`, adds the installation directory to the current user's `PATH`, supports silent Windows Installer switches, and removes its PATH entry on uninstall. It is a per-user package and does not require elevation or modify the machine `PATH`. Native ARM64 remains available through the portable WinGet archive; WiX 3.14 cannot produce an ARM64 MSI.

WiX treats every validation warning as an error. The build suppresses ICE64 for the shared `%LOCALAPPDATA%\Programs` parent because PromptMeUp must never claim or remove that directory, and ICE91 because the package is exclusively per-user. The harvested `PromptMeUp` installation directory and its descendants still receive explicit uninstall cleanup; all other enabled ICE checks remain strict.

## What maintainers need

- PowerShell 7, invoked as `pwsh -NoProfile`;
- the .NET 10 SDK selected by `global.json`;
- WinGet for manifest validation;
- WiX Toolset 3.14 for the optional x64 MSI (`heat.exe`, `candle.exe`, and `light.exe`).

The local-server example also uses Python when available; any static HTTP server bound only to loopback is equivalent.

Use `-SkipMsi` when only portable and WinGet artifacts are needed. Use `-SkipWingetValidation` only when WinGet is unavailable; publication candidates must always be validated.

## Create a release candidate

Preview all destinations and build choices without writing files:

```powershell
pwsh -NoProfile -File .\scripts\build-release-artifacts.ps1 -PlanOnly
```

Create the complete local test set:

```powershell
pwsh -NoProfile -File .\scripts\build-release-artifacts.ps1
```

The default manifest URLs point to `http://127.0.0.1:8765`. This is only for local testing. To generate a publication candidate, pass the immutable HTTPS directory containing the versioned release assets:

```powershell
pwsh -NoProfile -File .\scripts\build-release-artifacts.ps1 `
  -ArtifactBaseUrl 'https://github.com/umbertotechnopreneur/PromptMeUp/releases/download/v0.1.5'
```

The requested version must use three numeric parts because Windows Installer compares only numeric MSI versions. If `-Version` is omitted, the script reads `Version` from `PromptMeUp.csproj`.

## Experience the WinGet path before publishing

Start a temporary local file server in the package directory and leave it running:

```powershell
Set-Location .\artifacts\release\0.1.5\packages
python -m http.server 8765 --bind 127.0.0.1
```

In another administrator terminal, enable local manifests once:

```powershell
winget settings --enable LocalManifestFiles
```

Return to the repository in a normal, non-administrator terminal and install the local manifest for the current user:

```powershell
winget install --manifest .\artifacts\release\0.1.5\winget\UmbertoGiacobbi.PromptMeUp\0.1.5 --scope user
```

Open a new terminal so it receives the updated PATH, then verify without using an API key:

```powershell
hm --version --no-animation --no-emoji
hm -where
hm --status --no-animation --no-emoji
winget list --id UmbertoGiacobbi.PromptMeUp --exact
```

WinGet adds the archive directory to the current user's `PATH` because the manifest declares `ArchiveBinariesDependOnPath: true`. PromptMeUp can also inspect, remove, or restore that same user entry. Save the resolved executable path before testing removal, because a new terminal will no longer resolve `hm` until the entry is restored:

```powershell
$hmExecutable = (Get-Command hm -CommandType Application).Source
hm --path status
hm --path remove
& $hmExecutable --path install
```

Each changing action shows an exact preview and requires confirmation unless `--yes` is supplied. Open a new terminal after a change. Uninstall the local package from a normal terminal:

```powershell
winget uninstall --id UmbertoGiacobbi.PromptMeUp --exact --scope user
```

Then disable local manifests from an administrator terminal:

```powershell
winget settings --disable LocalManifestFiles
```

## Experience the MSI path

The standard per-user installer supports normal Windows Installer behavior from a non-administrator terminal:

```powershell
msiexec.exe /i .\artifacts\release\0.1.5\packages\PromptMeUp-0.1.5-win-x64.msi
```

For an unattended test in Windows Sandbox or another disposable environment:

```powershell
msiexec.exe /i .\artifacts\release\0.1.5\packages\PromptMeUp-0.1.5-win-x64.msi /qn /norestart
```

After installation, open a new terminal and verify both the installed command and the application-managed user PATH controls:

```powershell
$hmExecutable = (Get-Command hm -CommandType Application).Source
hm --version --no-animation --no-emoji
hm -where
hm --path status
hm --path remove
& $hmExecutable --path install
```

Uninstalling the MSI removes its user PATH entry and installed files:

```powershell
msiexec.exe /x .\artifacts\release\0.1.5\packages\PromptMeUp-0.1.5-win-x64.msi
```

The build never reads or packages `OPENAI_API_KEY`, `OPENAI_ADMIN_KEY`, settings, databases, logs, or the local application-data directory. Code signing is intentionally separate and must happen before final SHA-256 calculation and WinGet manifest generation.

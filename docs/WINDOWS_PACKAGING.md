# Windows release packaging

`scripts/build-release-artifacts.ps1` creates the Windows release set without publishing it or installing anything on the workstation.

## Outputs

For version `0.1.0`, the default output is `artifacts/release/0.1.0/`:

```text
packages/
  PromptMeUp-0.1.0-win-x64.zip
  PromptMeUp-0.1.0-win-arm64.zip
  PromptMeUp-0.1.0-win-x64.msi
  SHA256SUMS.txt
winget/UmbertoGiacobbi.PromptMeUp/0.1.0/
  UmbertoGiacobbi.PromptMeUp.yaml
  UmbertoGiacobbi.PromptMeUp.installer.yaml
  UmbertoGiacobbi.PromptMeUp.locale.en-US.yaml
release.json
```

The ZIP archives contain only `hm.exe`, the required `prompt` YAML resources, `LICENSE`, and `THIRD_PARTY_NOTICES.md`. They are self-contained and do not require a separate .NET runtime. The WinGet manifest declares PowerShell 7 as a package dependency because approved command execution uses `pwsh`.

The x64 MSI uses WiX Toolset 3.14, installs under `Program Files`, adds the installation directory to the system `PATH`, supports silent Windows Installer switches, and removes its PATH entry on uninstall. The MSI therefore requires elevation. Native ARM64 remains available through the portable WinGet archive; WiX 3.14 cannot produce an ARM64 MSI.

## Prerequisites

- PowerShell 7, invoked as `pwsh -NoProfile`;
- the .NET 10 SDK selected by `global.json`;
- WinGet for manifest validation;
- WiX Toolset 3.14 for the optional x64 MSI (`heat.exe`, `candle.exe`, and `light.exe`).

The local-server example also uses Python when available; any static HTTP server bound only to loopback is equivalent.

Use `-SkipMsi` when only portable and WinGet artifacts are needed. Use `-SkipWingetValidation` only when WinGet is unavailable; publication candidates must always be validated.

## Build locally

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
  -ArtifactBaseUrl 'https://github.com/umbertotechnopreneur/PromptMeUp/releases/download/v0.1.0'
```

The requested version must use three numeric parts because Windows Installer compares only numeric MSI versions. If `-Version` is omitted, the script reads `Version` from `PromptMeUp.csproj`.

## Test WinGet without publishing

Start a temporary local file server in the package directory and leave it running:

```powershell
Set-Location .\artifacts\release\0.1.0\packages
python -m http.server 8765 --bind 127.0.0.1
```

In another administrator terminal, enable local manifests once:

```powershell
winget settings --enable LocalManifestFiles
```

Return to the repository in a normal, non-administrator terminal and install the local manifest for the current user:

```powershell
winget install --manifest .\artifacts\release\0.1.0\winget\UmbertoGiacobbi.PromptMeUp\0.1.0 --scope user
```

Open a new terminal so it receives the updated PATH, then verify without using an API key:

```powershell
hm --version --no-animation --no-emoji
hm --status --no-animation --no-emoji
winget list --id UmbertoGiacobbi.PromptMeUp --exact
```

Do not run `hm --path install` for a WinGet-managed copy; WinGet owns installation, PATH registration, upgrades, and removal. Uninstall the test from the normal terminal:

```powershell
winget uninstall --id UmbertoGiacobbi.PromptMeUp --exact --scope user
```

Then disable local manifests from an administrator terminal:

```powershell
winget settings --disable LocalManifestFiles
```

## Test the MSI

The standard installer supports normal Windows Installer behavior:

```powershell
msiexec.exe /i .\artifacts\release\0.1.0\packages\PromptMeUp-0.1.0-win-x64.msi
```

For an unattended test in Windows Sandbox or another disposable environment:

```powershell
msiexec.exe /i .\artifacts\release\0.1.0\packages\PromptMeUp-0.1.0-win-x64.msi /qn /norestart
```

The build never reads or packages `OPENAI_API_KEY`, `OPENAI_ADMIN_KEY`, settings, databases, logs, or the local application-data directory. Code signing is intentionally separate and must happen before final SHA-256 calculation and WinGet manifest generation.

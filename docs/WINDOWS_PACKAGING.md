# PromptMeUp Windows release packaging

GitHub releases include unsigned EXE installers for Windows x64 and ARM64, plus portable archives. The [release guide](RELEASING.md#create-a-release-draft) explains how to generate a draft with one command. This guide covers the EXE builder and the separate local MSI, WinGet, and signed MSIX options.

## Unsigned EXE installers for x64 and ARM64

The release workflow uses `scripts/build-windows-installer.ps1` with Inno Setup 6 to package each Windows portable payload. The result is `PromptMeUp-<version>-win-x64-setup.exe` or `PromptMeUp-<version>-win-arm64-setup.exe`. A signing certificate is not required. Windows may display an unknown-publisher or SmartScreen warning because these installers are unsigned.

Each installer accepts its matching Windows CPU type and installs for the current user under `%LOCALAPPDATA%\Programs\PromptMeUp`. It adds that directory to the user's `PATH`; open a new terminal before using `hm`. PowerShell 7 must be installed separately for command execution. The installer does not start PromptMeUp, install a background service, or change the machine `PATH`.

Later versions update the same EXE installation. The installer rejects downgrades and refuses to overwrite a folder managed by another installation method. Uninstall removes the files recorded by the installer and its own PATH entry, while leaving ordinary PromptMeUp data separate. If you already use MSI or MSIX, continue with that package type or remove the old installation before switching. An existing MSIX execution alias can take precedence over the EXE copy.

To package an existing clean Windows portable payload locally, use Inno Setup 6.3 or later in the 6.x series. The Windows runner currently includes 6.7.1. This example assumes the portable payload for version `0.1.7` has already been built:

```powershell
pwsh -NoProfile -File .\scripts\build-windows-installer.ps1 `
  -PublishDirectory .\artifacts\portable\0.1.7\win-x64\payload `
  -OutputDirectory .\artifacts\windows-installer-x64 `
  -Architecture x64
```

Use the `win-arm64` payload and `-Architecture arm64` for ARM64. Pass `-IsccPath` if `ISCC.exe` is outside the standard Inno Setup 6 installation folders. Output must be a fresh directory under `artifacts`. The builder checks the executable's CPU type and version, bundled resources and notices, and allowed payload files. It compiles the installer without installing it, running the app, or changing PATH. Run installation checks separately on matching machines before publishing.

## Local MSI and WinGet packages

Use `scripts/build-release-artifacts.ps1` to make Windows ZIP downloads, WinGet manifests, and an optional x64 MSI installer for the current user. It creates the files locally; review them before publishing or installing.

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

The ZIP archives contain `hm.exe`, prompt and theme resources, PATH helpers, and the complete redistribution notice payload. They are self-contained and do not require a separate .NET runtime. The WinGet manifest declares PowerShell 7 as a package dependency because approved command execution uses `pwsh`.

After a successful build, the script removes temporary build, packaging, and smoke-test files. If the build fails, it keeps them to help you find the problem. A finished release folder holds the packages, WinGet manifests, checksums, and `release.json` you'll need to test or publish.

The x64 MSI uses WiX Toolset 3.14, installs under `%LOCALAPPDATA%\Programs\PromptMeUp`, adds the installation directory to the current user's `PATH`, supports silent Windows Installer switches, and removes its PATH entry on uninstall. It is a per-user package and does not require elevation or modify the machine `PATH`. Native ARM64 remains available through the portable WinGet archive; WiX 3.14 cannot produce an ARM64 MSI.

WiX treats every validation warning as an error. The build suppresses ICE64 for the shared `%LOCALAPPDATA%\Programs` parent because PromptMeUp must never claim or remove that directory, and ICE91 because the package is exclusively per-user. The harvested `PromptMeUp` installation directory and its descendants still receive explicit uninstall cleanup; all other enabled ICE checks remain strict.

## What maintainers need

- PowerShell 7, invoked as `pwsh -NoProfile`;
- the .NET 10 SDK selected by `global.json`;
- WinGet for manifest validation;
- WiX Toolset 3.14 for the optional x64 MSI (`heat.exe`, `candle.exe`, and `light.exe`).

The local-server example also uses Python when available; any static HTTP server bound only to loopback is equivalent.

Use `-SkipMsi` if you only need portable and WinGet packages. Use `-SkipWingetValidation` only if WinGet isn't available, and make sure you validate the manifests before publishing.

## Create a release candidate

Preview all destinations and build choices without writing files:

```powershell
pwsh -NoProfile -File .\scripts\build-release-artifacts.ps1 -PlanOnly
```

Create the complete local test set:

```powershell
pwsh -NoProfile -File .\scripts\build-release-artifacts.ps1
```

By default, manifest links point to `http://127.0.0.1:8765` for local testing. For files you plan to publish, pass the HTTPS address where that exact version's downloads will live. Keep those files unchanged once published:

```powershell
pwsh -NoProfile -File .\scripts\build-release-artifacts.ps1 `
  -ArtifactBaseUrl 'https://github.com/umbertotechnopreneur/PromptMeUp/releases/download/v0.1.5'
```

The requested version must use three numeric parts because Windows Installer compares only numeric MSI versions. If `-Version` is omitted, the script reads `Version` from `PromptMeUp.csproj`.

## Try WinGet before publishing

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

Each change shows a preview and asks for confirmation unless you pass `--yes`. Open a new terminal afterward. To uninstall the local package, use a normal terminal:

```powershell
winget uninstall --id UmbertoGiacobbi.PromptMeUp --exact --scope user
```

Then disable local manifests from an administrator terminal:

```powershell
winget settings --disable LocalManifestFiles
```

## Try the MSI installer

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

## Install with the `hm` execution alias

The optional MSIX package registers `hm.exe` through Windows' app execution aliases. You can then type `hm` without adding the build or installation folder to `PATH`. Portable archives and the MSI remain available.

Use Windows 10 version 2004 or later, PowerShell 7, a Windows SDK containing `MakeAppx.exe` and `SignTool.exe`, and an existing trusted code-signing certificate with its private key in `CurrentUser\My`. The signing certificate's subject becomes the package publisher. Keep that certificate and package name consistent for future updates.

Prepare a fresh self-contained folder and its redistribution notices. This example uses x64; use `win-arm64` and `-Architecture arm64` for Arm64:

```powershell
dotnet publish .\PromptMeUp\PromptMeUp.csproj --configuration Release `
  --runtime win-x64 --self-contained true -p:PublishSingleFile=false `
  -p:DebugType=None -p:DebugSymbols=false --output .\artifacts\msix\local\publish
pwsh -NoProfile -File .\scripts\export-third-party-notices.ps1 `
  -OutputDirectory .\artifacts\msix\local\publish -Runtime win-x64

pwsh -NoProfile -File .\scripts\build-msix.ps1 `
  -PublishDirectory .\artifacts\msix\local\publish `
  -OutputDirectory .\artifacts\msix\local\package `
  -Architecture x64 -CertificateThumbprint '<your existing code-signing certificate thumbprint>'
```

The thumbprint identifies your public certificate; you don't pass a private key or exported certificate to the script. The script copies the prepared app, runtime DLLs, prompts, themes, and license files. It makes tile images from `assets/PromptMeUp.ico`, checks the manifest, signs the MSIX, and checks the signature.

Use a fresh output folder. The script rejects links and unexpected files such as databases, logs, or credentials. It only builds the package: it doesn't install or run the app, run tests, or change certificate trust or `PATH`.

The four-part package version defaults to the published executable's file version. Use `-Version 0.1.5.1` when a packaging revision needs a higher version. Use `-SdkBinDirectory` to select the directory containing the SDK tools. An optional `-TimestampServer https://...` adds an RFC 3161 timestamp from your chosen signing service; without one, the signature's validity is limited by the certificate's expiry.

After reviewing the generated manifest under `package/payload`, install the signed package for the current user:

```powershell
Add-AppxPackage -Path .\artifacts\msix\local\package\PromptMeUp-0.1.5.0-win-x64.msix
Get-Command hm -CommandType Application
```

Windows exposes the alias through the user's `Microsoft\WindowsApps` directory. If another installation's `hm.exe` appears first, remove that installation's old `PATH` entry or uninstall it after checking which copy is active. Windows' **App execution aliases** settings let you enable or disable the MSIX alias. Do not run `hm --path install` for the MSIX copy.

The package uses the `win32App` runtime behavior and a normal console process. It keeps the existing local application-data and current-user environment behavior, and approved commands still run through the installed `pwsh`. The package declares only `runFullTrust`; it does not add a background service. See Microsoft's [packaged application types](https://devblogs.microsoft.com/insidemsix/types-of-packaged-applications/) and [execution alias schema](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-uap5-appexecutionalias).

To remove the default MSIX package and its alias:

```powershell
Get-AppxPackage -Name UmbertoGiacobbi.PromptMeUp | Remove-AppxPackage
```

Your ordinary PromptMeUp data directory remains separate from the package. Removing the MSIX does not remove that saved history or your current-user API key environment variables.

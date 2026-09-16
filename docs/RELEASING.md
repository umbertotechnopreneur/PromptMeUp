# Releasing PromptMeUp

The release workflow prepares unsigned Windows installers for x64 and ARM64, plus portable archives for Windows, Linux, and macOS. Start it when a version is ready. It runs the checks, packages the app, and creates a draft release with checksums. You review the draft and publish it. Merging code into `main` alone doesn't create a release.

## Create a release draft

First, merge the version you want to release into `main`. Set the three-part `Version` in `PromptMeUp/PromptMeUp.csproj` to a new number, such as `0.1.7`. The workflow uses the code and version on GitHub; uncommitted local changes are not included.

From a checkout of this repository with the [GitHub CLI](https://cli.github.com/manual/gh_workflow_run) signed in, run:

```powershell
gh workflow run release.yml --ref main -f mode=draft
```

Or open **Actions → Release → Run workflow**, choose `main`, leave `mode` set to `draft`, and click **Run workflow**. The workflow must already be merged into the default branch before GitHub can offer this trigger.

The workflow builds the selected commit and runs its quality checks. If they pass, it creates the matching tag, such as `v0.1.7`, and a draft containing:

- `PromptMeUp-0.1.7-win-x64-setup.exe` and `PromptMeUp-0.1.7-win-arm64-setup.exe`;
- six portable archives, one for each supported operating system and CPU type;
- `SHA256SUMS.txt` covering all eight downloads and GitHub build provenance attestations.

No signing certificate or personal access token needs to be added to repository secrets. The workflow uses GitHub's built-in token with release permissions limited to the delivery job. The EXE installers are unsigned, so Windows may show an unknown-publisher or SmartScreen warning. GitHub provenance and checksums do not replace a Windows code-signing certificate.

Follow the run under **Actions → Release**. When it finishes, open [Releases](https://github.com/umbertotechnopreneur/PromptMeUp/releases), review the draft and downloads, and choose **Publish release**. You can also publish a reviewed draft from the command line:

```powershell
gh release edit v0.1.7 --draft=false
```

Only that final action makes the draft public. Existing tags and releases are never overwritten. For the next release, merge a higher product version and run the workflow again.

## Checks that run on GitHub

The quality workflow runs for pull requests, pushes to `main`, manual runs, and releases. It checks required files and tools, formatting, XML comments, and bundled license notices. It also builds in Release mode with warnings treated as errors, runs tests, and tries basic CLI commands without credentials on Windows, Linux, and macOS.

CodeQL analyzes C# on pull requests, main pushes, and a weekly schedule. Dependency review rejects newly introduced high or critical vulnerabilities on pull requests. Dependabot proposes weekly NuGet and GitHub Actions updates. Actions are pinned to full commit hashes; the .NET SDK comes from global.json.

The required checks for `main` are Lint, Build (windows-latest), Build (ubuntu-latest), Build (macos-latest), Analyze C#, and Review dependencies. They must come from GitHub Actions. The intended rules are in [.github/main-protection.json](../.github/main-protection.json). Committing that file doesn't change GitHub's settings; apply and check those separately.

## Rehearse a release

To try packaging without creating a release or tag, select `rehearsal` in **Actions → Release → Run workflow**, or run:

```powershell
gh workflow run release.yml --ref main -f mode=rehearsal
```

It runs the quality checks and builds the same eight downloads. Download `release-bundle` from the finished Actions run.

For a local package, run PowerShell 7 from the repository root:

```powershell
pwsh -NoProfile -File ./scripts/build-portable-release.ps1 -Runtime win-x64
```

Choose `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, or `osx-arm64`. Windows packages are ZIP files. Linux and macOS use tar.gz to keep executable permissions; build those archives on a Unix machine. The script requires a fresh output folder so old files can't slip into a new package. For another run, use something like `-OutputDirectory artifacts/rehearsal-2`. Output must stay under `artifacts`.

The workflow tries basic commands only when the build machine matches the package's operating system and CPU type. Try the other packages on matching machines before publishing; building all six doesn't mean all six have been run.

## Start a release with a Git tag

You can also start the same draft workflow by pushing a version tag yourself:

1. Update the three-part `Version` in `PromptMeUp/PromptMeUp.csproj` through a reviewed pull request.
2. Wait for all main checks to succeed.
3. Create and push an annotated tag matching that version, for example v1.2.3. Repository automation agents must obtain explicit authorization before creating branches or worktrees; do not tag unfinished local work.
4. The release workflow verifies that the tagged commit belongs to main and that the tag exactly matches the project version. It reruns the complete quality workflow before packaging.
5. Review the generated draft, release notes, two Windows installers, six archives, `SHA256SUMS.txt`, and provenance attestations. Verify installation and startup on intended target machines before publishing the draft in GitHub.

Only the delivery job has release-write and attestation permissions. Pull-request quality jobs receive no release credentials. The workflow never overwrites an existing release or tag.

## What every portable package contains

About shows the build date and time in UTC and the name of the machine that compiled the app. The shared `scripts/BuildInformation.targets` file captures these values during `dotnet build` and `dotnet publish` and embeds them in the application assembly. They stay the same when the app is installed or copied to another computer. Generated values stay under ignored build directories.

For repeatable builds, supply the same `PromptMeUpBuildDateUtc` and `PromptMeUpBuildMachine` MSBuild properties each time. The date must use the round-trip UTC format, such as `2026-01-01T00:00:00.0000000+00:00`. Without these overrides, each build captures a fresh timestamp and regenerates the assembly information.

- The self-contained hm executable, localized prompt resources, and PATH helper scripts.
- The root MIT license and third-party attribution overview.
- Full upstream license texts and package-supplied notices under LICENSES.
- A THIRD_PARTY_INVENTORY.json recording the exact resolved application packages and the selected .NET runtime pack, without machine paths.
- BUILD_INFO.txt identifying the application version, target platform, and source commit.

The exporter reads the resolved application dependency graph, includes transitive packages, and copies license/notice files from the restored packages. The SDK's ILLink build tooling is also recorded and marked build-only; its binaries are not shipped. Runtime pack notices come from the exact restored version, not an arbitrary installed runtime. Unknown package families or unsupported license metadata stop packaging so attribution can be reviewed.

When dependencies change, review the root inventory, the upstream files in LICENSES, and the exporter mapping together. The runtime --third-party view remains a concise direct-dependency view.

## Verify a download

Compare an archive's SHA-256 with SHA256SUMS.txt from the same release:

```powershell
Get-FileHash ./PromptMeUp-1.2.3-win-x64.zip -Algorithm SHA256
gh attestation verify ./PromptMeUp-1.2.3-win-x64.zip --repo umbertotechnopreneur/PromptMeUp
```

The attestation check verifies GitHub Actions' record of where the package came from. You'll still need code review and testing. See [GitHub's attestation documentation](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations).

## Windows installers and signing

The release workflow builds EXE installers with Inno Setup on its Windows runner. Each installer contains the native self-contained app for its target CPU, resources, and full redistribution notices. Installation is for the current user and does not require a code-signing certificate or administrator privileges. PowerShell 7 is still needed to execute commands approved in PromptMeUp.

The [Windows packaging guide](WINDOWS_PACKAGING.md) covers the EXE installer builder and the separate local MSI, WinGet, and signed MSIX routes. This workflow does not sign Windows binaries, submit packages to a store, or submit WinGet manifests. Signed MSIX packaging still requires an existing trusted signing certificate.

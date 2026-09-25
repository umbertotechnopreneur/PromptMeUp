# Releasing PromptMeUp

The release workflow prepares two portable ZIP archives: Windows x64 and Windows ARM64. Start it when a version is ready. It runs the checks, packages the app, and creates a draft release with checksums. You review the draft and publish it. Merging code into `main` alone doesn't create a release. Microsoft Store MSIX packages are built locally through a separate script.

## Create a release draft

First, merge the version you want to release into `main`. Set the three-part `Version` in `PromptMeUp/PromptMeUp.csproj` to a new number, such as `0.1.7`. The workflow uses the code and version on GitHub; uncommitted local changes are not included.

From a checkout of this repository with the [GitHub CLI](https://cli.github.com/manual/gh_workflow_run) signed in, run:

```powershell
gh workflow run release.yml --ref main -f mode=draft
```

Or open **Actions → Release → Run workflow**, choose `main`, leave `mode` set to `draft`, and click **Run workflow**. The workflow must already be merged into the default branch before GitHub can offer this trigger.

The workflow builds the selected commit and runs its quality checks. If they pass, it creates the matching tag, such as `v0.1.7`, and a draft containing:

- `PromptMeUp-0.1.7-win-x64.zip` and `PromptMeUp-0.1.7-win-arm64.zip`;
- `SHA256SUMS.txt` covering both downloads and GitHub build provenance attestations.

No signing certificate or personal access token needs to be added to repository secrets. The workflow uses GitHub's built-in token with release permissions limited to the delivery job. GitHub provenance and checksums do not replace a Windows code-signing certificate.

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

It runs the quality checks and builds the same two ZIP downloads. Download `release-bundle` from the finished Actions run.

For a local package, run PowerShell 7 from the repository root:

```powershell
pwsh -NoProfile -File ./scripts/PromptMeUp.ps1 -Command portable -Runtime win-x64
```

Choose `win-x64` or `win-arm64` for Windows distribution. The script requires a fresh output folder so old files can't slip into a new package. For another run, use something like `-OutputDirectory artifacts/rehearsal-2`. Output must stay under `artifacts`.

The workflow tries basic commands only when the build machine matches the package's CPU type. Try the other ZIP on an ARM64 machine before publishing; cross-publishing does not run that app.

## Start a release with a Git tag

You can also start the same draft workflow by pushing a version tag yourself:

1. Update the three-part `Version` in `PromptMeUp/PromptMeUp.csproj` through a reviewed pull request.
2. Wait for all main checks to succeed.
3. Create and push an annotated tag matching that version, for example v1.2.3. Repository automation agents must obtain explicit authorization before creating branches or worktrees; do not tag unfinished local work.
4. The release workflow verifies that the tagged commit belongs to main and that the tag exactly matches the project version. It reruns the complete quality workflow before packaging.
5. Review the generated draft, release notes, two Windows ZIP archives, `SHA256SUMS.txt`, and provenance attestations. Verify startup on intended target machines before publishing the draft in GitHub.

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

## Microsoft Store MSIX packages

The GitHub release workflow does not create MSIX packages. On a Windows machine with the .NET 10 SDK and Windows SDK packaging tools, build the x64 and ARM64 Store packages locally:

```powershell
pwsh -NoProfile -File ./scripts/PromptMeUp.ps1 -Command store-msix -Version 1.0.0
```

The script creates unsigned MSIX files for Store submission only. It does not sign, install, upload, or publish them. Microsoft Store signs them after certification. See the [Windows packaging guide](WINDOWS_PACKAGING.md#build-msix-packages-for-microsoft-store) for the output paths and checks.

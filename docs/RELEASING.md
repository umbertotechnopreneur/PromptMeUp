# Releasing PromptMeUp

Most people will get PromptMeUp as a portable archive. Pushing a version tag starts the checks and prepares a draft release on GitHub. The maintainer reviews and publishes it. Merging code into `main` alone doesn't create a release.

## Checks that run on GitHub

The quality workflow runs for pull requests, pushes to `main`, manual runs, and releases. It checks required files and tools, formatting, XML comments, and bundled license notices. It also builds in Release mode with warnings treated as errors, runs tests, and tries basic CLI commands without credentials on Windows, Linux, and macOS.

CodeQL analyzes C# on pull requests, main pushes, and a weekly schedule. Dependency review rejects newly introduced high or critical vulnerabilities on pull requests. Dependabot proposes weekly NuGet and GitHub Actions updates. Actions are pinned to full commit hashes; the .NET SDK comes from global.json.

The required checks for `main` are Lint, Build (windows-latest), Build (ubuntu-latest), Build (macos-latest), Analyze C#, and Review dependencies. They must come from GitHub Actions. The intended rules are in [.github/main-protection.json](../.github/main-protection.json). Committing that file doesn't change GitHub's settings; apply and check those separately.

## Rehearse a release

To try the release process, run **Portable release** manually from `main`. It checks the code and builds six packages without creating a release or tag. Download `release-bundle` from the finished Actions run.

For a local package, run PowerShell 7 from the repository root:

```powershell
pwsh -NoProfile -File ./scripts/build-portable-release.ps1 -Runtime win-x64
```

Choose `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, or `osx-arm64`. Windows packages are ZIP files. Linux and macOS use tar.gz to keep executable permissions; build those archives on a Unix machine. The script requires a fresh output folder so old files can't slip into a new package. For another run, use something like `-OutputDirectory artifacts/rehearsal-2`. Output must stay under `artifacts`.

The workflow tries basic commands only when the build machine matches the package's operating system and CPU type. Try the other packages on matching machines before publishing; building all six doesn't mean all six have been run.

## Prepare a version

1. Update the three-part Version in PromptMeUp/PromptMeUp.csproj through a reviewed pull request.
2. Wait for all main checks to succeed.
3. Create and push an annotated tag matching that version, for example v1.2.3. Repository automation agents must obtain explicit authorization before creating branches or worktrees; do not tag unfinished local work.
4. The release workflow verifies that the tagged commit belongs to main and that the tag exactly matches the project version. It reruns the complete quality workflow before packaging.
5. Review the generated draft, release notes, six archives, SHA256SUMS.txt, and provenance attestations. Test installation and startup on intended target machines, then publish the draft in GitHub.

Only the delivery job has release-write and attestation permissions. Pull-request quality jobs receive no release credentials. The workflow never overwrites an existing release or tag.

## What every portable package contains

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

## Optional Windows distribution

The existing [Windows packaging guide](WINDOWS_PACKAGING.md) covers local MSI and WinGet manifest generation. That builder also includes the complete notice payload. MSI signing, notarization, store submissions, and WinGet submissions are not performed by the portable release workflow.

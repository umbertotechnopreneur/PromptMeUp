# From a useful change to a downloadable release

Portable archives are the primary distribution format. A versioned tag prepares a GitHub Release draft after validation; the maintainer chooses when to publish it. No release is created just by merging to main.

## Continuous integration

The quality workflow runs on pull requests, main pushes, manual dispatch, and calls from the release workflow. It verifies repository preflight, formatting, XML comments, resolved redistribution notices, Release builds with warnings as errors, tests, and credential-free CLI smoke checks on Windows, Linux, and macOS.

CodeQL analyzes C# on pull requests, main pushes, and a weekly schedule. Dependency review rejects newly introduced high or critical vulnerabilities on pull requests. Dependabot proposes weekly NuGet and GitHub Actions updates. Actions are pinned to full commit hashes; the .NET SDK comes from global.json.

The required main checks are Lint, Build (windows-latest), Build (ubuntu-latest), Build (macos-latest), Analyze C#, and Review dependencies. They are bound to the GitHub Actions app. The target policy is checked in as [.github/main-protection.json](../.github/main-protection.json); GitHub settings require separate application and verification, not just committing the file.

## Rehearse a release

Run Portable release manually from main. It runs the quality gate and builds six packages without creating a release or a tag. Download release-bundle from the completed Actions run.

For a local package, run PowerShell 7 from the repository root:

```powershell
pwsh -NoProfile -File ./scripts/build-portable-release.ps1 -Runtime win-x64
```

Supported runtime names are win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64. Windows output is ZIP; Linux and macOS output is tar.gz, preserving executable permissions. Build Unix archives on a Unix host. The script refuses existing output so a previous payload cannot silently contaminate a new package. For another rehearsal, choose a fresh directory such as `-OutputDirectory artifacts/rehearsal-2`; output must remain below artifacts.

Matching host architectures receive executable smoke checks. Cross-published architectures are packaged but need a matching machine for execution testing before publication. The workflow does not claim six native execution tests.

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

Attestation verification confirms provenance recorded by GitHub Actions; it does not replace code review or execution testing. See [GitHub's attestation documentation](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations).

## Optional Windows distribution

The existing [Windows packaging guide](WINDOWS_PACKAGING.md) covers local MSI and WinGet manifest generation. That builder also includes the complete notice payload. MSI signing, notarization, store submissions, and WinGet submissions are not performed by the portable release workflow.

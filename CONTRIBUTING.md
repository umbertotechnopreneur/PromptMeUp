# Contributing to PromptMeUp

Every contribution should make the next terminal step clearer, safer, or easier to understand.

PromptMeUp is deliberately small. Lead with the user outcome, keep the change focused, and bring concrete validation so the improvement is easy to trust and review.

## Choose the right contribution path

- Read [README.md](README.md), [Privacy and data flow](docs/PRIVACY.md), and [Security](SECURITY.md).
- Read `AGENTS.md` and `.github/copilot-instructions.md` before changing the repository.
- Search existing issues and pull requests.
- Never include API keys, tokens, prompt history, command output, personal data, local database files, logs, or private machine paths.
- Report vulnerabilities privately as described in [SECURITY.md](SECURITY.md).
- Open an issue before a large product or architecture change.

## Get a local build running

Requirements: .NET 10 SDK, Git, and PowerShell 7 for repository helpers.

```powershell
pwsh -NoProfile -File .\scripts\preflight.ps1
dotnet restore .\PromptMeUp.slnx
dotnet format .\PromptMeUp.slnx --verify-no-changes --no-restore
pwsh -NoProfile -File .\scripts\check-xml-comments.ps1
dotnet build .\PromptMeUp.slnx --configuration Release --no-restore --warnaserror
dotnet test .\PromptMeUp.slnx --configuration Release --no-build
```

## Protect the product promises

- Keep the application split into models, services, views, and the application orchestrator.
- Keep every command execution behind exact preview and explicit authorization.
- Treat AI risk scoring as advisory; retain conservative local checks.
- Keep secrets out of command-line arguments, settings, SQLite payloads, logs, tests, and screenshots.
- Put runtime AI prompts in `/prompt` as validated YAML with metadata and all six localized texts.
- Add a brief XML `<summary>` to every C# implementation method. Add small inline hints only where a non-obvious algorithm needs them.
- Keep `AGENTS.md` and `.github/copilot-instructions.md` aligned.
- Preserve cross-platform behavior and keep portable archives canonical; platform installers must remain optional release surfaces.

## Make the outcome easy to review

Explain the user-visible outcome, the safety/privacy impact, the files or layers changed, and the commands used to validate the result. Keep generated output and unrelated formatting out of the change.

AI tools may assist, but contributors remain responsible for correctness, security, attribution, and license compatibility.

Write repository prose and code comments in English; preserve all six runtime translations. See [governance](GOVERNANCE.md) for the maintainer workflow and [the release process](docs/RELEASING.md) for packaging. When adding or updating dependencies, review direct and transitive licenses and preserve their full upstream notices; package export fails for unknown dependency families.

By contributing, you confirm that you can submit the work under the repository's [MIT License](LICENSE).

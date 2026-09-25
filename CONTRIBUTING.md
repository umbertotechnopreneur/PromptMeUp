# Contributing to PromptMeUp

Help improve PromptMeUp with a bug fix, clearer instructions, or a useful command example.

Keep changes small and explain who they help. Tell me what you checked so I can review the change.

## Before you start

- Read [README.md](README.md), [Privacy and data flow](docs/PRIVACY.md), and [Security](SECURITY.md).
- Read `AGENTS.md` and `.github/copilot-instructions.md` before changing the repository.
- Search existing issues and pull requests.
- Never include API keys, tokens, prompt history, command output, personal data, local database files, logs, or private machine paths.
- Report vulnerabilities privately as described in [SECURITY.md](SECURITY.md).
- Open an issue before a large product or architecture change.

## Get a local build running

Requirements: .NET 10 SDK, Git, and PowerShell 7 for repository helpers.

Run `pwsh -NoProfile -File .\scripts\PromptMeUp.ps1` with no parameters for the interactive menu. Use `-Command` for repeatable local checks and CI.

```powershell
pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command preflight
dotnet restore .\PromptMeUp.slnx
pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command format
pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command check-format
pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command check-xml
dotnet build .\PromptMeUp.slnx --configuration Release --no-restore --warnaserror
```

If you're using a coding assistant, it must wait for an explicit request before running automated tests or CLI smoke tests (quick checks that launch the app). A request to implement, review, commit, or push isn't permission to run tests. When tests are requested, use the ones that fit the change, for example:

```powershell
dotnet test .\PromptMeUp.slnx --configuration Release --no-build
```

The formatting helper uses `dotnet format` to fix formatting. Run it with `-Verify` to check the result without changing files. CI uses that check too.

## Keep these rules in mind

- Keep data in models, app behavior in services, and display code in views. The application layer connects them.
- Show the exact command and ask for approval before running it.
- Keep the local risk checks. An AI review adds advice; it can't approve a command.
- Keep secrets out of command-line arguments, settings, SQLite payloads, logs, tests, and screenshots.
- Put the app's AI prompts in `/prompt`, using the required YAML fields and all six translations.
- Add a brief XML `<summary>` to every C# implementation method. Add small inline hints only where a non-obvious algorithm needs them.
- Keep `AGENTS.md` and `.github/copilot-instructions.md` aligned.
- Keep Windows, Linux, and macOS working. Portable archives are the main download format; installers are optional.

## Send your change for review

Explain what changes for the person using `hm`, which files you changed, and what you checked. Mention any effect on command safety or privacy. Leave build output and unrelated formatting out of the pull request.

You're responsible for the work you submit, including code written with AI tools. Check that it works as intended, handles data safely, and includes the right credits and licenses.

Write docs and code comments in plain English, as if you're explaining the project to a friend. Keep all six app translations up to date. See [how the project is run](GOVERNANCE.md) and [how releases are made](docs/RELEASING.md) for more details.

When you add or update a library, check its license and those of the libraries it depends on. Keep their full notices. Packaging stops if it finds a library the license exporter doesn't recognize.

By contributing, you confirm that you can submit the work under the repository's [MIT License](LICENSE).

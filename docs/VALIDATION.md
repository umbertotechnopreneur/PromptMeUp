# Validation guide

Every PromptMeUp change should preserve the same experience: readable output, explicit choices, bounded local behavior, and no surprise changes to the machine. Use a clean terminal and never use production API keys or confidential prompts for validation evidence.

## Prove the build is healthy

```powershell
pwsh -NoProfile -File .\scripts\preflight.ps1
dotnet restore .\PromptMeUp.slnx
dotnet format .\PromptMeUp.slnx --verify-no-changes --no-restore
pwsh -NoProfile -File .\scripts\check-xml-comments.ps1
dotnet build .\PromptMeUp.slnx --configuration Release --no-restore --warnaserror
dotnet test .\PromptMeUp.slnx --configuration Release --no-build
```

The GitHub Actions quality gate runs on pushes to `main`, pull requests, and manual dispatch. It runs the repository preflight, verifies formatting and XML comments, then builds and tests on Windows, Linux, and macOS.

Regression tests cover quoted/serialized JSON credentials, provider-bound command output, rejected and legacy preambles, Serilog exception privacy, HTTP body deadlines and limits, inherited process pipes, conservative command risk, long-answer visibility, Unix shell context, model-specific pricing bands, and indexed request summaries. HTTP and credential providers are synthetic; process tests run only inert PowerShell output/sleep commands and clean up their test child. The review-to-test mapping is recorded in [the September 2 review](../.github/tasks/review-2026-09-02.md).

## Prove the CLI is predictable

Use a disposable data directory:

```powershell
$env:PROMPTMEUP_DATA_DIR = Join-Path $PWD 'artifacts\smoke-data'
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --help --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --version --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --status --language vi --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --third-party --language fr --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- -where --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --path=status --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --install-font --dry-run --no-animation --no-emoji
```

Every command should exit `0`, preserve readable redirected output, and avoid an interactive prompt.

## Validate the first-run experience

- [ ] A clean first launch opens the frameless staged setup with clear whitespace, headings, and shortcuts.
- [ ] All six languages can be selected and the remaining form changes language immediately.
- [ ] API and admin key input reveals neither the value nor its character count, and is never reprinted.
- [ ] Model, thinking, detail, 500-word AI preamble, coarse location, command review, and prompt caching are visible.
- [ ] The preamble reports used/maximum/remaining word counts, accepts exactly 500 words, rejects 501, and shows localized validation in all six languages.
- [ ] The preamble rejects localized instruction overrides and attempts to forge or close its provider-facing delimiter.
- [ ] Every memory, output, and timeout limit rejects values outside its displayed range.
- [ ] The summary appears before save.
- [ ] Cancelling leaves setup incomplete.
- [ ] `Esc` cancels setup without saving and returns to the command center when setup was opened from it.
- [ ] `Ctrl+C` terminates an active prompt cleanly with exit code `130`.
- [ ] Saving persists non-secret settings and reports platform-appropriate key guidance.
- [ ] The optional connection test renders a short user prompt, progress indicator, formatted answer, and token snapshot, then rejects an unexpected response.

## Validate questions, chat, and command control

- [ ] A one-off query creates and closes one session.
- [ ] A one-off query and every completed chat turn display total context plus separate provider input/output token counts.
- [ ] A short chat retains prior turns, displays session cost, and exits cleanly.
- [ ] `/clear` clears active context and adds an audit event.
- [ ] `/run Get-Location` shows a low local risk assessment and exact command preview.
- [ ] Denying authorization executes nothing and records denial.
- [ ] Approving runs exactly once without elevation, displays output, and lets the next AI turn explain it.
- [ ] A destructive-looking command receives a high or critical local score even if AI review is unavailable.
- [ ] The output-sharing warning is visible before approval.
- [ ] A simulated key in command output is visible locally but redacted in SQLite and the next AI prompt.
- [ ] Timeout and output limits are honored.

## Validate usage visibility and local history

- [ ] The first relevant invocation of a local day refreshes public pricing once.
- [ ] `--costs` forces refresh and renders prices in a localized table.
- [ ] Without `OPENAI_ADMIN_KEY`, organization cost reads “not available” without failing local estimates.
- [ ] With a valid admin key in a safe test organization, current-month buckets are persisted and shown separately.
- [ ] `ai_requests` stores separate input, cached-input, cache-write, output, reasoning, total-token, and microdollar fields.
- [ ] `ai_sessions` and `ai_session_events` reconstruct each short session in sequence.
- [ ] `activity_audit.payload_json` remains valid JSON and contains no test secrets.

## Portable publish acceptance

Publish at least one current-machine runtime and launch the resulting `hm` directly. Confirm the `prompt` directory and both PATH helper scripts are beside it. Preview PATH install/status/remove; mutate only a disposable test account or a path you intend to keep.

Nerd Font validation should start with `--dry-run`. The real operation is opt-in and should be tested only when Oh My Posh is already installed.

## Windows release artifact acceptance

```powershell
pwsh -NoProfile -File .\scripts\build-release-artifacts.ps1 -PlanOnly
pwsh -NoProfile -File .\scripts\build-release-artifacts.ps1
```

Confirm that both portable ZIPs contain only `hm.exe`, `prompt/*.yaml`, `LICENSE`, and `THIRD_PARTY_NOTICES.md`; the current-architecture executable reports the requested package version; `winget validate` succeeds; and every checksum in `SHA256SUMS.txt` matches its package. In Windows Sandbox or another disposable environment, confirm that a non-admin MSI install targets `%LOCALAPPDATA%\Programs\PromptMeUp`, registers only the current-user `PATH`, `hm -where` resolves that installed binary, and uninstall removes the installed files and installer-owned PATH entry. Do not install either distribution format or mutate the host `PATH` during routine validation.

## Post-publication cleanup

After every successful commit and every successful push, remove Release build and test intermediates and confirm that cleanup did not change tracked files:

```powershell
dotnet clean .\PromptMeUp.slnx --configuration Release
git status --short
```

Generated installer and portable-package directories are not removed by `dotnet clean`; delete only an explicitly verified obsolete version directory under `artifacts/release`.

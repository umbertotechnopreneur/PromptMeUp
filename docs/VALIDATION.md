# Validation guide

Use a clean terminal and never use production API keys or confidential prompts for validation evidence.

## Automated checks

```powershell
pwsh -NoProfile -File .\scripts\preflight.ps1
dotnet restore .\PromptMeUp.slnx
dotnet format .\PromptMeUp.slnx --verify-no-changes --no-restore
pwsh -NoProfile -File .\scripts\check-xml-comments.ps1
dotnet build .\PromptMeUp.slnx --configuration Release --no-restore --warnaserror
dotnet test .\PromptMeUp.slnx --configuration Release --no-build
```

The manual GitHub Actions workflow runs lint plus cross-platform build only. Tests remain a required local handoff check while the product is in early preview.

## Non-interactive smoke checks

Use a disposable data directory:

```powershell
$env:PROMPTMEUP_DATA_DIR = Join-Path $PWD 'artifacts\smoke-data'
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --help --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --version --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --status --language vi --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --third-party --language fr --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --path=status --no-animation --no-emoji
dotnet run --project .\PromptMeUp\PromptMeUp.csproj --configuration Release -- --install-font --dry-run --no-animation --no-emoji
```

Every command should exit `0`, preserve readable redirected output, and avoid an interactive prompt.

## Interactive setup acceptance

- [ ] A clean first launch opens the double-border green-screen setup.
- [ ] All six languages can be selected and the remaining form changes language immediately.
- [ ] API and admin key values are masked and never reprinted.
- [ ] Model, thinking, detail, custom instruction, coarse location, command review, and prompt caching are visible.
- [ ] Every memory, output, and timeout limit rejects values outside its displayed range.
- [ ] The summary appears before save.
- [ ] Cancelling leaves setup incomplete.
- [ ] Saving persists non-secret settings and reports platform-appropriate key guidance.
- [ ] The optional connection test renders a short user prompt and teletype answer, then rejects an unexpected response.

## Chat and command acceptance

- [ ] A one-off query creates and closes one session.
- [ ] A short chat retains prior turns, displays session cost, and exits cleanly.
- [ ] `/clear` clears active context and adds an audit event.
- [ ] `/run Get-Location` shows a low local risk assessment and exact command preview.
- [ ] Denying authorization executes nothing and records denial.
- [ ] Approving runs exactly once without elevation, displays output, and lets the next AI turn explain it.
- [ ] A destructive-looking command receives a high or critical local score even if AI review is unavailable.
- [ ] The output-sharing warning is visible before approval.
- [ ] A simulated key in command output is visible locally but redacted in SQLite and the next AI prompt.
- [ ] Timeout and output limits are honored.

## Costs and persistence acceptance

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

Confirm that both portable ZIPs contain only `hm.exe`, `prompt/*.yaml`, `LICENSE`, and `THIRD_PARTY_NOTICES.md`; the current-architecture executable reports the requested package version; `winget validate` succeeds; the x64 MSI passes WiX validation; and every checksum in `SHA256SUMS.txt` matches its package. Do not install either distribution format on the host during routine validation.

# PromptMeUp validation guide

Check that people can read the answer, understand the limits, and decide what runs. Use a clean terminal and disposable data. Keep production API keys and confidential prompts out of validation evidence.

For agent-driven work, run automated tests and CLI smoke tests only when the user explicitly requests them. Requests to implement, review, commit, or push changes do not authorize test execution. The non-test checks below remain the default validation gate; test commands and behavioral checklists are available for explicitly requested testing.

## Prove the build is healthy

```powershell
pwsh -NoProfile -File .\scripts\preflight.ps1
dotnet restore .\PromptMeUp.slnx
dotnet format .\PromptMeUp.slnx --verify-no-changes --no-restore
pwsh -NoProfile -File .\scripts\check-xml-comments.ps1
dotnet build .\PromptMeUp.slnx --configuration Release --no-restore --warnaserror
```

When the user explicitly requests the automated test suite:

```powershell
dotnet test .\PromptMeUp.slnx --configuration Release --no-build
```

The local gate verifies formatting without editing files. To fix a reported formatting issue, use `scripts/format.ps1` and review its changes. The GitHub Actions quality gate runs on pushes to `main`, pull requests, and manual dispatch. CI applies and verifies formatting, checks XML comments, then builds, tests, and checks portable packages on Windows, Linux, and macOS.

Regression tests cover quoted/serialized JSON credentials, provider-bound command output, rejected and legacy preambles, Serilog exception privacy, HTTP body deadlines and limits, inherited process pipes, conservative command risk, long-answer visibility, Unix shell context, model-specific pricing bands, and indexed request summaries. HTTP and credential providers are synthetic; process tests run only inert PowerShell output/sleep commands and clean up their test child. The review-to-test mapping is recorded in [the September 2 review](../.github/tasks/review-2026-09-02.md).

## Prove the CLI is predictable

When the user explicitly requests CLI smoke tests, use a disposable data directory:

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
- [ ] A one-off query and every completed chat turn distinguish the estimated retained context, effective input budget, latest provider input/output counts, and cumulative session input/output counts.
- [ ] A short chat retains prior turns, displays session cost, and exits cleanly.
- [ ] `/clear` removes recent messages and adds an audit event while retaining saved notes, latest response metrics, and cumulative session usage.
- [ ] `/run Get-Location` shows a low local risk assessment and exact command preview.
- [ ] Denying authorization executes nothing and records denial.
- [ ] Approving runs exactly once without elevation, displays output, and lets the next AI turn explain it.
- [ ] A destructive-looking command receives a high or critical local score even if AI review is unavailable.
- [ ] The output-sharing warning is visible before approval.
- [ ] A simulated key in command output is visible locally but redacted in SQLite and the next AI prompt.
- [ ] Timeout and output limits are honored.

## Validate the shared settings screen

- [ ] `hm --setup`, `hm --ai-setup` (also `--ai-settings`), and `hm --theme` open the same workspace with General, AI, or Theme selected, including with a fresh data directory.
- [ ] At 60 columns by 20 rows or larger, the left sidebar keeps all seven sections accessible; redirected input/output does not open the interactive form.
- [ ] Switching sections keeps edits in one draft. Save applies changes directly, without a summary or confirmation page. An invocation-only `--language` choice does not change the saved language unless its field is edited.
- [ ] Tab reaches Save and Cancel, Left/Right moves between those buttons, and Enter activates the focused action. F6 moves between sidebar and fields.
- [ ] Theme preview updates live; cancelling restores the previous palette. The compatibility section menu also offers direct Save and Cancel.
- [ ] The context budget accepts 4,000–200,000 tokens and defaults to 16,000.
- [ ] An active `PROMPTMEUP_CONTEXT_TOKENS` override is explained and remains effective without being copied into the saved setting.
- [ ] Cancelling or pressing `Esc` leaves the previous settings intact. Saving does not refresh pricing or send an AI request unless the connection check is enabled; that choice defaults to off after initial setup.

## Validate saved notes and context limits

- [ ] `/remember` saves a project note, `/remember global` saves a shared preference, and `/memories` lists their IDs without an AI call.
- [ ] `/forget <id>` removes a listed note. Malformed IDs, empty notes, notes over 1,000 characters, and recognizable credentials report an error and leave chat open.
- [ ] Notes survive restart, remain available from subdirectories of the same Git root, and stay isolated from other projects. Outside Git, the current directory defines the scope.
- [ ] Saving the same note twice in one scope updates the existing record; a new note is rejected at the 100-note scope limit.
- [ ] Project notes are selected only when words match the question. Global notes remain eligible, but the final selection never exceeds five notes or 800 estimated tokens including its localized wrapper.
- [ ] Selected notes appear once in the outgoing context and cannot authorize a command or override the latest request.
- [ ] `/context` and `/status` retain the last response's input/output counts and show the same current estimate. Local commands do not add provider usage.
- [ ] The context estimate includes populated instructions, runtime details, selected notes, and retained messages, with `~` marking estimates.
- [ ] Older complete turns are pruned to fit the turn limit and effective input budget, with room reserved for the answer.
- [ ] A question that cannot fit is rejected before sending. Chat stays open and accepts a shorter question or `/clear`.
- [ ] Clearing messages or forgetting a note does not erase request history or reset usage already recorded for the session.
- [ ] Schema-v1 and schema-v2 databases upgrade to v3 with existing settings/history intact and the Cyan theme selected; invalid stored note data is reported instead of silently ignored.

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

# PromptMeUp repository instructions

These instructions apply to every change in this repository.

## Product writing and author voice

- Keep MailMeUp, PromptMeUp, and TrackMeUp visually consistent using [the MeUp style guide](docs/assets/meup/README.md). Use a common README structure and author signature, with a distinct accent color and concrete benefit for each product. Keep the main product purpose ahead of optional extras.

- Start each README with a headline that says what the app does and what the reader can use it for. Put the product benefit before architecture, branding, or project history.
- Apply this style throughout repository documentation: plain English, short sentences, concrete actions, and useful examples. Cut filler, vague slogans, hype, corporate language, and formulaic AI-sounding prose.
- Speak to the reader as "you". When speaking as the author, use "I", "me", and "my", never a company-style "we", "us", or "our". Umberto is the solo maintainer, with help from a few contributors; keep their credits accurate.
- Keep technical detail in the relevant reference guides. Preserve exact commands, UI labels, privacy facts, limitations, and the distinction between implemented, tested, and planned features. Do not promise unlimited capacity or untested compatibility.
- Preserve third-party quotations, license text, and historical records; these writing preferences apply to original project copy.

## Distribution and build defaults

- The commercial edition supports Windows 11 on x64 and ARM64 only. Distribute it only as MSIX.
- Linux and macOS are source-only: users must compile and adapt the source themselves. Do not provide compiled binaries, installers, or commercial support for these platforms, and do not imply that every project can run there unchanged.
- When a build is explicitly requested, default to a local development/debug build using the Debug configuration. A generic build or release request does not authorize a Microsoft Store package. Produce a Store version only when the owner explicitly asks for one; never upload or publish it without explicit authorization.
- Keep development/debug artifacts separate from Store release artifacts. These rules do not authorize running builds, changing release pipelines, creating tags, or publishing on their own.
- Windows distribution is MSIX-only. Do not propose or add portable ZIP, standalone EXE, or MSI distribution unless the owner explicitly changes this decision. This preference does not authorize packaging or workflow changes.

## Private business notes

- Keep UI mockups, design explorations, product roadmaps and internal product decisions in the owner's Obsidian product notes, not in this repository. Retain shipped assets, real screenshots, asset provenance and documentation needed to use, build or contribute to the software.

- Keep pricing, commercial strategy, launch plans, future product proposals, OAuth verification preparation, Store account procedures and owner checkpoints in the owner's Obsidian vault under `40_Business/MeUp/`, organized by product. Do not create or mirror these notes in public repositories.
- Keep current user and contributor documentation, public privacy policies and terms, licenses, attribution, build instructions, technical validation records and files required by code or CI in the repository. Split documents that mix public technical guidance with internal planning.
- For publication or Store listing work, first read `40_Business/MeUp/Business decisions.md` and the relevant product notes. The owner-approved purchase notice is preserved there; proposals and approved wording are not evidence of implemented licensing.
- Keep raw Store account exports outside Git. Do not add credentials, private data or machine-specific vault paths to repository files.

## Shared delivery workflow

- Never create a branch, commit, or push on your own initiative. Each action requires an explicit request from the owner; a request to edit files does not authorize Git delivery.
- For documentation-only or repository-instruction-only changes, keep edits local until the owner explicitly requests delivery. If authorized, use the current branch and include `[skip ci]` unless the owner requests CI. Do not treat this rule as permission to bypass repository protections.
- For all other changes, keep `main` protected. Make changes on a focused branch, open a pull request, and use squash merge only after required checks and conversations are resolved. Do not bypass branch protections, required checks, or review requirements for these changes. Delete the branch after a successful merge.
- Create MSIX release artifacts only through GitHub Actions. For explicit local debug testing, a signed MSIX may be built and installed from an ignored `artifacts/msix` subdirectory using the existing current-user certificate; do not upload, publish, tag, or describe it as a release. Create an annotated `v<version>` tag only after the matching source version is on `main`.
- Preserve unrelated working-tree changes. Never commit credentials, tokens, local data, logs, generated artifacts, or private machine paths.

## Context and token efficiency

- Do not read more than five files for a task without explicit user approval. If additional context is needed, ask first; this limit prevents whole-repository reading.
- Warn the user before an operation that could theoretically consume a large number of tokens, including broad repository reads, unbounded searches, or large output dumps.

- Read only task-relevant files and documentation sections; expand scope when dependencies or uncertainty require it.
- Reuse context already read. Re-read only when files changed, context is missing, or fresh evidence is needed.
- Start with scoped `rg` searches, then read relevant excerpts. Exclude generated files and bound command output; retrieve more only when needed.
- Batch independent read-only queries. Avoid repeated repository-wide scans or full file and log dumps.
- Make the smallest complete change; avoid unrelated refactoring, cleanup, documentation churn, or speculative abstractions.
- Use subagents only when explicitly requested.
- Keep progress updates focused on findings or blockers; report results, verification status, and remaining work concisely.

## Product boundaries

- PromptMeUp is a lightweight .NET 10 console assistant whose public command is `hm`.
- Skills, memory collection, proposals, and reminders are global. Do not introduce project-scoped labels, groups, availability, configuration, storage, or retention in `hm --skills`, `hm --learning`, or their Setup controls.
- Keep the application code portable across Windows, Linux, and macOS. Windows distribution is MSIX-only. Do not introduce a background agent or platform-specific runtime dependency.
- Keep GitHub as the project home and write public copy in product language before implementation detail.
- Use plain, friendly English in documentation: speak directly to the reader, use short sentences and practical examples, and explain technical terms when needed. Keep factual credits and privacy details accurate.
- Write repository documentation, README copy, contributor guidance, code comments, and project artwork text in English. Keep runtime UI and prompt translations in all six supported languages.
- Preserve the separation between `Models`, `Services`, `Views`, and the `Application` orchestrator.
- Runtime AI instructions belong in `/prompt` as versioned YAML with metadata and all six supported languages: `it`, `en`, `fr`, `de`, `es`, `vi`.

## Command safety and privacy

- Keep deterministic local risk scoring active. Optional AI review is advisory and cannot authorize execution.
- Run application-authorized commands without elevation through `pwsh -NoProfile -NonInteractive`, with timeout and bounded output.
- `--yes` must never authorize chat commands.
- Never accept secrets as command-line values or store them in settings, SQLite, Serilog, tests, screenshots, or documentation.
- Redact recognizable credentials before prompt/response persistence and before command output is sent to an AI follow-up.
- Preserve the user's full local command preview; redaction applies to persistence and provider-bound content, not the local review surface.

## Code and documentation

- PromptMeUp is in pre-production. Backward compatibility is not required: update affected callers and contracts directly instead of adding compatibility wrappers or legacy paths. Reuse shared workflows and helpers; keep differences between normal and direct command approval in their interaction views.
- `--direct` requires local risk scoring and a successful AI review. High or critical risk must block execution. Preview each eligible command and use a five-second cancellable countdown instead of a confirmation prompt, then reuse the normal output-analysis and conversation flow.
- Direct execution is enabled by default for questions and chat. Keep its persisted opt-out in `hm --setup`; `--direct` overrides that preference for the current session. Keep the session summary hidden by default during work, show it at exit, and override hiding when operating context reaches 80 percent. Explicit `/status` and `/context` requests must still show it.
- Add a brief XML `<summary>` to every C# implementation method, including constructors, tests, and private helpers.
- Add small inline comments only where a complex or non-obvious algorithm benefits from a logic hint.
- Use `ILogger<T>` in application code; keep Serilog configuration in the composition root.
- Keep views passive and free of HTTP, SQLite, secret-store, and process-execution behavior.
- Keep flexible audit/session data valid JSON and normalized usage/cost data in typed columns.
- Preserve terminal scrollback in every application flow: never clear the main terminal screen or scrollback. Fullscreen views may redraw a disposable alternate buffer and must restore the main buffer on exit. Mark new scrolling flows with intentional whitespace and accessible separators.
- Treat contrast as a product requirement: do not render user-facing information in dark grey. Use the shared terminal palette, with bright primary text and only high-contrast muted nuances for secondary metadata.
- Render every user-facing emoji with one visible space after it and a separating space when preceded by text. Do not add a leading space when an emoji starts a line or label; preserve intentional layout indentation and keep emoji spacing in shared view helpers.
- Do not use cards, boxed panels, or decorative enclosing frames unless the user explicitly requests them for that surface. Use open layouts with headings, grids, spacing, and separators instead.
- Use Spectre.Console layout primitives purposefully (for example grids, rules, and selection prompts) to convey hierarchy; do not reduce command, help, or status surfaces to undifferentiated text walls.
- Arrange form fields in two columns with right-aligned label text and left-aligned value text, followed by one blank row. Keep action bars and shortcut hints unboxed, with a single separator before the actions; use semantic button colors and a visible focus marker.
- Keep repository-wide rules in `AGENTS.md`; `.github/copilot-instructions.md` points here instead of duplicating them.
- Commit exclusions include `.env`, local databases, `bin/`, `obj/`, `artifacts/`, and `.vs/`.

## Working practices

- Read `AGENTS.md` before editing; no additional Copilot instruction read is needed when these rules are already in context.
- Obtain explicit user authorization before creating any Git branch or worktree.
- When creating a pull request, assign it to `umbertotechnopreneur` and add the existing repository labels that match its final scope.
- Use PowerShell 7 as `pwsh -NoProfile` for Windows automation.
- Use `apply_patch` for deliberate tracked-file edits and avoid broad formatting churn.
- Fail fast on invalid input and unsupported state; do not conceal failures with silent fallbacks.
- Run automated tests and CLI smoke tests only when the user explicitly requests them. Implementation, review, commit, and push requests do not authorize test execution.
- If this task attempted a build, clean its generated output once after the last build, requested test, packaging, or installation step, including after failure and before handoff. Use `dotnet clean .\PromptMeUp.slnx --configuration Release` with matching configuration, runtime, and output paths. Preserve requested deliverables and unrelated artifacts. Commit or push alone does not require cleanup.
- Record active work in `.github/tasks/todo.md` and move completed work to `.github/tasks/archive.md`.

## Required validation

Choose non-test checks by the changed files and run them once after the final relevant edit, before committing. Repeat only after relevant edits or to diagnose failures.

- Documentation-only or instruction-only changes: inspect the scoped diff and staging; skip preflight, restore, formatting, XML checks, build, and cleanup.
- C# or project/dependency changes: run the applicable checks below. Run XML checks for C# changes and restore only when dependency assets are missing or stale.
- Script, workflow, or runtime prompt changes: use applicable syntax, schema, or preflight checks; build only when compilation or packaging is affected.
- Automated tests and smoke tests still require an explicit user request. Required CI checks and branch protections remain unchanged.

Non-test commands for code changes:

```powershell
pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command preflight
dotnet restore .\PromptMeUp.slnx
dotnet format .\PromptMeUp.slnx --verify-no-changes --no-restore
pwsh -NoProfile -File .\scripts\PromptMeUp.ps1 -Command check-xml
dotnet build .\PromptMeUp.slnx --configuration Release --no-restore --warnaserror
```

Only when explicitly requested by the user, run the appropriate automated tests, such as `dotnet test .\PromptMeUp.slnx --configuration Release --no-build`, or proportionate CLI smoke tests with a disposable `PROMPTMEUP_DATA_DIR`. Run these checks before cleanup when they need the build output. Do not use real secrets or mutate PATH/fonts during validation.

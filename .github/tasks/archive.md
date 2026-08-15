# Task Archive

This archive tracks completed development tasks for reference and review.

## 2026-08-16 — Readability, resilience, and terminal UX refactor

- Corrected ordered multi-token query parsing and made SQLite initialization version-safe, transactional, repairable, and WAL-consistent.
- Extracted AI conversation, authorized-command, provider request/response, and SQLite schema responsibilities from oversized services without weakening command authorization or audit boundaries.
- Fixed one-shot query visibility and prevented scalar or malformed optional Costs API errors from aborting the requested AI operation.
- Rebuilt the terminal experience around compact frameless headings, whitespace, color, responsive status lines, staged setup, optional advanced settings, and secret input that reveals neither value nor length.
- Added `Esc` current-flow cancellation, fail-closed authorization cancellation, and `Ctrl+C` application shutdown; exact `/run` without a command now fails locally instead of reaching the model.
- Removed confirmed-unused PowerShell helpers and redundant contract members, automated the multi-platform quality gate, and documented post-commit/push cleanup.

Validation: preflight, restore, format verification, XML comments, Release build with 0 warnings/errors, 55/55 integrated tests, read-only CLI commands in disposable data directories, and live `Esc` / `Ctrl+C` prompt smokes passed.

## 2026-08-16 — Windows release artifact builder

- Added one PowerShell entry point with a read-only plan mode, bounded release output, and fail-fast prerequisite checks.
- Added deterministic self-contained `win-x64` and `win-arm64` ZIP archives for WinGet, multi-file schema 1.12 manifests, package hashes, and a machine-readable release summary.
- Added an optional per-user x64 MSI built and ICE-validated with WiX Toolset 3.14, including upgrade metadata, `%LOCALAPPDATA%\Programs\PromptMeUp` installation, and installer-owned user PATH registration.
- Added `hm --where` / `hm -where` with exact executable reporting, a copyable change-directory command, and an explicitly previewed and confirmed native file-manager action in all six UI languages.
- Added current-architecture version and executable-location smoke testing and excluded helper scripts, secrets, local data, logs, symbols, and build intermediates from distributable payloads.
- Added successful-build cleanup so `artifacts/release/<version>` retains only packages, checksums, WinGet manifests, and the release summary needed for user testing.
- Documented local WinGet installation, direct MSI testing, prerequisites, output layout, and the portable-first product boundary.

Validation: release `0.1.1` generation and `winget validate` passed; MSI tables confirmed `LocalAppDataFolder`, installer-owned child-directory cleanup, and user PATH registration; a silent upgrade from `0.1.0` returned exit code `0`, removed the old ProductCode, installed `0.1.1`, and left no machine PATH entry; packaged and installed `hm -where` smokes passed; the integrated quality gate passed 55/55 tests.

## 2026-08-12 — Initial PromptMeUp product foundation

- Created the .NET 10 `hm` console product with Spectre.Console and Serilog through `ILogger<T>`.
- Added six-language setup, query/chat flows, short memory, OpenAI Responses integration, prompt caching, token/context/cost status, and YAML runtime prompts.
- Added mandatory command preview/authorization, local and optional AI risk review, bounded PowerShell execution, and credential redaction.
- Added SQLite settings, normalized usage/pricing/costs, flexible session events, and activity audit storage.
- Added portable PATH management, an opt-in Nerd Font helper, MIT/public-repository documentation, tests, and a manual build/lint workflow.

Validation is recorded in `docs/VALIDATION.md` and the publication commit history.

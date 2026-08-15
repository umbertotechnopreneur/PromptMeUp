# Task Archive

This archive tracks completed development tasks for reference and review.

## 2026-08-16 — PromptMeUp 0.1.4 premium UI audit installer

- Bumped the product and current packaging examples from `0.1.3` to `0.1.4` for the completed cross-screen terminal UI audit.
- Built and independently checksum-verified the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, and validated WinGet manifests.
- Upgraded the local per-user installation to `0.1.4` without closing the existing `hm` session or requiring a reboot.
- Verified the installed binary and Windows Installer registration, exactly one user PATH entry, zero machine PATH entries, and an unchanged machine PATH.

Validation: full repository gate passed with 74/74 tests and zero build warnings/errors; installed version is `0.1.4.0`; MSI SHA-256 is `6B6A857CC458D37100E024111BF7D7DFC67C49D8434A853A4C0D39126FFAB62C`.

## 2026-08-16 — Cross-screen terminal UI audit

- Audited every terminal view for decorative cards, icon spacing, separator width, alignment, semantic colors, completed progress residue, and hard-coded user-facing copy.
- Centralized frameless right-label/left-value grids and applied them to status, session, setup, About, PATH, executable location, costs, and command-result metadata.
- Standardized emoji spacing and ASCII fallbacks, 80%-width section rules, semantic success/warning states, and clean Nerd Font dry-run/unsupported results.
- Localized setup validation, help placeholders, PATH previews, font states, and all command-line parser errors in Italian, English, French, German, Spanish, and Vietnamese.
- Added regression coverage for frameless status and command-result grids, localized font dry runs, localized parser errors, chat guidance, and Markdown-safe teletype output.

Validation: full repository gate passed with 74/74 tests, zero build warnings/errors, complete XML summaries, static view scan, and isolated help, status, PATH, font dry-run, and localized-error smoke tests.

## 2026-08-16 — Premium terminal hierarchy and chat feedback

- Removed the remaining decorative cards, standardized 80%-width separators with intentional leading whitespace, and guaranteed one visible space after every icon.
- Rebuilt status and session summaries as frameless label-value grids with right-aligned labels, left-aligned values, responsive status rows, and at most two session rows on standard wide terminals.
- Stacked active progress beneath its status text and made the complete progress surface disappear when work ends.
- Added a localized five-command chat guide, bounded prompt validation, used/maximum/remaining character feedback, consistent automatic user turns, and a Markdown-safe teletype response effect.
- Switched cost bands to semantic foreground colors, localized command risk and execution metadata, improved the command-center hierarchy, and added a default `Non fare nulla` executable-location action.

Validation: full repository gate passed with 70/70 tests, zero build warnings/errors, XML summaries complete, and isolated status plus executable-location smoke tests passed.

## 2026-08-16 — PromptMeUp 0.1.3 frameless UX installer

- Bumped the product and documented packaging examples from `0.1.2` to `0.1.3` for the frameless command-review refinement.
- Built and checksummed the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, and validated WinGet manifests.
- Upgraded the local installation from `0.1.2` to `0.1.3`; verified the installed binary, WinGet registration, one user PATH entry, and unchanged machine PATH.

Validation: full repository gate passed with 68/68 tests; MSI SHA-256 is `99674769B454C354E8B4CEA793792135FF88FE429900F86B9DF65D75BC78092E`; installer exit code was `0`.

## 2026-08-16 — Frameless command-review refinement

- Replaced cards in the command suggestion, command authorization, command result, shell header, session snapshot, and chat introduction with spacious emoji-led sections.
- Standardized every decorative section divider to end at 80% of the current terminal width and compacted session data into two metric rows.
- Added semantic low/medium/high/critical risk indicators, a structured command-result view, single-line cancellation notices, and a one-line assistant-plus-response-heading treatment.

## 2026-08-16 — PromptMeUp 0.1.2 installer refresh

- Bumped the product, assembly, package, and documented release version from `0.1.1` to `0.1.2` for the completed terminal UX and scoped-AI release.
- Built the portable `win-x64`/`win-arm64` archives, per-user `win-x64` MSI, checksums, release manifest, and validated WinGet manifests.
- Verified the MSI SHA-256 before installation, upgraded the local per-user installation from `0.1.1` to `0.1.2`, and confirmed the installed `hm.exe`, About metadata, uninstall registration, and fresh PowerShell command resolution.
- Confirmed the installer keeps exactly one user PATH entry and does not alter the machine PATH.

Validation: release build and `winget validate` passed; MSI upgrade returned exit code `0`; installed file version is `0.1.2.0`; `hm --version --no-animation --no-emoji` reports the version, GitHub repository, and creator site.

## 2026-08-16 — Premium Spectre Console and scoped AI assistance

- Preserved terminal scrollback across every application flow, with deliberate whitespace around invocations and an interrupted prompt's cancellation message rendered on its own line.
- Rebuilt the shared shell, chat, Markdown renderer, help, version/About page, setup, status, and costs screens around accessible Spectre panels, grids, semantic chips, progress, emoji/ASCII fallbacks, and a high-contrast shared palette.
- Added a branded `--version` About panel with the GitHub repository and creator site; grouped help by task category; and turned `--costs` into a semantic cost dashboard.
- Added strict structured responses for chat and one-shot queries, a Markdown command-candidate menu with safe default **Do not execute commands**, an `Avvia chat` continuation, and continued use of the existing exact-preview/risk/explicit-authorization gate.
- Added localized console-only chat/query system instructions and privacy-filtered runtime context (working directory, platform/shell, CPU, RAM, GPU), including network-path withholding and no generic-writing/image-generation behavior.
- Aligned GPT-5.6 prompt caching to reuse shape: stable explicit prefixes, implicit append-only chat checkpoints, explicit-only one-shot query caching, provider cache read/write metrics, and a post-response snapshot containing total context plus input/output tokens.

Validation: preflight, restore, formatter apply/verification, XML summary check, Release build with 0 warnings/errors, 68/68 integrated tests, `git diff --check`, and non-interactive `--version`, `--help`, and `--status` smoke checks passed.

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

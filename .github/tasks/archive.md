# Task Archive

This archive tracks completed development tasks for reference and review.

## 2026-09-02 — Repair PR #10 CI fixture failures

- Investigated the first PR quality run: Lint and macOS passed, while Windows exposed global SQLite pool cleanup racing other fixtures and Linux timed out before the inherited-pipe fixture emitted its expected marker.
- Replaced both global pool clears with cleanup scoped to the fixture's exact database connection pool, retaining parallel test execution.
- Removed PowerShell module discovery from the process fixture, wrote and flushed markers directly, and allowed CI startup headroom. The deadline remains 10 seconds, the return assertion remains below 20 seconds, and the independent child lifetime is 30 seconds, so waiting for the child still fails the regression.
- Preserved production code, timeout semantics, and all output/cancellation assertions; prepared the fix for the existing PR and its native CI matrix.

Validation: the full local gate passed with 148/148 tests and zero build warnings/errors. The parallel SQLite regression subset passed ten consecutive runs; isolated help/version CLI smoke checks passed. Native CI outcomes are recorded in PR #10 checks.

## 2026-09-02 — Resolve ten verified code-review findings

- Completed the privacy, deadline, chat/risk/shell, pricing, and summary plan; every finding in [the review](review-2026-09-02.md) now maps to tracked regression coverage.
- Protected quoted and serialized JSON credentials, rejected new credential-bearing preambles, sanitized legacy settings before use, and removed raw exception details from persistent diagnostics.
- Bounded complete HTTP/process operations, preserved completed assistant answers, corrected diagnostic risk classification and Unix shell instructions, and selected model-specific pricing bands from actual input usage.
- Restricted summary reads to the indexed month interval and preserved successful-request totals at period boundaries.
- Added 58 regression cases and updated privacy, architecture, costs, and validation guidance. Preserved the repository presentation and release work already merged through PR #9.

Validation: full required gate passed with 148/148 tests and zero build warnings/errors; isolated help, version, Italian/Vietnamese status, and French attribution smoke checks passed. No real credentials or AI requests were used. The OS-temporary smoke directory was retained because automatic approval review blocked its deletion; it is outside the repository and excluded from publication.

## 2026-09-02 — MIT repository, product presentation, and portable CI/CD

- Preserved the MIT license and upstream attribution, including transitive dependency notices and the exact self-contained runtime's notices in release packages.
- Added maintainer governance, CODEOWNERS, reproducible main/tag protection settings, dependency review, pinned Actions, and a six-target portable release workflow with checksums, provenance attestations, and a draft publication step.
- Enabled private vulnerability reporting and applied main protection to administrators, required checks, pull requests, linear history, and resolved conversations; blocked changes and deletion of version tags.
- Rebuilt the English README around the weekend-project story and UmbertoGiacobbiDotBiz team, with a retro banner, three explicitly illustrative screen renderings, and links to TrackMeUp, viewsapp.ai, and the creator's website.
- Aligned the English repository-writing rule in AGENTS.md and GitHub Copilot instructions. Preserved the other agent's application-code work.

Validation: the full required gate passed on an isolated baseline plus these changes, with 90/90 tests and zero Release-build warnings/errors. The Windows x64 portable archive passed credential-free startup/help/attribution smoke checks; all inventory notice references resolved inside the ZIP. GitHub-flavored Markdown rendering, local links, PowerShell parsing, and actionlint checks passed. The shared worktree's initial format check detected in-progress C# edits from the concurrent code task; those edits were excluded from this change.

## 2026-09-02 — Guided error diagnosis

- Added bounded text, UTF-8 log file, and stdin diagnosis with credential redaction and strict source validation.
- Added an evidence-led prompt and UI text in all six languages; reused exact per-command approval for follow-up checks.
- Validated parser boundaries, cancellation, redaction, Release build/tests, and isolated CLI smoke checks.
- Delivered as the first feature commit and dedicated PR in the terminal assistance series.

## 2026-08-16 — PromptMeUp 0.1.5 protected-preamble installer

- Bumped the product and current packaging examples from `0.1.4` to `0.1.5` for the multilingual protected-preamble release.
- Built and validated the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, checksums, and WinGet manifests.
- Upgraded the current-user installation from `0.1.4` to `0.1.5` silently without elevation or restart.
- Verified all seven installed payload files byte-for-byte against the x64 ZIP, MSI registration `0.1.5`, exactly one user PATH entry, zero machine PATH entries, and `hm --version` / `hm -where` output.

Validation: full repository gate passed with 90/90 tests and zero build warnings/errors; MSI SHA-256 is `1E7E19B2976897ADD724B47DE07F1B195657F94C80FBA71096D4449F6469671C`.

## 2026-08-16 — Multilingual AI preamble protection

- Reframed the optional setup instruction as a preamble appended to every user-facing chat and one-off query, with a hard 500-word limit and used/maximum/remaining word feedback.
- Added deterministic Unicode normalization and multilingual prompt-injection screening for Italian, English, French, German, Spanish, and Vietnamese, including instruction overrides, role forgery, prompt extraction, and delimiter breakout attempts.
- Added provider-bound defense in depth through explicit untrusted-data delimiters and versioned localized YAML rules that prevent the preamble from weakening system instructions or authorizing commands.
- Preserved existing SQLite compatibility, sanitized preambles before persistence and again before provider-bound prompt assembly, and documented the feature and its non-infallible security boundary.

Validation: full repository gate passed with 90/90 tests and zero build warnings/errors; isolated `--help` and `--status` smoke tests passed with a disposable data directory that was removed afterward.

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

## 2026-09-01 — Public open-source readiness

- Confirmed the public repository's canonical MIT license, package metadata, third-party notices, security policy, contribution guide, and cross-platform quality gate.
- Added Dependabot for NuGet and GitHub Actions, CodeQL analysis for C#, structured bug and feature forms, a pull-request template, and a public support path through GitHub Discussions.
- Enabled GitHub Discussions; disabled the duplicate Wiki; enabled Dependabot alerts and automatic security updates; and added public discovery topics and standard labels.
- Protected `main` with up-to-date required quality checks, pull requests, resolved conversations, linear history, and blocked force-pushes and deletions. Administrative bypass remains available for repository recovery.
- Ignored Windows `desktop.ini` noise and kept the optional MSI release path explicitly dependent on WiX Toolset 3.14 rather than treating it as an open-source prerequisite.

Validation: required repository gate passed with zero build warnings/errors and 90/90 tests; isolated `--version` and `--third-party` smoke tests passed with a disposable data directory; `git diff --check` passed. GitHub settings were read back after application.

## 2026-09-01 — Product motto

- Added the public motto `Yet another CLI AI assistant :-)` beneath the README title and in the terminal About surface for every language.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; the non-interactive `--version` smoke test rendered the exact motto; `git diff --check` passed.

## 2026-09-01 — Product-led public documentation

- Reframed the README, contributor, support, security, CLI, architecture, privacy, cost, validation, packaging, and runtime-resource documentation around user outcomes and durable product promises.
- Kept technical, licensing, privacy, and command-authorization details explicit while moving implementation mechanics behind the experience they support.
- Corrected the README navigation anchor, described `hm` as the cross-platform public command, and aligned contributor wording with portable archives as the canonical distribution plus optional platform installers.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; isolated non-interactive `--help` and `--version` smoke tests passed; `git diff --check` passed.

## 2026-09-01 — Cross-platform quality-gate repair

- Declared CRLF checkout behavior for C# files in `.gitattributes`, matching the existing repository-wide `.editorconfig` contract on Linux as well as Windows.
- Made the animated Markdown test strip host-emitted ANSI control sequences before asserting semantic text, while continuing to reject raw Markdown markers.
- Kept production rendering unchanged; the failure was isolated to checkout policy and test-output normalization.

Validation: full repository gate passed locally with zero build warnings/errors and 90/90 tests; the previously failing animation test passed in isolation; Git resolved `TerminalViewTests.cs` as `text eol=crlf`; `git diff --check` passed.

## 2026-09-02 — Synchronize local main without merge commits

- Backed up the 24 locally changed or added files and confirmed that their changes were already included in the remote tree; three-way comparisons of the nine differing documentation files matched the remote exactly.
- Fast-forwarded `main` from `6da473d` to `89ec9f0`, preserving the local work without creating commits, branches, or worktrees.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; isolated `--help`, `--version`, Vietnamese `--status`, and French `--third-party` smoke tests passed with a disposable data directory. `main` matched `origin/main`, and the fetched range contained no merge commits.

## 2026-09-02 — Ten actionable code-review findings

- Reviewed the current application at `89ec9f0` and recorded ten distinct findings in [the review report](review-2026-09-02.md), including source references, reproduction evidence, impact, and recommended corrections.
- Confirmed three credential-handling failures, two timeout failures, misleading local risk classification, lost assistant answers, a shell mismatch, ignored long-context prices, and an unnecessary historical table scan.
- Reproduced all ten cases in an ignored local probe project with isolated SQLite data and simulated HTTP responses; no real AI calls, credentials, PATH changes, or font changes were used.
- Preserved concurrent repository work and left application code, existing tests, and runtime prompts unchanged.

Validation: ten dedicated defect reproductions confirmed; the full repository gate passed with zero build warnings/errors and 90/90 existing tests. Isolated `--help` and `--version` smoke tests passed. The existing suite does not cover the reproduced failure cases.

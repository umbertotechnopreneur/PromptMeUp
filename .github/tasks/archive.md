# Task Archive

This archive tracks completed development tasks for reference and review.

## 2026-08-16 — Windows release artifact builder

- Added one PowerShell entry point with a read-only plan mode, bounded release output, and fail-fast prerequisite checks.
- Added deterministic self-contained `win-x64` and `win-arm64` ZIP archives for WinGet, multi-file schema 1.12 manifests, package hashes, and a machine-readable release summary.
- Added an optional machine-wide x64 MSI built and ICE-validated with WiX Toolset 3.14, including upgrade metadata and installer-owned system PATH registration.
- Added current-architecture executable smoke testing and excluded helper scripts, secrets, local data, logs, symbols, and build intermediates from distributable payloads.
- Documented local WinGet installation, direct MSI testing, prerequisites, output layout, and the portable-first product boundary.

Validation: release generation and `winget validate` passed; ZIP hashes were stable across repeated builds; MSI decompilation confirmed x64 metadata, payload, upgrade codes, and PATH registration; preflight, restore, format verification, XML comments, Release build with warnings as errors, and 15/15 tests passed.

## 2026-08-12 — Initial PromptMeUp product foundation

- Created the .NET 10 `hm` console product with Spectre.Console and Serilog through `ILogger<T>`.
- Added six-language setup, query/chat flows, short memory, OpenAI Responses integration, prompt caching, token/context/cost status, and YAML runtime prompts.
- Added mandatory command preview/authorization, local and optional AI risk review, bounded PowerShell execution, and credential redaction.
- Added SQLite settings, normalized usage/pricing/costs, flexible session events, and activity audit storage.
- Added portable PATH management, an opt-in Nerd Font helper, MIT/public-repository documentation, tests, and a manual build/lint workflow.

Validation is recorded in `docs/VALIDATION.md` and the publication commit history.

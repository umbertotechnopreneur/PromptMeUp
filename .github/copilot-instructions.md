---
name: PromptMeUp repository rules
description: Product, safety, architecture, documentation, and validation rules for PromptMeUp.
alwaysApply: true
---

# PromptMeUp repository rules

- PromptMeUp is a lightweight, portable .NET 10 console assistant exposed as `hm`. Portable archives remain canonical; release automation may also produce optional, versioned Windows installer artifacts. Do not add a background agent or platform-specific runtime dependency.
- Keep GitHub as the project home and write public copy in product language before implementation detail.
- Preserve `Models` / `Services` / `Views` / `Application` boundaries. Views never own HTTP, SQLite, secret storage, or process execution.
- Put runtime AI instructions in `/prompt` as versioned metadata-rich YAML with `it`, `en`, `fr`, `de`, `es`, and `vi` text.
- Every shell command requires exact preview and explicit per-command authorization. Local risk scoring always runs; AI review is advisory only.
- Execute application-authorized commands without elevation through `pwsh -NoProfile -NonInteractive`, with timeout and bounded output. `--yes` must never authorize chat commands.
- Never accept or persist secrets through command arguments, settings, SQLite, logs, tests, screenshots, or docs. Redact recognizable credentials before persistence or provider-bound command output.
- Use `ILogger<T>` in application code and configure Serilog only in the composition root.
- Add a brief XML `<summary>` to every C# implementation method, including constructors, tests, and private helpers. Add inline comments only as small hints for non-obvious logic.
- Keep typed usage/cost fields normalized and flexible audit/session payloads valid JSON.
- Preserve terminal scrollback in every application flow: never call a terminal clear operation. Mark new flows with intentional whitespace and accessible separators instead.
- Treat contrast as a product requirement: do not render user-facing information in dark grey. Use the shared terminal palette, with bright primary text and only high-contrast muted nuances for secondary metadata.
- Use Spectre.Console layout primitives purposefully (for example panels, grids, rules, and selection prompts) to convey hierarchy; do not reduce command, help, or status surfaces to undifferentiated text walls.
- Preserve unrelated work, obtain explicit user authorization before creating any Git branch or worktree, avoid broad formatting churn, use `pwsh -NoProfile` for PowerShell automation, and fail fast on invalid or unsupported state.
- After every successful commit and every successful push, run `dotnet clean .\PromptMeUp.slnx --configuration Release` and verify `git status --short`; keep the full validation gate before committing.
- Keep `AGENTS.md` and this file aligned when repository-wide rules change.
- Exclude credentials, `.env`, SQLite data, logs, private absolute paths, `bin/`, `obj/`, `artifacts/`, and `.vs/` from commits.

Before handoff, run preflight, `dotnet format --verify-no-changes`, `scripts/check-xml-comments.ps1`, Release build with warnings as errors, tests, and safe CLI smoke checks with a disposable `PROMPTMEUP_DATA_DIR`.

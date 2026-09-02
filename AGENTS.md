# PromptMeUp repository instructions

These instructions apply to every change in this repository.

## Product boundaries

- PromptMeUp is a lightweight .NET 10 console assistant whose public command is `hm`.
- Keep it portable across Windows, Linux, and macOS. Portable archives remain canonical; release automation may also produce optional, versioned Windows installer artifacts. Do not introduce a background agent.
- Keep GitHub as the project home and write public copy in product language before implementation detail.
- Write repository documentation, README copy, contributor guidance, code comments, and project artwork text in English. Keep runtime UI and prompt translations in all six supported languages.
- Preserve the separation between `Models`, `Services`, `Views`, and the `Application` orchestrator.
- Runtime AI instructions belong in `/prompt` as versioned YAML with metadata and all six supported languages: `it`, `en`, `fr`, `de`, `es`, `vi`.

## Command safety and privacy

- No shell command may run without an exact preview and explicit authorization for that one command.
- Keep deterministic local risk scoring active. Optional AI review is advisory and cannot authorize execution.
- Run application-authorized commands without elevation through `pwsh -NoProfile -NonInteractive`, with timeout and bounded output.
- Never accept secrets as command-line values or store them in settings, SQLite, Serilog, tests, screenshots, or documentation.
- Redact recognizable credentials before prompt/response persistence and before command output is sent to an AI follow-up.
- Preserve the user's full local command preview; redaction applies to persistence and provider-bound content, not the local review surface.

## Code and documentation

- Add a brief XML `<summary>` to every C# implementation method, including constructors, tests, and private helpers.
- Add small inline comments only where a complex or non-obvious algorithm benefits from a logic hint.
- Use `ILogger<T>` in application code; keep Serilog configuration in the composition root.
- Keep views passive and free of HTTP, SQLite, secret-store, and process-execution behavior.
- Keep flexible audit/session data valid JSON and normalized usage/cost data in typed columns.
- Preserve terminal scrollback in every application flow: never call a terminal clear operation. Mark new flows with intentional whitespace and accessible separators instead.
- Treat contrast as a product requirement: do not render user-facing information in dark grey. Use the shared terminal palette, with bright primary text and only high-contrast muted nuances for secondary metadata.
- Use Spectre.Console layout primitives purposefully (for example panels, grids, rules, and selection prompts) to convey hierarchy; do not reduce command, help, or status surfaces to undifferentiated text walls.
- Keep `AGENTS.md` and `.github/copilot-instructions.md` aligned when repository-wide rules change.
- Do not commit credentials, `.env` files, local databases, logs, build output, private absolute paths, or generated artifacts.

## Working practices

- Read this file and `.github/copilot-instructions.md` before editing.
- Preserve unrelated working-tree changes and keep changes scoped and reviewable.
- Obtain explicit user authorization before creating any Git branch or worktree.
- Use PowerShell 7 as `pwsh -NoProfile` for Windows automation.
- Use `apply_patch` for deliberate tracked-file edits and avoid broad formatting churn.
- Fail fast on invalid input and unsupported state; do not conceal failures with silent fallbacks.
- After every successful commit and every successful push, run `dotnet clean .\PromptMeUp.slnx --configuration Release` and verify `git status --short`; keep the full validation gate before committing.
- Record active work in `.github/tasks/todo.md` and move completed work to `.github/tasks/archive.md`.

## Required validation

```powershell
pwsh -NoProfile -File .\scripts\preflight.ps1
dotnet restore .\PromptMeUp.slnx
dotnet format .\PromptMeUp.slnx --verify-no-changes --no-restore
pwsh -NoProfile -File .\scripts\check-xml-comments.ps1
dotnet build .\PromptMeUp.slnx --configuration Release --no-restore --warnaserror
dotnet test .\PromptMeUp.slnx --configuration Release --no-build
```

Run proportionate CLI smoke tests with a disposable `PROMPTMEUP_DATA_DIR`. Do not use real secrets or mutate PATH/fonts during routine validation.

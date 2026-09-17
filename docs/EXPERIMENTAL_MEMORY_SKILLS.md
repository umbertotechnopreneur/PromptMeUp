# Experimental skills and memory

This experiment adds reusable terminal skills and reviewed learning to PromptMeUp. All features start disabled. OpenAI remains the AI provider, and the existing command approval workflow remains the execution boundary.

## Delivery steps

1. Record the scope and reuse decisions on the experimental branch.
2. Add a validated skill catalog with explicit activation and platform requirements.
3. Connect skill actions to local risk assessment, preview, and approval.
4. Deliver Git, filesystem, and concat-files skills and bounded ZIP import.
5. Add bounded contextual skill selection with visible activation.
6. Store opt-in observations and reviewable proposals separately from approved memories.
7. Reflect across observations with Dream, retaining source references.
8. Propose memory maintenance through a manual heartbeat and an optional due reminder.

## Reuse decisions

The companion CLI-Intelligence code is supplied by the project author. Reuse its skill metadata, catalog precedence, extraction records, reflection concepts, and maintenance timestamps. Adapt parsing to YamlDotNet and persistence to SQLite. Use PromptMeUp's command runner, credential redaction, localization, passive views, and OpenAI request accounting.

Keep UI rendering and provider-specific OpenRouter/Llama code out of the port. Do not import personal memories, machine configuration, or generated output. The metals-dev-monitor package is explicitly excluded.

## Skills checkpoint

Open `hm --skills` in a live terminal and enable the experiment for the current project. Inspect a package and explicitly enable its exact contents. Choose it for subsequent questions, or turn on contextual selection. Active skills are named before the answer. Changing any package file invalidates its activation.

Git provides status, log, and diff actions. Filesystem provides bounded list, read, and info actions. Concat-files takes a JSON object such as `{"InputFolder":".","OutputFolder":"./review-output"}`. It scans at most 10,000 entries and 8 MiB of selected source, skips linked entries, and writes uniquely named UTF-8 bundles. Generated bundles can contain private source; review them before sharing.

Import accepts a ZIP containing exactly one skill directory with `SKILL.md`, optional documentation, and standalone PowerShell scripts under `scripts/`. Limits are 128 entries, 64 KiB per file, and 1 MiB total. Imports start disabled and cannot overwrite a local package. Scripts depending on `$PSScriptRoot` or `$PSCommandPath` are not supported by the snapshot runner. Script parameters must be a JSON object containing only scalar values and no credentials.

Local packages in the application data directory override bundled packages with the same name. Activation is project-scoped. Import displays script contents before confirmation; running a script previews the full inspected source and parameters through the existing command approval workflow. `--yes` does not approve it.

Internal skill instructions and the low-trust skill context envelope are versioned YAML in all six languages. User-imported instructions remain user content. Legacy `[TOOL: ...]` examples are reference material, not executable calls.

## Validation record

Run the applicable repository non-test checks before code commits. Automated tests and CLI smoke tests require a separate explicit request. Portable releases remain exclusive to GitHub Actions.

Skills checkpoint: preflight, dependency restore, Release build (zero warnings/errors), XML method summaries, and PowerShell syntax parsing passed. No automated tests, CLI smoke tests, or imported scripts were executed.

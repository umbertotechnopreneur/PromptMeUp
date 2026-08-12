# Prompt — choose the right Markdown artifact

You are working in `{{WORKSPACE_PATH}}` on `{{TASK}}`.

First inspect the nearest `AGENTS.md`, the canonical `copilot-instructions.md`, and the existing task ledgers. Decide which Markdown artifact is appropriate before writing:

| Need | File |
| --- | --- |
| Work still to do | `todo.md` |
| A reusable rule for agents | `AGENTS.md` or `.github/instructions/*.instructions.md` |
| Product/repository contribution rules | `CONTRIBUTING.md` |
| A stable technical choice | `docs/adr/NNNN-{{slug}}.md` |
| A system/process explanation | `docs/architecture/{{slug}}.md` |
| A repeatable validation checklist | `docs/validation/{{slug}}.md` |
| A discovered lesson | `lessons.md` |
| A finished cross-repository task | `archive.md` |
| A user-facing feature or module overview | `README.md` |

Do not create a new Markdown file if an existing authoritative file already owns the information. Report the chosen path, why it fits, and the smallest content outline. Stop before editing if the scope or owner is unclear.

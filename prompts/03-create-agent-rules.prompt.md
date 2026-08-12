# Prompt — create scoped agent rules

Create or update agent guidance for `{{TARGET_PATH}}`.

Read the parent `AGENTS.md` and canonical repository `copilot-instructions.md` first. Keep the new file scoped and concise. Include:

- scope and precedence;
- required tools and commands;
- architecture/process boundaries;
- files that are authoritative;
- validation commands;
- security and secret-handling rules;
- explicit “preserve unrelated changes” guidance.

If Copilot-specific discovery is useful, add a matching `.github/instructions/{{slug}}.instructions.md` with front matter such as `applyTo: "{{GLOB}}"`. Do not duplicate or silently override repository-specific policy. Report every created or modified file.

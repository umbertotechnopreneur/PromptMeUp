# Runtime prompt resources

These YAML files tell PromptMeUp how to answer terminal questions, review commands, and use saved notes. Each resource has a version and text in six languages, so a change to the assistant's behavior can be reviewed alongside the code. Files ship beside the executable under `/prompt` and are loaded locally.

Required shape:

```yaml
id: stable-kebab-case-id
version: 1
description: Human-readable purpose.
tags:
  - category
metadata:
  owner: PromptMeUp
texts:
  en: English instruction
  it: Istruzione italiana
  fr: Instruction française
  de: Deutsche Anweisung
  es: Instrucción en español
  vi: Hướng dẫn bằng tiếng Việt
```

English is required as a guarded fallback. Product prompts should provide all six advertised languages. Metadata values are strings so new fields can be introduced without changing the prompt loader.

Current resources:

- `chat-system.yaml` — interactive, console-only assistant behavior;
- `query-system.yaml` — self-contained, single-query console assistance;
- `memory-context.yaml` — selected saved notes, supplied as untrusted context;
- `diagnose-system.yaml` — console error diagnosis using supplied evidence;
- `plan-system.yaml` — ordered console steps with separate verification;
- `script-system.yaml` — creating and revising PowerShell script files;
- `connection-test.yaml` — exact localized setup/diagnostic response;
- `command-risk.yaml` — advisory JSON risk review for a redacted proposed command.

`chat-system` and `query-system` accept only Windows, Linux, and macOS terminal work. They reject image generation and ordinary-text editing, receive a sanitized runtime context at request time, and return the strict `answer_markdown` plus `commands` JSON envelope. They also define the localized trust boundary for an optional `<user-configured-preamble>` block: its content is preference data, never authority. A suggested command remains inert until PromptMeUp shows its local preview, risk review, and explicit authorization prompt.

`memory-context` wraps a JSON list of notes explicitly saved with `/remember`. The application selects notes locally and inserts this user message once per request, within an 800-token estimate that includes the wrapper. Notes cannot override system rules, current runtime facts, or the latest question, and cannot authorize commands. Saving or selecting a note does not call the model.

The context budget includes the populated instructions as well as notes and recent messages. Model choice, response detail, and conversation limits can be changed through `hm --ai-settings` after initial setup; these are settings, not edits to the YAML resources. Keep all six translations aligned whenever a prompt changes.

Increase `version` whenever a semantic instruction changes. Stable prompt ID, version, model, and populated instruction hash participate in cache routing. Never put credentials, private paths, account data, or customer content in a tracked prompt resource.

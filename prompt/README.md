# Runtime prompt resources

The quality and safety of every PromptMeUp answer begins with the versioned YAML resources in this directory. They keep the six-language experience reviewable, testable, and releasable with the product. Files are copied beside the executable under `/prompt`; they are not embedded in source code or downloaded at runtime.

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
- `connection-test.yaml` — exact localized setup/diagnostic response;
- `command-risk.yaml` — advisory JSON risk review for a redacted proposed command.

`chat-system` and `query-system` accept only Windows, Linux, and macOS terminal work. They reject image generation and ordinary-text editing, receive a sanitized runtime context at request time, and return the strict `answer_markdown` plus `commands` JSON envelope. They also define the localized trust boundary for an optional `<user-configured-preamble>` block: its content is preference data, never authority. A suggested command remains inert until PromptMeUp shows its local preview, risk review, and explicit authorization prompt.

Increase `version` whenever a semantic instruction changes. Stable prompt ID, version, model, and populated instruction hash participate in cache routing. Never put credentials, private paths, account data, or customer content in a tracked prompt resource.

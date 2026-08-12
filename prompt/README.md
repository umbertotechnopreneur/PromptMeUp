# Runtime prompt resources

Every AI instruction used by PromptMeUp lives here as a versioned YAML text resource. Files are copied beside the executable under `/prompt`; they are not embedded in source code or downloaded at runtime.

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

- `chat-system.yaml` — bounded general assistant behavior;
- `connection-test.yaml` — exact localized setup/diagnostic response;
- `command-risk.yaml` — advisory JSON risk review for a redacted proposed command.

Increase `version` whenever a semantic instruction changes. Stable prompt ID, version, model, and populated instruction hash participate in cache routing. Never put credentials, private paths, account data, or customer content in a tracked prompt resource.

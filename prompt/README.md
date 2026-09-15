# The app's AI instructions

These YAML files tell PromptMeUp how to answer terminal questions, review commands, and use saved notes. Each file has a version and six translations, so you can review instruction changes alongside the code. They ship in the `prompt` folder next to `hm`.

Use this format:

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

Every product prompt needs all six languages. English is also required as a fallback. Metadata values are strings, which lets you add fields without changing the loader.

Current resources:

- `chat-system.yaml` — interactive, console-only assistant behavior;
- `query-system.yaml` — self-contained, single-query console assistance;
- `app-guide-*.yaml` — eight local product guide chapters loaded on demand as system context;
- `memory-context.yaml` — selected saved notes, supplied as untrusted context;
- `diagnose-system.yaml` — console error diagnosis using supplied evidence;
- `plan-system.yaml` — ordered console steps with separate verification;
- `script-system.yaml` — creating and revising PowerShell script files;
- `connection-test.yaml` — exact localized setup/diagnostic response;
- `command-risk.yaml` — advisory JSON risk review for a redacted proposed command.

`chat-system` and `query-system` cover terminal work on Windows, Linux, and macOS. They exclude image generation and general text editing. Each request includes a filtered summary of the machine and folder. The answer must use the required JSON fields, `answer_markdown`, `commands`, and `guide_topics`. Final answers return an empty `guide_topics` array.

Both prompts also explain, in the selected language, that the conversation takes place inside PromptMeUp. They include a concise overview of questions and chat, diagnostics, scripts, plans, previews, recipes, saved notes, usage, and customization. References to "this app" use that context unless the conversation identifies another program. The model can use only the material supplied with the request; this overview does not give it access to files, settings, or the full local history. Detailed settings procedures are kept out of these base instructions.

For natural-language questions about the app, the model can request up to two guide topics through `guide_topics`, leaving `answer_markdown` empty and `commands` empty. The available topics are `overview`, `settings`, `conversation`, `memories`, `commands`, `workflows`, `costs`, and `privacy`. The app loads only these packaged, localized YAML chapters, wraps their IDs and versions in `<app-guide>`, and makes one additional AI call to answer. It does not read arbitrary files or search the web. Guide context is limited to 3,000 estimated tokens including its wrapper; invalid, duplicate, or oversized requests fail explicitly.

Loaded chapters remain available for follow-up questions until `/clear` or replacement by other topics. A new topic is combined with retained topics when the total fits the two-topic limit; otherwise the newly requested topics replace them. Both AI calls contribute usage and estimated costs. Guide content is counted as system context and identified separately from the other system instructions. The guide explains that interactive settings and other app interfaces must be opened manually in the terminal, outside the chat command runner. Defaults in the guide are never evidence of the user's current configuration.

The optional `<user-configured-preamble>` block holds the user's personal instructions. The prompts tell the model to treat these as preferences that can't override the app's rules. Suggested commands still need a local preview, risk check, and the user's approval before they run.

`memory-context` wraps a JSON list of notes explicitly saved with `/remember`. The application selects notes locally and inserts this user message once per request, within an 800-token estimate that includes the wrapper. Notes cannot override system rules, current runtime facts, or the latest question, and cannot authorize commands. Saving or selecting a note does not call the model.

The context budget includes the populated instructions as well as notes and recent messages. Model choice, response detail, and conversation limits can be changed through `hm --ai-settings` after initial setup; these are settings, not edits to the YAML resources. Keep all six translations aligned whenever a prompt changes.

Increase `version` whenever you change what an instruction asks the model to do. The prompt ID, version, model, and a hash of the completed instructions help identify requests for caching. Keep credentials, private paths, account data, and customer content out of these files.

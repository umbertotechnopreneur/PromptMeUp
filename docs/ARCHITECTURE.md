# PromptMeUp architecture

PromptMeUp helps you understand and use your terminal. It answers questions, remembers notes you choose to save, and asks before running commands. The .NET 10 console host separates those choices from storage, provider calls, rendering, and process execution. It runs only when invoked through `hm`.

```mermaid
flowchart LR
    U["User · hm"] --> A["Application orchestrator"]
    A --> V["Spectre views"]
    A --> S["Services"]
    S --> O["OpenAI Responses API"]
    S --> P["Official pricing and Costs API"]
    S --> D["SQLite ledger"]
    S --> L["Serilog files"]
    S --> C["Authorized pwsh child process"]
    Y["Localized YAML prompts"] --> S
```

## Product boundaries in code

- `Models/` contains immutable settings, AI usage, pricing, command authorization, memory, status, and audit contracts.
- `Services/` owns SQLite, OpenAI, pricing, prompt loading, localization, recent conversation memory, saved notes, command risk, command execution, secrets, PATH, font support, redaction, and cost calculation.
- `Views/` owns Spectre.Console rendering and user input. Views do not call OpenAI, SQLite, or PowerShell.
- `Application/` coordinates one invocation through focused conversation and authorized-command workflows and is the only place that combines services with views.
- `Infrastructure/` resolves local application paths.
- `/prompt` contains versioned runtime instructions and metadata. `/prompts` contains contributor-facing development prompts and is not sent by the application.

Dependencies are wired through `Microsoft.Extensions.DependencyInjection`. Application code consumes `ILogger<T>`; Serilog is configured only at the composition root.

The invocation orchestrator delegates settings and credential forms to `SetupWorkflow` and reviewed PATH, executable-location, and font operations to `InstallationWorkflow`. `ApplicationActivityRecorder` shares best-effort non-session audit recording. `AuditSessionScope` opens and closes workflow sessions with explicit typed outcomes, including cleanup after cancellation.

`LennaWorkflow` is a small local display path. The composition root selects it before resolving the main application's settings, theme files, or AI configuration. Its service reads a bounded embedded RGB resource, and its passive view centers a Spectre canvas. It opens no database, calls no provider, and does not clear terminal history.

`HelpWorkflow` also runs before that initialization. Explicit help opens navigable sections in a supported live terminal and keeps grouped scrolling output elsewhere. Invalid command lines use the scrolling reference so the error remains visible. Terminal views use open layouts and separators; cards require an explicit product request.

`hm --setup`, `hm --ai-setup` (also `--ai-settings`), and `hm --theme` enter the same `SetupWorkflow` with General, AI, or Theme selected. The selected section is navigation state; all settings remain available in one draft. An invocation-only `--language` choice changes the form's display language without changing the saved language. Save persists the draft directly, without a summary or confirmation page. Saving does not refresh pricing or call the provider unless the user enables the optional connection check, which defaults to off after initial setup.

UI translations have one definition per key and language in the functionality-grouped `Services/Localization/UiTextCatalog.*.cs` files. Each entry names all six translations explicitly, and catalog assembly rejects duplicate keys. `LocalizationService` retains language selection and culture-aware formatting; runtime AI instructions remain in `/prompt`.

`AdaptiveSetupView` selects the fullscreen workspace or a compatibility section menu.
The fullscreen form owns keyboard focus and a temporary alternate terminal buffer;
it returns a draft for `SetupWorkflow` to save. Theme preview changes are reverted
when the view exits and applied after successful persistence. `ThemeCatalogService`
loads validated versioned JSON palettes from the packaged `themes` directory;
settings store only the selected identifier. `hm --theme` selects Theme in the
shared settings workspace.

Fullscreen settings and help share the product header and footer layout. Settings
keeps a sidebar visible from 60 columns, with General, AI, Credentials,
Conversation, Commands, Personalization, and Theme. Switching sections keeps the
local draft until Save or Cancel. Tab reaches those actions, and Left/Right moves
between their buttons. Help uses the same section-navigation
keys and presents colored command examples with matching parameter explanations.
The views keep this presentation separate from command parsing and execution.

## From question to answer

```mermaid
sequenceDiagram
    participant User
    participant App
    participant Memory
    participant OpenAI
    participant SQLite

    User->>App: hm question or chat turn
    App->>SQLite: Select saved notes locally
    App->>Memory: Reserve instructions and notes, then add user message
    Memory-->>App: Snapshot and prune count
    App->>OpenAI: YAML instruction + protected preamble + runtime snapshot + selected notes + active messages
    OpenAI-->>App: Structured Markdown answer + cited command candidates + usage
    App->>SQLite: Redacted request and ordered session events
    App-->>User: Markdown answer + cost/context status
```

The Responses request sets `store=false`. For chat and single queries, the instruction includes only a privacy-filtered runtime context (working directory, platform/shell, CPU, physical memory, and an available GPU label) so terminal guidance matches the active machine; it excludes account, host, network, serial, and secret data. `OpenAiService` owns HTTP, auditing, persistence, and pricing; small request-builder and response-parser components isolate the provider protocol and are tested without network access. Stable YAML instructions precede changing conversation content. Prompt cache routing is enabled by default and usage details are read from the provider response.

One HTTP deadline covers sending and reading the bounded response body, and recorded duration includes body delivery. Runtime context requests PowerShell 7 syntax on Windows, Linux, and macOS, with paths appropriate to the host.

The system prompts constrain the assistant to Windows, macOS, and Linux console help. They explicitly exclude image generation and ordinary prose editing. An optional setup preamble is Unicode-normalized, capped at 500 words, screened for multilingual instruction overrides and role forgery, and enclosed in a dedicated untrusted-data block. The localized YAML prompt tells the model to apply only compatible style or format preferences from that block. The Responses API uses a strict JSON schema for the rendered Markdown answer and any cited command candidates. The model has no tool or process-execution capability.

## From suggestion to authorized action

1. `/run` captures the exact proposed PowerShell text.
2. A conservative local rule produces a risk score and description.
3. When enabled and available, a redacted command is sent for an advisory AI review; the higher local/AI score wins.
4. The view renders the exact unredacted local command, score, Markdown explanation, and output-sharing notice.
5. Only an affirmative interactive answer creates an `ApprovedCommand` capability.
6. The execution service rejects missing or expired authorization, starts `pwsh -NoProfile -NonInteractive` as the current user, and applies timeout/output limits. PromptMeUp never requests elevation itself; an authorized command can still request it explicitly and is scored accordingly.
7. The user sees local stdout/stderr. Recognizable credentials are redacted before audit persistence and before the bounded result becomes a follow-up prompt.

No AI response can create authorization and `--yes` never applies to `/run`. A model candidate first appears in a menu whose default is **Do not execute commands**, then must pass this same authorization flow.

## Persistence

SQLite uses WAL mode, foreign keys, integer microdollars, UTC timestamps, and schema version `3`. Initialization upgrades older supported databases in a transaction, adding the saved context budget, note storage, and theme selection while retaining settings and history. A database from a newer schema is rejected.

| Table | Purpose |
| --- | --- |
| `app_settings` | Singleton non-secret setup and memory settings. |
| `persistent_memories` | Explicit note text, ID, project/global scope, and last-updated timestamp. |
| `ai_model_pricing` | Daily normalized model price snapshots. |
| `organization_costs` | Optional admin Costs API buckets. |
| `sync_state` | Named synchronization timestamps. |
| `ai_requests` | One normalized provider call, usage, estimated cost, IDs, redacted latest prompt/response, and outcome. |
| `ai_sessions` | Session header, model, language, kind, and lifecycle. |
| `ai_session_events` | Ordered flexible JSON prompt, response, command, output, pruning, and error events. |
| `activity_audit` | Flexible JSON records for setup, authorization, PATH, font, status, and other user activity. |

JSON payloads are validated before insertion. Credential-shaped properties and string values are redacted. SQLite errors are logged; provider results are not replaced by secondary telemetry failures.

## Artifact and helper boundaries

Artifacts use matching read/write limits and a separate generation budget; see
[configuration](CLI_REFERENCE.md#configure-artifact-limits).

Plans, recipes, and scripts share `AtomicFileWriter` for temporary-file publication and cleanup. Callers retain validation and localized errors and explicitly select overwrite behavior: plans replace progress files, while recipes and scripts require a new destination.

Commands and font helpers share bounded process I/O and cancellation cleanup.
Large approved commands travel over standard input. Font checks time out after
15 seconds; installation after two minutes. Command approval remains mandatory.

## Recent conversation and saved notes

Each query or chat receives an isolated `ConversationMemory`. It keeps recent user/assistant messages, caps user-input size, and removes the oldest complete turns to fit the turn and token limits. The default is 12 turns. Completed answers are rendered before pruning; an oversized completed turn can be dropped entirely from future context. There is no automatic summary.

`PersistentMemoryService` stores only notes explicitly saved through `/remember`. A note is limited to 1,000 characters, with up to 100 notes per scope. The project scope is a SHA-256 hash of the nearest Git root, falling back to the current working directory. Global notes are available from any project. Saving an identical note in the same scope refreshes its timestamp.

Selection uses local Unicode word overlap, with no provider call or embedding index. Global notes are always eligible; project notes require overlap with the current question. Candidates are sorted by overlap, project scope before global on ties, and recency. Selection removes duplicate text and keeps at most five notes within a 650-token content estimate. The localized `memory-context.yaml` wrapper and JSON note list form a user message capped at 800 estimated tokens; lower-ranked notes are removed until it fits. That message is prepended once per request and is not added to the conversation window.

New notes containing recognizable credentials are rejected. Reads reapply redaction and repair stored text when needed. Invalid note input leaves chat open; invalid stored data and database failures propagate rather than silently dropping memories.

The event ledger is not replayed into context. A new invocation starts with fresh recent messages and may recall saved notes. `/clear` removes recent messages while preserving saved notes, local history, the latest response metrics, and cumulative session usage. `/forget` deletes the selected note record; it does not rewrite old requests or retained assistant replies.

## Context budget and usage

For ordinary questions and chat, the input budget is the smallest of:

- the saved `ContextTokenBudget` (16,000 by default), or the `PROMPTMEUP_CONTEXT_TOKENS` override when present;
- the configured percentage of the model's context window;
- the model's context window minus the reserved response budget.

The saved budget and environment override accept 4,000–200,000 tokens. The environment value affects the current process and is not persisted. Plans and scripts use their separate artifact limits.

Before adding a question, `AiConversationWorkflow` reserves the populated instructions, runtime details, and selected notes, then gives the remaining input allowance to recent messages. If the new question and those fixed parts cannot fit, the request is rejected before an HTTP call. In chat, the limit error leaves the conversation open so the user can shorten the question or clear recent messages.

`ContextTokenEstimator` uses UTF-8 byte count divided by four, rounded up, with small message/request allowances. This is a local estimate, not provider tokenization. The status display marks it with `~` and compares retained input against both model capacity and the effective input budget. `/status` and `/context` recalculate this estimate, including any newly selected notes.

The latest response's provider input/output counters are kept separately in `ConversationState`. Cumulative input/output counts come from the session's recorded `ai_requests`. These counters describe usage already incurred; neither pruning nor `/clear` resets them. Diagnostic connection tests show their own latest-call usage without a retained chat-context estimate.

## Cross-platform boundaries

- The managed application, SQLite, OpenAI client, rendering, and memory are platform-neutral.
- Authorized commands require PowerShell 7 on every platform.
- Windows setup can persist API keys in current-user environment variables; Unix setup provides export guidance and does not edit a secret profile.
- PATH persistence uses the Windows user environment or one marked Unix profile block.
- Automatic Nerd Font installation is an optional Windows helper; other platforms receive manual guidance.

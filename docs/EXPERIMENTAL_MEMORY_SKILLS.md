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

## Learning checkpoint

Open `hm --learning` and enable the experiment for the current project. Capture and the reminder are separate opt-ins. Capture starts with future, completed user messages; it does not import old chats, diagnostic files, command output, or assistant answers. Messages above 4,000 characters are skipped. The local learning store keeps the newest 200 observations per project for up to 30 days. You can inspect and delete an observation or clear the collection.

The existing Memories screen still shows approved notes. Its **Proposals** action opens the separate review queue, also available as `hm --proposals`. Each proposal shows its type, reason, complete evidence, scope, and before/after text. You can edit an addition or merge, choose project or global scope for an addition, and then explicitly approve or reject it. A flag is informational and cannot alter memory. Changed or expired evidence invalidates a proposal.

Approval is transactional: a stale target, changed source, full destination, or duplicate note leaves the original memories unchanged. Learned notes retain provenance IDs and a kind (`memory`, `lesson`, `correction`, or `preference`). The queue is limited to 100 pending proposals and 200 recent reviewed/expired records per project. Reviewed records suppress equivalent repeated suggestions until they expire or are cleared.

## Dream checkpoint

Run `hm --dream` after capturing observations in at least two different sessions. Dream selects complete observations from multiple sessions within the configured message-size limit, up to 24 observations. It shows the exact serialized batch and any omitted count, then asks before sharing with OpenAI. A batch that cannot fit two sessions is rejected; no source is silently truncated.

The model may suggest up to eight additions. Every addition must reference observations from at least two captured sessions. Unknown source IDs, malformed output, recognizable credentials, and unsupported operations are rejected. Suggestions stay in the review queue; a successful model response never approves them.

## Heartbeat checkpoint

Run `hm --heartbeat` manually to inspect saved notes. Exact duplicates within one scope produce local merge proposals without OpenAI. You can separately approve sending a bounded batch of saved notes for AI review of merges, removals, or possible conflicts. The last-success time reflects a completed local scan, even if you decline the optional AI step. Failed AI analysis creates no AI proposals.

The optional reminder appears when you open a chat after seven days without a successful heartbeat. It does not start a task, call OpenAI, or run when `hm` is closed. There is no service or scheduled background agent.

The matching `/skills`, `/learning`, `/proposals`, `/dream`, and `/heartbeat` commands also work inside chat. All review and execution flows remain interactive; `--yes` does not bypass them.

## Privacy and limits

Disabling capture or the experiment clears current-project observations and proposals, not approved notes. Forgetting, merging, or removing an approved project memory also clears learning observations and proposals in that project; a global target clears learning data in every project. The review displays this permanent side effect before approval. This conservative policy prevents retained observations from immediately recreating removed text. In-flight captures and model responses are invalidated by the purge.

The 30-day limit applies to learning records, not ordinary AI audit history. Confirmed Dream and heartbeat requests and responses use the existing redacted request history and cost accounting; those records, backups, and content already sent to OpenAI are not erased by learning cleanup. See [Privacy](PRIVACY.md).

Context selection is deliberately small: explicit package selection takes precedence; optional automatic selection uses local keyword matching rather than embeddings or another AI request. Skill instructions and memory notes share a bounded context envelope. Imported instruction text remains in its original language; internal prompts and application controls support all six languages.

This first port includes three reviewed skills, not the companion application's entire catalog or tool protocol. Other packages can be inspected and imported individually when they meet the supported layout and script constraints. `metals-dev-monitor` remains blocked. ZIP import is not a sandbox: an approved script has your account's permissions, so inspect its complete contents and command preview.

Catalog and concat-files path checks reject arbitrary linked ancestors. On macOS, only the system aliases `/var`, `/tmp`, and `/etc` are accepted after checking that they resolve directly to their expected `/private` paths; those targets and their ancestors are checked too. Release packaging includes only the four files belonging to the three bundled skills, never local imports.

## Validation record

Run the applicable repository non-test checks before code commits. Automated tests and CLI smoke tests require a separate explicit request. Portable releases remain exclusive to GitHub Actions.

Skills checkpoint: preflight, dependency restore, Release build (zero warnings/errors), XML method summaries, and PowerShell syntax parsing passed. No automated tests, CLI smoke tests, or imported scripts were executed.

Learning checkpoint: preflight, formatting verification, XML method summaries, Release build (zero warnings/errors), and syntax/six-language validation of all 24 runtime prompts passed. Guide text remains within its context allowance. Added regression coverage for catalog/import, scalar parameters, reflection parsing/batching, CLI selection, retention, stale evidence, and transactional approval. These tests were compiled but not run. Release build output was cleaned after the final build. No live OpenAI request or interactive CLI smoke run was performed.

Final verification after explicit testing authorization: all 660 local tests passed. The macOS system-alias and Windows packaging follow-up passed formatting, XML summaries, a warning-free Release build, and syntax checks on all five changed PowerShell files. No release packaging or skill script was executed locally; packaging validation runs in GitHub Actions.

The experiment is available for review in [draft PR #43](https://github.com/umbertotechnopreneur/PromptMeUp/pull/43), on `codex/experimental-memory-skills`. `main` and release versions are unchanged.

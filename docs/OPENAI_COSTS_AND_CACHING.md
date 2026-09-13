# PromptMeUp memory, costs, and OpenAI caching

PromptMeUp keeps a short conversation useful without making token use or cost disappear behind the interface. It shows local estimates, provider-reported usage, and cache activity separately—and never presents a client-side estimate as provider billing.

## See what the conversation consumes

PromptMeUp shows three separate measures:

- **Active context:** the estimated input retained for the next request, compared with both the model's context window and the operational input budget.
- **Last request:** the provider-reported input and output counters for the last assistant call, including reported cache activity.
- **Session usage:** cumulative provider-reported input and output tokens from the current conversation's locally recorded calls, including failed requests that report usage. Repeatedly sent history is counted on every call; these totals do not measure occupied context space. AI command reviews use separate audit sessions and appear in overall `/costs` totals.

The active-context estimate includes:

- populated YAML and the optional protected setup preamble;
- the privacy-filtered current runtime context;
- selected persistent notes and their localized wrapper;
- retained conversation messages, including the latest prompt before sending and the completed answer afterward.

After a response, active context is recalculated from the messages that remain after pruning. It excludes hidden reasoning and prior response formatting that is not retained as conversation text. The last request's input and output are never added together and presented as occupied context. `/status` and `/context` refresh the estimate while retaining the last call's counters and cumulative session usage. An unsent future question cannot yet be included.

Local estimates are marked with `~`. They use a lightweight UTF-8 heuristic and can differ from provider tokenization; provider-reported counters remain separate. The database retains input, output, reasoning, cached-input, cache-write, and total token counters separately.

## Remember useful notes across sessions

In chat, use `/remember [global|project] <text>` to save a short note. The default scope is the current project. `/memories` lists global notes and notes for that project with their IDs; `/forget <id>` removes one of those notes.

Notes are stored in the existing local SQLite database, with at most 100 notes per scope and 1,000 characters per note. The project scope uses the nearest ancestor containing `.git`, or the current directory when no repository is found; its identifier is a hash of the directory. Global notes are available across projects using the same data directory.

Query and chat requests select at most five relevant notes locally. The complete memory message, including the localized wrapper and JSON data, is limited to 800 estimated tokens. This limit is part of the operational input budget. Selection and storage make no additional AI calls, and no automatic summaries or inferred memories are created.

Saved notes are untrusted user data: they cannot override system rules, the current environment, the latest request, or command authorization. Recognizable credentials are rejected on save; loaded content is redacted before provider transmission. Selected notes are shared with the AI provider and may appear in the ordinary redacted request audit.

`/clear` removes active conversation messages while keeping persistent notes, last-request counters, and cumulative session usage. `/forget` prevents future selection of a note; it does not erase earlier audited requests or usage records.

## Keep context useful and bounded

Setup exposes these controls; `hm --ai-settings` edits the AI and conversation
parameters after initial setup without repeating the full wizard:

- optional AI preamble: at most `500` Unicode words, with used/remaining counts and local multilingual prompt-injection screening;
- maximum user turns: `2–50` (default `12`);
- maximum characters per user message: `500–100,000` (default `16,000`);
- maximum context-window percentage: `10–95%` (default `70%`);
- operational input budget for ordinary query and chat: `4,000–200,000` estimated tokens (default `16,000`);
- maximum retained command-output characters: `1,000–32,768` (default `12,000`);
- authorized command timeout: `5–300` seconds (default `30`).

The operational input budget is saved with the AI settings. Optionally set
`PROMPTMEUP_CONTEXT_TOKENS` before launch to override the saved value for that
invocation with an integer from `4000` to `200000`. The effective input budget is
the smallest of the saved or overridden value, the configured model-window
percentage, and the model window minus reserved output tokens. It covers
instructions, selected notes, and retained conversation messages. Artifact and
diagnostic flows keep their own existing limits.

PromptMeUp accounts for populated instructions and selected notes, then removes the oldest complete turn groups until both the turn count and estimated input budget fit. It also leaves room for the provider's output limit within the model context window. It never drops only one side of an old user/assistant exchange when a complete pair is available. If a single new message or populated request still exceeds a configured boundary, the request is rejected with a visible explanation.

In chat, a context-limit error keeps the session open. A message that cannot fit even without earlier turns is rejected before pruning that history, so it can be shortened and retried.

Completed answers display in full; turns exceeding the memory budget are omitted from subsequent context. Ordinary responses have a 2 MiB body limit. Scripts and plans use [configurable artifact limits](CLI_REFERENCE.md#configure-artifact-limits), with 16,384 output tokens by default.

## Understand local request cost

Each successful Responses call can report:

- regular input tokens;
- cached input tokens;
- cache-write tokens where supported;
- output tokens;
- reasoning tokens;
- total tokens.

PromptMeUp multiplies those counters by the matching normalized public price row and stores exact integer microdollars in SQLite. Cached input and cache-write rates fall back to the normal input rate only when the official row does not publish a separate value. The UI labels these amounts as estimates.

Local totals include reported usage from incomplete or invalid responses. Request counts include successful calls and failures with reported tokens. These are local estimates, not account-wide totals; older discarded usage cannot be recovered.

Pricing or session-cleanup failures preserve completed responses and token counts. If pricing is unavailable, the cost estimate remains unknown.

## Keep pricing current

The first relevant app invocation after local midnight checks whether pricing was already synchronized that day. If not, PromptMeUp downloads and parses the official Standard pricing table. `hm --costs` forces a refresh. A failed refresh leaves the previous cache available and writes a diagnostic warning.

The parser stores model, service tier, context band, currency, input, cached-input, cache-write, output, source URL, and retrieval time as normalized fields. The product table currently displays Standard short-context rows.

Request estimates select the pricing band using actual provider input tokens and the returned model family, including dated snapshots. As verified on September 2, 2026, GPT-5.6 Sol/Terra/Luna, GPT-5.5, and GPT-5.4 use the long band above 272,000 input tokens; GPT-5.4 mini/nano keep their single band. Missing bands and unknown returned models leave the estimate unavailable. See the official [pricing table](https://developers.openai.com/api/docs/pricing), [GPT-5.6 Terra](https://developers.openai.com/api/docs/models/gpt-5.6-terra), [GPT-5.5](https://developers.openai.com/api/docs/models/gpt-5.5), and [GPT-5.4](https://developers.openai.com/api/docs/models/gpt-5.4) documentation.

With `OPENAI_ADMIN_KEY`, the same cost flow can refresh current-month organization cost buckets. That provider total is displayed separately from local request estimates.

## Prompt caching

Prompt caching is enabled by default and can be disabled in setup.

- Stable localized YAML instructions and the protected setup preamble are placed before changing conversation messages.
- A stable `prompt_cache_key` is derived from product name, model, prompt ID/version, and a short hash of the populated instruction; user text is not embedded in the key.
- GPT-5.6 requests use an explicit cache breakpoint immediately after the stable developer instruction only when that prefix meets the documented 1,024-token minimum; shorter requests keep automatic prefix caching enabled.
- Long interactive chats pair that stable breakpoint with implicit caching, so their append-only conversation history can create and reuse later checkpoints. Long one-shot queries use explicit-only caching, which reuses the stable instruction without paying to cache the unique request suffix.
- GPT-5.5 requests select its supported `24h` retention policy. Earlier supported models keep the provider/account default so zero-data-retention policy can choose in-memory behavior where applicable.
- The session snapshot shows cache reads and cache writes returned by the API, alongside the active-context estimate, last-request counters, and cumulative session usage.

Provider-side caching has minimum prompt-size, model, retention, and routing semantics that can change. PromptMeUp records actual provider counters instead of assuming every request received a cache hit.

Official references:

- [OpenAI pricing](https://developers.openai.com/api/docs/pricing)
- [OpenAI prompt caching](https://developers.openai.com/api/docs/guides/prompt-caching)
- [OpenAI model catalog](https://developers.openai.com/api/docs/models)

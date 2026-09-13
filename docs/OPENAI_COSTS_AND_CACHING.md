# Memory, context, and costs

Save a few useful notes between sessions, choose how much conversation to keep,
and check token usage as you work. These controls are available from the terminal:

| What you want to do | Command |
| --- | --- |
| Open settings at the model preferences, then choose Conversation for its limits | `hm --ai-setup` or `hm --ai-settings` |
| Check context and token usage during chat | `/status` or `/context` |
| Save a project fact | `/remember project Build this project with dotnet build.` |
| Save a preference across projects | `/remember global Keep terminal explanations concise.` |
| See or remove saved notes | `/memories`, then `/forget <id>` |
| Start with fresh conversation context | `/clear` |
| Check costs | `/costs` in chat, or `hm --costs` from your shell |

`hm --status` shows local configuration and storage readiness. The chat commands
`/status` and `/context` show the current conversation.

## Read the context and usage counters

Context tells you what the model can see in a request. Usage tells you how many
tokens your calls have consumed over time. PromptMeUp shows them separately:

| Measure | Meaning |
| --- | --- |
| Active context | Estimated input currently retained, compared with the model's context window and your input budget. |
| Last request | Input and output tokens reported by the provider for the last assistant call, with any reported cache reads and writes. |
| Session usage | Cumulative input and output tokens from this conversation's recorded calls. |

For example, sending the same history again adds to session usage even if the
active context has barely changed. Removing an old turn reduces active context;
it does not undo the tokens already used to send that turn.

Active context includes the app's instructions, your optional setup preamble,
the privacy-filtered machine and directory context, selected notes, and retained
conversation messages. After an answer, the estimate is recalculated from the
messages that remain. Hidden reasoning and response formatting that was not
kept as conversation text are excluded. A future question is included only once
you enter it, so the estimate may change before the next send.

A `~` marks a local estimate. PromptMeUp uses a lightweight estimate based on the
text's UTF-8 size; the provider's tokenizer may count differently. Reported
input and output remain separate from this estimate.

`/status`, `/context`, and `/clear` preserve the last-request and session counters.
Session totals include failed calls when the provider reports usage. AI command
reviews use separate audit sessions and appear in the overall `/costs` totals.

## Save notes you want to reuse

Use `/remember [global|project] <text>` in chat. Without a scope, the note belongs
to the current project. `/memories` lists your global notes and the current
project's notes, along with the IDs you can use with `/forget`.

Project scope follows the nearest directory containing `.git`, starting with
the current directory and searching upward. Outside a Git repository, it uses
the current directory. Global notes are available in projects sharing the same
local data directory. Notes are stored in SQLite, with a hashed directory path
as the project identifier.

Each note can contain up to **1,000 characters**. You can keep **100 global notes
and 100 notes per project**. Only notes you explicitly save become memories;
PromptMeUp does not create automatic summaries or infer facts to remember.

On each query or chat request, local selection considers global notes and
project notes that share words with the request. It ranks matches and loads at
most **five notes within 800 estimated tokens**, including the surrounding
instructions and JSON formatting. This allowance is part of your input budget.
Saving, listing, deleting, and selecting notes make no extra AI calls.

Selected notes are shared with the provider and may appear in the redacted
request audit. They cannot override system rules, the current environment,
your latest request, or command authorization. Recognizable credentials are
rejected on save and redacted again when notes are loaded.

`/clear` removes active conversation messages but keeps saved notes and recorded
usage. `/forget` deletes a saved note from future selection; text already in the
conversation, earlier audit records, and usage records remain. See
[privacy and data flow](PRIVACY.md) for local storage details.

## Choose how much conversation to keep

Run `hm --ai-setup` (also `hm --ai-settings`) to open the shared Settings screen
with AI selected. Choose Conversation in the left sidebar to change these four
limits. All preferences stay in one draft as you move between sections. Save
applies the draft directly; Cancel discards it.

| Conversation limit | Default | Range |
| --- | ---: | --- |
| Input budget | 16,000 estimated tokens | 4,000–200,000 |
| Retained user turns | 12 | 2–50 |
| Characters per user message | 16,000 | 500–100,000 |
| Share of the model's context window | 70% | 10–95% |

To override the saved input budget, set `PROMPTMEUP_CONTEXT_TOKENS` before launch
to an integer from `4000` to `200000`. The override applies while the environment
variable is set and does not change the saved value. The settings form shows a
notice when it is present.

The actual input limit is whichever is smallest: the saved or overridden budget,
the chosen share of the model window, or the space left after reserving output
tokens. Instructions, selected notes, and retained messages all count toward it.
This input budget applies to ordinary query and chat. Scripts, plans, and
diagnostics have their own limits.

As a conversation grows, PromptMeUp removes the oldest complete turns until the
turn count and input fit. User messages and their answers are removed together.
If a new request cannot fit even without older turns, the app explains the limit
and keeps chat open. That check happens before old history is removed, so you
can shorten the request and retry.

Completed answers appear in full. A turn that is too large to retain is omitted
from later context. Ordinary responses have a 2 MiB body limit; scripts and plans
use [configurable artifact limits](CLI_REFERENCE.md#configure-artifact-limits),
with 16,384 output tokens by default.

In the same Settings screen, Personalization contains the optional instruction
preamble (up to 500 Unicode words, checked locally for prompt injection). Commands
contains retained command output (1,000–32,768 characters, default 12,000) and
command timeout (5–300 seconds, default 30). `hm --setup` opens the workspace with
General selected.

## Read the cost estimate

`/costs` shows the dashboard without ending chat. `hm --costs` also forces a
pricing refresh. Costs calculated from locally recorded requests are estimates;
an available organization total is shown separately.

### How request costs are calculated

Each successful Responses call can report:

- regular input tokens;
- cached input tokens;
- cache-write tokens where supported;
- output tokens;
- reasoning tokens;
- total tokens.

The database keeps these counters separately. PromptMeUp applies the matching
public price row and stores the calculated amount as integer microdollars in
SQLite. Cached input and cache-write rates use the normal input rate only when
the official row has no separate value. The dashboard labels the result as an
estimate.

Local totals include reported usage from incomplete or invalid responses. Request counts include successful calls and failures with reported tokens. These are local estimates, not account-wide totals; older discarded usage cannot be recovered.

Pricing or session-cleanup failures preserve completed responses and token counts. If pricing is unavailable, the cost estimate remains unknown.

### Pricing refreshes

The first relevant app invocation after local midnight checks whether pricing was already synchronized that day. If not, PromptMeUp downloads and parses the official Standard pricing table. `hm --costs` forces a refresh. A failed refresh leaves the previous cache available and writes a diagnostic warning.

The parser stores model, service tier, context band, currency, input, cached-input, cache-write, output, source URL, and retrieval time as normalized fields. The product table currently displays Standard short-context rows.

Request estimates select the pricing band using actual provider input tokens and the returned model family, including dated snapshots. As verified on September 2, 2026, GPT-5.6 Sol/Terra/Luna, GPT-5.5, and GPT-5.4 use the long band above 272,000 input tokens; GPT-5.4 mini/nano keep their single band. Missing bands and unknown returned models leave the estimate unavailable. See the official [pricing table](https://developers.openai.com/api/docs/pricing), [GPT-5.6 Terra](https://developers.openai.com/api/docs/models/gpt-5.6-terra), [GPT-5.5](https://developers.openai.com/api/docs/models/gpt-5.5), and [GPT-5.4](https://developers.openai.com/api/docs/models/gpt-5.4) documentation.

With `OPENAI_ADMIN_KEY`, the same cost flow can refresh current-month organization cost buckets. That provider total is displayed separately from local request estimates.

## Prompt caching

Prompt caching lets the provider reuse an unchanged prefix of a request when it
is eligible. It is enabled by default; change it in the AI section of Settings,
opened directly with `hm --ai-setup` or `hm --ai-settings`. Conversation status
shows the cache reads and writes the provider actually reports. Enabling caching
does not guarantee a cache hit.

### Request layout and model policies

- Localized YAML instructions, the optional setup preamble, and runtime context are placed before conversation messages.
- A stable `prompt_cache_key` is derived from product name, model, prompt ID/version, and a short hash of the populated instruction; user text is not embedded in the key.
- For GPT-5.6, PromptMeUp adds an explicit cache breakpoint after the developer instruction when it estimates that prefix at 1,024 tokens or more. With a shorter prefix, requests use automatic caching.
- When that breakpoint is present, chat requests also enable implicit caching for growing conversation history. One-shot queries request explicit-only caching for the instruction prefix. Both request a `30m` cache lifetime.
- GPT-5.5 requests select `24h` retention. Earlier supported models leave retention to the provider/account default.

The provider determines cache eligibility, retention, and routing. Its policies
can change; the references below describe those details.

Official references:

- [OpenAI pricing](https://developers.openai.com/api/docs/pricing)
- [OpenAI prompt caching](https://developers.openai.com/api/docs/guides/prompt-caching)
- [OpenAI model catalog](https://developers.openai.com/api/docs/models)

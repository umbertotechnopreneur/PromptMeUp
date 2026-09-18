# Memory, context, and costs

Want `hm` to remember a useful detail? Curious about the usage numbers? This
guide explains saved notes, chat limits, and costs. Start with these commands:

| What you want to do | Command |
| --- | --- |
| Open settings at the model preferences, then choose Conversation for its limits | `hm --ai-setup` or `hm --ai-settings` |
| Check context and token usage during chat | `/status` or `/context` |
| Save a useful fact | `/remember I use PowerShell for terminal commands.` |
| Save a preference for future conversations | `/remember Keep terminal explanations concise.` |
| See or remove saved notes | `/memories`, then `/forget <id>` |
| Start fresh in chat | `/clear` |
| Check costs | `/costs` in chat, or `hm --costs` from your shell |

`hm --status` checks your setup and local storage. Inside chat, `/status` and
`/context` show what's being kept in the conversation.

## Read the context and usage counters

The model reads text in small pieces called **tokens**. **Context** is the text
it can see for the current question. **Usage** counts the tokens you've sent and
received over time. PromptMeUp shows them separately:

| Measure | Meaning |
| --- | --- |
| Active context | How much text is being kept, compared with the model's capacity and the limit you've chosen. |
| Last request | Tokens OpenAI reports for the last answer, including any reused or saved in its cache. |
| Session usage | Total input and output tokens recorded during this chat. |

For example, sending the same history again adds to session usage even if the
active context has barely changed. Removing an old turn reduces active context;
it does not undo the tokens already used to send that turn.

Context includes the app's instructions, your personal instructions from setup,
a filtered summary of your machine and folder, selected notes, and recent
messages. After each answer, `hm` counts what's still being kept. It leaves out
the model's hidden reasoning and response formatting that wasn't saved as chat
text. Your next question counts once you type it, so the estimate can change
before sending.

A `~` means an estimate made by `hm`. It uses the size of the text in UTF-8;
OpenAI may count tokens differently. The usage figures reported by OpenAI stay
separate from this estimate.

`/status`, `/context`, and `/clear` preserve the last-request and session counters.
Session totals include failed calls when the provider reports usage. AI command
reviews use separate audit sessions and appear in the overall `/costs` totals.

## Save notes you want to reuse

Use `/remember <text>` in chat or `hm --remember "your note"` in the terminal.
All saved memories form one list, shared across conversations using the same
local data folder. Changing the working folder does not hide them. `/memories`
lists notes and IDs; `hm --memories` opens the editor.

Each note can contain up to **1,000 characters**. You can add notes while the
total is below **100**. Older installations keep every existing note, including
any above that limit; those notes remain readable, editable, and deletable.
Only notes you save or suggestions you approve become saved memories.

For each question, `hm` looks for matching words in the shared list.
It picks the best matches, up to **five notes within 800 estimated tokens**.
That limit includes the notes' instructions and JSON formatting, and counts
toward your input budget. Saving, listing, editing, and selecting notes happen
on your machine. Forgetting by ID or exact text is local; searching by a
description sends batches of notes to OpenAI before you select and confirm.

Selected notes go to OpenAI with your question and may appear in the filtered
request history. They can't override the app's rules, facts about your machine,
your latest request, or command approval. Notes with recognizable secrets are
rejected when saved and filtered again when read.

`/clear` removes active conversation messages but keeps saved notes and recorded
usage. Forgetting a saved note removes it from future selection and clears all
collected messages and pending suggestions. Other notes, text already in the
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

In Settings, Personalization holds your optional personal instructions (up to
500 words, checked for attempts to override the app's rules). Commands lets you
choose how much command output to keep (1,000–32,768 characters, default 12,000)
and how long a command can run (5–300 seconds, default 30). `hm --setup` opens
Settings at General.

## Ask the app guide

Ordinary questions about PromptMeUp can load one or two relevant chapters from
the packaged guide in the interface language. The model requests chapter IDs
through a bounded structured response; the app reads only known local resources
and sends a second request with those chapters in its system instructions.
No command is executed by guide retrieval. A turn permits one guide-loading
round, and each provider request is recorded separately with its usage and cost.

Loaded chapters remain in active context for follow-up questions until replaced
or cleared with `/clear`. Their estimated tokens are included in System and
identified separately as an included guide share. User and assistant text have
their own colored segments. Selected saved notes remain user data; loading the
guide does not change their trust level.

The active-context bar describes retained input, not cumulative API usage. This
turn includes the cost of both calls; last-call counters describe the final
response, and session counters include every recorded call. If a required price
is unavailable, the complete turn or session cost is shown as unavailable.

The response contract uses a strict JSON schema following the
[Structured Outputs guide](https://developers.openai.com/api/docs/guides/structured-outputs).
All requested topics are validated locally, and a guide request cannot also
contain an answer or executable command suggestions.

## Read the cost estimate

Type `/costs` to check spending without leaving chat. From your shell,
`hm --costs` also refreshes prices. Costs worked out from your local request
history are estimates. If an organization total is available, it appears
separately.

### How request costs are calculated

Each successful Responses call can report:

- regular input tokens;
- cached input tokens;
- cache-write tokens where supported;
- output tokens;
- reasoning tokens;
- total tokens.

The database keeps these counts separately. PromptMeUp uses the matching public
prices to estimate the cost, then saves it in SQLite in millionths of a dollar.
It uses the normal input price for cached input or cache writes only when no
separate price is listed. The result is always labeled as an estimate.

If an answer fails or arrives incomplete but OpenAI reports token usage, those tokens still count. Request counts include successful calls and failures with reported usage. The totals cover this app's local records, not your whole account. Deleted usage records can't be recovered.

If a price lookup or session cleanup fails, you still keep completed answers and their token counts. Without a price, the cost stays unknown.

### Pricing refreshes

The first command that needs prices each day checks whether they're up to date. If needed, `hm` downloads OpenAI's Standard pricing table. `hm --costs` refreshes it on demand. If the download fails, the previous prices remain available and the app logs a warning.

For each price, the database keeps the model, service tier, request-size band, currency, token rates, source link, and download time in separate fields. The app's comparison table shows Standard prices for shorter requests.

Request estimates select the pricing band using actual provider input tokens and the returned model family, including dated snapshots. As verified on September 2, 2026, GPT-5.6 Sol/Terra/Luna, GPT-5.5, and GPT-5.4 use the long band above 272,000 input tokens; GPT-5.4 mini/nano keep their single band. Missing bands and unknown returned models leave the estimate unavailable. See the official [pricing table](https://developers.openai.com/api/docs/pricing), [GPT-5.6 Terra](https://developers.openai.com/api/docs/models/gpt-5.6-terra), [GPT-5.5](https://developers.openai.com/api/docs/models/gpt-5.5), and [GPT-5.4](https://developers.openai.com/api/docs/models/gpt-5.4) documentation.

With `OPENAI_ADMIN_KEY`, the same cost flow can refresh current-month organization cost buckets. That provider total is displayed separately from local request estimates.

## Prompt caching

Prompt caching lets OpenAI reuse the unchanged beginning of a request, such as
instructions sent with an earlier question. It's on by default. You can change
it in the AI section of Settings with `hm --ai-setup` or `hm --ai-settings`.
Chat status shows the cache usage OpenAI reports; turning it on doesn't mean
every request will reuse cached text.

### How the app asks for caching

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

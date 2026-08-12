# Memory, costs, and caching

PromptMeUp makes usage visible without pretending that a client-side estimate is provider billing.

## Context accounting

Before a request, PromptMeUp estimates tokens from UTF-8 payload size for:

- populated YAML and custom instructions;
- prior active conversation messages;
- the latest user prompt.

After a response, provider usage replaces the estimated input/output total. The status strip reports combined consumed context against the catalogued model window, while the database retains input, output, reasoning, cached-input, cache-write, and total token counters separately.

The client-side estimate is intentionally lightweight and can differ from provider tokenization. It is a preflight guard and UI signal, not an invoice.

## Short-memory limits

Setup currently exposes:

- maximum user turns: `2–50` (default `12`);
- maximum characters per message: `500–100,000` (default `16,000`);
- maximum context-window percentage: `10–95%` (default `70%`);
- maximum retained command-output characters: `1,000–32,768` (default `12,000`);
- authorized command timeout: `5–300` seconds (default `30`).

PromptMeUp reserves instruction space, then removes the oldest complete turn groups until both the turn count and estimated token budget fit. It never drops only one side of an old user/assistant exchange when a complete pair is available. If a single new message or populated request still exceeds a configured boundary, the request is rejected with a visible explanation.

## Local request cost

Each successful Responses call can report:

- regular input tokens;
- cached input tokens;
- cache-write tokens where supported;
- output tokens;
- reasoning tokens;
- total tokens.

PromptMeUp multiplies those counters by the matching normalized public price row and stores exact integer microdollars in SQLite. Cached input and cache-write rates fall back to the normal input rate only when the official row does not publish a separate value. The UI labels these amounts as estimates.

Today's and the current month's local estimates include only successful requests recorded by this PromptMeUp data directory. They are not account-wide totals.

## Pricing refresh

The first relevant app invocation after local midnight checks whether pricing was already synchronized that day. If not, PromptMeUp downloads and parses the official Standard pricing table. `hm --costs` forces a refresh. A failed refresh leaves the previous cache available and writes a diagnostic warning.

The parser stores model, service tier, context band, currency, input, cached-input, cache-write, output, source URL, and retrieval time as normalized fields. The product table currently displays Standard short-context rows.

With `OPENAI_ADMIN_KEY`, the same cost flow can refresh current-month organization cost buckets. That provider total is displayed separately from local request estimates.

## Prompt caching

Prompt caching is enabled by default and can be disabled in setup.

- Stable localized YAML instructions and custom settings are placed before changing conversation messages.
- A stable `prompt_cache_key` is derived from product name, model, prompt ID/version, and a short hash of the populated instruction; user text is not embedded in the key.
- GPT-5.6 requests use an explicit cache breakpoint immediately after the stable developer instruction only when that prefix meets the documented 1,024-token minimum; shorter requests keep automatic prefix caching enabled.
- GPT-5.5 requests select its supported `24h` retention policy. Earlier supported models keep the provider/account default so zero-data-retention policy can choose in-memory behavior where applicable.
- The status strip shows cache reads and cache writes returned by the API.

Provider-side caching has minimum prompt-size, model, retention, and routing semantics that can change. PromptMeUp records actual provider counters instead of assuming every request received a cache hit.

Official references:

- [OpenAI pricing](https://developers.openai.com/api/docs/pricing)
- [OpenAI prompt caching](https://developers.openai.com/api/docs/guides/prompt-caching)
- [OpenAI model catalog](https://developers.openai.com/api/docs/models)

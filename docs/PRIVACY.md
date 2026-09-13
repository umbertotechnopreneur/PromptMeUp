# PromptMeUp privacy and data flow

Your settings, saved notes, and conversation history live on your machine. When you ask a question, PromptMeUp sends OpenAI the instructions, selected notes, and recent messages that fit the request budget. When you approve a command, its output can also become part of the next question.

This page explains what is stored, what is sent, and what clearing a conversation actually removes.

## Local by default

PromptMeUp stores its own state in the platform's local application-data directory. `hm --status` prints the exact paths, and `PROMPTMEUP_DATA_DIR` can redirect them.

Local data includes:

- non-secret settings;
- the SQLite prompt, response, usage, session, command, and activity ledger;
- notes you explicitly save with `/remember`, in that same SQLite database;
- cached OpenAI pricing and optional organization cost buckets;
- Serilog diagnostic files, rolled daily and retained for up to 14 files;
- packaged YAML prompt resources beside the executable.

Prompt and audit history stays until you remove the PromptMeUp database or data directory. Closing `hm` or using `/clear` removes the recent messages from active chat context. It does not delete saved notes or local history.

## Saved notes

Use `/remember` in chat to save a note for the current project, or `/remember global` to save a preference for use across projects. `/memories` lists the current project's notes and global notes; `/forget <id>` deletes a note from that list. PromptMeUp does not automatically turn conversation history into saved notes.

Project notes belong to the nearest Git root, or the current directory when there is no Git root. The database stores a hash of that location as the project identifier. Note text is stored as text, so a path or other private detail you put in a note remains part of its content.

Saving, listing, deleting, and selecting notes happen locally, without an AI call. For a question, PromptMeUp selects up to five notes: global preferences are eligible across projects, and project notes need matching words in the question. The notes and their surrounding instructions together stay within 800 estimated tokens. Selected notes are sent to OpenAI with the request and may appear in its local audit history.

Notes containing recognizable credentials are rejected. Existing notes are checked again when read, and recognized credentials are scrubbed from their stored text. `/forget` prevents a note from being selected again; it does not erase copies already present in an active conversation, older request history, backups, or provider records.

## Secrets

- PromptMeUp recognizes only `OPENAI_API_KEY` and `OPENAI_ADMIN_KEY`.
- Keys are never accepted as CLI values, written to settings, placed in SQLite, or logged.
- On Windows, setup writes an entered key to current-user and current-process environment scope. Before launching `hm` again, fully close and reopen the terminal application, including its IDE host when applicable, so it stops inheriting an older key.
- On Linux and macOS, setup keeps the entered key only for the current process and provides shell/secret-manager guidance for future sessions.
- Request authorization headers are built outside the serialized provider payload.
- Credential-shaped properties, OpenAI key prefixes, bearer tokens, and common credential assignments are redacted from audit strings and normalized request history.

Quoted JSON credentials and serialized JSON strings are inspected before command output or history is persisted or used for an AI follow-up. Credential field recognition covers common names such as `accessToken`, `SecretAccessKey`, and `SessionToken`, including alternate casing and separators, while retaining ordinary token counters and types. JSON credential objects and arrays are redacted as complete values. PowerShell quoted assignments and here-strings are scanned through their closing delimiter; if capture ends inside the value, its remaining text is redacted. Local command and output previews remain complete.

New preambles containing recognizable credentials are rejected with a localized message. Legacy preambles are scrubbed in the current settings row before use; this does not erase older backups or previously stored history.

Persistent diagnostics retain error types, stable failure codes, status codes, and request identifiers rather than raw exception messages or nested exceptions. The local error surface can still show the provider's explanation.

Redaction is defensive, not infallible. Do not paste secrets into prompts or commands.

Credential-bearing command arguments are rejected. Conversation messages are
redacted before AI transmission; local command previews retain the exact text.

## Data sent to OpenAI

An ordinary AI request can contain:

- the localized YAML instruction selected for that operation;
- the optional setup preamble, after local Unicode normalization and multilingual prompt-injection screening, delimited as untrusted preference data for chat and one-off queries;
- for chat and one-off queries, a privacy-filtered runtime snapshot: the current working directory (with a recognized home directory rendered as `~`), operating-system and shell family, CPU summary, physical-memory summary, and GPU label when the portable runtime can expose one;
- coarse culture and time-zone context only when the location option is enabled;
- the bounded active user/assistant conversation;
- selected saved notes, labeled as untrusted context that cannot override the current request or authorize commands;
- an explicitly authorized command's redacted, bounded stdout/stderr when used for the next turn;
- model, reasoning, output-detail, cache-routing, and output-budget settings.

The runtime snapshot excludes user name, host name, network identity, device serial numbers, and secrets. It helps the answer match your terminal and operating system. PromptMeUp uses the model for console help, including diagnosis, plans, and scripts.

The ordinary question and chat budget defaults to 16,000 estimated input tokens, including instructions, runtime details, selected notes, and recent messages. Older turns are removed as needed to fit. `/context` and `/status` show the retained context estimate separately from the last response's token counts and the session's cumulative usage. A smaller active context does not undo requests already sent or charges already incurred.

After initial setup, `hm --ai-settings` changes the model, response preferences, and context limits locally. It does not send a test request or refresh pricing. See the [CLI reference](CLI_REFERENCE.md) for the settings and limits.

Prompt-injection screening is deterministic defense in depth, not a proof that arbitrary text is safe. The preamble is limited to 500 words, cannot contain the provider-facing delimiter, and is rejected when local rules recognize instruction overrides or role forgery in any supported language. The YAML system prompt independently tells the model to treat the delimited preamble only as untrusted style or format preferences.

The optional command review sends a redacted copy of the proposed command. The exact local command is still shown to the user and is used only for local execution after authorization.

PromptMeUp sets `store=false` on Responses API calls. OpenAI's account, abuse-monitoring, retention, regional, privacy, and billing policies still apply independently; review the current provider terms for your account.

## Pricing and organization costs

After setup, the first relevant invocation of a local day downloads the official public OpenAI pricing document. `hm --costs` forces that refresh.

If a valid `OPENAI_ADMIN_KEY` is available, the cost flow also requests current-month organization cost buckets from the OpenAI administration API. These requests send authorization and time-range parameters, not local prompts or command output. Returned cost buckets are stored in SQLite.

Without an admin key, organization cost remains unavailable and PromptMeUp shows local estimates calculated from provider token usage and cached public prices.

## Command output

The user sees command output locally before any model follow-up. The authorization screen warns that bounded stdout/stderr can be stored locally and sent to OpenAI. PromptMeUp redacts recognizable credentials first, but command output can still contain personal, proprietary, or otherwise sensitive data that no pattern can identify.

Cancel the command or leave chat instead of sending output that should remain local.

## Network destinations

By default PromptMeUp contacts only:

- `https://api.openai.com/v1/responses` for configured AI work;
- the official OpenAI developer pricing document for daily price refresh;
- the OpenAI organization Costs API when an admin key is available and cost synchronization is requested.

The Responses endpoint is visible in setup for verification, but this version accepts only the official `https://api.openai.com/v1/responses` destination. This prevents an OpenAI key from being redirected to an arbitrary HTTPS host through persisted settings.

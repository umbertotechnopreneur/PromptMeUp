# Your data in PromptMeUp

Your settings, saved notes, and chat history are stored on your machine. To answer a question, PromptMeUp sends OpenAI the app's instructions, selected notes, and recent messages that fit the request limit. Output from a command you approve can also be included in a follow-up.

This page explains what is stored, what is sent, and what clearing a conversation actually removes.

## What stays on your machine

PromptMeUp saves data in your operating system's local app-data folder. Run `hm --status` to see the exact paths. You can choose a different folder with `PROMPTMEUP_DATA_DIR`.

Local data includes:

- Settings, without API keys.
- A SQLite database with questions, answers, usage, sessions, commands, and activity history.
- Notes you save with `/remember`, in the same database.
- Saved OpenAI prices and, if you use an admin key, organization cost records.
- Diagnostic logs, started fresh each day, with up to 14 files kept.
- The app's YAML instruction files, kept beside the executable.

Questions and activity history stay until you remove the PromptMeUp database or data folder. Closing `hm` or using `/clear` stops recent messages from being included in the next question. It doesn't delete saved notes or local history.

## Saved notes

Use `/remember` in chat to save a note for the current project, or `/remember global` to save a preference for use across projects. `/memories` lists the current project's notes and global notes; `/forget <id>` deletes a note from that list. PromptMeUp does not automatically turn conversation history into saved notes.

Project notes belong to the nearest folder containing `.git`, looking upward from your current folder. Outside a Git project, they belong to the current folder. The database identifies the project with a hash — a code calculated from the folder path. The note itself is stored as text, including any paths or private details you put in it.

Saving, listing, deleting, and picking notes happen on your machine, without an AI call. For each question, PromptMeUp picks up to five notes. Global notes can be used across projects; project notes need words that match the question. Notes and their added instructions share a limit of 800 estimated tokens (small pieces of text). The selected notes go to OpenAI with your question and may also appear in the local request history.

The app rejects notes that contain secrets it recognizes. It checks saved notes again when reading them and removes recognized secrets from the stored text. `/forget` stops a note from being picked again. It doesn't erase copies already in the current chat, earlier request history, backups, or OpenAI's records.

## Secrets

- PromptMeUp reads keys from `OPENAI_API_KEY` and `OPENAI_ADMIN_KEY`.
- It won't accept keys as command arguments or save them in settings, SQLite, or logs.
- On Windows, setup makes an entered key available to the open app and saves it in your user's environment variables. Before starting `hm` again, fully close and reopen the terminal app, including the IDE if it hosts your terminal.
- On Linux and macOS, a key entered in setup lasts only for the open app. Setup explains how to use your shell or secret manager for later sessions.
- Keys used to connect to OpenAI go in the request's authorization header, separately from the text sent to the model.
- Before saving history, the app removes recognizable secrets such as OpenAI keys, bearer tokens, and values assigned to common credential names.

The secret filter also checks JSON, including JSON stored inside a quoted string. It recognizes names such as `accessToken`, `SecretAccessKey`, and `SessionToken`, with different casing or separators. It keeps ordinary usage counters but removes whole objects or lists when they hold credentials. In PowerShell, it checks quoted assignments and multiline strings through their closing marker. If captured output ends partway through a secret, the rest of that captured value is removed too.

You still see the full command and output in your local preview. Filtering applies to saved history and text sent to the AI.

Personal instructions entered in setup are checked too. The app rejects new instructions containing recognizable secrets and removes them from older saved instructions before use. Older backups and history aren't rewritten.

Diagnostic logs keep error types, error and status codes, and request IDs. They leave out raw exception messages and nested errors that might contain private text. The terminal can still show OpenAI's explanation of an error.

The filter can't catch everything. Don't paste secrets into questions or commands.

Command arguments containing recognizable secrets are rejected. Chat messages
are filtered before being sent to OpenAI.

## Experimental skills and learning

The experimental branch adds project-scoped settings, skill activation fingerprints, observations, memory proposals, and provenance to local SQLite storage. ZIP imports stay in the application data directory. These features start disabled. Activating a skill can send its instructions with later questions; scripts are never run just by activating them. Review imported packages as carefully as code you would run yourself.

Approved HTTP actions send the displayed request to a public HTTPS site; web search sends the query to DuckDuckGo's Instant Answer service. They do not support credentials, redirects, proxies, or private-network destinations. Skill output is not automatically sent to OpenAI. Windows clipboard actions expose or replace local text; screenshots save a new local PNG without uploading it. Images cannot be redacted: close sensitive windows first. System-info actions omit environment values and network addresses.

Optional AI command review can send the full filtered script and parameters, including a search query, HTTP body, or clipboard write text, to OpenAI before execution approval. Turn off AI command review in Settings to use local risk scoring only.

The separate `set_reminder` skill stores up to 50 credential-free notes per project in SQLite. Due notes appear at the next active chat input prompt and are then deleted; cancellation also deletes them. Turning the skill or experiment off pauses delivery without deleting pending notes. No background alarm, operating-system notification, or OpenAI call is involved.

With observation capture enabled, completed, directly entered user messages of up to 4,000 characters are filtered and retained for 30 days, up to the newest 200 per project. Longer messages are not captured. Assistant replies and command-output follow-ups are not observations. Dream shows a bounded batch and asks before sending it to OpenAI. Heartbeat checks exact duplicates locally and can request a separately confirmed AI review. There is no background process or automatic API call from the reminder.

Proposals remain separate from saved memories until you approve a displayed change. Disabling capture or the experiment clears its observations and proposals. Forgetting, merging, or removing a project memory also clears learning evidence and proposals for that project; a global target clears them for all projects. This conservative purge prevents learning evidence from immediately recreating removed text. Other approved memories, older request history, backups, and text already sent to OpenAI are not erased. Confirmed reflection requests and responses use the ordinary redacted AI history; the learning store's 30-day limit does not expire that history. Retention cleanup runs when learning data is accessed, not on a background timer.

See the [experimental guide](EXPERIMENTAL_MEMORY_SKILLS.md) for limits and review controls. Secret filtering is not a guarantee: do not capture or import confidential material you cannot share.

## Data sent to OpenAI

An ordinary AI request can contain:

- The app's instructions for that task, in your chosen language.
- Your optional name or nickname, only for questions and chat, if you set it in Personalization.
- Your optional personal instructions for questions and chat, after local checks. They are marked as preferences, not permission to change the app's rules.
- For questions and chat, your current folder (with a recognized home folder shown as `~`), operating system, shell, CPU and memory summary, and GPU name when available.
- General language-region and time-zone details, only if you enable the location option.
- Recent messages that fit the conversation limit.
- Selected saved notes. These can't override your request or approve commands.
- Captured output and errors from a command you approved, after filtering, when used in a follow-up.
- Your model, reasoning, answer length, caching, and output limit settings.

The machine summary doesn't include your username, computer name, network identity, serial numbers, or secrets. It helps `hm` suggest commands that fit your terminal and operating system.

Your preferred name is saved locally, never inferred from accounts, the computer, or memories. It is not added to automatic chat-control checks, command-risk reviews, Dream, heartbeat, or connection checks. Clear it in `hm --setup` → Personalization and Save to stop future inclusion. This does not erase earlier conversations, request history, or provider records.

Questions and chat have a default limit of 16,000 estimated input tokens, including instructions, machine details, notes, and recent messages. Older exchanges are left out as needed to fit. Use `/context` or `/status` to see what's being kept, alongside usage for the last answer and the whole chat. Keeping fewer messages doesn't undo earlier requests or charges.

`hm --setup`, `hm --ai-setup` (also `--ai-settings`), and `hm --theme` open Settings at different sections. Saving changes happens locally and doesn't refresh prices. Settings sends an AI request only if you turn on the connection check in Credentials. That check starts off after initial setup. See the [command guide](CLI_REFERENCE.md) for settings and limits.

Personal instructions have a 500-word limit. Local rules reject text that tries to impersonate system instructions, override the app's rules, or close the marker around your preferences. These checks cover all six languages. The app also tells the model to use this text only for compatible style and format preferences. Neither check guarantees that every attempt will be caught.

The optional AI command review sends a filtered copy of the proposed command. You still see the exact original command, and it runs locally only after you approve it.

PromptMeUp sets `store=false` on Responses API calls. OpenAI's account, abuse-monitoring, retention, regional, privacy, and billing policies still apply independently; review the current provider terms for your account.

## Pricing and organization costs

After setup, the first command that needs prices each day downloads OpenAI's public pricing page. `hm --costs` refreshes it on demand.

If you provide a valid `OPENAI_ADMIN_KEY`, `hm` also asks OpenAI for your organization's cost records for the current month. That request sends the key for authorization and the date range, without your questions or command output. The returned costs are saved in SQLite.

Without an admin key, you'll see local estimates based on recorded token usage and saved prices. The organization total will be unavailable.

## Command output

You see command output in the terminal before an AI follow-up. The approval screen explains that captured output and errors can be saved locally and sent to OpenAI. PromptMeUp removes secrets it recognizes first, but private details or company information can still get through.

Cancel the command or leave chat instead of sending output that should remain local.

## Where the app connects

By default PromptMeUp contacts only:

- `https://api.openai.com/v1/responses` for AI requests;
- the official OpenAI developer pricing document for daily price refresh;
- the OpenAI organization Costs API when an admin key is available and costs are being refreshed.

Setup shows the API address so you can check it. This version accepts only `https://api.openai.com/v1/responses`; changing saved settings can't redirect your OpenAI key to another server.

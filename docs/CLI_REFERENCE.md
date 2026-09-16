# PromptMeUp command guide

Ask `hm` a terminal question, check its answer, and choose whether to run a suggested command. `hm` means **help me** and works on Windows, Linux, and macOS.

## Ask or choose a command

```text
hm [question]
hm [command] [options]
```

Type your question after `hm`:

```powershell
hm "Which files have changed in Git?"
hm how do I list running dotnet processes?
```

Put the question in quotes if it contains shell characters such as `|`, `>`, `$`, or `*`. This keeps your shell from treating them as part of a command.

If `hm` needs more detail, it asks before suggesting a command. Choose
**Clarify the request or continue in chat** to reply in the same conversation.
If there are no commands and you're done, choose **Finish here**. Already in chat?
Just type your reply, or `/exit` to leave. When input or output is piped or saved
to a file, this follow-up menu doesn't open.

## Commands

| Command | Aliases | Behavior |
| --- | --- | --- |
| `--query <text>` | `-q` or positional text | Answers one question. In a live terminal, you can continue in chat or review a suggested command. |
| `--chat` | — | Starts a conversation about terminal work. |
| `--memories` | — | Opens the local memory manager to view, create, edit, or delete saved notes. |
| `--diagnose [text]` | `--file <log>` or stdin | Explains an error or log excerpt and suggests what to check next. |
| `--script <request>` | `--file <source>`, `--output <new.ps1>` | Creates or revises a PowerShell script for you to review and save. |
| `--plan <goal>` | `--plan --resume <id>` | Breaks a task into steps you can approve, check, and resume. |
| `--preview <operation>` | `--file`, `--output`, `--prefix`, `--pattern` | Shows which files a rename, copy, move, or deletion would affect. |
| `--recipes [action]` | — | Lists, saves, imports, exports, or reuses personal command recipes. |
| `--setup` | — | Opens Settings with General selected. |
| `--ai-setup` | `--ai-settings` | Opens the same Settings screen with AI selected. |
| `--theme` | — | Opens the same Settings screen with Theme selected and a live palette preview. |
| `--test-ai` | — | Checks that the configured model can answer a short request in your chosen language. |
| `--costs` | — | Shows usage and cost estimates, refreshing public prices and any available organization costs. |
| `--status` | — | Shows your settings and whether local storage is ready. |
| `--about` | `about` | Shows the HELP ME banner, project information, author, and license. |
| `--lenna` | `lenna` | Shows the bundled calibration portrait centered in the terminal, without an AI request. |
| `--third-party` | — | Shows direct runtime packages, versions, and licenses. |
| `--where` | `-where` | Prints the exact running executable and directory, then offers the native file manager or a change-directory command. |
| `--path [install\|remove\|status]` | `--path=<action>` | Manages only the current executable directory in the persistent user PATH. |
| `--install-font` | — | Runs the opt-in JetBrainsMono Nerd Font helper through an existing Oh My Posh installation. |
| `--help` | `-h`, `/?` | Opens a keyboard guide with examples and sections for AI work, insight, setup, and safety. |
| `--version` | `-v` | Shows application, .NET, and platform versions with links to the GitHub repository and creator site. |

Use one main command at a time.

Settings fills the terminal window when it supports fullscreen views and is at
least 60 columns wide and 20 rows tall. Use the sidebar to choose General, AI,
Credentials, Conversation, Commands, Personalization, Theme, or About.
`--setup`, `--ai-setup`, and `--theme` simply choose where you start.

Select About and press Enter or Right to read about the project. When you close
it, you'll return to your settings with unsaved changes still there. About is
also available in the smaller settings menu.

Up/Down selects a section; Enter or Right moves into its fields. F6 switches
between the sidebar and fields, and Ctrl+Left returns to the sidebar. Tab moves
through fields to Save and Cancel. Left/Right changes a field choice or moves
between the buttons when an action has focus; Enter activates the focused button.
Save keeps your changes right away. Cancel or Escape discards them and returns
to your terminal history. Smaller or less capable terminals use a section menu
with the same Save and Cancel actions. Languages show their flag, native name,
and code; `--no-emoji` uses plain text. See [terminal themes](TERMINAL_THEMES.md)
to pick colors or edit a theme file.

`hm --where` shows where your running copy lives. To switch to that folder, it gives you a `Set-Location -LiteralPath '...'` command on Windows, or `cd '...'` on Unix, to run yourself. An app can't change the folder of the shell that launched it. You can also open the folder in your file manager after reviewing and confirming that action.

## Browse the command guide

```powershell
hm --help
```

Help uses the same layout as Settings in supported terminals of at least
60 columns by 20 rows. Use Up/Down to pick a section, then Enter, Right, or Tab
to browse its commands. Up/Down scrolls the list; Home/End jumps to the beginning
or end. F6 or Ctrl+Left takes you back to the sidebar. Tab reaches Close.
Press Esc or Q to leave and return to your earlier terminal output.

The About sidebar entry opens the project information screen with Enter or Right.
Enter or Esc closes About and returns to the guide with its selection preserved.
In scrolling help, the About section shows the direct `hm about` command.

Each entry shows the command, an explanation, and examples you can try.
`hm` stays white; options and values have their own colors. Parts in square
brackets are optional and appear in yellow or amber. The notes below each
example explain which values you can change.

You don't need setup or an API key to read help. In small terminals, when output
is redirected, or when fullscreen views aren't supported, you get a scrolling
command list. Help after an invalid command also stays below the error so you
can still read it. If you shrink an open fullscreen guide too far, it asks you
to enlarge the window or close help.

## About PromptMeUp

```powershell
hm about
hm --about
```

About shows the HELP ME banner, app version, build date and time in UTC, build
machine, supported platforms, author, project links, and MIT license. In a supported live terminal of at least
60 columns by 20 rows, it opens a fullscreen view. Up/Down and PgUp/PgDn scroll
the content; Enter or Esc closes it and restores your terminal history.
Small terminals and redirected output receive a scrolling view. Narrow windows
and terminals without Unicode support use a compact HELP ME wordmark.
The command works offline without setup or an API key.

`hm about` also works with different capitalization, such as `hm About`.
Use `hm --query about` if you want to ask the AI about that word instead.

## Check terminal image rendering

```powershell
hm lenna
hm --lenna
```

This local command shows the bundled Lenna calibration portrait centered in the
terminal. It works offline and needs neither first-run setup nor an API key.
The picture fits the current window while preserving its proportions and the
terminal's scrollback. Use a terminal with ANSI color support; a window that is
too small produces a short explanation instead of a cropped picture.

`hm lenna` also works with different capitalization. Use `hm --query lenna` or a
longer question to ask the AI about Lenna. As elsewhere, use one command at a time.

## Change the model or conversation limits

Open Settings with the AI section selected:

```powershell
hm --ai-setup
```

`hm --ai-settings` opens the same page. Use the sidebar to reach chat limits and
the other settings. Limits use tokens, the small pieces of text a model reads.
Its context window is how much text it can work with at once.

| Setting | What it controls |
| --- | --- |
| AI enabled | Whether AI features are available. |
| Model | The model used for requests. |
| Reasoning effort | The model's reasoning setting. |
| Output detail | Compact, balanced, or detailed responses. |
| AI command review | Adds an AI opinion to the local command checks. You still decide what runs. |
| Prompt caching | Lets OpenAI reuse unchanged parts of requests when supported. |
| Conversation input budget | `4,000`–`200,000` estimated tokens; default `16,000`. |
| Retained user turns | How many question-and-answer exchanges to keep: `2`–`50`; default `12`. |
| Characters per user message | `500`–`100,000`; default `16,000`. |
| Share of the model context window | `10`–`95%`; default `70%`. |

Use the sidebar to open Conversation for its four limits, Commands for execution
limits, or any other section. General contains language; AI contains availability,
model preferences, advisory command review, and caching. Credentials contains key
replacement and an optional connection check. Personalization contains your
personal instructions and location preference.
Save applies all draft changes directly; Cancel keeps the previous settings.
After initial setup, the connection check defaults to off. Saving alone makes no
provider request and does not refresh pricing.

The AI section also lists supported models and cached standard token prices per
million tokens, with the selected model highlighted. Prices cover input, cached
input, and output; missing cached prices are marked unavailable. Use Ctrl+Up and
Ctrl+Down to scroll the overview. Opening it does not refresh prices.

To override the saved input budget, set `PROMPTMEUP_CONTEXT_TOKENS` before launch:

```powershell
$env:PROMPTMEUP_CONTEXT_TOKENS = '24000'
hm --chat
```

The override accepts the same `4000`–`200000` range and applies while that
environment variable is set. It does not change the saved value. The settings
form shows a notice when an override is present. See
[context and usage](#read-context-and-usage) for how the limits work together.

## Reuse personal recipes

```powershell
hm --recipes
hm --recipes save project-check --from-plan <completed-plan-id>
hm --recipes show project-check
hm --recipes run project-check
hm --recipes export project-check --output project-check.json
hm --recipes import --file inspect-folder.json
```

Recipes are saved routines you can run again. Make one from a plan you've
completed and confirmed, or import a JSON recipe you've reviewed. Saving and
exporting ask for confirmation and won't overwrite a recipe or file. Names
start with a letter and use 1–40 ASCII letters, digits, hyphens, or underscores.
You can keep up to 200 recipes.

Each run shows what you'll need and asks for any values, such as a folder name.
It creates a new plan you can pause and resume, with a fresh approval for every
command. Values go into `$hmParameters` as literal data, so they aren't pasted
into the script source. Recipe files keep parameter descriptions, not the values
you entered. The resulting plan and local history do keep the commands with
those values. Recognizable secrets are rejected. Each value can use up to
1,024 characters; the whole command must fit within 4,096 characters.

Recipes saved from completed plans retain the original working directory and
literal commands. To add parameters, export a definition, choose a new name,
declare its parameters, and reference them as `$hmParameters['name']` in commands
and checks before importing it. Set `directory` to `null` for an explicitly
portable recipe that uses the current directory. A minimal import example:

```json
{
  "version": 1,
  "name": "inspect-folder",
  "description": "Inspect a chosen folder.",
  "directory": null,
  "prerequisites": ["PowerShell 7 is available."],
  "parameters": [{ "name": "folder", "description": "Folder to inspect." }],
  "steps": [{
    "label": "List files",
    "command": "Get-ChildItem -LiteralPath $hmParameters['folder'] -ErrorAction Stop",
    "verification": "if (-not (Test-Path -LiteralPath $hmParameters['folder'] -PathType Container)) { exit 1 }",
    "expected": "The chosen folder exists and its listing is visible."
  }]
}
```

## Preview concrete file effects

```powershell
hm --preview rename --file ./logs --pattern '*.log' --prefix archived-
hm --preview copy --file ./report.txt --output ./backup
hm --preview move --file ./logs --pattern '*.log' --output ./archive
hm --preview delete --file ./logs --pattern '*.tmp'
```

This preview runs locally, without AI. It shows the files involved, their sizes,
where they'd go or what would be deleted, and any names already in use.
`--file` accepts a single file or files directly inside a folder. Copy and move
need a destination folder that already exists. The preview doesn't search
subfolders, follow links (including Windows reparse points), or simulate other
shell commands. It can show up to 1,000 matches from 10,000 scanned files.

With redirected input or output, you only get the preview. In a live terminal,
you can choose command review and approve each command separately. A name
already in use blocks the whole batch. Copy and move won't overwrite a file,
even if it appeared after the preview. After approval, `hm` checks file sizes,
timestamps, and links in their paths again. Other apps can still change files
while you work; the preview doesn't lock them. A failed or declined command
stops the remaining operations.

## Follow a resumable plan

Use `hm --plan "Build, test, and package this project"`. PromptMeUp creates one
to eight ordered PowerShell steps, saves their pending state locally, and shows
`hm --plan --resume <id>`. The plan must be resumed from its original directory.

Starting a plan doesn't approve its steps. You review and approve each action
and its separate check command, which is meant only to inspect the result.
After a check succeeds, `hm` asks you to confirm that the result looks right.
A failure, timeout, declined command, or unexpected result pauses the plan.

Just before an action starts, `hm` records its result as `outcome unknown`.
If the app closes unexpectedly, resuming starts with the check; it won't repeat
the action automatically. Only one copy of `hm` can guide a plan at a time.
The saved JSON keeps your goal, starting folder, commands, checks, and progress.
Plan content with recognizable secrets is rejected.

## Create or revise a script

Use `hm --script "Archive old logs with a report" --output archive-logs.ps1`.
To revise a script, add `--file existing.ps1` and choose a new output file. This
interactive flow shows the complete source and a line-by-line replacement diff,
then offers revision, validation, saving, or cancellation. Existing files are
never overwritten. Script input/output supports up to 1 MiB of UTF-8 source by
default. Embedded credentials and redaction placeholders are rejected.

The optional validation action asks you to approve a PowerShell syntax check.
It reads the script without running it and also uses PSScriptAnalyzer if that's
already installed. It doesn't install tools. Passing a syntax check doesn't mean
the script will do the right thing or be safe to run. Saving doesn't run it
either. Your request and the source you select are shared with OpenAI.

### Configure artifact limits

Set these environment variables before starting `hm`. Check their values with
`hm --status`.

| Variable | Default | Accepted values | Scope |
| --- | --- | --- | --- |
| `PROMPTMEUP_MAX_SCRIPT_MIB` | `1` | Integer `1`–`64` | UTF-8 script source, for both reading and saving. |
| `PROMPTMEUP_MAX_PLAN_MIB` | `8` | Integer `1`–`64` | Complete serialized UTF-8 JSON for plans and recipes. |
| `PROMPTMEUP_MAX_ARTIFACT_OUTPUT_TOKENS` | `16384` | Integer `1`–`65536` | Provider output budget for script and plan generation. |

```powershell
$env:PROMPTMEUP_MAX_SCRIPT_MIB = '4'
$env:PROMPTMEUP_MAX_PLAN_MIB = '16'
$env:PROMPTMEUP_MAX_ARTIFACT_OUTPUT_TOKENS = '32768'
hm --status
```

One MiB is 1,048,576 bytes. Reading and saving use the same size limit; lowering it
can block larger existing files. Model context and output limits still apply,
including reasoning tokens. A file fitting locally may be too large for one AI response.

## Diagnose an error

Use `hm --diagnose "restore failed"`, `hm --diagnose --file build.log`, or
`Get-Content build.log | hm --diagnose`. If you don't supply text in a live
terminal, `hm --diagnose` asks you to paste the error or log. Keep secrets out
of command arguments. File and piped input must fit your message limit and be
read within 30 seconds. If the text is empty or too long, pick a smaller excerpt.
`hm` removes secrets it recognizes before sending the text to OpenAI, but check
the excerpt for other private details yourself.

The answer explains what the log shows, what might have caused it, what's still
unclear, and what to check next. In a live terminal, you review and approve any
suggested check before it runs. Piped input only produces suggestions.

## Global options

| Option | Behavior |
| --- | --- |
| `--language <code>` / `-l <code>` | Uses `it`, `en`, `fr`, `de`, `es`, or `vi` for this run. |
| `--no-animation` | Turns off progress animations and other optional animation. |
| `--no-emoji` | Uses plain text symbols in place of emoji. |
| `--yes` / `-y` | Approves a PATH or font operation you've already reviewed; it never approves chat commands. |
| `--dry-run` | Previews Nerd Font installation without running Oh My Posh. |

`--dry-run` is accepted only with `--install-font`. Unknown options, unsupported languages, invalid PATH actions, missing values, and conflicting commands return exit code `2` without guessing.

PromptMeUp helps with terminal tasks. It doesn't generate images or write, edit, or translate general prose. AI questions include a filtered summary of your current folder, operating system, shell, CPU, memory, and GPU name when available, so the suggested commands fit your machine.

## Keep a conversation moving

Opening chat or starting a question shows a compact reminder of the saved-memory
commands. The reminder appears once, including when a question continues into chat.

| Control | Behavior |
| --- | --- |
| `/run <command>` | Checks risks locally and, optionally, with AI. Shows the exact command and asks before running it. Captured output can be shared in a follow-up. |
| `/remember [global\|project] <text>` | Saves a note for next time. Notes belong to the current project unless you choose `global`. |
| `/memories` | Lists saved global and current-project notes with their IDs. |
| `/forget <id>` | Deletes a saved global or current-project note from future selection. |
| `/clear` | Starts fresh in chat. Saved notes, usage counters, and earlier activity history stay. |
| `/costs` | Shows the cost dashboard without ending the chat. |
| `/status` or `/context` | Shows current context, the input budget, loaded notes, and token usage. |
| `/exit` | Ends the conversation. |
| `Esc` | Closes help or cancels the current interactive command without saving unfinished settings. |
| `Ctrl+C` | Cancels the whole application and returns exit code `130`. |

You must answer the approval prompt in a live terminal for every `/run` command.

### Remember something for next time

Open **Memories** in the left sidebar of Help (`hm`) or Settings (`hm --setup`), or run
`hm --memories`, to manage saved notes without an AI connection. Select a note to
read its full text, edit its text or scope, or delete it after confirmation. Choose
**Create** to add a project or global note. Saving a note applies immediately;
canceling its editor leaves the stored note unchanged. Returning to Settings keeps
any unfinished settings draft.

Save a preference or project fact in chat so you do not have to repeat it:

```text
/remember project Build this project with dotnet build.
/remember global Keep terminal explanations concise.
/memories
/forget <id>
```

Omit `project` to use the default project scope. Project notes belong to the
nearest parent directory containing `.git`, including the current directory. If
there is no Git repository, they belong to the current directory. Global notes
are available across projects that use the same local data directory.

Only notes you explicitly save become memories. Each note can contain up to
1,000 characters, with room for 100 global notes and 100 notes per project.
`/memories` lists the notes available here; use an ID from that list with `/forget`.

For each query or chat request, PromptMeUp considers global notes and project
notes that share words with the request. It loads at most five notes, within
800 estimated tokens including their formatting. Selection happens locally,
without an extra AI call. Saving more notes does not make every request larger.

Selected notes are sent to the provider and can appear in the redacted request
history. Notes cannot authorize a command or override your latest request.
Recognizable credentials are rejected when saving and redacted again when notes
are loaded. The notes live in the local SQLite database; project identifiers are
stored as hashes of their directory paths.

Use `/clear` to reset the conversation while keeping your notes. Use `/forget`
to remove a saved note from future selection. Neither command erases earlier
audit records or recorded usage, and `/forget` does not remove text already in
the active conversation.

### Ask about PromptMeUp

Ask in your own words, for example, "How do I change this app's conversation
budget?" or "How do saved memories work?" The assistant can request relevant
chapters from the guide included with your installed version. No special chat
command is needed.

PromptMeUp supplies up to two chapters in your selected language, then makes
one additional AI call for the answer. The guide stays available for follow-up
questions. New topics can replace earlier chapters; `/clear` removes the loaded
guide from the active context. Reading a chapter only reads packaged help. It
does not run commands or open your files or settings.

Both AI calls count toward token usage and estimated costs. If the guide and
your question cannot fit the configured input limit, the app explains the limit
instead of sending an oversized request. There is at most one guide-loading
round per question.

### Read context and usage

Use `/status` or `/context` during chat to see how much context is in use. The
same information appears after a completed answer. `hm --status`, run from your
shell, shows local configuration and storage readiness instead.

| Measure | How to read it |
| --- | --- |
| Active context | Estimated input currently retained, compared with the model's context window. |
| Context budget | That same input compared with your configured input limit. |
| System | Estimated instruction tokens, including supplied runtime details, message overhead, and any loaded guide. |
| Guide, included in system | The guide's share of system tokens; it is already included in the total. |
| User | Retained user text and selected saved-note data. |
| AI replies | Retained assistant text, including the latest answer when it fits. |
| Loaded memories | The number of selected notes and their share of the 800-token allowance. |
| Last request | Input and output tokens reported by the provider for the last assistant call. |
| Session usage | Input and output tokens accumulated across this conversation's recorded calls. |

A `~` marks an estimate. The budget bar uses separate colors for system, user,
and assistant content, with the remaining space left available. A numeric legend
keeps the categories clear without color. Guide tokens belong to System and
are not added twice. Reported output usage can exceed retained reply text because
the provider also counts tokens that are not kept as visible conversation text.

Active context includes instructions, machine and directory context, loaded
guide chapters, selected notes, and retained messages. It is recalculated
after old turns are removed. Your next question is included only once you enter
it, so the estimate can change before sending.

Session usage keeps growing when the same history is sent again. It measures
tokens used over time, not how much space the current conversation occupies.
Refreshing status or using `/clear` keeps the last-request and session counters.
Calls that fail but report usage count toward session totals. AI command reviews
are recorded separately and appear in the overall `/costs` totals.

For a guide-assisted answer, This turn includes both the guide-selection and
answer calls. Last request still shows only the final call, while Session usage
includes both. A turn or session cost is unavailable if a recorded billable call
has no known price, so a partial amount is not presented as a complete total.

Ordinary query and chat start with a 16,000-token input budget. The actual limit
is the smallest of your saved or overridden budget, your chosen percentage of
the model window, and the space left after reserving output tokens. Instructions
and notes count toward that limit, along with the conversation.

As the conversation grows, PromptMeUp removes the oldest complete turns to fit
the budget and turn limit. If a new request cannot fit even without older turns,
it shows an explanation and keeps chat open so you can shorten the request.
Completed answers are shown in full even when they are too large to keep for
the next turn. The saved or overridden input-token budget applies to ordinary
query and chat; scripts, plans, and diagnostics have their own input budgets.

See [memory, costs, and caching](OPENAI_COSTS_AND_CACHING.md) for more detail.

When an answer suggests commands, the first and default menu choice is **Do not execute commands**. Picking a command opens its preview and approval prompt. It doesn't run it yet.

## First run and redirected output

With no explicit command, `hm` opens help, including on first launch. Redirected output prints the command reference without opening an interactive view. Run `hm --setup` when you want to configure the application.

Use [`hm --ai-setup`](#change-the-model-or-conversation-limits) to select AI in
Settings, or `hm --setup` to select General. Conversation limits, including the
input budget, are in the Conversation section of that same screen.

General also shows your setup, recorded usage, and saved cost estimates. Use
`Ctrl+Up` / `Ctrl+Down` to scroll when the overview doesn't fit. You can also use
`--status` and `--costs` on their own.

Read-only commands such as `--help`, `--about`, `--version`, `--status`, `--third-party`, and `--path=status` support redirected output. Mutating PATH and font operations require a live prompt or `--yes`; font dry-run is non-mutating and can run unattended.

## Take `hm` with you

`hm --path install` does not copy, download, or install the application. It previews and adds the directory containing the current executable:

- Windows: current-user `PATH` environment variable;
- zsh: a marked block in `~/.zprofile`;
- fish: a marked block in `~/.config/fish/config.fish`;
- other Unix shells: a marked block in `~/.profile`.

Removal touches only the exact Windows entry or the clearly marked PromptMeUp block. Companion `hm-path.ps1` and `hm-path.sh` scripts call the same application flow.

## Data-directory override

Set `PROMPTMEUP_DATA_DIR` before launch to place the database and logs in a specific directory:

```powershell
$env:PROMPTMEUP_DATA_DIR = 'D:\PortableData\PromptMeUp'
hm --status
```

The prompt YAML resources remain beside the application because they are versioned product assets.

## Exit codes

| Code | Meaning |
| ---: | --- |
| `0` | Command completed or a user safely cancelled an optional operation. |
| `1` | The app, AI request, data storage, or a validation check failed. |
| `2` | Invalid command, or setup is needed but no interactive terminal is available. |
| `130` | Operation cancelled with Ctrl+C. |

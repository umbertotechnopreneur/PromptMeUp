# PromptMeUp CLI reference

Ask `hm` a terminal question, check its answer, and choose whether to run a suggested command. `hm` means **help me** and works on Windows, Linux, and macOS.

## Ask or choose a command

```text
hm [question]
hm [command] [options]
```

A positional phrase is treated as one question:

```powershell
hm "Which files have changed in Git?"
hm how do I list running dotnet processes?
```

Quote a question when the current shell would otherwise interpret punctuation, variables, pipes, redirection, or wildcard characters.

If your request is unclear, the assistant asks a focused question before suggesting
commands. In a live terminal, choose **Clarify the request or continue in chat** to
answer with more detail in the same conversation, keeping your original request
and the assistant's answer. Choose **Finish here** to stop when no commands were
suggested. In an existing chat, simply type the clarification at the next prompt;
`/exit` ends the conversation. Redirected input or output does not open an
interactive continuation menu.

## Commands

| Command | Aliases | Behavior |
| --- | --- | --- |
| `--query <text>` | `-q` or positional text | Answers one question. In a live terminal, you can continue in chat or review a suggested command. |
| `--chat` | — | Starts a conversation about terminal work. |
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
| `--status` | — | Shows local configuration and storage readiness. |
| `--about` | `about` | Shows the HELP ME banner, project information, author, and license. |
| `--lenna` | `lenna` | Shows the bundled calibration portrait centered in the terminal, without an AI request. |
| `--third-party` | — | Shows direct runtime packages, versions, and licenses. |
| `--where` | `-where` | Prints the exact running executable and directory, then offers the native file manager or a change-directory command. |
| `--path [install\|remove\|status]` | `--path=<action>` | Manages only the current executable directory in the persistent user PATH. |
| `--install-font` | — | Runs the opt-in JetBrainsMono Nerd Font helper through an existing Oh My Posh installation. |
| `--help` | `-h`, `/?` | Opens a keyboard guide with examples and sections for AI work, insight, setup, and safety. |
| `--version` | `-v` | Shows application, .NET, and platform versions with links to the GitHub repository and creator site. |

Only one top-level command can be selected per invocation.

Settings uses one fullscreen workspace in supported live terminals of at least
60 columns by 20 rows. Its left sidebar keeps General, AI, Credentials,
Conversation, Commands, Personalization, and Theme visible. Each settings switch
only selects the initial section; every section remains accessible.

Up/Down selects a section; Enter or Right moves into its fields. F6 switches
between the sidebar and fields, and Ctrl+Left returns to the sidebar. Tab moves
through fields to Save and Cancel. Left/Right changes a field choice or moves
between the buttons when an action has focus; Enter activates the focused button.
Save stores the draft directly. Cancel or Escape discards it and restores the
original terminal history. There is no separate summary or confirmation step.
Smaller or less capable terminals use a section menu with the same Save and
Cancel actions. Language choices include their country flag, native name, and
code, with plain text alternatives under `--no-emoji`. See
[terminal themes](TERMINAL_THEMES.md) for the built-in palettes and their editable
JSON definitions.

`hm --where` cannot change the working directory of the shell that launched it because child processes cannot modify their parent process. Its change-directory action therefore prints an exact `Set-Location -LiteralPath '...'` command on Windows (or `cd '...'` on Unix) for the user to run in the current terminal. Opening the native file manager always shows an exact preview and requires confirmation.

## Browse the command guide

```powershell
hm --help
```

In a supported live terminal of at least 60 columns by 20 rows, help opens a
fullscreen guide with the same header, section list, and spacing as setup.
The app version and project link sit at the right of the header. Sections have
descriptive icons without number prefixes. Up/Down selects a section; Enter,
Right, or Tab moves into its commands. There, Up/Down scrolls longer sections,
and Home/End jumps to their beginning or end. F6 or Ctrl+Left returns to the list.
The sidebar remains beside the commands at every supported fullscreen width.
Help and settings share the same responsive workspace and button renderer;
Tab reaches the emoji Close button in the footer.
Esc or Q closes the guide and restores your original terminal output.

Each entry starts with its command syntax. Descriptions and concrete examples
are indented two spaces, and argument notes four spaces; wrapped lines preserve
these insets. The lowercase `hm` stays white in every theme, including in the
header and scrolling reference; options and arguments use distinct colors so you
can read the command's parts at a glance. Parameter explanations keep those same
colors and explain what each argument means and which values you can change.
Optional groups in square brackets use the theme's yellow/amber warning color,
including groups with spaces or nested options, so they stand apart from switches.

Help works without setup or an API key. Small terminals, redirected output, and
terminals without an alternate buffer receive the full scrolling reference.
Help shown after an invalid command also stays in the current buffer, so the
error remains visible. Resizing an open guide redraws it; a window below the
minimum size shows a short instruction until you enlarge it or close help.

## About PromptMeUp

```powershell
hm about
hm --about
```

About presents the HELP ME banner with bright white H and M letters and a cyan,
violet, and pink gradient, followed by the app version, supported platforms,
author, project links, and MIT license. In a supported live terminal of at least
60 columns by 20 rows, it opens a fullscreen view. Up/Down and PgUp/PgDn scroll
the content; Enter or Esc closes it and restores your terminal history.
Small terminals and redirected output receive a scrolling view. Narrow windows
and terminals without Unicode support use a compact HELP ME wordmark.
The command works offline without setup or an API key.

Only the standalone positional word `about` selects this command, regardless of
casing. Use `hm --query about` to ask an AI question containing that word.

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

Only the standalone positional word `lenna` selects this command, regardless of
casing. `hm --query lenna` and longer questions that mention Lenna remain AI
queries. Use one command per invocation, as with the other commands.

## Change the model or conversation limits

Open Settings with the AI section selected:

```powershell
hm --ai-setup
```

`hm --ai-settings` is an equivalent alias. AI preferences and conversation limits
live in the same workspace as the other settings:

| Setting | What it controls |
| --- | --- |
| AI enabled | Whether AI features are available. |
| Model | The model used for requests. |
| Reasoning effort | The model's reasoning setting. |
| Output detail | Compact, balanced, or detailed responses. |
| AI command review | Whether the model adds an advisory review to the local command checks. |
| Prompt caching | Whether requests use supported provider caching. |
| Conversation input budget | `4,000`–`200,000` estimated tokens; default `16,000`. |
| Retained user turns | `2`–`50`; default `12`. |
| Characters per user message | `500`–`100,000`; default `16,000`. |
| Share of the model context window | `10`–`95%`; default `70%`. |

Use the sidebar to open Conversation for its four limits, Commands for execution
limits, or any other section. General contains language; AI contains availability,
model preferences, advisory command review, and caching. Credentials contains key
replacement and an optional connection check. Personalization contains the
instruction preamble and location preference.
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

Save a fully completed and confirmed plan as a local recipe, or import a reviewed
JSON definition. Saves and exports require confirmation and never overwrite an
existing recipe or file. Names use 1–40 ASCII letters, digits, hyphens, or
underscores and start with a letter. Local libraries contain at most 200 recipes.

Every run shows prerequisites and asks for parameter values interactively, then
creates a new resumable plan with fresh per-command approvals. Parameter values
are bound as literal entries in `$hmParameters`; they are never substituted into
the source text. Definitions store parameter descriptions, not invocation values.
The resulting run plan and ordinary audit retain the bound commands locally;
recognizable credentials are rejected. Each value is limited to 1,024 characters,
and the complete bound command must fit the plan's 4,096-character limit.

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

This local-only flow displays source, destination or deletion, byte counts, and
collisions. `--file` accepts one file or the immediate files of a directory;
copy/move require an existing destination directory. No directory recursion,
symbolic links, reparse points, or arbitrary shell-command simulation is supported.
Preview is limited to 1,000 matching files and 10,000 scanned files.

Redirected invocations only inspect. In a live terminal, opt into command review,
then approve every generated command separately. A collision blocks the whole
batch. Copy/move never overwrite existing destinations, including destinations
created after preview. Source size/time and link ancestry are checked again after
approval. This is a snapshot, not a filesystem lock: other processes can still
change files. A failure or declined command stops remaining operations.

## Follow a resumable plan

Use `hm --plan "Build, test, and package this project"`. PromptMeUp creates one
to eight ordered PowerShell steps, saves their pending state locally, and shows
`hm --plan --resume <id>`. The plan must be resumed from its original directory.

Starting guidance does not authorize any step. Each action and its separate,
read-only verification command receives the normal risk review, exact preview,
and individual confirmation. A successful verification is followed by a user
check against the declared expected result. Failure, timeout, denial, or a result
that does not match pauses the plan before later actions start.

Before starting an action, its state becomes `outcome unknown`. After a crash or
interruption, resume runs the verification first and never repeats that action
automatically. One process holds an exclusive lease while guiding a plan. Saved
plan JSON contains the goal, original directory, commands, verification, and
progress; credential-bearing plan content is rejected.

## Create or revise a script

Use `hm --script "Archive old logs with a report" --output archive-logs.ps1`.
To revise a script, add `--file existing.ps1` and choose a new output file. This
interactive flow shows the complete source and a line-by-line replacement diff,
then offers revision, validation, saving, or cancellation. Existing files are
never overwritten. Script input/output supports up to 1 MiB of UTF-8 source by
default. Embedded credentials and redaction placeholders are rejected.

The optional validation action previews a PowerShell parser command for explicit
approval. It parses the source as literal data and uses PSScriptAnalyzer if already
installed; it never evaluates the generated script or installs tooling. Syntax
success does not establish semantic correctness or safety. Saving does not run
the script. Requests and selected source are shared with the AI provider.

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
`Get-Content build.log | hm --diagnose`. With no supplied source in a live terminal,
`hm --diagnose` asks for evidence. Do not put credentials in command arguments.
File and pipe input is bounded by the configured message limit, with a 30-second
read deadline. Empty or oversized evidence is rejected; select a smaller excerpt.
Recognizable credentials are redacted before AI transmission. The selected text
is still shared with the provider, so choose the excerpt deliberately.

The answer separates observations, probable causes, missing evidence, and the
next verification. In a live terminal, a suggested check uses the existing exact
preview and per-command approval flow. Piped invocations only render suggestions.

## Global options

| Option | Behavior |
| --- | --- |
| `--language <code>` / `-l <code>` | Uses `it`, `en`, `fr`, `de`, `es`, or `vi` for this invocation. |
| `--no-animation` | Disables Spectre progress and other optional animation. |
| `--no-emoji` | Uses portable ASCII fallbacks for semantic emoji and markers. |
| `--yes` / `-y` | Preauthorizes only an already reviewed PATH or font operation; it never authorizes chat commands. |
| `--dry-run` | Previews Nerd Font installation without running Oh My Posh. |

`--dry-run` is accepted only with `--install-font`. Unknown options, unsupported languages, invalid PATH actions, missing values, and conflicting commands return exit code `2` without guessing.

PromptMeUp is scoped to terminal tasks. It does not generate images or rewrite, proofread, translate, or compose ordinary prose. Each AI request receives a privacy-filtered snapshot of the current directory, platform/shell, CPU, memory, and available GPU label so its command guidance matches the machine in use.

## Keep a conversation moving

| Control | Behavior |
| --- | --- |
| `/run <command>` | Performs local and optional AI risk review, displays the exact command, asks for authorization, runs it if approved, and offers its bounded result to the next AI turn. |
| `/remember [global\|project] <text>` | Saves an explicit persistent note; the default scope is the current project. |
| `/memories` | Lists saved global and current-project notes with their IDs. |
| `/forget <id>` | Deletes a saved global or current-project note from future selection. |
| `/clear` | Starts with fresh conversation context. Saved notes, usage counters, and earlier audit records remain. |
| `/costs` | Shows the cost dashboard without ending the chat. |
| `/status` or `/context` | Shows current context, the input budget, loaded notes, and token usage. |
| `/exit` | Ends the conversation. |
| `Esc` | Closes help or cancels the current interactive command without saving unfinished settings. |
| `Ctrl+C` | Cancels the whole application and returns exit code `130`. |

There is no command that silently approves `/run`. The authorization prompt must be answered in a live terminal for every command.

### Remember something for next time

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

### Read context and usage

Use `/status` or `/context` during chat to see how much context is in use. The
same information appears after a completed answer. `hm --status`, run from your
shell, shows local configuration and storage readiness instead.

| Measure | How to read it |
| --- | --- |
| Active context | Estimated input currently retained, compared with the model's context window. |
| Context budget | That same input compared with your configured input limit. |
| Loaded memories | The number of selected notes and their share of the 800-token allowance. |
| Last request | Input and output tokens reported by the provider for the last assistant call. |
| Session usage | Input and output tokens accumulated across this conversation's recorded calls. |

A `~` marks an estimate. Active context includes instructions, machine and
directory context, selected notes, and retained messages. It is recalculated
after old turns are removed. Your next question is included only once you enter
it, so the estimate can change before sending.

Session usage keeps growing when the same history is sent again. It measures
tokens used over time, not how much space the current conversation occupies.
Refreshing status or using `/clear` keeps the last-request and session counters.
Calls that fail but report usage count toward session totals. AI command reviews
are recorded separately and appear in the overall `/costs` totals.

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

When an AI answer cites command candidates, PromptMeUp presents a menu whose first and default item is **Do not execute commands**. Picking a candidate only opens the normal exact-preview and authorization flow; it never runs a command by itself.

## First run and redirected output

With no explicit command, `hm` opens help, including on first launch. Redirected output prints the command reference without opening an interactive view. Run `hm --setup` when you want to configure the application.

Use [`hm --ai-setup`](#change-the-model-or-conversation-limits) to select AI in
Settings, or `hm --setup` to select General. Conversation limits, including the
input budget, are in the Conversation section of that same screen.

General also contains the application-status and cached usage/cost overview. Use
`Ctrl+Up` / `Ctrl+Down` to scroll that overview in a short terminal. The standalone
`--status` and `--costs` commands remain available. The operating-budget bar occupies
its own row with its label aligned to the session overview's first label column.

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
| `1` | Runtime, provider, persistence, or validation failure. |
| `2` | Invalid command line or setup required in a non-interactive invocation. |
| `130` | Operation cancelled with Ctrl+C. |

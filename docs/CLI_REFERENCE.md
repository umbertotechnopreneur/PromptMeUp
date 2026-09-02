# CLI reference

`hm`, short for **help me**, turns a terminal question into a clear answer and an optional, explicitly approved next step. The same two-letter public command is used on Windows, Linux, and macOS.

## Ask or choose a command

```text
hm [question]
hm [command] [options]
```

A positional phrase is treated as one question:

```powershell
hm "come vedo quali file sono cambiati in git?"
hm how do I list running dotnet processes?
```

Quote a question when the current shell would otherwise interpret punctuation, variables, pipes, redirection, or wildcard characters.

## Commands

| Command | Aliases | Behavior |
| --- | --- | --- |
| `--query <text>` | `-q` or positional text | Starts one AI session, renders one answer, then offers a safe choice to start chat or inspect a cited command. |
| `--chat` | — | Opens a bounded interactive conversation for Windows, macOS, or Linux console work. |
| `--setup` | — | Opens the full first-run and AI settings form. |
| `--test-ai` | — | Runs the localized `connection-test.yaml` prompt and verifies the exact expected response. |
| `--costs` | — | Forces a pricing refresh, optionally refreshes organization costs, and renders the cost dashboard. |
| `--status` | — | Shows local configuration and storage readiness. |
| `--third-party` | — | Shows direct runtime packages, versions, and licenses. |
| `--where` | `-where` | Prints the exact running executable and directory, then offers the native file manager or a change-directory command. |
| `--path [install\|remove\|status]` | `--path=<action>` | Manages only the current executable directory in the persistent user PATH. |
| `--install-font` | — | Runs the opt-in JetBrainsMono Nerd Font helper through an existing Oh My Posh installation. |
| `--help` | `-h`, `/?` | Shows the command reference grouped by AI work, insight, setup, and safety. |
| `--version` | `-v` | Shows the PromptMeUp About box, application/.NET/platform details, GitHub repository, and creator site. |

Only one top-level command can be selected per invocation.

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
never overwritten. Script input/output is limited to 12,000 characters; embedded
credentials and redaction placeholders are rejected.

The optional validation action previews a PowerShell parser command for explicit
approval. It parses the source as literal data and uses PSScriptAnalyzer if already
installed; it never evaluates the generated script or installs tooling. Syntax
success does not establish semantic correctness or safety. Saving does not run
the script. Requests and selected source are shared with the AI provider.

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

`hm --where` cannot change the working directory of the shell that launched it because child processes cannot modify their parent process. Its change-directory action therefore prints an exact `Set-Location -LiteralPath '...'` command on Windows (or `cd '...'` on Unix) for the user to run in the current terminal. Opening the native file manager always shows an exact preview and requires confirmation.

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
| `/clear` | Clears active in-memory context. Persistent session events remain intact. |
| `/costs` | Shows the cost dashboard without ending the chat. |
| `/status` | Reprints the compact session snapshot. |
| `/exit` | Closes the session and marks its ledger complete. |
| `Esc` | Cancels the current interactive command; from the command center it exits the current flow. |
| `Ctrl+C` | Cancels the whole application and returns exit code `130`. |

There is no command that silently approves `/run`. The authorization prompt must be answered in a live terminal for every command.

When an AI answer cites command candidates, PromptMeUp presents a menu whose first and default item is **Do not execute commands**. Picking a candidate only opens the normal exact-preview and authorization flow; it never runs a command by itself.

## First run and redirected output

With no explicit command and no completed setup, `hm` opens setup. If input is redirected, PromptMeUp exits with an explanation instead of attempting an interactive form.

Read-only commands such as `--help`, `--version`, `--status`, `--third-party`, and `--path=status` support redirected output. Mutating PATH and font operations require a live prompt or `--yes`; font dry-run is non-mutating and can run unattended.

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

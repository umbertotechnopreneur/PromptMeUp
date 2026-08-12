# CLI reference

The executable and .NET tool command are both named `hm`, short for **help me**.

## Invocation

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
| `--query <text>` | `-q` or positional text | Starts one AI session, renders one answer, and closes the session. |
| `--chat` | — | Opens a bounded interactive conversation. |
| `--setup` | — | Opens the full first-run and AI settings form. |
| `--test-ai` | — | Runs the localized `connection-test.yaml` prompt and verifies the exact expected response. |
| `--costs` | — | Forces a pricing refresh, optionally refreshes organization costs, and renders the cost dashboard. |
| `--status` | — | Shows local configuration and storage readiness. |
| `--third-party` | — | Shows direct runtime packages, versions, and licenses. |
| `--path [install\|remove\|status]` | `--path=<action>` | Manages only the current executable directory in the persistent user PATH. |
| `--install-font` | — | Runs the opt-in JetBrainsMono Nerd Font helper through an existing Oh My Posh installation. |
| `--help` | `-h`, `/?` | Shows the command reference. |
| `--version` | `-v` | Shows the application, .NET, and runtime versions. |

Only one top-level command can be selected per invocation.

## Global options

| Option | Behavior |
| --- | --- |
| `--language <code>` / `-l <code>` | Uses `it`, `en`, `fr`, `de`, `es`, or `vi` for this invocation. |
| `--no-animation` | Disables the test-response teletype effect and other optional animation. |
| `--no-emoji` | Uses an ASCII banner marker instead of the Nerd Font icon. |
| `--yes` / `-y` | Preauthorizes only an already reviewed PATH or font operation; it never authorizes chat commands. |
| `--dry-run` | Previews Nerd Font installation without running Oh My Posh. |

`--dry-run` is accepted only with `--install-font`. Unknown options, unsupported languages, invalid PATH actions, missing values, and conflicting commands return exit code `2` without guessing.

## Chat controls

| Control | Behavior |
| --- | --- |
| `/run <command>` | Performs local and optional AI risk review, displays the exact command, asks for authorization, runs it if approved, and offers its bounded result to the next AI turn. |
| `/clear` | Clears active in-memory context. Persistent session events remain intact. |
| `/costs` | Shows the cost dashboard without ending the chat. |
| `/status` | Reprints the fixed runtime status contract. |
| `/exit` | Closes the session and marks its ledger complete. |

There is no command that silently approves `/run`. The authorization prompt must be answered in a live terminal for every command.

## First run and redirected output

With no explicit command and no completed setup, `hm` opens setup. If input is redirected, PromptMeUp exits with an explanation instead of attempting an interactive form.

Read-only commands such as `--help`, `--version`, `--status`, `--third-party`, and `--path=status` support redirected output. Mutating PATH and font operations require a live prompt or `--yes`; font dry-run is non-mutating and can run unattended.

## Portable PATH management

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

<h1 align="center">PromptMeUp</h1>

<p align="center"><strong>Two letters between a question and a safer next step.</strong></p>

<p align="center">
  Ask <code>hm</code> how to do something from your terminal. Get a clear answer, keep the conversation moving,
  and—only when you choose—preview and authorize the exact command that should run.
</p>

<p align="center">
  <a href="#meet-hm"><strong>Meet hm</strong></a>
  ·
  <a href="#start-a-conversation"><strong>Start locally</strong></a>
  ·
  <a href="docs/PRIVACY.md"><strong>Read the privacy model</strong></a>
</p>

<p align="center">
  <a href="https://github.com/umbertotechnopreneur/PromptMeUp/actions/workflows/quality.yml"><img src="https://github.com/umbertotechnopreneur/PromptMeUp/actions/workflows/quality.yml/badge.svg" alt="Manual quality gate" /></a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&amp;logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-4F46E5" alt="Windows, Linux, and macOS" />
  <img src="https://img.shields.io/badge/status-early%20preview-F59E0B" alt="Early preview" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-22C55E" alt="MIT License" /></a>
</p>

## Meet `hm`

`hm` stands for **help me**. It is the small command you reach for when the terminal knows what happened but you do not yet know the next command.

```powershell
hm "come annullo l'ultimo commit locale senza perdere le modifiche?"
```

PromptMeUp is designed around three everyday moments:

- **Ask naturally.** Describe the result you want instead of remembering every flag.
- **Understand first.** Read a concise answer with simple, safe Markdown rendered directly in the terminal.
- **Act deliberately.** Preview an exact PowerShell command, inspect a local and optional AI risk review, then explicitly authorize or cancel it.

Nothing in a model response runs automatically. Every command begins as a proposal.

## A focused assistant, not another terminal platform

PromptMeUp keeps the experience intentionally light:

1. Ask a one-off question with `hm "…"`, or open a short conversation with `hm --chat`.
2. Continue for a few turns while a bounded in-memory context keeps the thread coherent.
3. Type `/run <command>` when a proposed command is worth trying.
4. Review the exact command, its risk score, and a plain-language description.
5. Approve it yourself. PromptMeUp captures bounded output and can ask the model to explain the result in the next turn.

The fixed status strip keeps the useful numbers visible: provider, model, thinking level, prompt cost, response cost, session cost, context usage, and cache read/write tokens.

## Safety is part of the interaction

PromptMeUp does not treat fluent AI text as authorization.

- A deterministic local review always scores common read-only, network, privileged, destructive, and broad-filesystem patterns.
- An OpenAI review can add context when enabled, but it remains advisory and can never approve execution.
- The exact command is always shown before authorization.
- PromptMeUp starts commands as the current user through `pwsh -NoProfile -NonInteractive` and never requests elevation itself; a command that explicitly asks for higher privileges is flagged for review and may still trigger the platform's own prompt.
- Authorization expires and applies only to the displayed command.
- Captured output has time and size limits.
- Recognizable API keys, bearer tokens, and credential assignments are redacted before local persistence or an AI follow-up.

PromptMeUp is not a sandbox. The user remains responsible for every approved command and should inspect paths, arguments, and expected effects.

## Start a conversation

PromptMeUp is currently an early source preview; public binary releases are not published yet.

Requirements:

- .NET 10 SDK;
- Git;
- PowerShell 7 (`pwsh`) for command execution and Windows repository helpers;
- an OpenAI API key for AI features.

```powershell
git clone https://github.com/umbertotechnopreneur/PromptMeUp.git
Set-Location .\PromptMeUp
dotnet build .\PromptMeUp.slnx --configuration Release
dotnet run --project .\PromptMeUp\PromptMeUp.csproj -- --setup
```

The first interactive launch also opens setup automatically. The AS/400-inspired form lets you choose the interface language, model, thinking level, answer detail, optional instructions, command review, prompt caching, and short-memory limits. It can finish with a small teletype connection test.

On Windows, a key entered in setup is written to the current user's `OPENAI_API_KEY` environment variable and made available to the running process. On Linux and macOS it is loaded only for that process; PromptMeUp then tells you to export it through your shell or preferred secret manager. Keys are never stored in SQLite or accepted as command-line arguments.

```powershell
$env:OPENAI_API_KEY = 'your-key-from-your-secret-store'
hm --setup
```

`OPENAI_ADMIN_KEY` is optional. When present, `hm --costs` can also request organization-level cost buckets; ordinary prompts need only `OPENAI_API_KEY`.

## Everyday commands

| Command | Experience |
| --- | --- |
| `hm "question"` | Ask one question and close the session. |
| `hm --chat` | Open a short multi-turn conversation. |
| `hm --setup` | Reopen the full AI and memory setup. |
| `hm --test-ai` | Run the localized YAML connection test with teletype output. |
| `hm --costs` | Refresh and show model pricing, local estimates, and optional organization cost. |
| `hm --status` | Show setup, key readiness, database, logs, prompt resources, and price-cache state. |
| `hm --language fr` | Use `it`, `en`, `fr`, `de`, `es`, or `vi` for this invocation. |
| `hm --third-party` | Show a polished list of direct runtime dependencies and licenses. |
| `hm --path status` | Inspect the portable PATH entry. |
| `hm --install-font --dry-run` | Preview the optional JetBrainsMono Nerd Font helper. |

Inside chat:

- `/run <command>` starts the mandatory review and authorization flow;
- `/status` shows the current session status strip;
- `/costs` shows the local cost dashboard;
- `/clear` clears active short-term context but keeps the audit ledger;
- `/exit` closes the session.

Use `hm --help` for the compact reference or read [CLI reference](docs/CLI_REFERENCE.md).

## Portable by design

PromptMeUp has no application installer. Publish a folder, move that folder wherever you want, and add or remove that exact folder from your user `PATH`.

```powershell
dotnet publish .\PromptMeUp\PromptMeUp.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output .\artifacts\publish\win-x64

.\artifacts\publish\win-x64\hm.exe --path install
```

The `--path` screen always previews the exact directory and persistence target. Use `--yes` only for an already reviewed non-interactive install or removal. Companion scripts are included in publish output:

```powershell
pwsh -NoProfile -File .\hm-path.ps1 -Action install
pwsh -NoProfile -File .\hm-path.ps1 -Action remove
```

Linux and macOS builds include `hm-path.sh`, which maintains one clearly marked block in the appropriate user shell profile. Supported publish targets are `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`.

## Premium terminal details, optional font

Colors, panels, progress, tables, and status bars work in a normal modern terminal. A Nerd Font adds the intended icon treatment.

```powershell
hm --install-font --dry-run
hm --install-font
```

The installer is opt-in, Windows-only, and follows the existing `oh-my-posh font install JetBrainsMono --headless` path. PromptMeUp does not install Oh My Posh. Select the font in your terminal profile after installation. `--no-emoji` and `--no-animation` keep output usable in constrained terminals and automation.

## Short memory with a complete local trail

Active chat memory is deliberately simple: a sliding window of recent complete turns. Setup controls the maximum number of turns, message size, context-window percentage, command-output size, and execution timeout. When a limit is reached, the oldest complete turns leave active context; PromptMeUp does not invent a summary.

The local SQLite ledger is separate from active memory. It records session headers, ordered prompt/response events, normalized token and cost usage, command authorization activity, bounded command output, and flexible JSON audit payloads. This makes a short session reviewable even after its active model context has been pruned.

Prompt templates and metadata live in `/prompt` as localized YAML resources. Stable instructions are placed before changing conversation content so OpenAI prompt caching can work by default. Read [Memory, costs, and caching](docs/OPENAI_COSTS_AND_CACHING.md) for the exact behavior.

## What leaves the computer

When AI is enabled, PromptMeUp sends the selected YAML instruction, configured optional instruction, bounded active conversation, and any explicitly authorized command result used for a follow-up to the configured OpenAI Responses endpoint. The optional command-risk review sends a redacted form of the proposed command. The optional location setting sends coarse culture and time-zone context, not a requested precise position.

Local diagnostic logs, SQLite audit/history, settings, and cached pricing stay in the platform's local application-data directory. Run `hm --status` to see the exact paths. Set `PROMPTMEUP_DATA_DIR` to choose another data directory.

Read [Privacy and data flow](docs/PRIVACY.md) before using real or confidential content. OpenAI account, API, retention, and billing terms remain separate from this MIT-licensed application.

## Build and contribute

```powershell
pwsh -NoProfile -File .\scripts\preflight.ps1
dotnet restore .\PromptMeUp.slnx
dotnet format .\PromptMeUp.slnx --verify-no-changes --no-restore
pwsh -NoProfile -File .\scripts\check-xml-comments.ps1
dotnet build .\PromptMeUp.slnx --configuration Release --no-restore --warnaserror
dotnet test .\PromptMeUp.slnx --configuration Release --no-build
```

The GitHub quality workflow is manual by design and currently performs only lint and cross-platform build jobs. Start with [CONTRIBUTING.md](CONTRIBUTING.md), then use the [validation guide](docs/VALIDATION.md).

## Repository map

- `PromptMeUp/` — the `hm` application, split into models, services, views, infrastructure, and orchestration.
- `PromptMeUp.Tests/` — focused parser, memory, localization, pricing, redaction, cost, and endpoint-policy tests.
- `prompt/` — localized YAML resources sent by the runtime.
- `scripts/` — preflight, XML-comment policy, repository helpers, and portable PATH companions.
- `docs/` — durable architecture, privacy, usage, cost/caching, and validation guidance.
- `prompts/` — contributor-facing development prompts; these are separate from runtime AI instructions.
- `.github/` — manual quality workflow, repository guidance, and task records.

## Project documentation

- [CLI reference](docs/CLI_REFERENCE.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Privacy and data flow](docs/PRIVACY.md)
- [Memory, costs, and caching](docs/OPENAI_COSTS_AND_CACHING.md)
- [Validation guide](docs/VALIDATION.md)
- [Security policy](SECURITY.md)
- [Third-party notices](THIRD_PARTY_NOTICES.md)
- [Contributing](CONTRIBUTING.md)
- [Code of conduct](CODE_OF_CONDUCT.md)

## License

PromptMeUp is open source under the [MIT License](LICENSE).

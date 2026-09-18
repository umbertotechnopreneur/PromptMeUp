<p align="center">
  <img src="docs/assets/promptmeup-banner.png" alt="PromptMeUp — Ask naturally. Understand first. You decide. A weekend idea. An everyday helper." width="100%" />
</p>

<h1 align="center">PromptMeUp — help with your next terminal command</h1>

<p align="center">
  Ask a question, understand the answer, and decide what to run.<br />
  Available as <code>hm</code> on Windows, Linux, and macOS.
</p>

<p align="center">
  <a href="#meet-hm"><strong>Meet hm</strong></a> ·
  <a href="#get-started"><strong>Get started</strong></a> ·
  <a href="https://umbertogiacobbi.biz/promptmeup/?utm_source=github&amp;utm_medium=referral&amp;utm_campaign=promptmeup&amp;utm_content=readme_product_page"><strong>Product page</strong></a> ·
  <a href="docs/PRIVACY.md"><strong>Privacy</strong></a> ·
  <a href="https://github.com/umbertotechnopreneur/PromptMeUp/discussions"><strong>Join the conversation</strong></a>
</p>

<p align="center">
  <a href="https://github.com/umbertotechnopreneur/PromptMeUp/actions/workflows/quality.yml"><img src="https://github.com/umbertotechnopreneur/PromptMeUp/actions/workflows/quality.yml/badge.svg" alt="Quality gate" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-22C55E" alt="MIT License" /></a>
  <img src="https://img.shields.io/badge/Windows%20%C2%B7%20Linux%20%C2%B7%20macOS-8B5CF6" alt="Windows, Linux, and macOS" />
  <img src="https://img.shields.io/badge/early%20preview-F59E0B" alt="Early preview" />
</p>

Forgot a command? Stuck on an error? Ask `hm` in your own words. It uses your OpenAI API account to explain what to try and suggest PowerShell commands. You read them and decide what to run.

> [!NOTE]
> PromptMeUp is still an early version. There are no ready-to-download releases yet, but you can [build it yourself](#get-started).

> [!NOTE]
> **Distributed MSIX installers are temporarily unavailable.** Portable downloads remain the supported option. Maintainers can still create self-signed MSIX packages locally for Windows testing; see the [Windows packaging guide](docs/WINDOWS_PACKAGING.md#install-with-the-hm-execution-alias).

## Meet `hm`

`hm` means **help me**. Start with a question:

```powershell
hm "How do I undo my last local commit without losing my changes?"
```

Read the answer, ask a follow-up, or take a closer look at a suggested command. Nothing runs automatically. You can say no to a command and keep chatting.

Each menu choice has a number starting at **0**: with up to ten choices, press its digit to select it immediately; with more choices, type the number and press Enter. Selecting a command opens its exact preview and risk assessment, where you still decide whether to authorize it. If the AI suggests no commands, a single question offers **Finish here** or **Continue in chat**, while an ongoing chat goes straight back to the message prompt.

Have a few questions? Start with `hm --chat`. Your earlier terminal output stays where it was, and `hm` closes when you're done. Nothing keeps running in the background.

In chat and interactive diagnostics, pasted text keeps its line breaks and waits for you to press Enter before sending. Use the arrow keys to review or edit it first. This requires a terminal that supports bracketed paste, which marks pasted text separately from keyboard input; diagnostic files can also be supplied with `--input-file`.

## A few moments with `hm`

![Example of asking hm to find large files, reading its answer, and choosing not to run the command.](docs/assets/screen-ask-green.png)

<table>
<tr>
<td width="50%" valign="top">
<strong>Pause before you run</strong><br /><br />
<img src="docs/assets/screen-review-amber.png" alt="Example of reviewing a command and its risks before choosing to run or cancel it." />
<br />Review the exact command before approving it.
</td>
<td width="50%" valign="top">
<strong>Your starting point</strong><br /><br />
Run <code>hm</code> to browse help, or <code>hm --setup</code> to open settings.<br /><br />
General shows your setup, usage, and estimated costs. Use the sidebar to change the model, API keys, chat limits, or colors.
</td>
</tr>
</table>

*These are illustrations with sample content, not screenshots. The colors and layout in your terminal may look different.*

## Remember the details you keep repeating

Save a note directly from the terminal:

```powershell
hm --remember "Prefer short explanations."
hm --remember "I usually work with C# and .NET 10."
hm --forget "the preference about short explanations"
```

`--remember` saves your exact text locally and confirms the saved ID. `--forget` accepts an ID, exact note text, or a description. IDs and exact text need no AI call. Descriptions send batches of saved notes to AI to find matches, with normal token costs. Choose a matching note by pressing its number, then confirm deletion in a separate menu. Press `0` to cancel; `9` shows the next page when needed. Deletion requires an interactive terminal, even with `--yes`. Missing matches, lookup errors, or notes changed during lookup are never reported as deleted. `hm /remember ...` and `hm /forget ...` also use these flows.

Save a preference or a useful project fact from inside chat:

```text
/remember Prefer short explanations.
/remember I usually work with C# and .NET 10.
/memories
```

All saved notes are shared across your chats, wherever you open `hm`. Use `/memories` to see what is saved and `/forget <id>` to remove a note. Existing notes are kept when you upgrade.

Choose **Memories** in the Help or Settings sidebar, or run `hm --memories`, to read, create, edit, and delete saved notes locally. Saving applies immediately; deletion asks for confirmation. Chat and the first question show a compact reminder of the memory commands.

`hm` picks up to five notes to include with a question, using shared words and recency. Saving a note makes no extra AI call. See [how memories work](docs/OPENAI_COSTS_AND_CACHING.md) for the limits and how notes are picked.

## Keep your settings in one place

```powershell
hm --setup
hm --ai-setup
hm --theme
```

These commands open the same Settings screen at General, AI, or Theme. `--ai-settings` also works in place of `--ai-setup`. Pick a section in the sidebar, make your changes, then choose Save or Cancel. You can try theme colors before saving.

Choose **About** in Help or Settings to read about the project. When you close it, you'll be back where you left off, with any unsaved settings still there. You can also type `hm about`.

In chat, `/context` or `/status` shows how much of the conversation `hm` is keeping for the next question. The default limit is **16,000 estimated tokens** — the small pieces of text an AI model reads. You'll also see usage for the last answer and the whole chat. A `~` means the number is an estimate.

Use `/clear` to start fresh in the same chat. Your saved notes and usage totals stay. If a question is too long, `hm` lets you shorten it and try again.

Want less output? Say **“Hide the session summary and command previews”** in chat. You can hide or show either one separately, for example **“Show the session summary”**. The choice lasts for this chat, survives `/clear`, and resets when you start a new chat. Existing terminal output stays in your scrollback.

`/status` and `/context` still show the summary when you ask. Hiding command previews removes the suggested-command menu; commands may still appear in answers. Use `/run <command>` to review one: its exact preview, risk assessment, and approval remain required. Understanding these chat preferences uses an additional AI call, included in usage and cost totals.

## Get started

To build locally, install the **.NET 10 SDK**, **Git**, and **PowerShell 7**. AI features use your own OpenAI API account.

```powershell
git clone https://github.com/umbertotechnopreneur/PromptMeUp.git
cd PromptMeUp
dotnet build PromptMeUp.slnx --configuration Release
dotnet run --project PromptMeUp/PromptMeUp.csproj -- --setup
dotnet run --project PromptMeUp/PromptMeUp.csproj -- "How do I list the largest files here?"
```

Setup walks you through your language, model, answer style, and command checks. Choose from English, Italian, French, German, Spanish, and Vietnamese. You don't need a special font. Add `--no-emoji` or `--no-animation` if you prefer simpler output.

Enter your API key in setup or use your usual secret manager. Don't put it in a command. On Windows, setup can save the key for your user account. It works right away in the open `hm` session. Before starting `hm` again, fully close and reopen your terminal app — and your IDE too, if that's where the terminal runs. On Linux and macOS, a key entered in setup lasts only for that session; use your shell or secret manager to make it available next time.

The other examples use the shorter `hm` command. When running from source, replace it with `dotnet run --project PromptMeUp/PromptMeUp.csproj --` and keep the same arguments. Ready-to-download builds will appear on [GitHub Releases](https://github.com/umbertotechnopreneur/PromptMeUp/releases).

### Use a portable build

You can make a portable build for Windows, Linux, or macOS on x64 or Arm64. Keep its files together. To use `hm` from any folder, run `hm --path install` from that copy and review the proposed PATH change. Use `hm --path status` to check it or `hm --path remove` to undo it. The [release guide](docs/RELEASING.md#rehearse-a-release) has the build commands.

### Windows installers

The release workflow also prepares EXE installers for Windows x64 and ARM64. They install for your user account and add `hm` to your PATH. PowerShell 7 is still required to execute commands. These installers are unsigned, so Windows may show an unknown-publisher warning. See the [Windows packaging guide](docs/WINDOWS_PACKAGING.md#unsigned-exe-installers-for-x64-and-arm64) for upgrades and other installation methods.

## More than a single command

You can give `hm` a build log, ask for a script, or work through a task step by step. Saving a script doesn't run it. Plans and saved routines ask you to approve each command. File previews show what would change and flag files that are already there.

| When you want to… | Use |
| --- | --- |
| Ask one question | `hm "your question"` |
| Keep the conversation going | `hm --chat` |
| Manage saved memories | `hm --memories` |
| Save a note | `hm --remember "note"` |
| Forget a saved note | `hm --forget "id or description"` |
| Diagnose an error or log | `hm --diagnose --file build.log` |
| Draft or revise a PowerShell script | `hm --script "your request"` |
| Work through a plan and resume it later | `hm --plan "your goal"` |
| See what would happen to your files | `hm --preview copy --file report.txt --output backup` |
| Save or reuse a personal routine | `hm --recipes` |
| Open settings at AI preferences | `hm --ai-setup` or `hm --ai-settings` |
| Open settings at general preferences | `hm --setup` |
| Open settings at the theme preview | `hm --theme` |
| Check your setup | `hm --status` |
| Check usage and estimated costs | `hm --costs` |
| See commands and options | `hm --help` |
| See the libraries behind the app | `hm --third-party` |

The [command guide](docs/CLI_REFERENCE.md) has more examples and options. PromptMeUp helps with terminal work: commands, tools, errors, files, and scripts. It doesn't write general prose or generate images.

## Experimental skills and learning

On the experimental branch, you can try [skills and reviewed learning](docs/EXPERIMENTAL_MEMORY_SKILLS.md). Start with `hm --skills` or `hm --learning`. Everything starts disabled; Dream and heartbeat suggest memory changes for you to review, and nothing runs in the background.

## Before you run a command

Before anything runs, `hm` checks the command on your machine and shows you the exact text and possible risks. You can also turn on an extra AI review. You make the final decision, and each approval is for one command and lasts for a limited time.

Approved commands run through PowerShell with your user account's permissions. They have a time limit and a limit on how much output `hm` captures, but they can still change your real files and system settings. They don't run in an isolated test environment.

Your settings, notes, logs, and request history stay on your machine. To answer you, OpenAI receives your question, recent messages, selected notes, some details about your terminal, and any command output you share for a follow-up. `hm` removes secrets it recognizes, but it can't catch every private detail.

See [what is saved and shared](docs/PRIVACY.md) for the details. The app is MIT-licensed; OpenAI's terms and API charges still apply.

## A weekend project that stayed

We built PromptMeUp over a weekend, then kept reaching for it at [UmbertoGiacobbiDotBiz](https://umbertogiacobbi.biz/promptmeup/?utm_source=github&utm_medium=referral&utm_campaign=promptmeup&utm_content=readme_origin_story). So we decided to share it. It's a small project, and hearing how you use it helps us decide what to improve next.

## Get involved

Found a rough edge? Have a small improvement in mind? Start a [discussion](https://github.com/umbertotechnopreneur/PromptMeUp/discussions), [report a bug](https://github.com/umbertotechnopreneur/PromptMeUp/issues/new/choose), or read the [contribution guide](CONTRIBUTING.md).

Documentation and contributions are in English; the app supports all six interface languages.

| For users | For contributors |
| --- | --- |
| [Command guide](docs/CLI_REFERENCE.md) | [How the code is organized](docs/ARCHITECTURE.md) |
| [Privacy](docs/PRIVACY.md) | [Build and testing guide](docs/VALIDATION.md) |
| [Costs and conversation memory](docs/OPENAI_COSTS_AND_CACHING.md) | [Release process](docs/RELEASING.md) |
| [Support](SUPPORT.md) | [How the project is run](GOVERNANCE.md) |

Security issue? [Report it privately](https://github.com/umbertotechnopreneur/PromptMeUp/security/advisories/new), following our [security policy](SECURITY.md).

## License and credits

PromptMeUp is released under the **[MIT License](LICENSE)**. Use it, adapt it, and build on it while keeping the copyright and license notice.

Created by **Umberto Giacobbi**. Thanks to everyone who contributes and to the people behind the libraries we use. See the [library credits](THIRD_PARTY_NOTICES.md), [license texts](LICENSES/README.md), and [artwork credits](docs/assets/README.md).

---

<h2 align="center">More from the same workshop</h2>

<table>
<tr>
<td width="33%" valign="top">
<h3>TrackMeUp</h3>
A Windows app that keeps a record of your work on your computer, so you can find where you left off.<br /><br />
<a href="https://github.com/umbertotechnopreneur/TrackMeUp"><strong>Explore TrackMeUp →</strong></a>
</td>
<td width="33%" valign="top">
<h3>viewsapp.ai</h3>
Curious about what else we are building? Make this your next stop.<br /><br />
<a href="https://www.viewsapp.ai/?utm_source=github&amp;utm_medium=referral&amp;utm_campaign=promptmeup&amp;utm_content=readme_workshop_viewsapp"><strong>Discover viewsapp.ai →</strong></a>
</td>
<td width="33%" valign="top">
<h3>Umberto Giacobbi</h3>
More about me and the things I'm building. Come say hello.<br /><br />
<a href="https://umbertogiacobbi.biz/?utm_source=github&amp;utm_medium=referral&amp;utm_campaign=promptmeup&amp;utm_content=readme_workshop_creator"><strong>Visit my website →</strong></a>
</td>
</tr>
</table>

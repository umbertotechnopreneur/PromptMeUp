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

Use `hm` when you need help with a command, a flag, or an error message. Describe the problem in your own words. PromptMeUp uses your OpenAI API account to explain what to do and suggest PowerShell commands, which you can review before running.

> [!NOTE]
> PromptMeUp is an early source preview. There are no public binary releases yet. To try it, [build it locally](#get-started).

## Meet `hm`

`hm` means **help me**. Start with a question:

```powershell
hm "How do I undo my last local commit without losing my changes?"
```

Read the answer, continue into a conversation, or inspect a suggested command. Nothing in the answer runs automatically. You can decline a command and keep asking questions.

For several related questions, start with `hm --chat`. Your earlier terminal output stays visible, and `hm` stops when you exit; there is no background agent.

## A few moments with `hm`

![Green CRT rendering of a question about finding large files, an explanation, a suggested command, and a menu with execution declined.](docs/assets/screen-ask-green.png)

<table>
<tr>
<td width="50%" valign="top">
<strong>Pause before you run</strong><br /><br />
<img src="docs/assets/screen-review-amber.png" alt="Amber and cyan CRT rendering of the exact command, local risk review, and explicit approval or cancellation." />
<br />Review the exact command before approving it.
</td>
<td width="50%" valign="top">
<strong>Your starting point</strong><br /><br />
<img src="docs/assets/screen-center-violet.png" alt="Violet and cyan CRT rendering of the command center, showing sample settings and navigation." />
<br />A question, a conversation, or a quick look at your settings.
</td>
</tr>
</table>

*These images illustrate the product with sample content. They are not screenshots or selectable themes; the real layout adapts to your terminal.*

## Remember the details you keep repeating

Save a preference or a useful project fact from inside chat:

```text
/remember global Prefer short explanations.
/remember This project uses .NET 10.
/memories
```

Notes without `global` belong to the current project. They remain available the next time you open `hm`. Use `/memories` to see what is saved and `/forget <id>` to remove a note.

Each request can include up to five selected notes, within 800 estimated tokens. Project notes are matched by shared words in your question; global notes can be included across projects. Saving a note makes no extra AI call. [How memories work](docs/OPENAI_COSTS_AND_CACHING.md).

## Keep your settings in one place

```powershell
hm --setup
hm --ai-setup
hm --theme
```

All three commands open the same Settings screen with General, AI, or Theme selected. `--ai-settings` remains an alias for `--ai-setup`. Use the left sidebar to reach General, AI, Credentials, Conversation, Commands, Personalization, and Theme. Move freely between sections, then choose Save to keep the draft or Cancel to discard it. Theme colors update as you preview them.

Ordinary questions and chat use a default input budget of **16,000 estimated tokens**. In chat, `/context` or `/status` shows how much is currently retained, the model's capacity, and your chosen budget. The last call's input/output tokens and the session totals appear separately. A `~` marks a local estimate.

Use `/clear` to start a fresh conversation within the session. It keeps saved notes and usage totals. If a question is too large, `hm` leaves the chat open so you can shorten it and try again.

## Get started

To build locally, install the **.NET 10 SDK**, **Git**, and **PowerShell 7**. AI features use your own OpenAI API account.

```powershell
git clone https://github.com/umbertotechnopreneur/PromptMeUp.git
cd PromptMeUp
dotnet build PromptMeUp.slnx --configuration Release
dotnet run --project PromptMeUp/PromptMeUp.csproj -- --setup
dotnet run --project PromptMeUp/PromptMeUp.csproj -- "How do I list the largest files here?"
```

Setup asks for your language, model, answer style, and command-review preferences. The interface supports English, Italian, French, German, Spanish, and Vietnamese. It works without a special font; use `--no-emoji` or `--no-animation` if you prefer simpler output.

Enter your API key through setup or your preferred secret manager, never as a command argument. On Windows, setup can save it to your current user's environment. The new key works immediately in the current `hm` process; before the next launch, fully close and reopen the terminal application, including the IDE if it hosts the terminal. On Linux and macOS, an entered key lasts for that process; configure your shell or secret manager for future launches.

The examples elsewhere in this README use the `hm` command from a published build. While working from source, use `dotnet run --project PromptMeUp/PromptMeUp.csproj --` followed by the same arguments. Future downloads will appear on [GitHub Releases](https://github.com/umbertotechnopreneur/PromptMeUp/releases).

### Use a portable build

You can publish a portable build for Windows, Linux, or macOS on x64 or Arm64. Keep its files together and add the folder containing `hm` to your user `PATH`. From a running copy, `hm --path install` previews that change; `hm --path status` and `hm --path remove` let you check or undo it. See [packaging a local build](docs/RELEASING.md#rehearse-a-release) for the commands.

## More than a single command

Give `hm` a build log to investigate, ask it for a script draft, or work through a plan one step at a time. Scripts are saved for review; plans and recipes ask for fresh approval before running commands. File previews show the proposed changes and any collisions before you proceed.

| When you want to… | Use |
| --- | --- |
| Ask one question | `hm "your question"` |
| Keep the conversation going | `hm --chat` |
| Diagnose an error or log | `hm --diagnose --file build.log` |
| Draft or revise a PowerShell script | `hm --script "your request"` |
| Work through a plan and resume it later | `hm --plan "your goal"` |
| Inspect file effects before approval | `hm --preview copy --file report.txt --output backup` |
| Save or reuse a personal routine | `hm --recipes` |
| Open settings at AI preferences | `hm --ai-setup` or `hm --ai-settings` |
| Open settings at general preferences | `hm --setup` |
| Open settings at the theme preview | `hm --theme` |
| Check configuration | `hm --status` |
| Understand usage and estimates | `hm --costs` |
| See commands and options | `hm --help` |
| See the libraries behind the app | `hm --third-party` |

See the [CLI reference](docs/CLI_REFERENCE.md) for options and examples. PromptMeUp is for terminal work: commands, tools, errors, files, and scripts. General writing and image generation are outside its scope.

## Before you run a command

`hm` checks each command locally and shows its risk assessment with the exact text to be run. The optional AI review adds an opinion; you still make the decision. An approval applies to that command only and expires.

Approved commands run as your current user through PowerShell, with a time limit and a limit on captured output. They can change real files and system state. PromptMeUp does not sandbox them.

Settings, saved notes, logs, and request history are stored on your machine. For an AI answer, OpenAI receives your question, recent conversation, selected notes, some information about your terminal environment, and any command output you choose to share for a follow-up. `hm` filters recognizable credentials, but cannot identify every kind of confidential information.

Read [Privacy and data flow](docs/PRIVACY.md) for the details. OpenAI service terms and API charges are separate from this MIT-licensed app.

## A weekend project that stayed

PromptMeUp started as a pet project built over a weekend. We kept using it for everyday terminal questions in our team at [UmbertoGiacobbiDotBiz](https://umbertogiacobbi.biz/promptmeup/?utm_source=github&utm_medium=referral&utm_campaign=promptmeup&utm_content=readme_origin_story), so we decided to share it. It is still a small project, and feedback from real use helps us decide what to work on next.

## Get involved

Found a rough edge? Have a small improvement in mind? Start a [discussion](https://github.com/umbertotechnopreneur/PromptMeUp/discussions), [report a bug](https://github.com/umbertotechnopreneur/PromptMeUp/issues/new/choose), or read the [contribution guide](CONTRIBUTING.md).

Documentation and contributions are in English; the app supports all six interface languages.

| For users | For contributors |
| --- | --- |
| [CLI reference](docs/CLI_REFERENCE.md) | [Architecture](docs/ARCHITECTURE.md) |
| [Privacy](docs/PRIVACY.md) | [Validation](docs/VALIDATION.md) |
| [Costs and conversation memory](docs/OPENAI_COSTS_AND_CACHING.md) | [Release process](docs/RELEASING.md) |
| [Support](SUPPORT.md) | [Governance](GOVERNANCE.md) |

Security issue? [Report it privately](https://github.com/umbertotechnopreneur/PromptMeUp/security/advisories/new), following our [security policy](SECURITY.md).

## License and credits

PromptMeUp is released under the **[MIT License](LICENSE)**. Use it, adapt it, and build on it while keeping the copyright and license notice.

Created by **Umberto Giacobbi**, with appreciation for every contributor and the libraries that make it possible. See [third-party attribution](THIRD_PARTY_NOTICES.md), [upstream license texts](LICENSES/README.md), and [artwork provenance](docs/assets/README.md).

---

<h2 align="center">More from the same workshop</h2>

<table>
<tr>
<td width="33%" valign="top">
<h3>TrackMeUp</h3>
Find your workday again. A local-first memory for Windows that helps you recover the context you thought you had lost.<br /><br />
<a href="https://github.com/umbertotechnopreneur/TrackMeUp"><strong>Explore TrackMeUp →</strong></a>
</td>
<td width="33%" valign="top">
<h3>viewsapp.ai</h3>
Curious about what else we are building? Make this your next stop.<br /><br />
<a href="https://www.viewsapp.ai/?utm_source=github&amp;utm_medium=referral&amp;utm_campaign=promptmeup&amp;utm_content=readme_workshop_viewsapp"><strong>Discover viewsapp.ai →</strong></a>
</td>
<td width="33%" valign="top">
<h3>Umberto Giacobbi</h3>
The person, the products, and the ideas behind the work. Come say hello.<br /><br />
<a href="https://umbertogiacobbi.biz/?utm_source=github&amp;utm_medium=referral&amp;utm_campaign=promptmeup&amp;utm_content=readme_workshop_creator"><strong>Visit my website →</strong></a>
</td>
</tr>
</table>

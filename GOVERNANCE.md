# How PromptMeUp is run

I'm [Umberto Giacobbi](https://github.com/umbertotechnopreneur), the maintainer of PromptMeUp. I built it over a weekend and kept using it for daily terminal work. A few contributors help with the project.

I decide what goes into the project and when to release it. Contributions are welcome: help make terminal work easier while keeping the app small, portable, and careful with commands and private data. For a big change, start a discussion before writing the code.

## How changes land

Changes go to `main` through a pull request. Required checks must pass on the latest changes, review comments must be resolved, and history must stay linear (no merge commits). Force pushes and deletion of `main` are blocked. The rules are recorded in [.github/main-protection.json](.github/main-protection.json) and are intended to apply to administrators too.

There's currently one maintainer with write access. Pull requests and automated checks are required, but a second person's approval isn't: GitHub doesn't let authors approve their own pull requests. CODEOWNERS sends review requests to the maintainer. This means the maintainer's own changes don't get an independent human review.

When a second maintainer joins, require one approval, code-owner review, and approval after the last push.

Use English for repository writing. Runtime UI and prompt translations retain all six supported languages. The same contributor rules are maintained in [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Releases and security

The [release process](docs/RELEASING.md) checks the code, builds portable packages with their license notices, and prepares a draft. The maintainer reviews it before publishing. Optional Windows MSI and WinGet packages have a [separate guide](docs/WINDOWS_PACKAGING.md).

Please [report security problems privately](https://github.com/umbertotechnopreneur/PromptMeUp/security/advisories/new). Use [Discussions](https://github.com/umbertotechnopreneur/PromptMeUp/discussions) for help and [issues](https://github.com/umbertotechnopreneur/PromptMeUp/issues/new/choose) for bugs or improvements. I maintain this in my own time, so I can't promise a response time.

## Contribution and attribution

You keep the copyright to your contributions and share them under the project's [MIT License](LICENSE). If you add someone else's work, include its source and license. Work made with AI tools gets the same review as everything else. You remain responsible for its accuracy, permissions, and credits.

Linking to another project doesn't mean its creators endorse PromptMeUp. Everyone taking part should follow the [code of conduct](CODE_OF_CONDUCT.md).

# Keeping PromptMeUp small and useful

PromptMeUp is a personal open-source project maintained by [Umberto Giacobbi](https://github.com/umbertotechnopreneur). It began as a weekend experiment and found a practical place in the UmbertoGiacobbiDotBiz team's daily work.

The maintainer makes release and scope decisions. Contributions are welcome when they improve the terminal experience while preserving explicit command approval, privacy, portability, and a small runtime footprint. Discuss substantial changes before implementation.

## How changes land

Changes reach main through a pull request, with up-to-date required checks, resolved review conversations, and linear history. Force pushes and branch deletion are blocked. The intended protection also applies to administrators; its reproducible configuration is in [.github/main-protection.json](.github/main-protection.json).

The repository currently has one maintainer with write access. Pull requests and automated checks are required, but the approving-review count is zero: GitHub does not allow authors to approve their own pull requests. CODEOWNERS routes contributions to that maintainer. When a second maintainer is appointed, require one approving review, code-owner review, and approval after the last push. This is a documented single-maintainer tradeoff, not independent human review.

Use English for repository writing. Runtime UI and prompt translations retain all six supported languages. The same contributor rules are maintained in [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Releases and security

The [release process](docs/RELEASING.md) validates the source, builds portable packages, includes redistribution notices, and prepares a draft for the maintainer to inspect and publish. Optional Windows MSI and WinGet packaging remain documented separately.

Report vulnerabilities through [private security reporting](https://github.com/umbertotechnopreneur/PromptMeUp/security/advisories/new). Use [Discussions](https://github.com/umbertotechnopreneur/PromptMeUp/discussions) for support and [issue forms](https://github.com/umbertotechnopreneur/PromptMeUp/issues/new/choose) for reproducible bugs or focused improvements. This pet project does not promise a support or response-time SLA.

## Contribution and attribution

Contributors retain copyright in their contributions and submit them under the project's [MIT License](LICENSE). Include the provenance and license of any third-party material you add. AI-assisted work receives the same review as any other contribution; the submitter remains responsible for accuracy, permissions, and attribution.

Names and links to other projects identify their creators and destinations; they do not imply third-party endorsement. Community participation follows the [code of conduct](CODE_OF_CONDUCT.md).

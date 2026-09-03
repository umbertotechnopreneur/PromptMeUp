# PromptMeUp documentation

Choose the guide that matches the PromptMeUp outcome you need, then go as deep as the work requires:

- [CLI reference](CLI_REFERENCE.md) — commands, switches, chat controls, exit codes, and portability.
- [Retro CLI manual (PDF)](PromptMeUp-CLI-Manual.pdf) — printable command card on page one, followed by a short guide to the newer safe-workflow commands.
- [Architecture](ARCHITECTURE.md) — model/view/service boundaries, request flow, persistence, and safety gates.
- [Privacy and data flow](PRIVACY.md) — what remains local, what reaches OpenAI, and how credentials are handled.
- [Memory, costs, and caching](OPENAI_COSTS_AND_CACHING.md) — bounded context, token accounting, pricing refresh, and OpenAI cache semantics.
- [Windows release packaging](WINDOWS_PACKAGING.md) — portable ZIPs, WinGet manifests, MSI generation, and local install testing.
- [Release process](RELEASING.md) — CI checks, portable archives, dependency notices, checksums, and release drafts.
- [Validation guide](VALIDATION.md) — repeatable build, lint, test, and manual acceptance checks.

New durable design notes can start from [`document.template.md`](../document.template.md).

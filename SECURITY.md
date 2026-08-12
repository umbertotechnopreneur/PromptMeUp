# Security policy

## Reporting a vulnerability

Please do not open a public issue for a vulnerability that could expose secrets, execute commands unexpectedly, corrupt local history, or bypass authorization.

Use GitHub's private security-advisory flow for this repository. Include the affected version or commit, platform, reproduction steps, impact, and any safe diagnostic evidence. Remove API keys, prompt content, command output, usernames, and local paths before attaching material.

## Security boundaries

PromptMeUp is an assistant, not a sandbox:

- every shell command requires an exact preview and explicit user authorization;
- the optional AI risk score is advisory and cannot grant execution permission;
- PromptMeUp starts commands as the current user through `pwsh -NoProfile -NonInteractive` and never requests elevation itself; an explicitly authorized command can still ask the platform for higher privileges and is risk-scored accordingly;
- command output is bounded and recognizable credentials are redacted before persistence or an AI follow-up;
- API keys are never accepted as command-line arguments or stored in SQLite;
- provider responses can still be wrong, incomplete, or unsafe, so the user remains responsible for each approved command.

Supported security updates currently target the latest commit on `main`; public binary releases are not yet published.

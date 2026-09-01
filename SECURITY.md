# Security policy

PromptMeUp is designed so an AI answer is never the same thing as permission to act. Exact command preview, local risk scoring, explicit authorization, bounded execution, and credential redaction are product boundaries—not optional modes.

## Report a vulnerability privately

Please do not open a public issue for a vulnerability that could expose secrets, execute commands unexpectedly, corrupt local history, or bypass authorization.

Use GitHub's private security-advisory flow for this repository. Include the affected version or commit, platform, reproduction steps, impact, and any safe diagnostic evidence. Remove API keys, prompt content, command output, usernames, and local paths before attaching material.

## Security promises and limits

PromptMeUp is an assistant, not a sandbox:

- every shell command requires an exact preview and explicit user authorization;
- the optional AI risk score is advisory and cannot grant execution permission;
- PromptMeUp starts commands as the current user through `pwsh -NoProfile -NonInteractive` and never requests elevation itself; an explicitly authorized command can still ask the platform for higher privileges and is risk-scored accordingly;
- command output is bounded and recognizable credentials are redacted before persistence or an AI follow-up;
- the optional 500-word AI preamble is normalized, checked locally for prompt-injection patterns in all six supported languages, and sent as explicitly untrusted preference data; this is defense in depth and not a guarantee against every possible injection;
- API keys are never accepted as command-line arguments or stored in SQLite;
- provider responses can still be wrong, incomplete, or unsafe, so the user remains responsible for each approved command.

Supported security updates currently target the latest commit on `main`; public binary releases are not yet published.

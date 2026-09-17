# PromptMeUp security policy

You decide what runs. PromptMeUp shows the exact command, checks its risks on your machine, and asks for approval. It also limits run time and captured output, and removes recognizable secrets before saving or sharing text. These checks are always part of the app.

## Report a vulnerability privately

Please do not open a public issue for a vulnerability that could expose secrets, execute commands unexpectedly, corrupt local history, or bypass authorization.

Use [GitHub's private security report](https://github.com/umbertotechnopreneur/PromptMeUp/security/advisories/new). Tell us which version or commit you used, your operating system, how to reproduce the problem, and what it could affect. Remove API keys, prompts, command output, usernames, and local paths from anything you attach.

## What the checks do — and their limits

Approved commands run on your real machine. Keep these limits in mind:

- You see the exact shell command and approve it before it runs.
- The optional AI review gives advice. It can't grant permission to run anything.
- Commands start as your user through `pwsh -NoProfile -NonInteractive`. PromptMeUp doesn't ask for administrator access itself. A command you approve can still ask for higher permissions, and the risk check takes that into account.
- The app limits captured command output and removes recognizable secrets before saving it or sending it to the AI.
- Your optional personal instructions can contain up to 500 words. Local checks in all six languages look for attempts to override the app's rules, and the AI is told to treat the text only as preferences. These checks can't catch every attempt.
- API keys are never accepted as command-line arguments or stored in SQLite.
- AI answers can be wrong, incomplete, or unsafe. Read each command before approving it.

Security fixes go to the latest code on `main`. There are no public ready-to-download releases yet.

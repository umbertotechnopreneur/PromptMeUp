# Prompt — create a reusable PowerShell helper

Create `{{SCRIPT_PATH}}` for `{{PURPOSE}}`.

Requirements:

- PowerShell 7, `[CmdletBinding()]`, `$ErrorActionPreference = 'Stop'`;
- `-Help` and explicit path parameters;
- read-only behavior by default;
- `-WhatIf` or an explicit opt-in switch for destructive actions;
- validated paths restricted to the requested workspace;
- no credentials or machine-specific secrets;
- concise output suitable for terminal and redirected logs.

Add a short usage example to `scripts/README.md`, run the script with `pwsh -NoProfile`, and report the exact result. Do not delete or move anything during validation unless the user explicitly authorized it.

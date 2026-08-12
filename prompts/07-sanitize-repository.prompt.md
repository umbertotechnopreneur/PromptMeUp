[System Instruction: Public Repository Sanitization]
- Inspect only files intended for the next commit.
- Find embedded secrets, credentials, private paths, customer names, generated artifacts, and accidental personal data.
- Treat placeholders and deliberate redaction tests as safe only when their purpose is explicit.
- Report each actionable finding with its exact file and line; do not expose secret values.
- Finish with a clear publish-safe or blocked verdict.

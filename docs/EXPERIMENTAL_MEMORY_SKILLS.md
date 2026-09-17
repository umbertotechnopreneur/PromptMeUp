# Experimental skills and memory

This experiment adds reusable terminal skills and reviewed learning to PromptMeUp. All features start disabled. OpenAI remains the AI provider, and the existing command approval workflow remains the execution boundary.

## Delivery steps

1. Record the scope and reuse decisions on the experimental branch.
2. Add a validated skill catalog with explicit activation and platform requirements.
3. Connect skill actions to local risk assessment, preview, and approval.
4. Deliver Git, filesystem, and concat-files skills and bounded ZIP import.
5. Add bounded contextual skill selection with visible activation.
6. Store opt-in observations and reviewable proposals separately from approved memories.
7. Reflect across observations with Dream, retaining source references.
8. Propose memory maintenance through a manual heartbeat and an optional due reminder.

## Reuse decisions

The companion CLI-Intelligence code is supplied by the project author. Reuse its skill metadata, catalog precedence, extraction records, reflection concepts, and maintenance timestamps. Adapt parsing to YamlDotNet and persistence to SQLite. Use PromptMeUp's command runner, credential redaction, localization, passive views, and OpenAI request accounting.

Keep UI rendering and provider-specific OpenRouter/Llama code out of the port. Do not import personal memories, machine configuration, or generated output. The metals-dev-monitor package is explicitly excluded.

## Verification

Run the applicable repository non-test checks before code commits. Automated tests and CLI smoke tests require a separate explicit request. Portable releases remain exclusive to GitHub Actions.

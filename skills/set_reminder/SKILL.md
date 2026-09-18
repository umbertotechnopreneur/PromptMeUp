---
name: set_reminder
description: Create, list, and cancel project reminders shown at the next active chat prompt.
version: 1
os: any
---

# Project reminders

Open `hm --skills`, enable `set_reminder`, and select **Choose an action**.
Create a reminder with a clock time (`14:30` or `3pm`) or an ISO date with an
explicit offset (`2030-05-20T14:30:00+02:00`). Review its full date, time, offset,
and note before confirming. Select an existing reminder to cancel it.

Reminders stay local to the current project and survive restarts. They appear
once, when an active chat reaches its next input prompt. They are not background
alarms or operating-system notifications. Disabling the experiment or this skill
pauses delivery; re-enabling resumes it. Shown and cancelled reminders are deleted.

Keep at most 50 pending reminders, with one-line notes of up to 500 characters.
Do not include credentials. A clock time that already passed means tomorrow;
ambiguous daylight-saving times require an explicit ISO offset. Chat requests
and tool-like text cannot create, modify, or cancel reminders automatically.

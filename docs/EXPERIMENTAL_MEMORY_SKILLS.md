# Skills and memory: quick start

Skills and memory are optional and start off. Saved notes work independently of these options. `metals-dev-monitor` is not included and cannot be imported.

You can ask in chat: "How do I enable skills?", "How do I use memory?", "How do Dream and heartbeat work?", or "How do I set a reminder?" Asking for help does not activate features or approve actions.

Open `hm --setup` for **Skills**, **Memory**, **Saved memories**, and **Privacy**. Skills and Memory are fields in the same form: **Save** applies your choices and **Cancel** discards them. Opening a section runs nothing. **Saved memories** is a separate note manager; changes saved there are immediate and are not undone by Settings Cancel. Existing commands such as `--learning` keep their names.

## Use a skill

1. In Settings → Skills, turn on **Skills and memory** for this project.
2. Read a skill's instructions and scripts, then set it to **Yes**. Turn on choosing skills based on the question if you want relevant skills selected for you.
3. Choose **Save**. Enabling a skill does not run its actions or collect your messages.

For actions, open `hm --skills`, choose the enabled skill, then **Choose an action to run**. Review and approve the command. **Use this skill for upcoming questions** fixes a skill for your questions without running it. This direct menu also supports enabling skills and importing a ZIP.

| Skill | What you can do |
| --- | --- |
| `git` | Inspect status, recent commits, and differences. |
| `filesystem` | List files, read a bounded text file, or inspect its details. |
| `concat-files` | Collect source files into new text bundles. |
| `clipboard` | Read or replace text in the Windows clipboard. |
| `screenshot` | Save a new local PNG on Windows; no automatic upload. |
| `system_info` | Inspect system, disk, process, or network summaries; environment names only. |
| `http_request` | Make a public HTTPS GET, HEAD, POST, or PUT request. |
| `web_search` | Get DuckDuckGo instant answers and related topics, not a full web search. |
| `timezone_convert` | List time zones or convert a date and time. |
| `set_reminder` | Create, list, or cancel project reminders. |

For script actions, edit the suggested JSON parameters. For example:

```json
{"InputFolder":".","OutputFolder":"./review-output"}
{"Url":"https://example.com","Method":"GET"}
{"Query":"PowerShell Get-FileHash","MaxResults":5}
{"FromTimeZone":"Europe/Rome","ToTimeZone":"Asia/Ho_Chi_Minh","Time":"2030-05-20T14:30"}
```

Each line is a separate example for concat-files, HTTP, web search, or time zones. Each skill's screen explains its own parameters. Script actions need PowerShell 7; Git also needs Git. Unsupported skills are marked unavailable.

For `set_reminder`, choose **manage**, then create a note with a time such as `14:30` or `2030-05-20T14:30:00+02:00`. Confirm the displayed date and note. Up to 50 reminders survive restarts. Due reminders appear at the next chat input prompt in the same project, not while you are typing or while the app is closed. Turning off the skill or the project's Skills and memory switch pauses delivery.

Choosing skills based on the question uses only enabled skills. **Import a skill from ZIP** accepts one skill folder with `SKILL.md` and optional standalone `scripts/*.ps1` (1 MiB total; 64 KiB per file). Local skills override bundled names. Changed contents need approval again. Imported scripts are not sandboxed; inspect them carefully. `--yes` cannot approve skill actions.

You can import up to 64 local packages. If a skill's instructions exceed the chat allowance, shorten them before enabling or selecting it; the app explains the limit. This allowance includes the instructions' final formatting, not just their character count.

## Use memories

Save a note directly with `/remember This project uses .NET 10.` in chat. Add `global` after `/remember` for a note shared across projects. Open `hm --memories` to read, edit, or delete saved notes.

For suggestions from future messages, open Settings → Memory. Turn on the shared project switch, choose to keep your messages, read the notice, give consent, and **Save**. The project switch alone never starts collection. To stop collecting, turn it off, confirm deletion of collected messages and suggestions, and Save; saved memories remain.

| Command | When to use it |
| --- | --- |
| `hm --learning` | Manage message collection directly; read or clear collected messages. |
| `hm --dream` | After at least two captured sessions, ask for possible new memories. |
| `hm --proposals` | Read the evidence, edit if needed, then approve or reject each suggestion. |
| `hm --heartbeat` | Check saved memories for duplicates or other suggested changes. |

These commands also work in chat as `/skills`, `/learning`, `/dream`, `/proposals`, and `/heartbeat`. Dream shows the messages before asking to send them to OpenAI. Heartbeat checks exact duplicates locally; AI review is optional. Neither saves or changes a memory without your approval. The reminder in Settings → Memory only reminds you to review your memories; it does not run heartbeat.

## Keep control of your data

- Collection keeps the newest 200 user messages per project and hides recognizable credentials. Messages over 4,000 characters are skipped. Messages and suggestions older than 30 days are removed when those records are next used. Stopping collection clears messages and suggestions, not saved memories. Deleting or merging a memory also clears collected messages and suggestions in its scope, with a warning first.
- HTTP sends your request to the chosen public site; web search sends your query to DuckDuckGo. No credentials, private-network destinations, redirects, or proxy access are supported. Output is not automatically sent to OpenAI, but optional AI command review sends script parameters before execution approval. Disable AI command review in Settings to keep that review local.
- Close sensitive windows before screenshots: images cannot hide private details. Clipboard output and generated bundles can also be private. Reminders stay local. Clearing collected messages does not erase earlier history or information already shared; see [Privacy](PRIVACY.md).

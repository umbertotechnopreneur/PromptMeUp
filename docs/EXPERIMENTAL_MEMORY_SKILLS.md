# Skills and memory: quick start

Available on the experimental branch, with PromptMeUp's existing interface and OpenAI provider. Everything experimental starts disabled. `metals-dev-monitor` is not included and cannot be imported.

## Use a skill

1. Open `hm --skills` and enable the experiment for your current project.
2. Open a skill, inspect its contents, and choose **Enable this exact package**.
3. Choose **Use in subsequent questions** for guidance, or **Choose an action** to run it. Review the command and approve it explicitly. Activation alone never runs anything.

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

For `set_reminder`, choose **manage**, then create a note with a time such as `14:30` or `2030-05-20T14:30:00+02:00`. Confirm the displayed date and note. Up to 50 reminders survive restarts. Due reminders appear at the next chat input prompt in the same project, not while you are typing or while the app is closed. Disabling the skill or experiment pauses delivery.

Optional contextual selection chooses relevant enabled skills locally. **Import a skill ZIP** accepts one package with `SKILL.md` and optional standalone `scripts/*.ps1` (1 MiB total; 64 KiB per file). Local packages override bundled names. Changed contents need approval again. Imported scripts are not sandboxed; inspect them carefully. `--yes` cannot approve skill actions.

## Use memories

Save a note directly with `/remember This project uses .NET 10.` in chat. Add `global` after `/remember` for a note shared across projects. Open `hm --memories` to read, edit, or delete saved notes.

| Command | When to use it |
| --- | --- |
| `hm --learning` | Opt into capturing future completed user messages; inspect or clear them. |
| `hm --dream` | After at least two captured sessions, ask for possible new memories. |
| `hm --proposals` | Read the evidence, edit if needed, then approve or reject each suggestion. |
| `hm --heartbeat` | Check saved memories for duplicates or suggested maintenance. |

These commands also work in chat as `/skills`, `/learning`, `/dream`, `/proposals`, and `/heartbeat`. Dream shows the exact evidence before asking to send it to OpenAI. Heartbeat checks exact duplicates locally; AI review is optional. Neither saves or changes a memory without your approval. The optional weekly maintenance reminder does not run heartbeat for you.

## Keep control of your data

- Learning keeps up to 200 filtered user messages per project for 30 days; messages over 4,000 characters are skipped. Disabling capture clears observations and proposals, not approved memories. Forgetting or merging a memory also clears learning evidence in its scope, with a warning first.
- HTTP sends your request to the chosen public site; web search sends your query to DuckDuckGo. No credentials, private-network destinations, redirects, or proxy access are supported. Output is not automatically sent to OpenAI, but optional AI command review sends script parameters before execution approval. Disable AI command review in Settings to keep that review local.
- Close sensitive windows before screenshots: images cannot be redacted. Clipboard output and generated bundles can also be private. Reminders stay local. Learning cleanup does not erase earlier AI audit history or information already shared; see [Privacy](PRIVACY.md).

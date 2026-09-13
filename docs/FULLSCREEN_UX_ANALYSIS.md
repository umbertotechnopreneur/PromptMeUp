# Fullscreen terminal flow inventory

Date: 2026-09-13. Scope: design analysis of the current working tree, including the in-progress AI settings and persistent-memory changes. No runtime implementation is included. Counts describe this source snapshot, not a released executable.

PromptMeUp can use temporary fullscreen forms for configuration and sustained review while retaining waterfall output for chat and short commands. The proposed inventory contains **12 functional pages and one optional navigation hub**. These are design groupings of existing behavior, not 12 existing fullscreen implementations.

## Stable screen numbers

| ID | Proposed screen | Entry / contents | Priority |
| --- | --- | --- | --- |
| 1 | Setup: General | `hm --setup`, or first `hm`: language and AI enabled state | High |
| 2 | Setup: Credentials | API/admin key configured state and optional replacement, with non-echoing input | High |
| 3 | Setup: AI behavior | Model, supported reasoning level, answer detail | High |
| 4 | Setup: Preferences | Custom instructions, location context, advisory command review, prompt caching | High |
| 5 | Setup: Advanced | Context token budget, turns, message size, context percentage, command output limit, timeout, endpoint | High |
| 6 | Setup: Review and save | Review all changes, return to a section, save, optional connection check | High |
| 7 | AI settings | `hm --ai-settings`: ten editable fields grouped into AI behavior and context limits, with save confirmation | High |
| 8 | Recipe parameters | `hm --recipes run <name>`: prerequisites and parameter fields; then reuse screens 9 and 10 | High |
| 9 | Plan overview | `hm --plan <goal>` / `--plan --resume <id>`: objective, steps, state, start/resume | High |
| 10 | Plan step | Selected step, full command, risk, authorization, output, verification, result confirmation | High |
| 11 | Script workspace | `hm --script <request>`: source, differences, validation, revise/save actions | Medium |
| 12 | File effects review | `hm --preview <operation> --file <path>`: source/destination/effects, collisions, individual command review, progress | Medium |
| 13 | Main navigation hub (optional) | `hm` after setup: one fullscreen menu before returning to the selected flow | Optional |

Screens 1–6 are sections of one setup workspace, not six new switches. Conditional sections can be skipped when irrelevant. Screen 7 should reuse the fields and validation used by setup. The initial setup and `--setup` share the same pages. Recipe execution reuses the plan pages. Each plan step and each reviewed file reuses its respective page, rather than creating another screen.

Shared overlays/components are excluded from the page count: model selector, secret entry, save/discard confirmation, connection result, script destination/revision input, and exact-command authorization. They still require implementation effort. A compact alternative can merge screens 9 and 10, reducing the total to 11 core pages, but keeping them separate makes planning and execution easier to distinguish.

## Current interaction burden

Counts are successful user responses, including confirmations, without validation retries or cancellation. They are not counts of source-code prompt call sites. Conditional paths and repeated loops are distinguished explicitly.

| Flow | Current responses | Source evidence |
| --- | --- | --- |
| Full setup | 9 with AI disabled and advanced settings skipped; typically 14 with existing keys and connection-check choice; up to 23 with both key entries and all seven advanced fields | `Views/SetupView.cs`, `CollectCore`, `PromptForSecret`, `CollectModelSettings` |
| AI settings | 11: ten fields plus save; currently three sequential stages | `Views/SetupView.cs`, `CollectAiSettings` |
| New plan | `1 + 3S`, or 4–25 for 1–8 successful steps: start, then authorize command, authorize verification, confirm result | `Application/PlanWorkflow.cs:52,81,98,99`; `Services/PlanStore.cs:102` |
| Recipe run | `2 + P + 3S`, up to 38 for 12 parameters and eight steps | `Application/RecipeWorkflow.cs:63,67,73`; `Views/RecipeView.cs:65`; `Services/RecipeStore.cs:143` |
| Script | Repeating action menu; direct save takes two responses with `--output`, three without; revisions and validation add interactions, so no fixed maximum | `Application/ScriptWorkflow.cs:33,38,39,51,61` |
| File preview | `1 + F`, up to 1001 for 1000 files: review consent plus separate authorization for each command | `Application/FilePreviewWorkflow.cs:22,27,36,40`; `Services/FilePreviewService.cs:10` |

For a resumed plan, the count depends on saved state: one resume confirmation, three responses per pending step, and two per step requiring verification again. Failures, denials, collisions and cancellation can end a flow earlier. File previews with redirected input/output only display the preview.

Fullscreen improves navigation and context; it does not remove the explicit authorizations in these totals. In particular, the file-review page must not turn a large batch into blanket execution permission.

## Flows that should retain waterfall output

| Flow | Current input burden | Decision |
| --- | --- | --- |
| Chat, query, `/run` | Conversation turns and occasional command selection/authorization | Preserve conversation history and the existing flow |
| `--diagnose` | Zero or one initial text input, followed by the conversation workflow | Waterfall |
| `--recipes list/show` | Zero prompts | Waterfall |
| `--recipes save/import/export` | One confirmation | Waterfall |
| `--path` | One selection and at most one confirmation | Waterfall |
| `--path status` | Zero | Waterfall |
| `--path install/remove` | Zero or one, depending on changes and `--yes` | Waterfall |
| `--where` / `-where` | One selection, plus confirmation only for opening the directory; zero with redirect | Waterfall |
| `--install-font` | One confirmation; zero with `--yes` or `--dry-run` | Waterfall |
| `--status`, `--costs` | Zero prompts | Keep structured panels/tables in scrollback |
| Help, version, third-party notices | Zero prompts | Waterfall |
| `--test-ai` | No form collection | Waterfall progress/result |
| `/memories`, `/remember`, `/forget` | No additional prompts beyond the chat command | Keep inline chat behavior |

The parser exposes 19 `AppCommand` routes: the main entry and 18 command families. Auxiliary flags such as `--file`, `--output`, `--language`, `--yes`, `--dry-run`, and rendering flags are not separate screens. Memory currently has chat commands, not a standalone CLI manager.

The existing main menu contains ten actions plus Exit. It does not currently expose AI settings, diagnose, script, plan, preview or recipes. Adding these to optional screen 13 would be an explicit navigation improvement, not a literal conversion of today's menu.

## Interaction and implementation direction

Use a temporary terminal viewport with a fixed header, section navigation, editable body, contextual help and a keyboard footer. Prefer Tab/Shift+Tab for focus, arrows for selections, Enter for the focused action, visible Back/Next controls, and Escape for cancellation of the active flow. Preserve the current distinction between Escape and Ctrl+C.

Keep unsaved field state in memory, allow revisiting previous sections and validate near the affected field. Save through the application orchestrator only after review. Secret input must remain non-echoing; summaries show configured/missing state only. Do not expose secret lengths through masking. Keep all six runtime languages, the high-contrast shared palette, ASCII fallbacks and shared emoji spacing.

The current stack is .NET 10 with Spectre.Console 0.57.2. Its [Layout primitives](https://spectreconsole.net/console/widgets/layout/) can express the structure. A true multi-field form requires explicit focus, input and state management: [LiveDisplay documentation](https://spectreconsole.net/console/live/live-display/) warns against mixing Live with interactive prompts, progress or status. Wrapping the existing prompt sequence in Live is insufficient.

Keep Views passive. Application coordinates submissions, cancellation, validation and workflow transitions; Services retain HTTP, secret storage, persistence and process execution. Retain deterministic risk scoring, advisory-only AI review, full local command previews and explicit authorization for each command. `--yes` must not acquire broader execution meaning.

### Preserving terminal history

An alternate screen buffer retains the previous main-buffer history and restores it on exit, but the temporary screen has no scrollback of its own. Long content therefore needs internal scrolling, and exit should append a compact, non-sensitive result summary to the main waterfall. See [Microsoft alternate-screen behavior](https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences#alternate-screen-buffer) and [Xterm control sequences](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-The-Alternate-Screen-Buffer).

The repository currently says never to clear the terminal. A future implementation should align both instruction files to distinguish preserving the main buffer/history from repainting a disposable alternate buffer. Xterm's alternate-buffer entry initializes that temporary buffer. This analysis does not change either rule.

The [Spectre.Console 0.57.2 implementation](https://github.com/spectreconsole/spectre.console/blob/0.57.2/src/Spectre.Console/Extensions/AnsiConsoleExtensions.Screen.cs) checks ANSI/alternate-buffer capabilities and restores the main screen in `finally`. Its callback is synchronous: do not pass an `async void` delegate. The existing console facade forwards ANSI operations while ignoring Clear requests. Ensure buffer, cursor and style restoration on save, Escape, Ctrl+C and exceptions.

For redirected input/output or insufficient terminal capabilities, deliberately select the waterfall path. Narrow terminals need stacked groups and scrolling instead of clipped fields. Windows, Linux and macOS behavior, resizing and six-language text fit remain future implementation validation, not verified properties of the image concepts.

## Visual concepts

- **A — Cyan setup:** the existing pale-blue/cyan identity, section sidebar, central form and contextual help. Recommended base direction.
- **B — Green setup:** the same information in an AS/400-inspired green form with inverse focus and dotted label leaders.
- **C — Amber plan:** a workbench with step navigation, full command preview, risk, explicit authorization and output area.

These are ImageGen design proposals, not application screenshots. English artwork text and invented non-sensitive content are used. Generated previews and their exact prompts are local review artifacts under the ignored `artifacts/fullscreen-concepts` directory; they are not runtime assets or files to commit.

Recommended implementation order: screen 7 as a bounded first form; screens 1–6 using shared controls; screens 8–10 for longer workflows; screens 11–12 afterward. Screen 13 remains optional. Final scope is selected by the user using the stable numbers above.

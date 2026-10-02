# Terminal UI components

Conversation workflows (chat, query, explain, diagnose, plan, and script) write to the normal terminal buffer. Their output remains in scrollback. Settings and complex pickers use the shared `FullscreenViewport` alternate buffer and restore the normal buffer when closed.

## Keyboard navigation

- At the chat input or a conversation decision menu, `Ctrl+PageUp` and `Ctrl+PageDown` open previous or next retained turns. Opening history preserves the current draft, caret, and menu selection.
- `F2` opens the latest expandable tool result or error.
- In the history viewer, `Left` and `Right` change turns; `Up` and `Down` scroll; `PageUp` and `PageDown` change pages; `Home` and `End` jump within the selected turn.
- `Enter`, `Escape`, or `F2` closes the viewer. It never sends a message, approves a command, or executes an action.
- Inline decision menus use arrow keys and `Enter`. Numbered command suggestions also accept a single digit when there are at most ten choices. Approval defaults to **No**.

The history viewer requires an interactive ANSI terminal with alternate-buffer support and at least 60 columns by 20 rows. Unsupported and redirected terminals receive complete output inline instead of inaccessible collapsed results. Completed one-shot results offer a local review prompt before returning to the shell.

## Shared controls

| Component | Purpose | Options and behavior |
| --- | --- | --- |
| `TerminalTable` | Consistent data tables | Themed headings, cell padding, optional rounded border and row separators. Existing Spectre cells retain their individual styles and alignment. |
| `TerminalTurnHeader` | User, assistant, plan, script, and tool identity | Optional separator; open layout using existing theme accents. |
| `TerminalActivityRow` | Working, completed, failed, or cancelled activity | Quiet symbol, measured elapsed time, clipped label, text-only fallback. |
| `TerminalSessionStrip` | Model, mode, state, context, cost, and elapsed time | Indivisible blocks; secondary metrics disappear before essential context counts. |
| `TerminalPromptBar` | Input-area chrome | Independently optional separators, status, and token breakdown. |
| `TerminalDisclosure` | Full tool output and long errors on demand | Keeps exit status and the first error visible; complete inline fallback. |
| `TerminalConversationPrompt` | Inline choices and approvals | Reuses typed menu choices, session metrics, and history navigation. |
| `TerminalHistoryView` | Read-only retained turns and full results | Disposable alternate buffer, independent paging, resize handling. |
| `TerminalStateScope` | Discrete terminal-title states | Saves and restores the terminal title using the terminal's title stack. |

System, user, tool, and assistant token counts stay visible in the context legend. Guide tokens are already included in system tokens. The thirteen-cell meter uses the same proportional allocation as the full context summary. Counts are estimates when indicated with `~`; elapsed time is measured, not an ETA.

The navigation transcript is separate from provider context and saved chat. It keeps at most 128 entries and two million source characters in memory for the current invocation. Older entries can leave this local navigation buffer without being removed from terminal scrollback. Oversized entries are printed inline rather than hidden. Nothing is sent to an AI provider by opening history or details.

Full command and script previews remain visible before authorization. The reusable UI does not change risk scoring, direct-mode gates, countdowns, or approval requirements.

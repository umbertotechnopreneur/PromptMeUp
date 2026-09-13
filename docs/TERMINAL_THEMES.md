# Terminal themes

Choose the colors that make PromptMeUp comfortable to read. Run `hm --theme` to open the shared Settings screen with Theme selected. Preview a palette and choose Save when ready. General, AI, Credentials, Conversation, Commands, and Personalization remain accessible in the left sidebar; `--setup` and `--ai-setup` open this same screen with General or AI selected.

The available built-in themes are:

| Theme | Appearance | File |
| --- | --- | --- |
| Cyan | Bright cyan and blue accents on a dark blue background; the default | `themes/cyan.json` |
| AS/400 Green | Bright green accents inspired by classic terminal forms | `themes/green.json` |
| Amber | Warm amber accents on a dark background | `themes/amber.json` |

The saved theme applies to the shared terminal palette and fullscreen forms. Save stores the selected theme identifier with the rest of the draft. Cancel restores the original palette and discards the draft; there is no need to edit settings or pass color values as command arguments.

## Using fullscreen forms

The Settings screen keeps related preferences together and offers Save and Cancel directly. Chat and short commands retain the scrolling terminal flow.

Labels align to the right and values to the left in two columns, with a blank line between fields. Setup and help share an open layout without cards. The header shows the app version and a GitHub link on the right, with a divider underneath. Another divider separates the content from its action buttons and keyboard shortcuts. Button colors distinguish navigation, saving, and cancellation; the focused button also has a visible `>` marker.

The sidebar remains visible in fullscreen terminals of at least 60 columns by 20 rows. Use the up/down arrows to go straight to the settings you need, then Enter or the right arrow to edit them. Your changes stay in the draft as you move between sections; choose Save to apply them directly. Section icons describe their purpose, and language choices show a flag, native name, and language code. Use `--no-emoji` for plain text alternatives.

| Key | Action |
| --- | --- |
| Up/down arrows | Select a section when the section list has focus; otherwise move between fields or actions |
| Tab / Shift+Tab | Move between fields and Save/Cancel |
| F6 | Switch between the sidebar and fields |
| Ctrl+Left | Return to the sidebar |
| Left/right arrows | Change the focused choice, move the cursor while editing text, or move between Save and Cancel when a button has focus |
| PgUp / PgDn | Change sections |
| Enter | Edit a field, accept its value, or choose an action |
| Ctrl+U while editing | Empty the current input |
| Escape | Cancel the current form |

Secret replacement fields never display their contents. Leave a replacement blank to keep the existing configured key. Credentials reports configured or missing status without revealing key values or lengths.

Fullscreen forms use a temporary alternate terminal buffer. Leaving a form restores the main terminal and its earlier scrollback. The form itself does not add its intermediate screens to that history. The application never clears the main screen or scrollback.

Forms require an interactive terminal with ANSI and alternate-buffer support, at least 60 columns and 20 rows. If the terminal becomes smaller, enlarge it or press Escape to cancel. Less capable live terminals use a section menu with an explanatory notice and the same direct Save/Cancel actions. Redirected input/output cannot collect settings and retains the existing error behavior.

## Customizing a theme file

Theme definitions are ordinary JSON files in the `themes` directory beside the application executable. Portable archives include that directory. Keep it with the executable when moving the application.

To change a built-in palette, edit its color values and restart PromptMeUp. To add a palette, copy a theme to a new file and give it a matching unique identifier. For example, a theme with `"id": "my-theme"` belongs in `themes/my-theme.json`. Select the new theme with `hm --theme`.

This is the complete Cyan definition:

```json
{
  "version": 1,
  "id": "cyan",
  "name": "Cyan",
  "colors": {
    "background": "#081820",
    "primary": "#F4FAFF",
    "muted": "#BDCED8",
    "accent": "#80DEEA",
    "info": "#69D2FF",
    "divider": "#7B9DAC",
    "success": "#7CE6A2",
    "warning": "#FFD479",
    "error": "#FF9B9B",
    "selectionBackground": "#80DEEA",
    "selectionForeground": "#081820"
  }
}
```

Use the roles to preserve a clear hierarchy:

| Color | Purpose |
| --- | --- |
| `background` | Surface behind the form and themed content |
| `primary` | Main text and field values |
| `muted` | Secondary explanations and metadata, still easy to read |
| `accent` | Headings and emphasis |
| `info` | Informational content |
| `divider` | Borders and separators |
| `success`, `warning`, `error` | Outcome and validation messages |
| `selectionBackground`, `selectionForeground` | Focused choices and actions |

Every color must use six-digit `#RRGGBB` notation. Named colors, transparency, and terminal markup are not accepted. PromptMeUp validates a minimum contrast ratio of 4.5:1 for text against `background`, 4.5:1 for selection text against its selection background, and 3:1 for dividers against `background`.

## File requirements

Schema version 1 requires exactly the properties shown above. Unknown, duplicate, and missing properties are errors.

- Identifiers contain 1–32 characters, start with a lowercase ASCII letter, and use only lowercase ASCII letters, digits, and hyphens. The filename must match the identifier exactly, followed by `.json`.
- Names contain 1–48 visible characters, without control characters or surrounding whitespace.
- Each file is at most 16 KiB. The directory contains 1–32 theme files, including the required `cyan` theme, and identifiers must be unique.
- A saved theme must exist in the directory. Invalid or missing themes produce an explicit error; they are not silently ignored.

Keep a copy of palettes you customize before replacing an application installation with a new release.

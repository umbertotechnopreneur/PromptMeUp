# Terminal themes

Pick colors you like with `hm --theme`. Try a theme, then choose Save to keep it or Cancel to go back. You're in the same Settings screen as `--setup` and `--ai-setup`. Use the sidebar to reach the other settings.

The available built-in themes are:

| Theme | Appearance | File |
| --- | --- | --- |
| Cyan | Bright cyan and blue accents on a dark blue background; the default | `themes/cyan.json` |
| AS/400 Green | Bright green accents inspired by classic terminal forms | `themes/green.json` |
| Amber | Warm amber accents on a dark background | `themes/amber.json` |
| Ocean | Turquoise and sky blue on deep ocean blue | `themes/ocean.json` |
| Cobalt | Electric blue on dark cobalt | `themes/cobalt.json` |
| Violet | Lavender and lilac on dark plum | `themes/violet.json` |
| Rose | Rose pink on dark burgundy | `themes/rose.json` |
| Coral | Warm coral on dark terracotta | `themes/coral.json` |
| Forest | Leaf green on a deep forest background | `themes/forest.json` |
| Mint | Fresh mint and ice blue on dark teal | `themes/mint.json` |
| Midnight | Periwinkle on nearly black indigo | `themes/midnight.json` |
| Coffee | Caramel and cream on dark coffee brown | `themes/coffee.json` |
| Graphite | Neutral silver on charcoal | `themes/graphite.json` |

The palette preview pairs each colored status label with a short explanation: success means a completed action, warning means attention is needed, and error means an action failed. Labels and theme names are available in all six interface languages.

The preview shows where the theme file lives, who made it, a website link, and a short description. Older themes may show "Not provided" for missing details. Use Ctrl+Up and Ctrl+Down to scroll if the overview doesn't fit. The app finds the file path when loading the theme; it doesn't save that path in the theme file or settings.

Your saved theme is used throughout the app, including fullscreen forms. Save keeps the theme choice with your other changes. Cancel brings back the original colors and discards your changes.

## Using fullscreen forms

The Settings screen keeps related preferences together and offers Save and Cancel directly. Chat and short commands retain the scrolling terminal flow.

Field names and values appear side by side, with space between rows. The app version and GitHub link are at the top; buttons and shortcuts are at the bottom. A `>` marks the selected button, so you can find it without relying on color alone.

Values use bright text in every theme. The value you're editing is bold and underlined. Save and Cancel have icons; `--no-emoji` replaces them with plain text.

The sidebar remains visible in fullscreen terminals of at least 60 columns by 20 rows. Use the up/down arrows to go straight to the settings you need, then Enter or the right arrow to edit them. Your changes stay in the draft as you move between sections; choose Save to apply them directly. Section icons describe their purpose, and language choices show a flag, native name, and language code. Use `--no-emoji` for plain text alternatives.

| Key | Action |
| --- | --- |
| Up/down arrows | Select a section when the section list has focus; otherwise move between fields or actions |
| Tab / Shift+Tab | Move between fields and Save/Cancel |
| F6 | Switch between the sidebar and fields |
| Ctrl+Left | Return to the sidebar |
| Ctrl+Up / Ctrl+Down | Scroll the selected section's overview, including theme metadata |
| Left/right arrows | Change the focused choice, move the cursor while editing text, or move between Save and Cancel when a button has focus |
| PgUp / PgDn | Change sections |
| Enter | Edit a field, accept its value, or choose an action |
| Ctrl+U while editing | Empty the current input |
| Escape | Cancel the current form |

API key fields keep what you type hidden. Leave a replacement field blank to keep your existing key. Credentials tells you whether a key is set, without showing it or its length.

When you leave a fullscreen form, your earlier terminal output is still there.
The form uses a separate temporary screen, so moving between settings doesn't
fill your terminal history or clear it.

Fullscreen forms need a terminal with ANSI colors, support for a separate temporary screen, and a window at least 60 columns wide and 20 rows tall. If you shrink the window too far, enlarge it or press Escape to cancel. Other live terminals use a smaller section menu with Save and Cancel. Settings can't collect input when input or output is redirected; the app reports an error.

## Customizing a theme file

Theme definitions are ordinary JSON files in the `themes` directory beside the application executable. Portable archives include that directory. Keep it with the executable when moving the application.

To change a built-in palette, edit its color values and restart PromptMeUp. To add a palette, copy a theme to a new file and give it a matching unique identifier. For example, a theme with `"id": "my-theme"` belongs in `themes/my-theme.json`. Select the new theme with `hm --theme`.

This is the complete Cyan definition:

```json
{
  "version": 3,
  "id": "cyan",
  "name": "Cyan",
  "author": "PromptMeUp contributors",
  "website": "https://github.com/umbertotechnopreneur/PromptMeUp",
  "description": "Bright cyan and blue accents on a deep blue background.",
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

Each color has a job:

| Color | Purpose |
| --- | --- |
| `background` | Background behind text and forms |
| `primary` | Main text and field labels; settings values always use whitesmoke |
| `muted` | Extra details and explanations, still easy to read |
| `accent` | Headings and emphasis |
| `info` | Informational content |
| `divider` | Lines between sections |
| `success`, `warning`, `error` | Messages showing what worked, needs attention, or failed |
| `selectionBackground`, `selectionForeground` | Focused choices and actions |

Every color must use six-digit `#RRGGBB` notation. Named colors, transparency, and terminal markup are not accepted. PromptMeUp validates a minimum contrast ratio of 4.5:1 for text (including the fixed whitesmoke field values) against `background`, 4.5:1 for selection text against its selection background, and 3:1 for dividers against `background`.

## File requirements

Version 3 theme files need exactly the fields shown above. The `version` field tells the app which file format you're using. All 13 built-in themes include an author, website, and description. Extra, repeated, or missing fields cause an error.

Older custom themes still work. Version 1 uses `version`, `id`, `name`, and `colors`; version 2 also has `author` and `description`. Missing details appear as "Not provided", and the app doesn't rewrite your files. To update a theme, set `version` to `3` and add your own `author`, `website`, and `description`.

- Identifiers contain 1–32 characters, start with a lowercase ASCII letter, and use only lowercase ASCII letters, digits, and hyphens. The filename must match the identifier exactly, followed by `.json`.
- Names contain 1–48 visible characters, without control characters or surrounding whitespace.
- Authors contain 1–80 visible characters and descriptions 1–240. Both must be nonempty, single-line text without surrounding whitespace. Bundled metadata uses English; translated theme names and semantic preview labels belong to the UI catalog.
- Websites are absolute HTTP or HTTPS URLs of at most 2,048 characters, without credentials, control characters, or surrounding whitespace. Bundled themes link to the PromptMeUp GitHub project.
- Each file is at most 16 KiB. The directory contains 1–32 theme files, including the required `cyan` theme, and identifiers must be unique.
- A saved theme must exist in the directory. Invalid or missing themes produce an explicit error; they are not silently ignored.

Keep a copy of palettes you customize before replacing an application installation with a new release.

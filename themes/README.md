# Terminal themes

This folder holds the thirteen built-in themes. Run `hm --theme` to try their colors and save your favorite. The preview explains each status color and shows the theme's file path, author, website, and description. Ctrl+Up and Ctrl+Down scroll the details.

Want to make your own? The [theme guide](../docs/TERMINAL_THEMES.md#customizing-a-theme-file) has a complete example and explains what each color does. Settings saves only your theme choice. Colors and author details stay in the JSON file; the app finds the file path when it loads the theme.

Keep the `themes` folder next to `hm`; portable builds, release archives, and Windows installers include it. Windows packaging checks that the default `cyan.json` is there. If a theme is invalid or your saved choice is missing, the app reports an error instead of quietly choosing another theme.

To add a palette, copy one of the bundled JSON files and change its filename, `id`, `name`, `author`, `website`, `description`, and colors. The filename must match the identifier: for example, `my-theme.json` uses `"id": "my-theme"`. Identifiers begin with a lowercase letter and contain only lowercase ASCII letters, digits, and hyphens, up to 32 characters. Names contain 1–48 visible characters with no leading or trailing whitespace.

Schema version 3 requires exactly `version`, `id`, `name`, `author`, `website`, `description`, and `colors`. Authors contain 1–80 visible characters and descriptions 1–240, without surrounding whitespace or line breaks. Websites are absolute HTTP or HTTPS URLs of at most 2,048 characters, without credentials, control characters, or surrounding whitespace. Bundled themes link to the PromptMeUp GitHub project. Metadata in bundled files is English; UI labels are translated separately.

Legacy version 1 files require only `version`, `id`, `name`, and `colors`. Version 2 files additionally require `author` and `description`. Both versions remain readable and are never rewritten automatically; absent metadata appears as "Not provided" in the preview. To upgrade, use version 3 and supply all three metadata fields. The colors object requires all eleven semantic roles shown in the bundled files. Unknown, duplicate, missing, or null required properties are rejected. Every color uses six-digit `#RRGGBB` notation; terminal color names, escape sequences, and markup are unsupported. Up to 32 files are loaded, each at most 16 KiB.

All text roles, including secondary metadata, status colors, and fixed whitesmoke (`#F5F5F5`) settings values, must reach a contrast ratio of at least 4.5:1 against the background. Selected action text must reach 4.5:1 against the selection background. Dividers must reach 3:1 against the background. These checks run when the catalog loads.

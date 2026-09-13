# Terminal themes

PromptMeUp includes Cyan, Green, and Amber palettes for its terminal forms. Select a theme in setup to preview its colors and save the choice. The theme identifier is stored with local preferences; theme files contain colors only.

The application loads the `themes` directory next to `hm`. Portable builds, published archives, and Windows installers include every JSON file in this directory. Windows release staging checks that the required default `cyan.json` is present. Invalid themes and unavailable saved identifiers stop theme loading with an explicit error.

To add a palette, copy one of the bundled JSON files and change its filename, `id`, `name`, and colors. The filename must match the identifier: for example, `ocean.json` uses `"id": "ocean"`. Identifiers begin with a lowercase letter and contain only lowercase ASCII letters, digits, and hyphens, up to 32 characters. Names contain 1–48 visible characters with no leading or trailing whitespace.

Schema version 1 requires exactly `version`, `id`, `name`, and `colors`. The colors object requires all eleven semantic roles shown in the bundled files. Unknown, duplicate, missing, or null properties are rejected. Every color uses six-digit `#RRGGBB` notation; terminal color names, escape sequences, and markup are unsupported. Up to 32 files are loaded, each at most 16 KiB.

All text roles, including secondary metadata and status colors, must reach a contrast ratio of at least 4.5:1 against the background. Selected text must reach 4.5:1 against the selection background. Dividers must reach 3:1 against the background. These checks run when the catalog loads.

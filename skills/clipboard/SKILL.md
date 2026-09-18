---
name: clipboard
description: Read or write bounded clipboard text locally on Windows.
version: 1.0.1
icon: "📋"
color: "#94E2D5"
metadata:
  openclaw:
    os: [win32]
    requires:
      bins: [pwsh]
---

# Clipboard

Open `hm --skills`, enable this package, then choose **Run action** and `run`.

- Read: `{"Action":"read"}`
- Write: `{"Action":"write","Text":"Hello"}`

Both actions require command approval. Reads return at most 4 KiB of UTF-8 text; longer contents are marked as truncated. Writes accept up to 4 KiB, within the action's JSON input limit. Never copy credentials into an action.

Results stay in the local command view and redacted audit history. They are not automatically sent to OpenAI. This skill does not inspect images or other clipboard formats.

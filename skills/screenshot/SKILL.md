---
name: screenshot
description: Save an explicitly approved Windows screen capture to a new local PNG file.
version: 1.0.0
metadata:
  openclaw:
    os: [win32]
    requires:
      bins: [pwsh]
---

# Screenshot

Open `hm --skills`, enable this package, then run it with a new PNG path:

`{"Mode":"active_window","OutputPath":"capture.png"}`

`Mode` accepts `full_screen` (the default), `active_window` or `active_monitor`. The active window is the foreground window **when the approved command executes**, which may be your terminal. Hidden parts of a window are not reconstructed.

Hide credentials and private information before approving. Captures stay in the selected file: no clipboard copy, automatic image upload or OpenAI vision request is performed. Redaction does not remove secrets from images.

The parent directory must already exist on a local drive. Linked paths and existing output files are rejected. Captures are limited to 16 million pixels, 16,384 pixels per dimension and a 64 MiB PNG. Windows PowerShell 7 must provide its bundled `System.Drawing.Common` assembly; nothing is installed automatically.

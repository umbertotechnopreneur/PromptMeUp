# README images

## Real app screenshots

The project owner supplied four Italian PromptMeUp screenshots on September 20, 2026. The help and About screens show version 0.1.5. These are captures of the application, not generated interfaces.

| Asset | Content | Privacy mask |
| --- | --- | --- |
| [promptmeup-command-it-framed-v1.png](promptmeup-command-it-framed-v1.png) | An answer, session usage, and command choices | Personal path in the example |
| [promptmeup-help-it-framed-v1.png](promptmeup-help-it-framed-v1.png) | Help and command examples | None |
| [promptmeup-chat-it-framed-v1.png](promptmeup-chat-it-framed-v1.png) | Chat welcome and available commands | Current directory and personal prompt name |
| [promptmeup-about-it-framed-v1.png](promptmeup-about-it-framed-v1.png) | Version, credits, and project links | Build-machine name |

## Presentation edits

At the owner's explicit request, Python/Pillow was used to crop the screenshots, hide personal identifiers with opaque rectangles, and center each on a 2944 × 1792 navy canvas with the same fine border. The crop removes terminal tabs, taskbar fragments, or unrelated shell decorations. Different aspect ratios are padded rather than stretched.

Retained app pixels remain at their original size. No text, commands, numbers, colors, or interface elements were redrawn or resampled. The frame and privacy masks are presentation edits, not app features. The README links each preview to its full-size image.

Crop rectangles use original-image coordinates, with right and bottom edges excluded:

| Capture | Original size | Crop (left, top, right, bottom) |
| --- | --- | --- |
| Help | 2862 × 1720 | 1, 64, 2828, 1664 |
| About | 2848 × 1718 | 8, 48, 2834, 1648 |
| Chat | 2830 × 1450 | 0, 205, 2830, 1450 |
| Command response | 2596 × 1708 | 0, 64, 2596, 1708 |

All four final images were visually inspected. Earlier generative retouch attempts changed details and were rejected; they are not used in the repository. The supplied source files remain unchanged outside the repository.

## Shared MeUp artwork

The conceptual family illustration and its generation prompt are documented in [the MeUp visual guide](meup/README.md). It remains separate from app screenshots.

The earlier generated terminal mockups, old banner, and unframed help copy were removed at the owner's request after their README references were replaced.

The project offers its screenshot assets under the root MIT license to the extent rights apply. This does not grant endorsement or rights to third-party trademarks.

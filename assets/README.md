# Project assets

## Application icon

`PromptMeUp.ico` is the Windows executable icon: a geometric `hm_` terminal mark on a dark tile. It contains 32-bit PNG frames at 16, 24, 32, 48, 64, 128, and 256 pixels, including transparent corners.

The 128- and 256-pixel frames have a Matrix-inspired green phosphor treatment: green `#68FF90` on near-black `#020805`, with restrained glow and scanlines. The smaller frames retain the original cyan `#67E8F9` and navy `#0B1220` artwork unchanged for clarity at small sizes. MSIX tiles are derived from the 256-pixel frame.

The icon was drawn for PromptMeUp with simple vector strokes. It uses no font files, external artwork, or image-generation service. The icon and its source are covered by the repository's [MIT license](../LICENSE).

To reproduce the icon on Windows with PowerShell 7:

```powershell
pwsh -NoProfile -File .\assets\generate-icon.ps1 -PreviewDirectory .\artifacts\icon-preview
```

The script replaces only `assets/PromptMeUp.ico` and writes optional PNG previews under the ignored `artifacts` directory. It uses Windows' built-in drawing APIs and installs nothing. `PromptMeUp.csproj` references the icon through `ApplicationIcon` so subsequent builds include it in the executable.

## Lenna terminal portrait

`lenna.rgb.gz` preserves the 78 × 78 RGB pixels from the existing `src/YAi.Client.CLI/Lenna.ps1` resource in the author's archived [YAi! project](https://github.com/umbertotechnopreneur/YAi). It is embedded in `hm`, so `hm lenna` works offline in portable archives and Windows packages. The C# renderer uses Spectre.Console's canvas; it does not run the legacy script.

The import reads the ANSI art as text, preserving background-only cells and the top/bottom colors of each half-block. Its decoded payload is exactly 18,252 bytes of RGB8 pixels in row order. To regenerate it from a local copy of that resource:

```powershell
pwsh -NoProfile -File .\assets\import-lenna.ps1 -SourcePath <path-to-Lenna.ps1>
```

The source script's SHA-256 is `C2A50345AD4C3EDA8468CC58D6E93C81F3769579044557438B1878006B0BB867`. The imported gzip resource's SHA-256 is `52252CAC30B1750AF07EB1A5D9DF0CB8F692C15B2877433E56BFD44F9444F35F`.

This legacy artwork has separate attribution and terms in [lenna-NOTICE.txt](../LICENSES/lenna-NOTICE.txt) and [lenna-legacy-LICENSE.txt](../LICENSES/lenna-legacy-LICENSE.txt). The project's MIT license does not relicense the portrait. The importer and the new C# rendering code are original PromptMeUp code under MIT.

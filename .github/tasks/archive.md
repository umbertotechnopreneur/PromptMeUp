# Task Archive

This archive tracks completed development tasks for reference and review.

## 2026-09-16 — Prepare manual release drafts with x64 and ARM64 installers

- Added a manual `draft` mode to the release workflow and retained `rehearsal` for artifact-only runs. The draft uses the verified main commit and product version; tag-triggered drafts remain supported. Existing tags and releases are never overwritten, and public publication stays a separate maintainer action.
- Added unsigned, per-user Windows EXE installers for x64 and ARM64 using Inno Setup 6. The builder validates native architecture, product version, notices, and allowed payload files. The installer protects existing unmanaged folders and newer versions, adds only a user PATH entry, and removes only its owned entry on uninstall.
- Included both installers with the six portable archives, complete checksums, and build attestations. Added matching runtime cleanup to packaging jobs and documented the exact GitHub CLI command, browser trigger, unsigned status, and final publishing step.

Validation: preflight, restore, formatting verification, XML method comments, Release build with zero warnings or errors, standard .NET cleanup, actionlint, PowerShell syntax parsing, whitespace checks, and independent static review passed. No automated tests, CLI smoke checks, workflow runs, or installer executions were performed. Automatic approval review rejected the command to prepare the portable Inno compiler with "blocked by policy", so EXE compilation remains unverified locally. Prepared on an authorized branch; pull-request review and merge remain pending.

## 2026-09-16 — Pull main and install version 0.1.7

- Fast-forwarded `main` to `ac43fae`, restored the existing repository-rule changes, and resolved the task-archive conflict by retaining both incoming and local entries.
- Increased the product version from `0.1.5` to `0.1.7`, above the locally installed `0.1.6`. Built, signed, and installed Windows x64 MSIX `0.1.7.0` with the existing package identity and trusted local certificate.
- Verified healthy package registration, the executable version, SHA-256 equality for all 283 installed payload files, and the execution alias targeting the new package.
- Confirmed that the GitHub release workflow currently builds portable archives only. The separate MSIX builder supports x64 and ARM64; the MSI builder supports x64 only. Public MSIX distribution still needs a signing identity trusted by recipients and release assets to be published.

Validation: preflight, restore, formatting verification, XML method comments, Release build and Windows publish with zero warnings or errors, package signature and installation integrity, standard .NET cleanup, and whitespace checks passed. Runtime cleanup was scoped to the application project because the test project has no runtime-specific restore target. Automated tests, CLI smoke checks, and live AI calls were not run; installation verification did not launch the application. Kept the installer and verification evidence under ignored artifacts. Automatic approval review blocked recursive removal of the remaining temporary directories, which are still present. No changes were committed or published.

## 2026-09-16 — Preserve multiline paste, number command choices, and install the update

- Added a bounded multiline editor for chat and interactive diagnostics. Bracketed paste keeps normalized line breaks, blank lines, and trailing whitespace as editable text until a separate Enter submits it. Oversized insertions preserve the existing draft, and cursor movement and deletion respect Unicode text elements.
- Enabled and restored Windows virtual-terminal input around the prompt, retained cancellation, and limited redraws to owned input rows without clearing terminal history. Documented the bracketed-paste terminal requirement and the diagnostic file-input alternative.
- Numbered command-menu choices from zero. Up to ten entries accept a digit immediately; larger menus require Enter. Empty AI suggestions return to an ongoing chat prompt or offer Finish/Continue after a single question. Command selection still opens the existing preview and authorization flow.
- Added decoder and editing regression sources, and prevented CI environment enrichment from overriding explicit ANSI settings in context-bar test fixtures. Preserved unrelated local source comments.
- Built, signed, and installed MSIX revision `0.1.5.15`. Verified healthy registration, all 282 installed payload hashes, and the `hm` execution alias. Prepared the changes for the existing pull request.

Validation: preflight, restore, formatting verification, XML method comment checks, Release build and Windows publish, package signature verification, installed-file integrity, independent code review, and whitespace checks passed. The build reported zero warnings and errors. Automated tests, CLI smoke checks, and live AI calls were not run locally; installation verification did not launch the application.

## 2026-09-16 — Control chat visibility through natural language

- Added an isolated, strictly parsed LLM intent call for directly typed chat messages, with six-language instructions and local confirmations for independent session-summary and command-suggestion visibility changes.
- Kept preferences within the current chat, retained them across `/clear`, and allowed `/status` and `/context` to display the summary once. Hidden suggestions never bypass the exact preview, risk assessment, and authorization required by `/run`.
- Counted the classification call in session usage and turn costs, including guide-assisted answers. Recalled notes, previous messages, and command output cannot enter the display classifier. Mixed requests apply changes after the answer succeeds.
- Updated chat guidance and documentation, added parser and workflow regression sources, and adapted existing accounting checks for the extra call. Preserved the two unrelated local code comments.

Validation: repository preflight, restore, formatting verification, XML method comment checks, Release build with zero warnings or errors, and whitespace review passed. Automated tests, CLI smoke checks, and live AI calls were not run.

## 2026-09-15 — Record pull-request ownership and labels

- Aligned `AGENTS.md` and `.github/copilot-instructions.md`: every newly created pull request must be assigned to `umbertotechnopreneur` and carry existing repository labels appropriate to its final scope.
- Prepared the app-guide, context-bar, accounting, and documentation changes for a dedicated pull request after confirming that the open dependency-update pull requests are unrelated. Kept the two pre-existing local code comments outside the commit scope.

Validation: repository preflight, restore, formatting verification, XML method comments, Release build with warnings as errors, whitespace checks, and an independent scope review passed. Automated tests and CLI smoke checks were not run.

## 2026-09-15 — Load the app guide through conversation and show context by role

- Added eight versioned guide chapters in all six languages. Chat version 8 and query version 7 can request up to two relevant chapters through a strict response contract when a natural-language question needs app details. Each turn permits one additional AI call, with local topic validation and budget checks before sending the guide.
- Retained loaded chapters as system context for follow-up questions, replacing them when needed and clearing them with `/clear`. Detailed settings procedures remain in the guide. Each provider call records its own usage and cost; turn totals include both calls, and incomplete pricing makes aggregate costs unavailable.
- Split estimated active context into system, user messages, and retained AI answers. Added a colored budget bar, numerical legend, free capacity, and an explicit guide count already included in system tokens, with readable symbols for terminals without color.
- Added focused regression sources for guide loading, response contracts, request payloads, workflow reuse and clearing, call accounting, session costs, and bar rendering. Preserved the unrelated local comments.

Validation: preflight, restore, formatting verification, XML method comment checks, and Release build passed with zero warnings or errors. Static YAML inspection confirmed 48 localized guide sections and matching built copies. Automated tests, CLI smoke checks, and live AI calls were not run. The installed MSIX remains unchanged.

## 2026-09-15 — Keep base application context focused on capabilities

- Replaced detailed context-budget settings procedures in all six chat and query translations with an overview of diagnostics, scripts, plans, previews, recipes, saved notes, usage, and customization.
- Preserved the host-app identity and existing safety rules, and clarified that the model only receives supplied material and cannot independently inspect local files, settings, or full history. Increased the chat prompt to version 7 and the query prompt to version 6, with matching existing version assertions and documentation.
- Assessed a local, versioned guide split by topic for future on-demand assistance. Guide retrieval remains a design proposal and was not implemented.

Validation: preflight, restore, formatting verification, XML comment checks, Release build with zero warnings or errors, YAML parsing and six-language checks, independent content review, and whitespace checks passed. Automated tests and CLI smoke checks were not run. The installed MSIX remains unchanged.

## 2026-09-15 — Give chat and query prompts application context

- Added two aligned paragraphs in all six languages identifying PromptMeUp as the host application and explaining its settings, operating context budget, environment override, and chat usage controls. References to "this app" now have product context unless the conversation identifies another program.
- Kept interactive settings navigation in the answer with no commands offered for chat execution. Preserved the existing console scope, clarification, safety, preference, and JSON instructions, and avoided treating default settings as current user values.
- Increased the chat prompt to version 6 and the query prompt to version 5, updated existing version assertions, and documented the added context. Preserved unrelated local comments and the earlier installation record.

Validation: preflight, restore, formatting verification, XML comment checks, Release build with zero warnings or errors, YAML parsing and six-language checks, independent content review, and whitespace checks passed. Automated tests and CLI smoke checks were not run. The installed MSIX remains unchanged.

## 2026-09-15 — Build and install the current main revision

- Published `main` revision `a3a3ce7` with the existing uncommitted source comments as a self-contained Windows x64 application, including redistribution notices and source-build metadata.
- Created, signed, and installed MSIX revision `0.1.5.14` with the existing package identity and trusted certificate. Verified healthy registration, matching hashes for all 274 packaged and installed payload files, and the `hm` execution alias targeting the updated installation.

Validation: preflight, restore, formatting verification, XML comment checks, Release build and Windows publish with zero warnings or errors, package manifest and signature verification, checksum and installed-file integrity, and whitespace checks passed. Automated tests and CLI smoke checks were not run; installation verification did not launch the application.

## 2026-09-14 — Prepare memory management and build details for review

- Prepared the local memory manager, chat reminders, compile-time About metadata, regression coverage, and product documentation for a focused pull request. Preserved unrelated local source comments and excluded generated installer artifacts.
- Reviewed memory scope isolation, transactional updates, credential rejection, deletion confirmation, and draft preservation with no blocking findings.

Validation: preflight, restore, formatting verification, XML comment checks, Release build with zero warnings or errors, and whitespace checks passed. The same implementation previously passed all 424 automated tests and signed-MSIX installation verification; tests were not repeated for this review preparation.

## 2026-09-14 — Show compile-time build details and install the updated MSIX

- Added the build date and time in UTC and the build machine to About in all six languages. A shared MSBuild target embeds the values during compilation; a metadata reader supplies the passive view without reading the runtime machine or clock.
- Documented repeatable-build overrides and rejected malformed dates, non-UTC offsets, impossible calendar dates, and disabled assembly metadata generation before compilation.
- Published the current local changes, created and signed MSIX revision `0.1.5.13`, and installed it with the existing identity and trusted certificate. Verified healthy registration, matching hashes for all 274 payload files, the installed assembly metadata, and the updated `hm` execution-alias target.

Validation: preflight, restore, formatting verification, XML comment checks, Release build and Windows publish with zero warnings or errors, all 424 automated tests, six focused MSBuild metadata checks, manifest and signature verification, installed-file integrity, and whitespace checks passed. Installation verification read the installed files without launching the application.

## 2026-09-14 — Test and build the current local changes

- Ran the full automated test suite at the user's request. All 424 tests passed, including the new memory-management coverage, in about 28 seconds.
- Completed the final Release build with warnings treated as errors and preserved the existing local changes.

Validation: preflight, restore, formatting verification, XML comment checks, all 424 automated tests, Release build with zero warnings or errors, and whitespace checks passed. CLI smoke checks were not run.

## 2026-09-14 — Pull the latest documentation

- Fast-forwarded `main` to `fa8109c` and restored the existing local memory-management work. Resolved README and task-archive conflicts by keeping the incoming documentation and the local additions.
- Verified that all 26 local source and test files remained byte-for-byte unchanged, including the six new files, and reviewed the three merged documentation files.

Validation: preflight, restore, formatting verification, XML comment checks, Release build with zero warnings or errors, and whitespace checks passed. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Friendlier documentation

- Rewrote the README and current user guides with shorter sentences, direct explanations, and everyday words. Explained chat memory, token usage, command approval, file previews, and privacy without changing the command examples.
- Simplified contributor, support, security, project, release, theme, prompt, and artwork guidance. Kept technical limits, credits, and license information, and corrected two outdated settings checklist entries to match the current Save action and About navigation.
- Added the same plain-English writing rule to both repository instruction files.

Validation: preflight, restore, formatting verification, XML comment checks, and the Release build passed with zero warnings or errors. Static documentation checks found no new broken local links or heading references, unchanged executable and configuration examples, balanced code fences, and no whitespace errors. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Install the memory management update

- Published the current working tree, including the dedicated memory manager and compact chat/query reminders, as a self-contained Windows x64 application with redistribution notices.
- Created, signed, and installed MSIX revision `0.1.5.12` using the existing package identity and trusted certificate. Included build metadata identifying the base revision and uncommitted source state.
- Verified healthy package registration, matching SHA-256 hashes for all 274 packaged and installed payload files, and an `hm` execution alias targeting the updated package.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, manifest and signature validation, package checksum, installed-file integrity, execution-alias verification, and whitespace checks passed. Automated tests and CLI smoke checks were not run; installation verification did not launch the application.

## 2026-09-14 — Discover and manage saved memories

- Added compact six-language reminders for `/remember`, `/memories`, and `/forget` when starting a question or chat, without repeating the reminder when a question continues into chat.
- Added a dedicated Memories sidebar entry in Help and Settings, plus `hm --memories`, for local browsing, creation, editing, and confirmed deletion of global and current-project notes. Opening the manager preserves the parent menu, settings draft, and main terminal scrollback.
- Added a responsive alternate-buffer browser with full scrollable note details, bounded compact-menu previews, scope and text editing, cancellation-first deletion, and draft preservation after validation errors. The compact terminal flow exposes the same operations.
- Added atomic exact-identifier updates with existing credential validation, scope isolation, duplicate rejection, and capacity checks. Authored regression coverage for update boundaries, scope moves, rejected inputs, atomic failure, cancellation, and CLI routing; updated the query rendering assertions and product guidance.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build with zero warnings/errors, and whitespace checks passed. Independent source reviews covered navigation, dependency injection, local-only operation, draft preservation, keyboard focus, empty states, layout bounds, cancellation, and six-language completeness. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Prepare the latest About navigation installer

- Published commit `ec96ae7` as a self-contained Windows x64 application with redistribution notices and created signed MSIX revision `0.1.5.11` using the existing package identity and signing certificate.
- Verified the package signature, version, publisher, execution alias, checksum, source revision, and SHA-256 integrity of all 273 packaged payload files. The installer is ready for installation; the installed application was not updated.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, manifest validation, signature verification, payload integrity, and whitespace checks passed. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — About navigation in Help and Settings

- Added an About sidebar entry to Help and the settings workspace, opening the existing project information screen with Enter or Right. The compact settings menu opens the same screen and returns to the previous editable section; scrolling help retains the direct `hm about` command.
- Preserved the settings draft, language and theme preview, and menu selection when returning from About. Each fullscreen view releases its alternate buffer before opening the next screen, preserving the main terminal and scrollback.
- Added labels and focus-aware keyboard guidance in all six supported languages, kept help notices within narrow terminal widths, and updated the product guides and existing help-view construction.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build with zero warnings/errors, and whitespace checks passed. Independent source reviews covered keyboard navigation, empty action pages, dependency injection, compact rendering, draft preservation, and alternate-buffer ownership. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Consistent help and settings workspaces

- Unified help and settings through the same responsive workspace and action-button renderers. Help retains its sidebar at every supported width and matches settings navigation spacing, section emphasis, margins, and footer actions.
- Kept `hm` explicitly white in commands, descriptions, usage hints, and the shared header. Descriptions and examples use two-space insets, with four-space argument notes and preserved wrapped-line indentation. Static help uses the same entry renderer.
- Distinguished optional square-bracket groups with the theme's yellow/amber color, preserving spaces, nested groups, and quoted values.
- Built, signed, and installed MSIX revision `0.1.5.9`; verified healthy registration, all 273 installed payload hashes, and the WindowsApps alias targeting that revision.
- Included the subsequent optional-parameter coloring in MSIX revision `0.1.5.10`, repeated the non-test validation gate, and verified its signature, healthy registration, all 273 payload hashes, and updated alias target before preparing the complete source changes for main.

Validation: preflight, restore, solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, whitespace checks, signature validation, and installation integrity checks passed. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — About artwork and richer settings overviews

- Added `hm about` and `hm --about`, with a six-language fullscreen information page, the original HELP ME gradient artwork and white H/M initials, project links, version, and attribution. The standalone command opens before settings or AI services are resolved; compact and redirected terminals receive scrolling output, and fullscreen exit restores the main buffer.
- Added the selected theme's complete source path, author, website hyperlink, and description to Theme settings. Metadata follows the live draft and uses the existing scrollable overview. Schema version 3 adds a validated website to all 13 bundled palettes; versions 1 and 2 remain readable, and the computed local source path is excluded from serialization.
- Shared the existing model-price comparison between Costs and AI settings, highlighting the selected model and retaining supported models with unavailable rates. The comparison uses cached standard short-context prices and adapts its columns to the available width. Short settings viewports reserve space for both focused fields and scrollable details.
- Updated six-language UI labels, theme and CLI guidance, and existing theme assertions while preserving unrelated work in the shared checkout.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build with zero warnings/errors, and whitespace checks passed. Independent source reviews covered routing, alternate-buffer restoration, compact layout, metadata privacy, backward compatibility, and price semantics. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — General overview, help entry point, and expanded themes

- Replaced the command center with help for plain `hm`, including first launch and redirected output. General settings now combines draft application status with cached usage and costs; short terminals can scroll the overview with Ctrl+Up/Down, and the scrolling compatibility view shows it too.
- Aligned the operating-budget label with the session overview's first label column while retaining the separate Spectre progress bar and localized values.
- Added semantic explanations beside palette samples, whitesmoke settings values, and clear Save/Cancel action emojis. Added ten bundled themes, backfilled version-two author/description metadata for the original three, and retained explicit support for legacy version-one theme files.
- Built, signed, and installed MSIX revision `0.1.5.8`. Verified healthy registration, all 273 installed payload hashes, and the WindowsApps `hm` alias target.

Validation: preflight, restore, solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, whitespace checks, package identity/signature verification, and installed payload integrity checks passed. Automated tests and CLI smoke checks were not run. Concurrent work in the shared checkout was preserved.

## 2026-09-14 — Fix the budget bar and align leading emoji

- Removed redundant leading padding from the shared icon-prefix helper while retaining trailing separation, intentional layout margins, and spacing after fullscreen navigation markers. Aligned both contributor instruction files and updated the existing spacing assertions.
- Fixed the operating-budget crash by providing Spectre's progress task with the localized budget label instead of an empty description. Extremely narrow terminals retain the budget text without constructing a bar below its minimum width.
- Published, signed, and installed MSIX revision `0.1.5.7` with the latest fixes. Confirmed healthy registration, matching SHA-256 hashes for all 263 installed payload files, and a WindowsApps execution alias targeting that version.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build and Windows publish with zero warnings/errors, manifest and signature verification, and installed-file integrity checks passed. Independent source review confirmed emoji call-site spacing and the exact Spectre package's progress contracts. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Install the unified settings MSIX

- Published the current working tree as a self-contained Windows x64 application, exported redistribution notices, and signed MSIX revision `0.1.5.5` with the existing trusted certificate.
- Installed the update and verified healthy registration, matching SHA-256 hashes for all 263 payload files, and an AppExecLink targeting the new version through the WindowsApps `hm.exe` alias.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build, Windows publish, manifest validation, and package signature verification passed. Automated tests and CLI smoke checks were not run; installation verification did not launch the application.

## 2026-09-14 — Unified settings workspace and clearer terminal feedback

- Consolidated General, AI, Credentials, Conversation, Commands, Personalization, and Theme into one settings draft with an emoji sidebar. Setup, AI setup (including the existing AI settings alias), and theme switches only choose the opening section; the main menu has one Settings entry.
- Removed the fullscreen summary and F10 step. Save validates and persists directly; Cancel restores the draft language and palette. Tab reaches Save/Cancel and Left/Right moves between their buttons. Shortcuts distinguish muted key labels from brighter actions.
- Preserved section navigation in compact terminals and added a shared section-menu compatibility view. Exposed complete focused labels, hid credential replacements, previewed theme colors, and kept connection checks opt-in after initial setup.
- Moved the operating budget onto its own row with a Spectre progress bar and the existing localized estimate, capacity, and percentage. Added a blank line immediately after the closing thank-you message.
- Updated all six UI languages and the product guides while preserving concurrent command-clarification work separately.

Validation: preflight, restore, full-solution formatting verification, XML method summaries, Release build with zero warnings/errors, and whitespace checks passed. Independent source reviews covered routing, keyboard focus, compact geometry, cancellation, and privacy. Automated tests and CLI smoke checks were not run.

## 2026-09-14 — Clarify ambiguous requests without closing the session

- Fixed the suggestion menu page size for answers with no suggested commands and chat answers with only one suggestion.
- Added an explicit clarification/chat continuation choice and neutral finish guidance when no commands are suggested, using the existing conversation and authorization workflow. Updated all six UI languages and the CLI reference.
- Versioned the query and chat prompts to ask a focused clarification question and withhold commands until necessary details are available, with guidance in all six languages and matching existing version assertions.

Validation: preflight, restore, XML method summaries, Release build with zero warnings/errors, scoped formatting verification, and `git diff --check` passed. Full-solution formatting verification reported line-ending and encoding issues in unrelated concurrent settings/UI changes; those files were preserved. Independent source review covered small menus, defaults, conversation continuity, privacy, authorization, and localization. Automated tests and CLI smoke checks were not run; the installed executable was not replaced.

## 2026-09-14 — Consistent setup and help navigation

- Added a shared fullscreen header with a divider, app version, and project link aligned to the right. Reopened setup starts in the section list; Up/Down selects sections and Enter/Right moves to fields while preserving the draft and review/save step.
- Removed sidebar numbers, added meaningful section icons, and kept country flags, native names, and language codes consistent in choices and summaries. Emoji-free rendering remains available.
- Matched help to the setup layout and focus model. Help entries show complete examples with lowercase white `hm`, colored options and values, descriptions using the same colors, and explanations of useful arguments. All new text is available in the six supported languages.
- Added a blank line after the closing project separator and removed the redundant application-status heading. Updated the product guides to describe the new navigation and examples.
- Published, signed, and installed MSIX revision `0.1.5.4`, including the coordinated fullscreen footer and conditional range improvements.

Validation: the shared preflight, restore, formatting verification, XML method summaries, and Release build passed with zero warnings or errors. Independent source reviews covered keyboard focus, draft preservation, narrow layouts, and example rendering. Windows publish and package signature verification passed; installed registration reports `Ok`, all 263 payload files match their source hashes, and the WindowsApps execution alias targets the updated package. Automated tests and application smoke tests were not run.

## 2026-09-14 — Conditional help range and consistent fullscreen footers

- Show the localized displayed-line range only when the visible help content overflows. Remove trailing empty spacing rows from the count, retain scroll clamping on resize, and render primary white text with bold numbers on one bounded row.
- Share the notice, separator, action, and shortcut layout between fullscreen help and forms, preserving two-column margins, variable form notices, semantic action colors, and compact localized hints. Both surfaces use the shared fullscreen header.
- Updated the displayed-line wording in all six supported languages, including "Righe visualizzate" in Italian, and prepared an illustrative layout preview outside the repository.

Validation: preflight, restore, full formatting verification, XML method summaries, and the Release build passed with zero warnings or errors. Independent source review covered footer geometry, range visibility, clipping, focus, and resize behavior. Automated tests and CLI smoke tests were not run.

## 2026-09-14 — Release checkpoint and main-branch policy

- Prepared the completed fullscreen forms, themes, navigable help, local image display, artwork, and MSIX packaging changes for the requested commit and push on `main`.
- Reviewed the complete file inventory and confirmed that only intended source, documentation, redistribution notices, themes, and curated artwork are included. Build output, signing material, logs, and local data remain excluded.
- Verified that GitHub already requires pull requests and the existing quality checks on `main`, with administrator bypass enabled. Force pushes and branch deletion remain disabled; no protection settings needed changing.

Validation: preflight, restore, formatting verification, XML method summaries, and the Release build passed with zero warnings or errors. The remote branch matched the local starting commit. Automated tests and application smoke tests were not run.

## 2026-09-13 — Open terminal layouts, fullscreen help, and Matrix artwork

- Removed cards and enclosing frames from runtime views and recorded the no-card rule in both repository instruction files. Forms align labels to the right and values to the left in two columns, with one blank row between fields, semantic navigation colors, and a single action separator.
- Kept long values on one terminal row with Unicode-aware clipping and sized the editor from the actual value column. Reviewed focus visibility, review scrolling, narrow layouts, and resize behavior.
- Added fullscreen help with five navigable sections, scrollable descriptions, keyboard hints in all six languages, and Escape/Q exit. Supported terminals use a disposable alternate buffer; redirected output and CLI errors retain the scrolling reference.
- Recovered the Lenna ANSI resource from the author's archived YAi! project and preserved its 78-by-78 RGB pixels in a compact embedded image. `hm lenna` and `hm --lenna` render it with Spectre.Console, centered in both directions, without AI configuration, a database connection, or a download. Preserved the legacy attribution and source license in release notices.
- Gave the 128- and 256-pixel icon frames green phosphor, glow, and scanlines while preserving smaller frames byte-for-byte. Removed the banner's horizontal underscore and retained its separate block cursor.
- Updated product guides, artwork provenance, and the displayed dependency versions. Produced, signed, and installed MSIX revision `0.1.5.3` after the user closed the earlier running session.

Validation: preflight, restore, formatting verification, XML method summaries, Release build, and self-contained Windows publish passed with no warnings or errors. Independent source reviews covered both fullscreen layouts and standalone command routing. Checked icon frame invariance, visual artwork, exact RGB dimensions, the published embedded resource, exported notices, package manifest, and signing verification. Installed registration reports `Ok`, all 263 payload files match their source hashes, and `hm` resolves through the WindowsApps execution alias. Automated tests and application smoke tests were not run.

## 2026-09-13 — Install the current build and verify command resolution

- Published the current working tree as a self-contained Windows x64 build and installed signed MSIX revision `0.1.5.2` using the existing package identity and trusted signing certificate.
- Verified healthy installed registration and SHA-256 equality for all 261 packaged files, including the application assembly, themes, prompts, and manifest.
- Confirmed both current-process and persisted machine/user PATH resolve `hm` exclusively through the WindowsApps execution alias; inspected its AppExecLink data to verify that it targets the newly installed revision. No persistent PATH change was necessary.

Validation: preflight, restore, formatting verification, XML method summaries, Release build, self-contained publish, license export, manifest validation, package signing, signature verification, installation integrity, and command-resolution checks passed. No automated tests or application smoke tests were run; verification did not launch the application.

## 2026-09-13 — Signed MSIX installation and project icon

- Added a repeatable MSIX packaging script for prepared self-contained Windows builds, including prompts, themes, redistribution notices, architecture checks, manifest validation, certificate-store signing, and signature verification.
- Installed local MSIX revision `0.1.5.1` with the `hm.exe` execution alias and normal Win32 application-data behavior, using an already trusted certificate without exporting its private key or changing trust settings.
- Removed the previous per-user MSI with its standard uninstaller and verified that its executable and user PATH entry were removed while the existing database remained unchanged.
- Added `assets/PromptMeUp.ico`, a reproducible vector-based generator, and provenance notes. The seven icon resolutions are embedded in the executable and reused for the MSIX tiles.
- Documented local MSIX packaging, signing, installation, updates, aliases, and removal alongside the existing MSI instructions.

Validation: preflight, restore, formatting verification, XML method-comment checks, Release build, and self-contained publish passed with zero warnings or errors. Validated the ICO frame directory, visually inspected the icon and executable resource, and verified the SDK package signature, healthy installed registration, matching executable/tile hashes, and WindowsApps command resolution. No automated tests or CLI smoke tests were run; application processes were not launched during validation. Existing unrelated source changes were preserved.

## 2026-09-13 — Command notice spacing and STDERR emphasis

- Added a blank line before and after the combined command-risk advisory and output-sharing notice.
- Confirmed both output warning icons use the shared helper's surrounding spaces; isolated slow blinking to the STDERR label, respecting disabled animations and keeping the icon and output content steady.

Validation: preflight, restore, formatting verification, XML method summaries, and Release build passed with zero warnings or errors. No automated tests or CLI smoke tests were run. Blink appearance depends on terminal support; the installed executable was not replaced.

## 2026-09-13 — Fullscreen setup and JSON terminal themes

- Implemented revisitable fullscreen setup sections, the focused AI settings form, and a standalone `hm --theme` chooser with live palette preview and explicit review/save.
- Kept draft settings and non-echoing credential inputs local to the views. Added keyboard focus, bounded text editing, Unicode-aware cursor windows, internal scrolling, resize handling, six-language hints, and a sequential compatibility path.
- Preserved main terminal history through a disposable alternate buffer and aligned both repository instruction files with that behavior.
- Added validated Cyan, AS/400 Green, and Amber JSON palettes, applied semantic colors throughout shared views, and persisted the selected identifier with a transactional schema-v3 upgrade.
- Included theme resources in build/publish output and Windows ZIP/MSI staging, documented customization, and added regression sources for validation, migration, and command routing.
- Reviewed cancellation, secret rendering, startup and menu integration; corrected focused menu saves so a temporary display-language override cannot change the saved language.

Validation: preflight, restore, formatting verification, XML method summaries, Release build with 0 warnings/errors, whitespace review, and inspection of copied theme JSON resources passed. Automated tests and CLI smoke tests were not run; new regression sources compiled only. Interactive terminal behavior still requires an explicitly requested smoke/manual check. The installed executable was not replaced; no branch or commit was created by this task.

## 2026-09-13 — GitHub Actions failure fixes

- Traced the main quality-gate failures to ANSI styling in semantic output assertions and a second SQLite connection pool left open during Windows fixture cleanup.
- Preserved all visible-text assertions and narrow-terminal coverage while removing ANSI control sequences from comparisons; released both fixture-specific database pools without clearing other fixtures' pools.
- Fixed the attribution failure on dependency PR #8 by recognizing the native SQLite package and checking its declared license file against the reviewed upstream text. Unknown or changed license declarations still stop packaging.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors in a temporary source snapshot, preserving concurrent fullscreen/theme work. Notice export passed for the current main dependency graph (26 packages) and the exact PR #8 graph with SQLite 3.53.4 (27 packages). No local automated tests or CLI smoke tests were run; the existing GitHub Actions workflows provide those checks after push.

## 2026-09-13 — Product documentation refresh

- Reworked the README around everyday terminal tasks, with direct examples for saved notes, context counters, and the shorter AI settings flow.
- Updated the CLI, memory/cost, privacy, architecture, validation, and prompt guides to match the implemented behavior and limits.
- Simplified the documentation index and support copy; identified the printable manual as an earlier edition and directed readers to the current web guides.
- Checked 51 local links and heading targets and reviewed the guides against the implementation.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors on a temporary source snapshot of `a9f0532` plus this documentation update. The shared workspace's formatting and build checks encountered concurrent, unfinished fullscreen/theme edits, which were preserved outside this change. Automated tests and CLI smoke tests were not run because they were not requested.

## 2026-09-13 — Commit the remaining local work

- Included the remaining credential-redaction fixes, synthetic regression sources, full terminal/IDE restart guidance, repository validation rules, review resolution, and fullscreen design analysis in one user-requested checkpoint.
- Reviewed the pending file inventory and privacy changes before staging all non-ignored work.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors. Automated tests and CLI smoke tests were not run because they were not requested.

## 2026-09-13 — Shared workflow, storage, and localization behavior

- Centralized audit session lifecycles with typed outcomes and cancellation-independent cleanup; script validation now records its actual result.
- Shared chat turn submission and action handling while retaining completed-turn cost across recoverable input-limit failures.
- Reused settings-summary formatting and atomic artifact writes, preserving row order, encoding, validation, and explicit overwrite policies.
- Consolidated 396 UI keys into nine functional catalogs with six named translations per key; static source comparison preserved all 2,376 effective translations.
- Extracted setup and installation workflows from the invocation orchestrator and shared non-session activity recording.
- Preserved unrelated local privacy fixes and documentation changes outside the refactoring commit, including their relocated translation entries.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors. Existing localization test source was adapted and compiled; automated tests and CLI smoke tests were not run because they were not requested.

## 2026-09-13 — Bounded memories, context accounting, and AI settings

- Added explicit project/global memories with local relevance selection, credential protection, and an 800-estimated-token request envelope.
- Separated retained active context, operational input budget, last-call provider counters, and cumulative conversation usage; `/status`, `/context`, and `/clear` preserve accounting.
- Added a persisted 16,000-token default input budget, an optional invocation override, and an additive SQLite version-two migration.
- Added `hm --ai-settings` to change only model behavior and context limits without repeating setup or changing credentials.
- Kept oversized chat requests recoverable without discarding earlier turns, and documented all commands and limits.

Validation: preflight, restore, formatting verification, XML method-comment checks, and Release build passed with zero warnings or errors. Regression test sources were added and compiled; automated tests and CLI smoke tests were not run because they were not requested.

## 2026-09-13 — Fullscreen terminal design census

- Added [the fullscreen UX analysis](../../docs/FULLSCREEN_UX_ANALYSIS.md) with stable selection numbers for 12 functional pages and one optional navigation hub, source-backed interaction counts, and flows that should retain waterfall output.
- Reviewed Spectre.Console feasibility, alternate-buffer history preservation, keyboard behavior, shared forms and individual command authorization.
- Prepared and visually inspected three ImageGen concepts: cyan setup, green setup, and an amber plan workspace. Kept generated previews and prompts in the ignored local artifacts directory.
- Completed analysis only; implementation scope remains for the user to select. Preserved unrelated work and created no branch or commit.

Validation: preflight, restore, formatting verification, XML method summaries, and Release build passed with 0 warnings/errors. Whitespace review passed. No automated tests or CLI smoke tests were run.

## 2026-09-13 — Privacy and key-guidance review fixes

- Resolved all four findings from [the September 13 review](review-2026-09-13.md).
- Shared credential-field classification between text and structural audit redaction, covering common token/key names and preserving typed usage metadata.
- Added complete JSON and PowerShell value handling, including duplicate properties, escaped names, credential-valued containers, doubled quotes, backticks, here-strings, truncated values, and placeholder suffixes.
- Added the terminal-restart hint to organization-cost authentication warnings and clarified the full terminal/IDE restart in all six languages, the README, and the privacy guide.
- Verified that local command/output previews remain exact while synthetic credentials are removed from actual SQLite records and captured provider requests.

Validation: preflight, restore, formatting verification, XML method comments, and Release build with 0 warnings/errors passed. The automated checks launched under the instructions read at task start completed with 320/320 tests and eight isolated CLI smoke checks passing. No further tests were run after the concurrent explicit-request-only testing rule was discovered. No real provider calls, credential changes, PATH changes, or font changes were made; the installed executable was not replaced.

## 2026-09-13 — Tests only on explicit user request

- Aligned `AGENTS.md`, Copilot instructions, contributor guidance, and the validation guide so agent-run automated tests and CLI smoke tests require an explicit user request.
- Kept preflight, restore, formatting, XML-comment checks, and Release builds in the default non-test validation gate. Requests to implement, review, commit, or push changes do not authorize test execution.

Validation: reviewed the documentation diff and checked whitespace. No tests or smoke tests were run for this documentation change, as requested by the user.

## 2026-09-13 — Repository checkpoint validation

- Reviewed the accumulated console presentation, language flags, Windows key guidance, shared project banner, and MSI directory-cleanup changes before publishing the repository checkpoint.
- Confirmed that the four findings in the September 13 review remain tracked separately.

Validation: preflight, restore, formatting verification, XML method comments, Release build with 0 warnings/errors, 225/225 tests, and 15 isolated CLI smoke checks passed. Smoke checks covered help/status in all six languages, version output, invalid arguments, and missing setup; they verified one project banner per invocation and preserved terminal scrollback. No real credentials, provider requests, PATH changes, or font changes were used.

## 2026-09-13 — Current directory and shared project banner

- Added a dedicated current-directory row to the invocation header, supplied by the application and escaped for terminal display.
- Added `RenderProjectBanner` with localized thanks for using hm (help me), a GitHub star/suggestion invitation, the repository link, and linked copyright attribution.
- Reused the banner at the end of help and on process exit without duplicate output; normal, application-error, and initialization-error exits retain their expected exit codes.
- Reviewed the new UI and existing credential, authorization, execution, and redaction paths. Four open findings are documented in [the September 13 code review](review-2026-09-13.md) and tracked separately in the active task list.

Validation: preflight, restore, formatting verification, XML method comments, Release build with 0 warnings/errors, and 225/225 tests passed. Fifteen isolated CLI smoke checks covered help/status in all six languages, invalid arguments, missing setup, and invalid startup configuration. Narrow-terminal, literal bracketed-path, emoji-option, and banner-deduplication rendering tests passed. No real provider requests, credential changes, PATH changes, or font changes were made. The installed application was not replaced.

## 2026-09-13 — API key session-restart guidance

- Added localized restart guidance after API or admin key changes on Windows, where an existing terminal can retain a stale process environment value.
- Added the same guidance to HTTP 401 authentication failures and locally detected missing or invalid API keys without changing unrelated provider errors.
- Covered authentication, non-authentication, and non-Windows error rendering with focused regression tests.

Validation: preflight, restore, formatting verification, XML method comments, Release build with 0 warnings/errors, and 221/221 tests passed.

## 2026-09-13 — Updated per-user MSI installation

- Downloaded the official WiX Toolset 3.14.1 binaries for an isolated, non-administrator build after the system installer required elevation and .NET Framework 3.5.
- Repaired MSI harvesting for container-only payload directories by attaching uninstall cleanup to a descendant component.
- Built and WinGet-validated the current x64 portable ZIP and per-user MSI, then verified the MSI against `SHA256SUMS.txt`.
- Installed the MSI successfully, removed the obsolete portable PATH entry and moved its versioned directory to the Recycle Bin.
- Verified MSI registration, the stable `%LOCALAPPDATA%\Programs\PromptMeUp` executable, exactly one user PATH entry, no machine PATH entry, and the installed six-flag setup menu.

Validation: MSI SHA-256 is `EF4BB8C0EB908CFB3265156C31225CD361C4FDFA1EBADA75AC2284A3F314A151`; Windows Installer returned exit code `0`; preflight, restore, formatting verification, XML comments, Release build with 0 warnings/errors, 218/218 tests, packaged smoke tests, WinGet validation, and installed interactive setup smoke passed.

## 2026-09-13 — Emoji spacing, language flags, and light accent

- Standardized user-facing emoji rendering with one visible space before and after every icon while preserving compact ASCII fallbacks for `--no-emoji`.
- Added country-flag emoji to all six language choices in setup.
- Replaced the purple accent and selection highlight with the shared very-light near-cyan `lightskyblue1` color.
- Added the emoji-spacing rule to both repository-wide instruction files and regression coverage for spacing and flags.

Validation: preflight, restore, formatting verification, XML method comments, Release build with 0 warnings/errors, 218/218 tests, isolated CLI `--version`/`--help`/`--status` smoke checks, and emoji-enabled rendering passed.

## 2026-09-06 — Automatic formatting fixes before the CI gate

- Added `scripts/format.ps1` with applying and read-only verification modes.
- Updated the quality workflow to apply supported `dotnet format` fixes before the final formatting check and summarize runner-only changes.
- Documented the local workflow in `CONTRIBUTING.md` and `docs/VALIDATION.md`.

Validation: preflight, restore, automatic formatting, formatting verification, XML method comments, Release build with 0 warnings/errors, 216/216 tests, CLI `--version`/`--help`/`--third-party` smoke checks, and `git diff --check` passed.

## 2026-09-05 — PR #20 Windows Unicode output

- Fixed the large-source PowerShell bootstrap to emit UTF-8 explicitly. The Windows CI runner otherwise replaced Vietnamese characters with question marks; parent-side UTF-8 decoding cannot recover them.
- Preserved the existing Unicode assertions, command preview, and exit behavior. Local tests and builds were deferred at the user's request; the PR's existing CI covers the correction.

## 2026-09-05 — Current executable, PATH, and concise documentation

- Uninstalled the previous MSI and installed the verified current portable build in the user's stable application directory. PATH selects this copy, which survives repository build cleanup.
- Verified all 39 installed payload files, four installed CLI smoke checks, and an unchanged application database. Repeated the full gate: 216/216 tests and a Release build without warnings or errors.
- Shortened artifact, privacy, and cost guidance. Cleaned .NET Release, runtime-specific, and Debug outputs; ignored directory deletion remains pending because the execution policy rejected it.

## 2026-09-05 — Prominent README early-access notice

- Added a GitHub Note alert immediately after the introduction, using PromptMeUp as the product name and linking directly to the source-build instructions.
- Retained the existing installation-section guidance and confirmed that the public GitHub Releases page has no published releases.

Validation: checked the alert placement, section link, and whitespace with `git diff --check`. Documentation-only follow-up; the preceding full validation gate remains applicable to the unchanged application code.

## 2026-09-05 — Review improvements and configurable artifact limits

- Rejected credential-bearing query arguments consistently and redacted every conversation message before provider transmission while preserving exact local command previews.
- Preserved completed provider responses when optional local pricing or session cleanup fails; recorded actual usage and estimated costs for incomplete or invalid content responses and included billable failures in local totals.
- Shared bounded process execution between approved commands and font helpers, including cancellation cleanup, output caps, and helper deadlines. Large approved source travels over standard input to avoid platform command-line limits.
- Replaced small fixed script and plan limits with configurable UTF-8 budgets: 1 MiB scripts and 8 MiB serialized plans/recipes by default, each configurable from 1 to 64 MiB. Unified read/write checks and exposed effective limits in localized status output.
- Separated artifact payload size from ordinary chat limits and reserved model context for the configurable artifact output budget (16,384 tokens by default). Updated both versioned artifact prompts in all six languages.
- Completed six-language local risk explanations and documented configuration, privacy, accounting, and process boundaries.

Validation: the required local gate passed with 216/216 tests and zero Release-build warnings/errors. Eleven isolated CLI smoke checks covered help/version, status in all six languages, overrides, invalid configuration, and credential-argument rejection. No real provider requests or PATH/font changes were performed. Disposable smoke data remains under ignored `artifacts/`. PATH resolves the existing August 16 installation, independently of the new repository Release build; the installation was inspected and left unchanged.

## 2026-09-02 — Repair PR #10 CI fixture failures

- Investigated the first PR quality run: Lint and macOS passed, while Windows exposed global SQLite pool cleanup racing other fixtures and Linux timed out before the inherited-pipe fixture emitted its expected marker.
- Replaced both global pool clears with cleanup scoped to the fixture's exact database connection pool, retaining parallel test execution.
- Removed PowerShell module discovery from the process fixture, wrote and flushed markers directly, and allowed CI startup headroom. The deadline remains 10 seconds, the return assertion remains below 20 seconds, and the independent child lifetime is 30 seconds, so waiting for the child still fails the regression.
- Preserved production code, timeout semantics, and all output/cancellation assertions; prepared the fix for the existing PR and its native CI matrix.

Validation: the full local gate passed with 148/148 tests and zero build warnings/errors. The parallel SQLite regression subset passed ten consecutive runs; isolated help/version CLI smoke checks passed. Native CI outcomes are recorded in PR #10 checks.

## 2026-09-02 — Resolve ten verified code-review findings

- Completed the privacy, deadline, chat/risk/shell, pricing, and summary plan; every finding in [the review](review-2026-09-02.md) now maps to tracked regression coverage.
- Protected quoted and serialized JSON credentials, rejected new credential-bearing preambles, sanitized legacy settings before use, and removed raw exception details from persistent diagnostics.
- Bounded complete HTTP/process operations, preserved completed assistant answers, corrected diagnostic risk classification and Unix shell instructions, and selected model-specific pricing bands from actual input usage.
- Restricted summary reads to the indexed month interval and preserved successful-request totals at period boundaries.
- Added 58 regression cases and updated privacy, architecture, costs, and validation guidance. Preserved the repository presentation and release work already merged through PR #9.

Validation: full required gate passed with 148/148 tests and zero build warnings/errors; isolated help, version, Italian/Vietnamese status, and French attribution smoke checks passed. No real credentials or AI requests were used. The OS-temporary smoke directory was retained because automatic approval review blocked its deletion; it is outside the repository and excluded from publication.

## 2026-09-02 — MIT repository, product presentation, and portable CI/CD

- Preserved the MIT license and upstream attribution, including transitive dependency notices and the exact self-contained runtime's notices in release packages.
- Added maintainer governance, CODEOWNERS, reproducible main/tag protection settings, dependency review, pinned Actions, and a six-target portable release workflow with checksums, provenance attestations, and a draft publication step.
- Enabled private vulnerability reporting and applied main protection to administrators, required checks, pull requests, linear history, and resolved conversations; blocked changes and deletion of version tags.
- Rebuilt the English README around the weekend-project story and UmbertoGiacobbiDotBiz team, with a retro banner, three explicitly illustrative screen renderings, and links to TrackMeUp, viewsapp.ai, and the creator's website.
- Aligned the English repository-writing rule in AGENTS.md and GitHub Copilot instructions. Preserved the other agent's application-code work.

Validation: the full required gate passed on an isolated baseline plus these changes, with 90/90 tests and zero Release-build warnings/errors. The Windows x64 portable archive passed credential-free startup/help/attribution smoke checks; all inventory notice references resolved inside the ZIP. GitHub-flavored Markdown rendering, local links, PowerShell parsing, and actionlint checks passed. The shared worktree's initial format check detected in-progress C# edits from the concurrent code task; those edits were excluded from this change.

## 2026-09-02 — Guided error diagnosis

- Added bounded text, UTF-8 log file, and stdin diagnosis with credential redaction and strict source validation.
- Added an evidence-led prompt and UI text in all six languages; reused exact per-command approval for follow-up checks.
- Validated parser boundaries, cancellation, redaction, Release build/tests, and isolated CLI smoke checks.
- Delivered as the first feature commit and dedicated PR in the terminal assistance series.

## 2026-09-03 — Reconcile diagnosis PR with main

- Moved diagnosis history away from the archive append point changed by main, and separated planned feature work from the shared baseline task-status entry.
- Preserved every diagnosis and planned-feature entry and rebased the PR onto current main to satisfy its required up-to-date checks without a merge commit.
- Limited manual conflict corrections to the two task ledgers; production code and runtime prompts match the validated integration of the original feature with main.

Validation: the original feature branch passed its full required gate with 98/98 tests; the integrated source passed with 156/156 tests and zero build warnings/errors. Diagnosis help and unconfigured-input smoke checks passed with isolated data. Native CI results for the updated branch are recorded in PR #11.

## 2026-08-16 — PromptMeUp 0.1.5 protected-preamble installer

- Bumped the product and current packaging examples from `0.1.4` to `0.1.5` for the multilingual protected-preamble release.
- Built and validated the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, checksums, and WinGet manifests.
- Upgraded the current-user installation from `0.1.4` to `0.1.5` silently without elevation or restart.
- Verified all seven installed payload files byte-for-byte against the x64 ZIP, MSI registration `0.1.5`, exactly one user PATH entry, zero machine PATH entries, and `hm --version` / `hm -where` output.

Validation: full repository gate passed with 90/90 tests and zero build warnings/errors; MSI SHA-256 is `1E7E19B2976897ADD724B47DE07F1B195657F94C80FBA71096D4449F6469671C`.

## 2026-08-16 — Multilingual AI preamble protection

- Reframed the optional setup instruction as a preamble appended to every user-facing chat and one-off query, with a hard 500-word limit and used/maximum/remaining word feedback.
- Added deterministic Unicode normalization and multilingual prompt-injection screening for Italian, English, French, German, Spanish, and Vietnamese, including instruction overrides, role forgery, prompt extraction, and delimiter breakout attempts.
- Added provider-bound defense in depth through explicit untrusted-data delimiters and versioned localized YAML rules that prevent the preamble from weakening system instructions or authorizing commands.
- Preserved existing SQLite compatibility, sanitized preambles before persistence and again before provider-bound prompt assembly, and documented the feature and its non-infallible security boundary.

Validation: full repository gate passed with 90/90 tests and zero build warnings/errors; isolated `--help` and `--status` smoke tests passed with a disposable data directory that was removed afterward.

## 2026-08-16 — PromptMeUp 0.1.4 premium UI audit installer

- Bumped the product and current packaging examples from `0.1.3` to `0.1.4` for the completed cross-screen terminal UI audit.
- Built and independently checksum-verified the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, and validated WinGet manifests.
- Upgraded the local per-user installation to `0.1.4` without closing the existing `hm` session or requiring a reboot.
- Verified the installed binary and Windows Installer registration, exactly one user PATH entry, zero machine PATH entries, and an unchanged machine PATH.

Validation: full repository gate passed with 74/74 tests and zero build warnings/errors; installed version is `0.1.4.0`; MSI SHA-256 is `6B6A857CC458D37100E024111BF7D7DFC67C49D8434A853A4C0D39126FFAB62C`.

## 2026-08-16 — Cross-screen terminal UI audit

- Audited every terminal view for decorative cards, icon spacing, separator width, alignment, semantic colors, completed progress residue, and hard-coded user-facing copy.
- Centralized frameless right-label/left-value grids and applied them to status, session, setup, About, PATH, executable location, costs, and command-result metadata.
- Standardized emoji spacing and ASCII fallbacks, 80%-width section rules, semantic success/warning states, and clean Nerd Font dry-run/unsupported results.
- Localized setup validation, help placeholders, PATH previews, font states, and all command-line parser errors in Italian, English, French, German, Spanish, and Vietnamese.
- Added regression coverage for frameless status and command-result grids, localized font dry runs, localized parser errors, chat guidance, and Markdown-safe teletype output.

Validation: full repository gate passed with 74/74 tests, zero build warnings/errors, complete XML summaries, static view scan, and isolated help, status, PATH, font dry-run, and localized-error smoke tests.

## 2026-08-16 — Premium terminal hierarchy and chat feedback

- Removed the remaining decorative cards, standardized 80%-width separators with intentional leading whitespace, and guaranteed one visible space after every icon.
- Rebuilt status and session summaries as frameless label-value grids with right-aligned labels, left-aligned values, responsive status rows, and at most two session rows on standard wide terminals.
- Stacked active progress beneath its status text and made the complete progress surface disappear when work ends.
- Added a localized five-command chat guide, bounded prompt validation, used/maximum/remaining character feedback, consistent automatic user turns, and a Markdown-safe teletype response effect.
- Switched cost bands to semantic foreground colors, localized command risk and execution metadata, improved the command-center hierarchy, and added a default `Non fare nulla` executable-location action.

Validation: full repository gate passed with 70/70 tests, zero build warnings/errors, XML summaries complete, and isolated status plus executable-location smoke tests passed.

## 2026-08-16 — PromptMeUp 0.1.3 frameless UX installer

- Bumped the product and documented packaging examples from `0.1.2` to `0.1.3` for the frameless command-review refinement.
- Built and checksummed the self-contained x64/ARM64 ZIPs, x64 MSI, release manifest, and validated WinGet manifests.
- Upgraded the local installation from `0.1.2` to `0.1.3`; verified the installed binary, WinGet registration, one user PATH entry, and unchanged machine PATH.

Validation: full repository gate passed with 68/68 tests; MSI SHA-256 is `99674769B454C354E8B4CEA793792135FF88FE429900F86B9DF65D75BC78092E`; installer exit code was `0`.

## 2026-08-16 — Frameless command-review refinement

- Replaced cards in the command suggestion, command authorization, command result, shell header, session snapshot, and chat introduction with spacious emoji-led sections.
- Standardized every decorative section divider to end at 80% of the current terminal width and compacted session data into two metric rows.
- Added semantic low/medium/high/critical risk indicators, a structured command-result view, single-line cancellation notices, and a one-line assistant-plus-response-heading treatment.

## 2026-08-16 — PromptMeUp 0.1.2 installer refresh

- Bumped the product, assembly, package, and documented release version from `0.1.1` to `0.1.2` for the completed terminal UX and scoped-AI release.
- Built the portable `win-x64`/`win-arm64` archives, per-user `win-x64` MSI, checksums, release manifest, and validated WinGet manifests.
- Verified the MSI SHA-256 before installation, upgraded the local per-user installation from `0.1.1` to `0.1.2`, and confirmed the installed `hm.exe`, About metadata, uninstall registration, and fresh PowerShell command resolution.
- Confirmed the installer keeps exactly one user PATH entry and does not alter the machine PATH.

Validation: release build and `winget validate` passed; MSI upgrade returned exit code `0`; installed file version is `0.1.2.0`; `hm --version --no-animation --no-emoji` reports the version, GitHub repository, and creator site.

## 2026-08-16 — Premium Spectre Console and scoped AI assistance

- Preserved terminal scrollback across every application flow, with deliberate whitespace around invocations and an interrupted prompt's cancellation message rendered on its own line.
- Rebuilt the shared shell, chat, Markdown renderer, help, version/About page, setup, status, and costs screens around accessible Spectre panels, grids, semantic chips, progress, emoji/ASCII fallbacks, and a high-contrast shared palette.
- Added a branded `--version` About panel with the GitHub repository and creator site; grouped help by task category; and turned `--costs` into a semantic cost dashboard.
- Added strict structured responses for chat and one-shot queries, a Markdown command-candidate menu with safe default **Do not execute commands**, an `Avvia chat` continuation, and continued use of the existing exact-preview/risk/explicit-authorization gate.
- Added localized console-only chat/query system instructions and privacy-filtered runtime context (working directory, platform/shell, CPU, RAM, GPU), including network-path withholding and no generic-writing/image-generation behavior.
- Aligned GPT-5.6 prompt caching to reuse shape: stable explicit prefixes, implicit append-only chat checkpoints, explicit-only one-shot query caching, provider cache read/write metrics, and a post-response snapshot containing total context plus input/output tokens.

Validation: preflight, restore, formatter apply/verification, XML summary check, Release build with 0 warnings/errors, 68/68 integrated tests, `git diff --check`, and non-interactive `--version`, `--help`, and `--status` smoke checks passed.

## 2026-08-16 — Readability, resilience, and terminal UX refactor

- Corrected ordered multi-token query parsing and made SQLite initialization version-safe, transactional, repairable, and WAL-consistent.
- Extracted AI conversation, authorized-command, provider request/response, and SQLite schema responsibilities from oversized services without weakening command authorization or audit boundaries.
- Fixed one-shot query visibility and prevented scalar or malformed optional Costs API errors from aborting the requested AI operation.
- Rebuilt the terminal experience around compact frameless headings, whitespace, color, responsive status lines, staged setup, optional advanced settings, and secret input that reveals neither value nor length.
- Added `Esc` current-flow cancellation, fail-closed authorization cancellation, and `Ctrl+C` application shutdown; exact `/run` without a command now fails locally instead of reaching the model.
- Removed confirmed-unused PowerShell helpers and redundant contract members, automated the multi-platform quality gate, and documented post-commit/push cleanup.

Validation: preflight, restore, format verification, XML comments, Release build with 0 warnings/errors, 55/55 integrated tests, read-only CLI commands in disposable data directories, and live `Esc` / `Ctrl+C` prompt smokes passed.

## 2026-08-16 — Windows release artifact builder

- Added one PowerShell entry point with a read-only plan mode, bounded release output, and fail-fast prerequisite checks.
- Added deterministic self-contained `win-x64` and `win-arm64` ZIP archives for WinGet, multi-file schema 1.12 manifests, package hashes, and a machine-readable release summary.
- Added an optional per-user x64 MSI built and ICE-validated with WiX Toolset 3.14, including upgrade metadata, `%LOCALAPPDATA%\Programs\PromptMeUp` installation, and installer-owned user PATH registration.
- Added `hm --where` / `hm -where` with exact executable reporting, a copyable change-directory command, and an explicitly previewed and confirmed native file-manager action in all six UI languages.
- Added current-architecture version and executable-location smoke testing and excluded helper scripts, secrets, local data, logs, symbols, and build intermediates from distributable payloads.
- Added successful-build cleanup so `artifacts/release/<version>` retains only packages, checksums, WinGet manifests, and the release summary needed for user testing.
- Documented local WinGet installation, direct MSI testing, prerequisites, output layout, and the portable-first product boundary.

Validation: release `0.1.1` generation and `winget validate` passed; MSI tables confirmed `LocalAppDataFolder`, installer-owned child-directory cleanup, and user PATH registration; a silent upgrade from `0.1.0` returned exit code `0`, removed the old ProductCode, installed `0.1.1`, and left no machine PATH entry; packaged and installed `hm -where` smokes passed; the integrated quality gate passed 55/55 tests.

## 2026-08-12 — Initial PromptMeUp product foundation

- Created the .NET 10 `hm` console product with Spectre.Console and Serilog through `ILogger<T>`.
- Added six-language setup, query/chat flows, short memory, OpenAI Responses integration, prompt caching, token/context/cost status, and YAML runtime prompts.
- Added mandatory command preview/authorization, local and optional AI risk review, bounded PowerShell execution, and credential redaction.
- Added SQLite settings, normalized usage/pricing/costs, flexible session events, and activity audit storage.
- Added portable PATH management, an opt-in Nerd Font helper, MIT/public-repository documentation, tests, and a manual build/lint workflow.

Validation is recorded in `docs/VALIDATION.md` and the publication commit history.

## 2026-09-01 — Public open-source readiness

- Confirmed the public repository's canonical MIT license, package metadata, third-party notices, security policy, contribution guide, and cross-platform quality gate.
- Added Dependabot for NuGet and GitHub Actions, CodeQL analysis for C#, structured bug and feature forms, a pull-request template, and a public support path through GitHub Discussions.
- Enabled GitHub Discussions; disabled the duplicate Wiki; enabled Dependabot alerts and automatic security updates; and added public discovery topics and standard labels.
- Protected `main` with up-to-date required quality checks, pull requests, resolved conversations, linear history, and blocked force-pushes and deletions. Administrative bypass remains available for repository recovery.
- Ignored Windows `desktop.ini` noise and kept the optional MSI release path explicitly dependent on WiX Toolset 3.14 rather than treating it as an open-source prerequisite.

Validation: required repository gate passed with zero build warnings/errors and 90/90 tests; isolated `--version` and `--third-party` smoke tests passed with a disposable data directory; `git diff --check` passed. GitHub settings were read back after application.

## 2026-09-01 — Product motto

- Added the public motto `Yet another CLI AI assistant :-)` beneath the README title and in the terminal About surface for every language.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; the non-interactive `--version` smoke test rendered the exact motto; `git diff --check` passed.

## 2026-09-01 — Product-led public documentation

- Reframed the README, contributor, support, security, CLI, architecture, privacy, cost, validation, packaging, and runtime-resource documentation around user outcomes and durable product promises.
- Kept technical, licensing, privacy, and command-authorization details explicit while moving implementation mechanics behind the experience they support.
- Corrected the README navigation anchor, described `hm` as the cross-platform public command, and aligned contributor wording with portable archives as the canonical distribution plus optional platform installers.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; isolated non-interactive `--help` and `--version` smoke tests passed; `git diff --check` passed.

## 2026-09-01 — Cross-platform quality-gate repair

- Declared CRLF checkout behavior for C# files in `.gitattributes`, matching the existing repository-wide `.editorconfig` contract on Linux as well as Windows.
- Made the animated Markdown test strip host-emitted ANSI control sequences before asserting semantic text, while continuing to reject raw Markdown markers.
- Kept production rendering unchanged; the failure was isolated to checkout policy and test-output normalization.

Validation: full repository gate passed locally with zero build warnings/errors and 90/90 tests; the previously failing animation test passed in isolation; Git resolved `TerminalViewTests.cs` as `text eol=crlf`; `git diff --check` passed.

## 2026-09-02 — Synchronize local main without merge commits

- Backed up the 24 locally changed or added files and confirmed that their changes were already included in the remote tree; three-way comparisons of the nine differing documentation files matched the remote exactly.
- Fast-forwarded `main` from `6da473d` to `89ec9f0`, preserving the local work without creating commits, branches, or worktrees.

Validation: full repository gate passed with zero build warnings/errors and 90/90 tests; isolated `--help`, `--version`, Vietnamese `--status`, and French `--third-party` smoke tests passed with a disposable data directory. `main` matched `origin/main`, and the fetched range contained no merge commits.

## 2026-09-02 — Ten actionable code-review findings

- Reviewed the current application at `89ec9f0` and recorded ten distinct findings in [the review report](review-2026-09-02.md), including source references, reproduction evidence, impact, and recommended corrections.
- Confirmed three credential-handling failures, two timeout failures, misleading local risk classification, lost assistant answers, a shell mismatch, ignored long-context prices, and an unnecessary historical table scan.
- Reproduced all ten cases in an ignored local probe project with isolated SQLite data and simulated HTTP responses; no real AI calls, credentials, PATH changes, or font changes were used.
- Preserved concurrent repository work and left application code, existing tests, and runtime prompts unchanged.

Validation: ten dedicated defect reproductions confirmed; the full repository gate passed with zero build warnings/errors and 90/90 existing tests. Isolated `--help` and `--version` smoke tests passed. The existing suite does not cover the reproduced failure cases.

## 2026-09-02 — PowerShell script workshop

- Added complete script generation, revision diffs, named-parameter guidance, and explicit saving to new files.
- Added authorized parser-only validation and optional installed PSScriptAnalyzer support; source is never evaluated.
- Added tests for parser isolation, invalid syntax, artifact shape, option scope, and overwrite prevention.
- Validated through the full repository gate and isolated CLI smoke checks; delivered in the second stacked PR.
## 2026-09-02 — Resumable guided plans

- Added bounded 1–8-step console plans with durable local progress and an explicit resume identifier.
- Kept action and verification commands behind separate risk review, exact preview, and approval gates.
- Marked intent before execution; interrupted actions are verified on resume and never replayed automatically.
- Added exclusive plan leases, working-directory checks, user-confirmed outcomes, tests, and six-language guidance.
- Delivered after the full gate as the third stacked feature PR.
## 2026-09-02 — Concrete file-effect previews

- Added bounded local snapshots for copy, move, prefix rename, and delete operations without AI or shell simulation.
- Displayed exact mappings, bytes, and collisions; blocked links, unsupported operations, recursion, and overwrites.
- Rechecked snapshot metadata after review, preserved per-command approval, and stopped batches on failure or denial.
- Added tests for no-mutation previews, collisions, stale source, late destinations, literal quoting, links, and CLI scope.
- Delivered after the full gate as the fourth stacked feature PR.
## 2026-09-03 — Personal parameterized recipes

- Added local recipe listing, inspection, import/export, and saving from completed plans.
- Added schema validation, declared parameters, prerequisite review, and quoted data binding without source substitution.
- Every reuse creates a new resumable plan with fresh approvals; imports cannot inherit progress or authorization.
- Added tests for parameter quoting, credential rejection, definition isolation, overwrite prevention, schema/status handling, and CLI scope.
- Completed the five-feature series with one commit and assigned stacked PR per feature after the full validation gate.

## 2026-09-03 — Resolve SQLite dependency PR conflicts

- Integrated current `main` into PR #3, preserving Microsoft.Data.Sqlite 10.0.11 and the three Microsoft.Extensions 10.0.11 updates already merged into `main`.
- Aligned the affected runtime dependency versions in the CLI attribution table and third-party notices.

Validation: the full repository gate passed with zero build warnings/errors and 182/182 tests. Isolated `--help`, `--version`, `--third-party`, and `--status` smoke checks passed with disposable data directories; the attribution output shows the four resolved 10.0.11 versions. `git diff --check` passed.

## 2026-09-03 — Printable CLI manual and command discovery

- Reviewed the latest feature commit and added README guidance for the five new workflows: diagnosis, PowerShell script drafts, resumable plans, file-effect previews, and reusable command recipes.
- Clarified that the portable archive needs no installation: add the extracted `hm` folder to the user PATH, with `hm --path install`, `status`, and `remove` scoped to that entry.
- Added a retro-styled two-page PDF manual: a printable all-command card on page one and workflow details, examples, and safety notes on page two.
- Marked PDF files as binary so Git preserves the validated document bytes instead of applying text line-ending conversion.

Validation: preflight, restore, format verification, XML comment check, Release build with zero warnings/errors, and 182/182 tests passed. The isolated `--help --no-animation --no-emoji` smoke test passed with a disposable `PROMPTMEUP_DATA_DIR`; `git diff --check` passed. The final two-page PDF was rendered to PNG and visually inspected.

## 2026-09-04 — GitHub documentation discovery and GA4 attribution

- Reframed the README title and opening copy around PromptMeUp's searchable product identity: a safety-first, open-source AI command-line assistant for Windows, Linux, and macOS.
- Made the primary user, contributor, privacy, security, architecture, packaging, release, and validation document titles self-describing when surfaced as individual GitHub pages.
- Added a consistent GA4 campaign taxonomy only to the three README links that lead to owned, analytics-enabled websites: `github` / `referral` / `promptmeup`, with placement-specific `utm_content` values.
- Kept internal navigation, GitHub actions and community links, downloads, security reporting, third-party sources, and template URLs untagged so canonical destinations and analytics attribution remain clean.

Validation: all three tagged destinations returned HTTP 200 and retained their complete UTM query strings. Repository preflight, restore, format verification, XML comment check, Release build with zero warnings/errors, and 182/182 tests passed. The isolated `--help --no-animation --no-emoji` smoke test passed with a disposable `PROMPTMEUP_DATA_DIR`; `git diff --check` passed.

## 2026-09-04 — Live GitHub discovery metadata

- Replaced the live GitHub repository description with concise product language identifying PromptMeUp as a lightweight, open-source, safety-first AI CLI for Windows, Linux, and macOS.
- Preserved the existing relevant topics and added `ai-assistant`, `command-line`, `cli-tool`, `developer-tools`, `dotnet-10`, and `openai-api` for more precise GitHub discovery.
- Left the repository homepage unset because there is not yet a dedicated PromptMeUp landing page; a generic personal or unrelated product URL would weaken the repository's identity.

Validation: GitHub's repository API returned the exact new description and the expected 14-topic set after the remote update. No release was created; the related README and documentation improvements are published through the dedicated documentation branch and pull request.

## 2026-09-04 — Dedicated product-page attribution

- Added a prominent README link to the dedicated PromptMeUp product page on `umbertogiacobbi.biz`.
- Routed the origin-story link to the same product page and normalized all PromptMeUp `utm_content` values to snake_case.
- Kept internal, community, release, and GitHub-native links untagged so campaign attribution remains limited to owned analytics-enabled destinations.

Validation: repository preflight, restore, format verification, XML comment check, Release build with zero warnings/errors, and 182/182 tests passed; `git diff --check` passed for the README change.
